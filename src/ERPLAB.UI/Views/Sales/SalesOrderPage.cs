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
    /// 銷貨單主明細表維護頁面 (Master-Detail Pattern)。
    /// 繼承自 BasePage，負責處理銷貨單據之完整生命週期操作。
    /// 實作 Table-Valued Parameter (TVP) 批次寫入、DataGridView 快速鍵盤輸入 (盲打)、狀態機防呆與自訂單據取號邏輯。
    /// </summary>
    public partial class SalesOrderPage : BasePage
    {
        // =====================================================================
        // 服務注入與全域狀態宣告
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

        // 表單狀態列舉，用於控制 UI 互動模式與唯讀限制
        private FormState _currentState = FormState.Browse;

        // 追蹤客戶實體 ID，作為資料庫關聯之唯一依據
        private int _selectedCustomerID = 0;

        public SalesOrderPage()
        {
            InitializeComponent();

            // 套用擴充方法開啟雙重緩衝，改善 DataGridView 渲染效能與滾動卡頓
            dgvSalesMaster.EnableDoubleBuffering(true);
            dgvSalesDetail.EnableDoubleBuffering(true);

            _salesService = new SalesOrderService();
            _custService = new CustomerService();
            _prodService = new ProductService();
            _geoService = new GeographyService();

            // 初始化資料綁定來源
            _bsMaster = new BindingSource();
            _bsDetail = new BindingSource();
            _masterBindingList = new ExtendedBindingList<SalesMaster>();
            _detailBindingList = new ExtendedBindingList<SalesDetail>();

            // 統一於建構子掛載生命週期與控制項事件，確保執行順序
            this.Load += SalesOrderPage_Load;

            // 透過 BindingSource 之 CurrentChanged 事件監聽焦點轉移
            _bsMaster.CurrentChanged += BsMaster_CurrentChanged;

            // 明細快速輸入與即時試算事件綁定
            dgvSalesDetail.CellEndEdit += DgvSalesDetail_CellEndEdit;
            dgvSalesDetail.RowsRemoved += (s, e) => RecalculateTotalAmount();
            dgvSalesDetail.DefaultValuesNeeded += DgvSalesDetail_DefaultValuesNeeded;

            // 資料列標頭序號繪製與自適應 (支援資料綁定、新增與刪除事件)
            dgvSalesDetail.DataBindingComplete += (s, e) => UpdateRowHeaderNumbers();
            dgvSalesDetail.RowsAdded += (s, e) => UpdateRowHeaderNumbers();
            dgvSalesDetail.RowsRemoved += (s, e) => UpdateRowHeaderNumbers();

            // 主檔操作工具列
            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
            btnPost.Click += BtnPost_Click;
            btnVoid.Click += BtnVoid_Click;

            // 明細資料列排序控制
            btnMoveUp.Click += btnMoveUp_Click;
            btnMoveDown.Click += btnMoveDown_Click;

            // 資料檢索與分頁控制
            txtKeyword.KeyDown += TxtKeyword_KeyDown;
            btnSearch.Click += btnSearch_Click;
            btnRefresh.Click += btnRefresh_Click;
            chkShowVoided.CheckedChanged += async (s, e) => await SearchDataAsync();
            ucPagination.PageChanged += async (s, e) => await SearchDataAsync();

            cmbCity.SelectedIndexChanged += CmbCity_SelectedIndexChanged;
            cmbDistrict.SelectedIndexChanged += CmbDistrict_SelectedIndexChanged;

            // 客戶檢索功能
            txtCustomerNo.KeyDown += txtCustomerNo_KeyDown;
            txtCustomerNo.TextChanged += TxtCustomerNo_TextChanged;
            btnLookupCustomer.Click += BtnLookupCustomer_Click;

            // 綁定 Grid 繪圖事件：處理已註銷或作廢資料之視覺提示
            dgvSalesMaster.CellFormatting += dgvSalesMaster_CellFormatting;
        }

        private async void SalesOrderPage_Load(object? sender, EventArgs e)
        {
            // =====================================================================
            // 權限檢核 (RBAC)：依據使用者授權動態顯示操作按鈕
            // 呼叫 BasePage 提供之 RequirePermission 進行控制項的實體隱藏
            // =====================================================================
            RequirePermission("ACT_SALE_ADD", btnAdd);
            RequirePermission("ACT_SALE_EDIT", btnEdit);
            RequirePermission("ACT_SALE_APPROVE", btnPost);
            RequirePermission("ACT_SALE_VOID", btnVoid);

            // 根據新增或修改之授權，決定儲存與取消按鈕之可見性
            bool canWrite = SessionContext.HasPermission("ACT_SALE_ADD") || SessionContext.HasPermission("ACT_SALE_EDIT");
            btnSave.Visible = canWrite;
            btnCancel.Visible = canWrite;

            SetupMasterGridColumns();
            SetupDetailGridColumns();

            _bsMaster.DataSource = _masterBindingList;
            dgvSalesMaster.DataSource = _bsMaster;

            _bsDetail.DataSource = _detailBindingList;
            dgvSalesDetail.DataSource = _bsDetail;

            // 載入初始資料
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
                // 快速輸入版型：配置 Textbox 以支援無滑鼠之純鍵盤輸入作業
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductNo", DataPropertyName = "ProductNo_Display", HeaderText = "商品代碼 (輸入)", Width = 150 });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", DataPropertyName = "ProductName_Display", HeaderText = "商品名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", DataPropertyName = "Qty", HeaderText = "數量", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", DataPropertyName = "UnitPrice", HeaderText = "單價", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal_Display", DataPropertyName = "SubTotal_Display", HeaderText = "小計", Width = 120, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight, BackColor = Color.WhiteSmoke } });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Remark", DataPropertyName = "Remark", HeaderText = "備註", Width = 150 });

                // 隱藏關聯鍵欄位，供後端邏輯對映使用
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", DataPropertyName = "ProductID", Visible = false });
                dgvSalesDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineNo", DataPropertyName = "LineNo", Visible = false });
            }
        }

        // =====================================================================
        // 地理資料連動與客戶檢索功能
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

            // 帶入預設聯絡地址與地理資訊
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
        // 資料查詢與綁定作業
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
            // 防呆處理：僅處理 Enter 鍵事件，且限制於編輯模式下觸發
            if (e.KeyCode != Keys.Enter || _currentState == FormState.Browse) return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            string inputNo = txtCustomerNo.Text.Trim();
            if (string.IsNullOrEmpty(inputNo)) return;

            try
            {
                // 呼叫資料存取層：透過分頁引擎設定取回首筆資料，減少網路與資料庫負載
                var result = await _custService.GetCustomersAsync(1, 1, false, inputNo);

                // 進行字串嚴格比對 (不區分大小寫)
                var match = result.Items.FirstOrDefault(c => c.CustomerNo.Equals(inputNo, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    ApplySelectedCustomer(match);

                    // 自動切換焦點至出貨地址欄位，優化輸入流程
                    txtShipAddress.Focus();
                }
                else
                {
                    MessageBox.Show("找不到此客戶代碼！", "查無資料", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // 防呆處理：若查無資料，清除選取狀態以防寫入錯誤關聯 ID
                    _selectedCustomerID = 0;
                    txtCustomerName.Clear();
                    txtCustomerNo.SelectAll();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查詢客戶時發生異常：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                // 若當前頁碼因資料刪除等原因導致越界，重新計算最後頁碼並再次執行查詢
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

                    // 因已暫停 CurrentChanged 事件，需手動呼叫 UI 更新
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
        // 明細資料管理與同步作業
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
            // 狀態徽章 (Status Badge) 與按鈕文字之動態渲染
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
        // 資料列標頭序號處理
        // 賦予 DataGridViewRow HeaderCell 數值，配合列寬自適應設計
        // =====================================================================
        private void UpdateRowHeaderNumbers()
        {
            // 暫停畫面佈局更新以防畫面閃爍
            dgvSalesDetail.SuspendLayout();

            foreach (DataGridViewRow row in dgvSalesDetail.Rows)
            {
                // 略過允許新增資料時最末端之空白列
                if (row.IsNewRow) continue;

                row.HeaderCell.Value = (row.Index + 1).ToString();
            }

            dgvSalesDetail.ResumeLayout(true);
        }

        // =====================================================================
        // 明細資料編輯與即時試算功能
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

            // 處理商品代碼輸入，向後端查詢對應資訊並回填明細欄位
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

            // 監聽數量或單價變動，觸發結束編輯模式並重新計算總計
            if (colName == "Qty" || colName == "UnitPrice" || colName == "ProductNo")
            {
                dgvSalesDetail.EndEdit();
                RecalculateTotalAmount();
                dgvSalesDetail.InvalidateRow(e.RowIndex); // 強制重繪當前列，更新小計顯示
            }
        }

        private void dgvSalesMaster_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // 確保索引在合法範圍內
            if (e.RowIndex >= 0 && e.RowIndex < dgvSalesMaster.Rows.Count)
            {
                var salesMaster = dgvSalesMaster.Rows[e.RowIndex].DataBoundItem as SalesMaster;

                // 若單據狀態為已註銷 (3) 或已作廢 (4)，變更列樣式以提醒使用者
                if (salesMaster != null && (salesMaster.Status == 3 || salesMaster.Status == 4) && e.CellStyle != null)
                {
                    e.CellStyle.ForeColor = System.Drawing.Color.DarkGray;
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
        // 狀態機 (State Machine) 控制邏輯
        // 依據單據狀態與操作模式控制前端介面屬性
        // =====================================================================
        private void SetUIState(FormState state)
        {
            _currentState = state;
            bool isEditing = (state == FormState.Add || state == FormState.Edit);
            bool isBrowse = (state == FormState.Browse);

            var master = _bsMaster.Current as SalesMaster;
            // 權限防呆控制：僅草稿狀態且處於編輯模式下開放資料異動
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

            // 明細 Grid 狀態控制
            dgvSalesDetail.ReadOnly = !canEditFields;
            dgvSalesDetail.AllowUserToAddRows = canEditFields;
            dgvSalesDetail.AllowUserToDeleteRows = canEditFields;

            btnMoveUp.Enabled = canEditFields;
            btnMoveDown.Enabled = canEditFields;

            dgvSalesMaster.Enabled = !isEditing;
            pnlSearch.Enabled = !isEditing;

            // 基礎 CRUD 按鈕狀態
            btnAdd.Enabled = !isEditing;
            btnEdit.Enabled = !isEditing && master != null && isDraft;
            btnSave.Enabled = isEditing;
            btnCancel.Enabled = isEditing;
            btnRefresh.Enabled = !isEditing;

            // 單據狀態流轉控制按鈕防呆
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
        // 資料異動作業 (新增、修改、狀態切換與樂觀鎖處理)
        // =====================================================================
        private void BtnAdd_Click(object? sender, EventArgs e) => SetUIState(FormState.Add);
        private void BtnEdit_Click(object? sender, EventArgs e) => SetUIState(FormState.Edit);

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            SetUIState(FormState.Browse);
            if (_bsMaster.Current is SalesMaster master)
            {
                BindMasterUI(master);
                // 放棄修改時重新載入明細，還原畫面與資料庫同步狀態
                _ = LoadDetailDataAsync(master.SalesID);
            }
            else { ClearMasterUI(); }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            // 強制結束 DataGridView 編輯狀態，確保數值推入資料繫結集合
            dgvSalesDetail.EndEdit();
            _bsDetail.EndEdit();

            // 基礎資料檢核
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

            // 過濾並剃除未填寫商品代碼或數量之無效明細列
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

            // 將 UI 畫面資料對映回記憶體實體 (DTO Mapping)
            string front = txtShipZipFront.Text.Trim();
            string rear = txtShipZipRear.Text.Trim();
            currentMaster.ShipZipCode = string.IsNullOrEmpty(rear) ? front : front + rear;

            currentMaster.SalesDate = dtpSalesDate.Value;
            currentMaster.CustomerID = _selectedCustomerID;
            currentMaster.ShipDistrictID = (int)cmbDistrict.SelectedValue;
            currentMaster.ShipAddress = txtShipAddress.Text.Trim();
            currentMaster.Remark = string.IsNullOrWhiteSpace(txtRemark.Text) ? null : txtRemark.Text.Trim();
            currentMaster.UpdateUser = SessionContext.CurrentAccountID;

            // 依序指派明細行號
            for (int i = 0; i < validDetails.Count; i++) validDetails[i].LineNo = i + 1;

            bool success = await SafeExecuteAsync(async () =>
            {
                if (_currentState == FormState.Add)
                {
                    // 執行分散式交易與 TVP 批次寫入
                    currentMaster = await _salesService.CreateSalesOrderAsync(currentMaster, validDetails, SessionContext.CurrentAccountID);
                }
                else if (_currentState == FormState.Edit)
                {
                    // 執行樂觀鎖檢查與草稿更新作業
                    byte[] newRowVersion = await _salesService.UpdateSalesOrderDraftAsync(currentMaster, validDetails, SessionContext.CurrentAccountID);
                }
            },
            reloadDataAction: async () => await SearchDataAsync());

            if (success)
            {
                string actionName = _currentState == FormState.Add ? "新增" : "更新";
                MessageBox.Show($"{actionName}成功！單號：{currentMaster.SalesNo}", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        // 單據狀態作業流程 (審核過帳、註銷草稿、作廢單據)
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
                    // 執行單據狀態切換與樂觀鎖防禦
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
        // 明細資料列排序調整處理
        // 操作指標改變資料順序，以變更實際寫入資料庫之排序
        // =====================================================================
        private void btnMoveUp_Click(object? sender, EventArgs e)
        {
            if (_currentState == FormState.Browse || _bsDetail.Current == null) return;

            int currentIndex = _bsDetail.Position;

            // 防止首列向上移動
            if (currentIndex <= 0) return;

            var item = _detailBindingList[currentIndex];

            // 暫停 ListChanged 事件，降低重複繪圖之成本
            _detailBindingList.RaiseListChangedEvents = false;

            _detailBindingList.RemoveAt(currentIndex);
            _detailBindingList.Insert(currentIndex - 1, item);

            _detailBindingList.RaiseListChangedEvents = true;

            // 呼叫 ResetBindings 強制更新，並跟隨定位至移動後的位置
            _bsDetail.ResetBindings(false);
            _bsDetail.Position = currentIndex - 1;
        }

        private void btnMoveDown_Click(object? sender, EventArgs e)
        {
            if (_currentState == FormState.Browse || _bsDetail.Current == null) return;

            int currentIndex = _bsDetail.Position;

            // 若 DataGridView 允許新增資料 (AllowUserToAddRows = true)，清單最末會自動產生一筆空列。
            // 使用 Count 計算索引，即可略過此末端保留列之影響。
            int maxIndex = _detailBindingList.Count - 1;
            if (currentIndex >= maxIndex) return;

            var item = _detailBindingList[currentIndex];

            _detailBindingList.RaiseListChangedEvents = false;

            _detailBindingList.RemoveAt(currentIndex);
            _detailBindingList.Insert(currentIndex + 1, item);

            _detailBindingList.RaiseListChangedEvents = true;

            _bsDetail.ResetBindings(false);
            _bsDetail.Position = currentIndex + 1;
        }
    }
}