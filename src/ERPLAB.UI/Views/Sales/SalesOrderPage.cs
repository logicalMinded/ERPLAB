using ERPLAB.BLL.Services;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using ERPLAB.UI.Core;
using ERPLAB.UI.Views.BaseData;
//using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.UI.Views.Sales
{
    /// <summary>
    /// 銷貨單主明細表維護模組 (Master-Detail 模式)。
    /// 核心展示：TVP 批次寫入、Grid 盲打試算、4 維狀態機鎖死、與獨立微交易取號。
    /// </summary>
    public partial class SalesOrderPage : BasePage
    {
        // =====================================================================
        // 💡 三層式架構：全數轉軌至 BLL Services
        // =====================================================================
        private readonly SalesOrderService _salesService;
        private readonly CustomerService _custService;
        private readonly ProductService _prodService;
        private readonly GeographyService _geoService;

        private BindingSource _bsMaster;
        private BindingSource _bsDetail;
        private ExtendedBindingList<SalesMaster> _masterBindingList;
        private ExtendedBindingList<SalesDetail> _detailBindingList;

        private List<Base_City> _cityList = new();
        private List<Base_District> _allDistrictList = new();

        // 狀態機定義：精確控制畫面行為與防呆
        private FormState _currentState = FormState.Browse;

        // 追蹤廠商實體 ID，防範字串脫鉤
        private int _selectedCustomerID = 0;

        public SalesOrderPage()
        {
            InitializeComponent();

            // 💡 物理優化：強制開啟 Grid 的雙重緩衝，解決捲動與載入時的渲染延遲
            dgvSalesMaster.EnableDoubleBuffering(true);
            dgvSalesDetail.EnableDoubleBuffering(true);

            _salesService = new SalesOrderService();
            _custService = new CustomerService();
            _prodService = new ProductService();
            _geoService = new GeographyService();

            _bsMaster = new BindingSource();
            _bsDetail = new BindingSource();
            _masterBindingList = new ExtendedBindingList<SalesMaster>();
            _detailBindingList = new ExtendedBindingList<SalesDetail>();

            // 💡 綁定生命週期與按鈕事件
            this.Load += SalesOrderPage_Load;

            _bsMaster.CurrentChanged += BsMaster_CurrentChanged;

            // 💡 明細盲打試算引擎事件
            dgvSalesDetail.CellEndEdit += DgvSalesDetail_CellEndEdit;
            dgvSalesDetail.RowsRemoved += (s, e) => RecalculateTotalAmount();
            dgvSalesDetail.DefaultValuesNeeded += DgvSalesDetail_DefaultValuesNeeded;
            // 💡 掛載三個觸發點，確保任何增刪改查，行號永遠精確連動
            dgvSalesDetail.DataBindingComplete += (s, e) => UpdateRowHeaderNumbers(); // 撈取 DB 綁定完成時
            dgvSalesDetail.RowsAdded += (s, e) => UpdateRowHeaderNumbers();           // 盲打新增了一列時
            dgvSalesDetail.RowsRemoved += (s, e) => UpdateRowHeaderNumbers();         // 刪除了一列時

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
            btnPost.Click += BtnPost_Click;
            btnVoid.Click += BtnVoid_Click;

            btnMoveUp.Click += btnMoveUp_Click;
            btnMoveDown.Click += btnMoveDown_Click;

            txtKeyword.KeyDown += TxtKeyword_KeyDown;
            btnSearch.Click += btnSearch_Click;
            btnRefresh.Click += btnRefresh_Click;
            chkShowVoided.CheckedChanged += async (s, e) => await SearchDataAsync();
            ucPagination.PageChanged += async (s, e) => await SearchDataAsync();

            cmbCity.SelectedIndexChanged += CmbCity_SelectedIndexChanged;
            cmbDistrict.SelectedIndexChanged += CmbDistrict_SelectedIndexChanged;

            txtCustomerNo.KeyDown += txtCustomerNo_KeyDown;
            txtCustomerNo.TextChanged += TxtCustomerNo_TextChanged;
            btnLookupCustomer.Click += BtnLookupCustomer_Click;

            dgvSalesMaster.CellFormatting += dgvSalesMaster_CellFormatting;
        }

        private async void SalesOrderPage_Load(object? sender, EventArgs e)
        {
            // =====================================================================
            // 🛡️ [防禦引擎 A] UI 啟動瞬間發動 RBAC 物理斷路
            // =====================================================================
            RequirePermission("ACT_SALE_ADD", btnAdd);
            RequirePermission("ACT_SALE_EDIT", btnEdit);
            RequirePermission("ACT_SALE_APPROVE", btnPost);
            RequirePermission("ACT_SALE_VOID", btnVoid);

            bool canWrite = SessionContext.HasPermission("ACT_SALE_ADD") || SessionContext.HasPermission("ACT_SALE_EDIT");
            btnSave.Visible = canWrite;
            btnCancel.Visible = canWrite;

            SetupMasterGridColumns();
            SetupDetailGridColumns();

            _bsMaster.DataSource = _masterBindingList;
            dgvSalesMaster.DataSource = _bsMaster;

            _bsDetail.DataSource = _detailBindingList;
            dgvSalesDetail.DataSource = _bsDetail;

            // 載入基礎資料
            await LoadGeographyDataAsync();
            await SearchDataAsync();
            SetUIState(FormState.Browse);
        }

        private void SetupMasterGridColumns()
        {
            dgvSalesMaster.AutoGenerateColumns = false;
            if (dgvSalesMaster.Columns.Count == 0)
            {
                dgvSalesMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SalesNo", HeaderText = "銷貨單號", Width = 140 });
                dgvSalesMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SalesDate", HeaderText = "單據日期", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
                dgvSalesMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName_Display", HeaderText = "廠商名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
                dgvSalesMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount", HeaderText = "總金額", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } });
            }
        }

        private void SetupDetailGridColumns()
        {
            dgvSalesDetail.AutoGenerateColumns = false;
            dgvSalesDetail.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            if (dgvSalesDetail.Columns.Count == 0)
            {
                // 💡 極速盲打版型：全數使用 TextBox 支援鍵盤速打
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductNo", DataPropertyName = "ProductNo_Display", HeaderText = "商品代碼 (輸入)", Width = 150 });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", DataPropertyName = "ProductName_Display", HeaderText = "商品名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", DataPropertyName = "Qty", HeaderText = "數量", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", DataPropertyName = "UnitPrice", HeaderText = "單價", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal_Display", DataPropertyName = "SubTotal_Display", HeaderText = "小計", Width = 120, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight, BackColor = Color.WhiteSmoke } });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Remark", DataPropertyName = "Remark", HeaderText = "備註", Width = 150 });

                // 隱藏的實體關聯鍵，供 C# 背後抓取使用
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", DataPropertyName = "ProductID", Visible = false });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineNo", DataPropertyName = "LineNo", Visible = false });
            }
        }

        // =====================================================================
        // 🌍 [地理連動與廠商速查引擎]
        // =====================================================================
        private async Task LoadGeographyDataAsync()
        {
            try
            {
                _cityList.Clear();
                _cityList.AddRange(await _geoService.GetActiveCitiesAsync());
                _allDistrictList.Clear();
                _allDistrictList.AddRange(await _geoService.GetAllActiveDistrictsAsync());

                cmbCity.SelectedIndexChanged -= CmbCity_SelectedIndexChanged;
                cmbCity.DataSource = _cityList;
                cmbCity.DisplayMember = "CityName";
                cmbCity.ValueMember = "CityID";
                cmbCity.SelectedIndex = -1;
                cmbCity.SelectedIndexChanged += CmbCity_SelectedIndexChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入地理字典檔失敗：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CmbCity_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbCity.SelectedValue == null || !int.TryParse(cmbCity.SelectedValue.ToString(), out int cityId))
            {
                cmbDistrict.DataSource = null;
                txtShipZipFront.Clear();
                return;
            }

            var filteredDistricts = _allDistrictList.Where(d => d.CityID == cityId).OrderBy(d => d.SortSeq).ToList();
            cmbDistrict.SelectedIndexChanged -= CmbDistrict_SelectedIndexChanged;
            cmbDistrict.DataSource = filteredDistricts;
            cmbDistrict.DisplayMember = "DistrictName";
            cmbDistrict.ValueMember = "DistrictID";
            cmbDistrict.SelectedIndex = -1;
            cmbDistrict.SelectedIndexChanged += CmbDistrict_SelectedIndexChanged;
            txtShipZipFront.Clear();
        }

        private void CmbDistrict_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbDistrict.SelectedItem is Base_District sd) txtShipZipFront.Text = sd.ZipCode;
            else txtShipZipFront.Clear();
        }

        private void TxtCustomerNo_TextChanged(object? sender, EventArgs e)
        {
            if (_currentState == FormState.Browse) return;
            _selectedCustomerID = 0;
            txtCustomerName.Clear();
        }

        private void BtnLookupCustomer_Click(object? sender, EventArgs e)
        {
            if (_currentState == FormState.Browse) return;

            using (var lookupForm = new CustomerLookupForm())
            {
                if (lookupForm.ShowDialog() == DialogResult.OK)
                    ApplySelectedCustomer(lookupForm.SelectedCustomer);
            }
        }

        private void ApplySelectedCustomer(Customer c)
        {
            if (c == null) return;
            _selectedCustomerID = c.CustomerID;
            txtCustomerNo.TextChanged -= TxtCustomerNo_TextChanged;
            txtCustomerNo.Text = c.CustomerNo;
            txtCustomerNo.TextChanged += TxtCustomerNo_TextChanged;
            txtCustomerName.Text = c.CustomerName;

            // 快照：帶入預設地址與地理資訊
            string zip = c.CustomZipCode ?? string.Empty;
            if (zip.Length >= 3)
            {
                txtShipZipFront.Text = zip.Substring(0, 3);
                txtShipZipRear.Text = zip.Length > 3 ? zip.Substring(3) : string.Empty;
            }
            else if (zip.Length > 0)
            {
                txtShipZipFront.Text = zip;
                txtShipZipRear.Clear();
            }
            else { txtShipZipFront.Clear(); txtShipZipRear.Clear(); }

            txtShipAddress.Text = c.Address;

            if (c.DistrictID > 0 && _allDistrictList != null)
            {
                var district = _allDistrictList.FirstOrDefault(d => d.DistrictID == c.DistrictID);
                if (district != null)
                {
                    cmbCity.SelectedValue = district.CityID;
                    cmbDistrict.SelectedValue = c.DistrictID;
                }
            }
            else { cmbCity.SelectedIndex = -1; }
        }

        // =====================================================================
        // 🔄 [資料流引擎] 搜尋與主檔綁定
        // =====================================================================
        private async void TxtKeyword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await SearchDataAsync();
            }
        }

        private async void txtCustomerNo_KeyDown(object? sender, KeyEventArgs e)
        {
            // 物理防呆：只攔截 Enter 鍵，且瀏覽模式下絕對鎖死不執行
            if (e.KeyCode != Keys.Enter || _currentState == FormState.Browse) return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            string inputNo = txtCustomerNo.Text.Trim();
            if (string.IsNullOrEmpty(inputNo)) return;

            try
            {
                // 💡 呼叫 DAL 點查詢：利用分頁引擎限制只撈 1 筆，將網路 I/O 壓至最低
                var result = await _custService.GetCustomersAsync(1, 1, false, inputNo);

                // 嚴格比對字串 (無視大小寫)
                var match = result.Items.FirstOrDefault(c => c.CustomerNo.Equals(inputNo, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    ApplySelectedCustomer(match);

                    // 💡 盲打人體工學：查到廠商後，游標瞬間自動跳往「出貨地址」，雙手不離鍵盤
                    txtShipAddress.Focus();
                }
                else
                {
                    MessageBox.Show("找不到此廠商代碼！", "查無資料", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // 防禦：查無資料時，物理銷毀記憶體 ID 並清空畫面，逼迫重新驗證
                    _selectedCustomerID = 0;
                    txtCustomerName.Clear();
                    txtCustomerNo.SelectAll(); // 方便使用者直接重打
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查詢廠商時發生異常：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnSearch_Click(object? sender, EventArgs e)
        {
            btnSearch.Enabled = false;
            try
            {
                ucPagination.ResetToFirstPage();
                await SearchDataAsync();
            }
            finally { btnSearch.Enabled = true; }
        }

        private async void btnRefresh_Click(object? sender, EventArgs e)
        {
            btnRefresh.Enabled = false;
            try
            {
                txtKeyword.Clear();
                //chkShowVoided.CheckedChanged -= async (s, ev) => await SearchDataAsync();
                //chkShowVoided.Checked = false;
                //chkShowVoided.CheckedChanged += async (s, ev) => await SearchDataAsync();

                ucPagination.ResetToFirstPage();
                await SearchDataAsync();
            }
            finally { btnRefresh.Enabled = true; }
        }

        private async Task SearchDataAsync()
        {
            string keyword = txtKeyword.Text.Trim();
            bool showVoided = chkShowVoided.Checked;

            long? lastSelectedId = _bsMaster.Current is SalesMaster currentMaster ? currentMaster.SalesID : null;

            int pageSize = ucPagination.PageSize;
            int currentPage = ucPagination.CurrentPage;

            try
            {
                var result = await _salesService.GetSalesOrdersAsync(currentPage, pageSize, keyword, showVoided);

                if (result.Items.Count == 0 && result.TotalCount > 0)
                {
                    int correctLastPage = (int)Math.Ceiling((double)result.TotalCount / pageSize);
                    ucPagination.ForceCurrentPage(correctLastPage);
                    result = await _salesService.GetSalesOrdersAsync(correctLastPage, pageSize, keyword, showVoided);
                }

                _bsMaster.CurrentChanged -= BsMaster_CurrentChanged;

                _masterBindingList.Clear();
                _masterBindingList.AddRange(result.Items);

                if (_bsMaster.Count > 0)
                {
                    var targetMaster = _masterBindingList.FirstOrDefault(m => m.SalesID == lastSelectedId);
                    int targetIndex = targetMaster != null ? _bsMaster.IndexOf(targetMaster) : 0;
                    _bsMaster.Position = targetIndex;

                    targetMaster = (SalesMaster)_bsMaster.Current;

                    // 💡 手動呼叫，因為事件已脫鉤
                    BindMasterUI(targetMaster);
                    await LoadDetailDataAsync(targetMaster.SalesID);
                }
                else
                {
                    ClearMasterUI();
                    SetUIState(_currentState);
                }

                _bsMaster.CurrentChanged += BsMaster_CurrentChanged;
                ucPagination.BindTotalCount(result.TotalCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"資料載入失敗：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =====================================================================
        // 🔄 [明細資料流] 點擊主檔，非同步撈取明細
        // =====================================================================
        private async void BsMaster_CurrentChanged(object? sender, EventArgs e)
        {
            if (_currentState != FormState.Browse) return;

            if (_bsMaster.Current is SalesMaster master)
            {
                BindMasterUI(master);
                await LoadDetailDataAsync(master.SalesID);
            }
        }

        private async Task LoadDetailDataAsync(long salesId)
        {
            try
            {
                var details = await _salesService.GetSalesDetailsAsync(salesId);
                _detailBindingList.Clear();
                _detailBindingList.AddRange(details);
                RecalculateTotalAmount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"明細載入失敗：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindMasterUI(SalesMaster m)
        {
            if (m == null)
            {
                MessageBox.Show("系統無法取得當前操作的單據！", "狀態異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetUIState(FormState.Browse);
                return;
            }

            splitRight.Panel1.SuspendLayout();

            _selectedCustomerID = m.CustomerID;
            txtSalesNo.Text = m.SalesNo;
            dtpSalesDate.Value = m.SalesDate;
            txtCustomerNo.Text = m.CustomerNo_Display;
            txtCustomerName.Text = m.CustomerName_Display;

            string zip = m.ShipZipCode ?? string.Empty;
            if (zip.Length >= 3)
            {
                txtShipZipFront.Text = zip.Substring(0, 3);
                txtShipZipRear.Text = zip.Length > 3 ? zip.Substring(3) : string.Empty;
            }
            else if (zip.Length > 0) { txtShipZipFront.Text = zip; txtShipZipRear.Clear(); }
            else { txtShipZipFront.Clear(); txtShipZipRear.Clear(); }

            if (m.ShipDistrictID > 0 && _allDistrictList != null)
            {
                var district = _allDistrictList.FirstOrDefault(d => d.DistrictID == m.ShipDistrictID);
                if (district != null)
                {
                    cmbCity.SelectedValue = district.CityID;
                    cmbDistrict.SelectedValue = m.ShipDistrictID;
                }
            }
            else { cmbCity.SelectedIndex = -1; cmbDistrict.SelectedIndex = -1; }

            txtShipAddress.Text = m.ShipAddress;
            txtRemark.Text = m.Remark;

            // =====================================================================
            // 💡 [視覺引擎] 4 維狀態機徽章渲染
            // =====================================================================
            switch (m.Status)
            {
                case (byte)DocumentStatus.Draft:
                    lblStatusBadge.Text = "📝 未過帳 (草稿)";
                    lblStatusBadge.ForeColor = Color.DarkOrange;
                    btnVoid.Text = "❌ 註銷草稿";
                    break;
                case (byte)DocumentStatus.Posted:
                    lblStatusBadge.Text = "🔒 已過帳 (正式)";
                    lblStatusBadge.ForeColor = Color.Green;
                    btnVoid.Text = "🚫 作廢沖銷";
                    break;
                case (byte)DocumentStatus.Cancelled:
                    lblStatusBadge.Text = "❌ 已註銷";
                    lblStatusBadge.ForeColor = Color.Gray;
                    btnVoid.Text = "狀態已終結";
                    break;
                case (byte)DocumentStatus.Voided:
                    lblStatusBadge.Text = "🚫 已作廢 (沖銷)";
                    lblStatusBadge.ForeColor = Color.Red;
                    btnVoid.Text = "狀態已終結";
                    break;
            }

            if (m.SalesID > 0)
            {
                string creator = m.CreateUserNo_Display ?? "未知";
                string updater = m.UpdateUserNo_Display ?? "未知";
                lblAuditTrail.Text = $"建檔：{creator} ({m.CreateTime:yyyy/MM/dd HH:mm}) ｜ 最後異動：{updater} ({m.UpdateTime:yyyy/MM/dd HH:mm})";
                lblAuditTrail.Visible = true;
            }
            else { lblAuditTrail.Visible = false; }

            SetUIState(_currentState);
            splitRight.Panel1.ResumeLayout(true);
        }

        private void ClearMasterUI()
        {
            _selectedCustomerID = 0;
            txtSalesNo.Text = "[儲存後自動配發]";
            dtpSalesDate.Value = DateTime.Now;
            txtCustomerNo.Clear();
            txtCustomerName.Clear();
            cmbCity.SelectedIndex = -1;
            cmbDistrict.SelectedIndex = -1;
            txtShipZipFront.Clear();
            txtShipZipRear.Clear();
            txtShipAddress.Clear();
            txtRemark.Clear();

            _detailBindingList.Clear();
            lblTotalAmount.Text = "總計：$0";

            lblStatusBadge.Text = "📝 新增草稿";
            lblStatusBadge.ForeColor = Color.Blue;
            lblAuditTrail.Visible = false;
            btnVoid.Text = "❌ 註銷單據";
        }

        // =====================================================================
        // 🔢 [視覺引擎] 原生列首行號重編 (Native Row Header Numbering)
        // 核心職責：將實體順序轉化為列首的文字，完美支援 WinForms 的自動寬度調整。
        // =====================================================================
        private void UpdateRowHeaderNumbers()
        {
            // 物理防呆：暫停佈局，防止迴圈賦值時引發畫面閃爍
            dgvSalesDetail.SuspendLayout();

            foreach (DataGridViewRow row in dgvSalesDetail.Rows)
            {
                // 跳過最下方那行帶有星號 (*) 的待新增列
                if (row.IsNewRow) continue;

                // 💡 將列索引 (Index + 1) 直接塞給原生的 HeaderCell
                row.HeaderCell.Value = (row.Index + 1).ToString();
            }

            dgvSalesDetail.ResumeLayout(true);
        }

        // =====================================================================
        // ⚡ [極速盲打試算引擎] (Blind-Typing & Calculation)
        // =====================================================================
        private void DgvSalesDetail_DefaultValuesNeeded(object? sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells["Qty"].Value = 1;
            e.Row.Cells["UnitPrice"].Value = 0m;
        }

        private async void DgvSalesDetail_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _currentState == FormState.Browse) return;

            var colName = dgvSalesDetail.Columns[e.ColumnIndex].Name;

            // 💡 盲打查詢：輸入代碼，離開格子瞬間去 DB 把商品找回來回填
            if (colName == "ProductNo")
            {
                string? inputNo = dgvSalesDetail.Rows[e.RowIndex].Cells["ProductNo"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(inputNo))
                {
                    try
                    {
                        var product = await _prodService.GetProductByNoAsync(inputNo);
                        if (product != null)
                        {
                            dgvSalesDetail.Rows[e.RowIndex].Cells["ProductNo"].Value = product.ProductNo;
                            dgvSalesDetail.Rows[e.RowIndex].Cells["ProductID"].Value = product.ProductID;
                            dgvSalesDetail.Rows[e.RowIndex].Cells["ProductName"].Value = product.ProductName;
                            dgvSalesDetail.Rows[e.RowIndex].Cells["UnitPrice"].Value = product.SalesPrice;
                        }
                        else
                        {
                            MessageBox.Show("查無此商品代碼！");
                            dgvSalesDetail.Rows[e.RowIndex].Cells["ProductNo"].Value = string.Empty;
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("查詢商品失敗：" + ex.Message); }
                }
            }

            // 即時試算：單價或數量改變時，強迫結束編輯並重新加總
            if (colName == "Qty" || colName == "UnitPrice" || colName == "ProductNo")
            {
                dgvSalesDetail.EndEdit();
                RecalculateTotalAmount();
                dgvSalesDetail.InvalidateRow(e.RowIndex); // 觸發小計欄位重繪
            }
        }

        private void dgvSalesMaster_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // 確保索引在合法範圍內
            if (e.RowIndex >= 0 && e.RowIndex < dgvSalesMaster.Rows.Count)
            {
                // 取得該列綁定的實體物件
                var salesMaster = dgvSalesMaster.Rows[e.RowIndex].DataBoundItem as SalesMaster;

                // 若該廠商已被停用 (IsActive == false)
                if (salesMaster != null && (salesMaster.Status == 3 || salesMaster.Status == 4) && e.CellStyle != null)
                {
                    // 💡 字體顏色改為深灰色
                    e.CellStyle.ForeColor = System.Drawing.Color.DarkGray;

                    // 💡 加上物理刪除線 (Strikeout)，產生強烈的視覺斷層，警告使用者此為無效資料
                    e.CellStyle.Font = new System.Drawing.Font(dgvSalesMaster.Font, System.Drawing.FontStyle.Strikeout);
                }
            }
        }

        private void RecalculateTotalAmount()
        {
            decimal total = _detailBindingList.Sum(d => d.UnitPrice * d.Qty);
            lblTotalAmount.Text = $"總計：{total:N0}";
        }

        // =====================================================================
        // ⚙️ [狀態機引擎] 單據生命週期唯讀鎖死
        // =====================================================================
        private void SetUIState(FormState state)
        {
            _currentState = state;
            bool isEditing = (state == FormState.Add || state == FormState.Edit);
            bool isBrowse = (state == FormState.Browse);

            var master = _bsMaster.Current as SalesMaster;
            // 💡 核心防線：只有狀態 1 (未過帳/草稿) 且處於編輯模式時，才允許修改資料
            bool isDraft = (state == FormState.Add) || (master != null && master.Status == (byte)DocumentStatus.Draft);
            bool canEditFields = isEditing && isDraft;

            txtSalesNo.ReadOnly = true;
            txtCustomerNo.ReadOnly = !canEditFields;
            btnLookupCustomer.Enabled = canEditFields;
            txtShipZipFront.ReadOnly = true;
            txtShipZipRear.ReadOnly = !canEditFields;
            txtShipAddress.ReadOnly = !canEditFields;
            txtRemark.ReadOnly = !canEditFields;
            dtpSalesDate.Enabled = canEditFields;
            cmbCity.Enabled = canEditFields;
            cmbDistrict.Enabled = canEditFields;

            // 💡 明細 Grid 防線
            dgvSalesDetail.ReadOnly = !canEditFields;
            dgvSalesDetail.AllowUserToAddRows = canEditFields;
            dgvSalesDetail.AllowUserToDeleteRows = canEditFields;

            btnMoveUp.Enabled = canEditFields;
            btnMoveDown.Enabled = canEditFields;

            dgvSalesMaster.Enabled = !isEditing;
            pnlSearch.Enabled = !isEditing;

            // 基礎 CRUD 按鈕狀態
            btnAdd.Enabled = !isEditing;
            btnEdit.Enabled = !isEditing && master != null && isDraft; // 只有草稿能進入 Edit
            btnSave.Enabled = isEditing;
            btnCancel.Enabled = isEditing;
            btnRefresh.Enabled = !isEditing;

            // =====================================================================
            // 🛡️ [狀態推進按鈕防線] 動態切換作廢/註銷語意
            // =====================================================================
            btnPost.Enabled = isBrowse && master != null && master.Status == (byte)DocumentStatus.Draft;
            btnVoid.Enabled = isBrowse && master != null &&
                              (master.Status == (byte)DocumentStatus.Draft || master.Status == (byte)DocumentStatus.Posted);

            if (state == FormState.Add)
            {
                ClearMasterUI();
                txtCustomerNo.Focus();
            }

            ucPagination.SetUIState(isBrowse);
        }

        // =====================================================================
        // 💾 [交易引擎] TVP 批次寫入與 SqlTransaction 存檔
        // =====================================================================
        private void BtnAdd_Click(object? sender, EventArgs e) => SetUIState(FormState.Add);
        private void BtnEdit_Click(object? sender, EventArgs e) => SetUIState(FormState.Edit);

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            SetUIState(FormState.Browse);
            if (_bsMaster.Current is SalesMaster master)
            {
                BindMasterUI(master);
                // 💡 放棄修改時，必須重撈明細，確保畫面還原為 DB 真實狀態
                _ = LoadDetailDataAsync(master.SalesID);
            }
            else { ClearMasterUI(); }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            // 1. 強制結束 Grid 編輯狀態，確保盲打數值推入 BindingList
            dgvSalesDetail.EndEdit();
            _bsDetail.EndEdit();

            // 2. 前端物理防呆
            if (cmbDistrict.SelectedValue == null || string.IsNullOrWhiteSpace(txtCustomerNo.Text) || string.IsNullOrWhiteSpace(txtShipAddress.Text))
            {
                MessageBox.Show("藍色欄位為必填欄位！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_selectedCustomerID <= 0)
            {
                MessageBox.Show("請確認有效的廠商代碼！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCustomerNo.Focus();
                return;
            }

            // 💡 物理洗淨：剃除 Grid 最後一行的空白新增列，以及沒有打商品 ID 的髒資料
            var validDetails = _detailBindingList.Where(d => d.ProductID > 0 && d.Qty > 0).ToList();
            if (validDetails.Count == 0)
            {
                MessageBox.Show("銷貨單至少需要輸入一筆有效的商品明細 (數量不可為 0)！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSave.Enabled = false;
            btnCancel.Enabled = false;

            var currentMaster = _currentState == FormState.Add ? new SalesMaster() : (_bsMaster.Current as SalesMaster);
            if (currentMaster == null) return;

            // 3. DTO Mapping
            string front = txtShipZipFront.Text.Trim();
            string rear = txtShipZipRear.Text.Trim();
            currentMaster.ShipZipCode = string.IsNullOrEmpty(rear) ? front : front + rear;

            currentMaster.SalesDate = dtpSalesDate.Value;
            currentMaster.CustomerID = _selectedCustomerID;
            currentMaster.ShipDistrictID = (int)cmbDistrict.SelectedValue; // 💡 確保實體有此欄位
            currentMaster.ShipAddress = txtShipAddress.Text.Trim();
            currentMaster.Remark = string.IsNullOrWhiteSpace(txtRemark.Text) ? null : txtRemark.Text.Trim();
            currentMaster.UpdateUser = SessionContext.CurrentAccountID;

            // 💡 物理安插：為明細補上嚴格連續的行號 (LineNo)
            for (int i = 0; i < validDetails.Count; i++) validDetails[i].LineNo = i + 1;

            bool success = await SafeExecuteAsync(async () =>
            {
                if (_currentState == FormState.Add)
                {
                    // 🚀 發動分散式交易 + TVP 批次寫入
                    currentMaster = await _salesService.CreateSalesOrderAsync(currentMaster, validDetails, SessionContext.CurrentAccountID);
                }
                else if (_currentState == FormState.Edit)
                {
                    // 🚀 發動樂觀鎖 + 明細砍掉重練
                    byte[] newRowVersion = await _salesService.UpdateSalesOrderDraftAsync(currentMaster, validDetails, SessionContext.CurrentAccountID);
                }
            },
            reloadDataAction: async () => await SearchDataAsync());

            if (success)
            {
                string actionName = _currentState == FormState.Add ? "新增" : "更新";
                MessageBox.Show($"{actionName}成功！單號：{currentMaster.SalesNo}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (_currentState == FormState.Add)
                {
                    txtKeyword.Clear();
                    ucPagination.ResetToFirstPage();
                }
                SetUIState(FormState.Browse);
                await SearchDataAsync();
                _bsMaster.LocateTo<SalesMaster>(m => m.SalesNo == currentMaster.SalesNo);

            }

            if (_currentState == FormState.Add || _currentState == FormState.Edit)
            {
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
            }

        }

        // =====================================================================
        // 🔐 [狀態推進引擎] 審核過帳與作廢
        // =====================================================================
        private async void BtnPost_Click(object? sender, EventArgs e)
        {
            await ChangeStatusAsync("審核過帳", (byte)DocumentStatus.Draft, (byte)DocumentStatus.Posted);
        }

        private async void BtnVoid_Click(object? sender, EventArgs e)
        {
            var master = _bsMaster.Current as SalesMaster;
            if (master == null) return;

            byte expected = master.Status;
            byte target = expected == (byte)DocumentStatus.Draft ? (byte)DocumentStatus.Cancelled : (byte)DocumentStatus.Voided;
            string action = expected == (byte)DocumentStatus.Draft ? "註銷草稿" : "作廢單據 (財務沖銷)";

            await ChangeStatusAsync(action, expected, target);
        }
        private async Task ChangeStatusAsync(string actionName, byte expectedStatus, byte targetStatus)
        {
            var currentMaster = _bsMaster.Current as SalesMaster;
            if (currentMaster == null) return;

            if (MessageBox.Show($"確定要將單據 [{currentMaster.SalesNo}] 執行【{actionName}】嗎？\n此操作不可逆轉！",
                "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                bool success = await SafeExecuteAsync(async () =>
                {
                    // 🚀 執行狀態機單向推進與樂觀鎖防禦
                    _ = await _salesService.UpdateOrderStatusAsync(
                        currentMaster.SalesID,
                        expectedStatus,
                        targetStatus,
                        currentMaster.RowVersion,
                        SessionContext.CurrentAccountID);
                },
                reloadDataAction: async () => await SearchDataAsync());

                if (success)
                {
                    MessageBox.Show($"單據已成功{actionName}。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await SearchDataAsync();
                    _bsMaster.LocateTo<SalesMaster>(m => m.SalesNo == currentMaster.SalesNo);
                }
            }
        }

        // =====================================================================
        // ↕️ [排序引擎] 明細列上下移動 (精確防呆版)
        // 核心職責：純記憶體指標交換，維持實體的絕對純潔性。
        // =====================================================================
        private void btnMoveUp_Click(object? sender, EventArgs e)
        {
            // 防線 1：非編輯模式，或根本沒有選取資料時，物理阻斷
            if (_currentState == FormState.Browse || _bsDetail.Current == null) return;

            // 取得目前 BindingSource 鎖定的實體索引
            int currentIndex = _bsDetail.Position;

            // 防線 2：如果已經在最頂端 (Index 0)，無法再往上移
            if (currentIndex <= 0) return;

            // 💡 物理交換 (Memory Swap)：從舊位置拔除，插入新位置 (上移一格)
            var item = _detailBindingList[currentIndex];

            // 物理凍結：暫停事件觸發，防止重繪閃爍
            _detailBindingList.RaiseListChangedEvents = false;

            _detailBindingList.RemoveAt(currentIndex);
            _detailBindingList.Insert(currentIndex - 1, item);

            _detailBindingList.RaiseListChangedEvents = true;

            // 💡 強制大管家重整，並將焦點跟隨到移動後的新位置
            _bsDetail.ResetBindings(false);
            _bsDetail.Position = currentIndex - 1;
        }
        private void btnMoveDown_Click(object? sender, EventArgs e)
        {
            // 防線 1：非編輯模式，或根本沒有選取資料時，物理阻斷
            if (_currentState == FormState.Browse || _bsDetail.Current == null) return;

            int currentIndex = _bsDetail.Position;

            // 防線 2：如果已經在最底端，無法再往下移
            // 注意：若 DataGridView 開啟 AllowUserToAddRows，最底下會有一行「星號空白列」。
            // _detailBindingList.Count 只包含「真正已輸入的實體」，不受幽靈空白列影響，
            // 故最高索引絕對是 Count - 1。
            int maxIndex = _detailBindingList.Count - 1;
            if (currentIndex >= maxIndex) return;

            // 💡 物理交換 (Memory Swap)：從舊位置拔除，插入新位置 (下移一格)
            var item = _detailBindingList[currentIndex];

            _detailBindingList.RaiseListChangedEvents = false;

            _detailBindingList.RemoveAt(currentIndex);
            _detailBindingList.Insert(currentIndex + 1, item);

            _detailBindingList.RaiseListChangedEvents = true;

            // 💡 強制大管家重整，並將焦點跟隨到移動後的新位置
            _bsDetail.ResetBindings(false);
            _bsDetail.Position = currentIndex + 1;
        }
    }
}