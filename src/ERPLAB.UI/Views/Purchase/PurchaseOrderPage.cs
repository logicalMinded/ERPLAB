using ERPLAB.BLL.Services;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using ERPLAB.UI.Core;
using ERPLAB.UI.Views.BaseData;
//using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.UI.Views.Purchase
{
    /// <summary>
    /// 進貨單主明細表維護頁面 (Master-Detail Pattern)。
    /// 繼承自 BasePage，負責處理進貨單據之完整生命週期操作。
    /// 實作 Table-Valued Parameter (TVP) 批次寫入、DataGridView 快速鍵盤輸入 (盲打)、狀態機防呆與自訂單據取號邏輯。
    /// </summary>
    public partial class PurchaseOrderPage : BasePage
    {
        // =====================================================================
        // 服務注入與全域狀態宣告
        // =====================================================================
        private readonly PurchaseOrderService _purchService;
        private readonly VendorService _vendService;
        private readonly ProductService _prodService;
        private readonly GeographyService _geoService;

        private BindingSource _bsMaster;
        private BindingSource _bsDetail;
        private ExtendedBindingList<PurchaseMaster> _masterBindingList;
        private ExtendedBindingList<PurchaseDetail> _detailBindingList;

        private List<Base_City> _cityList = new();
        private List<Base_District> _allDistrictList = new();

        // 表單狀態列舉，用於控制 UI 互動模式與唯讀限制
        private FormState _currentState = FormState.Browse;

        // 追蹤廠商實體 ID，作為資料庫關聯之唯一依據
        private int _selectedVendorID = 0;

        public PurchaseOrderPage()
        {
            InitializeComponent();

            // 套用擴充方法開啟雙重緩衝，改善 DataGridView 渲染效能與滾動卡頓
            dgvPurchaseMaster.EnableDoubleBuffering(true);
            dgvPurchaseDetail.EnableDoubleBuffering(true);

            _purchService = new PurchaseOrderService();
            _vendService = new VendorService();
            _prodService = new ProductService();
            _geoService = new GeographyService();

            // 初始化資料綁定來源
            _bsMaster = new BindingSource();
            _bsDetail = new BindingSource();
            _masterBindingList = new ExtendedBindingList<PurchaseMaster>();
            _detailBindingList = new ExtendedBindingList<PurchaseDetail>();

            // 統一於建構子掛載生命週期與控制項事件，確保執行順序
            this.Load += PurchaseOrderPage_Load;

            // 透過 BindingSource 之 CurrentChanged 事件監聽焦點轉移
            _bsMaster.CurrentChanged += BsMaster_CurrentChanged;

            // 明細快速輸入與即時試算事件綁定
            dgvPurchaseDetail.CellEndEdit += DgvPurchaseDetail_CellEndEdit;
            dgvPurchaseDetail.RowsRemoved += (s, e) => RecalculateTotalAmount();
            dgvPurchaseDetail.DefaultValuesNeeded += DgvPurchaseDetail_DefaultValuesNeeded;

            // 資料列標頭序號繪製與自適應 (支援資料綁定、新增與刪除事件)
            dgvPurchaseDetail.DataBindingComplete += (s, e) => UpdateRowHeaderNumbers();
            dgvPurchaseDetail.RowsAdded += (s, e) => UpdateRowHeaderNumbers();
            dgvPurchaseDetail.RowsRemoved += (s, e) => UpdateRowHeaderNumbers();

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

            // 廠商檢索功能
            txtVendorNo.KeyDown += txtVendorNo_KeyDown;
            txtVendorNo.TextChanged += TxtVendorNo_TextChanged;
            btnLookupVendor.Click += BtnLookupVendor_Click;

            // 綁定 Grid 繪圖事件：處理已作廢資料之視覺提示
            dgvPurchaseMaster.CellFormatting += dgvPurchaseMaster_CellFormatting;
        }

        private async void PurchaseOrderPage_Load(object? sender, EventArgs e)
        {
            // =====================================================================
            // 權限檢核 (RBAC)：依據使用者授權動態顯示操作按鈕
            // 呼叫 BasePage 提供之 RequirePermission 進行控制項的實體隱藏
            // =====================================================================
            RequirePermission("ACT_PURC_ADD", btnAdd);
            RequirePermission("ACT_PURC_EDIT", btnEdit);
            RequirePermission("ACT_PURC_APPROVE", btnPost);
            RequirePermission("ACT_PURC_VOID", btnVoid);

            // 根據新增或修改之授權，決定儲存與取消按鈕之可見性
            bool canWrite = SessionContext.HasPermission("ACT_PURC_ADD") || SessionContext.HasPermission("ACT_PURC_EDIT");
            btnSave.Visible = canWrite;
            btnCancel.Visible = canWrite;

            SetupMasterGridColumns();
            SetupDetailGridColumns();

            _bsMaster.DataSource = _masterBindingList;
            dgvPurchaseMaster.DataSource = _bsMaster;

            _bsDetail.DataSource = _detailBindingList;
            dgvPurchaseDetail.DataSource = _bsDetail;

            // 載入初始資料
            await SearchDataAsync();
            SetUIState(FormState.Browse);
        }

        private void SetupMasterGridColumns()
        {
            dgvPurchaseMaster.AutoGenerateColumns = false;
            if (dgvPurchaseMaster.Columns.Count == 0)
            {
                dgvPurchaseMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PurchaseNo", HeaderText = "進貨單號", Width = 150 });
                dgvPurchaseMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PurchaseDate", HeaderText = "單據日期", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
                dgvPurchaseMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "VendorName_Display", HeaderText = "廠商名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
                dgvPurchaseMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount", HeaderText = "總金額", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } });
            }
        }

        private void SetupDetailGridColumns()
        {
            dgvPurchaseDetail.AutoGenerateColumns = false;
            dgvPurchaseDetail.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            if (dgvPurchaseDetail.Columns.Count == 0)
            {
                // 快速輸入版型：配置 Textbox 以支援無滑鼠之純鍵盤輸入作業
                dgvPurchaseDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductNo", DataPropertyName = "ProductNo_Display", HeaderText = "商品代碼 (輸入)", Width = 150 });
                dgvPurchaseDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", DataPropertyName = "ProductName_Display", HeaderText = "商品名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
                dgvPurchaseDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", DataPropertyName = "Qty", HeaderText = "數量", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvPurchaseDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", DataPropertyName = "UnitPrice", HeaderText = "單價", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvPurchaseDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal_Display", DataPropertyName = "SubTotal_Display", HeaderText = "小計", Width = 120, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight, BackColor = Color.WhiteSmoke } });
                dgvPurchaseDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Remark", DataPropertyName = "Remark", HeaderText = "備註", Width = 150 });

                // 隱藏關聯鍵欄位，供後端邏輯對映使用
                dgvPurchaseDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", DataPropertyName = "ProductID", Visible = false });
                dgvPurchaseDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineNo", DataPropertyName = "LineNo", Visible = false });
            }
        }

        // =====================================================================
        // 廠商檢索功能
        // =====================================================================
        private void TxtVendorNo_TextChanged(object? sender, EventArgs e)
        {
            if (_currentState == FormState.Browse) return;
            _selectedVendorID = 0;
            txtVendorName.Clear();
        }

        private void BtnLookupVendor_Click(object? sender, EventArgs e)
        {
            if (_currentState == FormState.Browse) return;

            using (var lookupForm = new VendorLookupForm())
            {
                if (lookupForm.ShowDialog() == DialogResult.OK)
                    ApplySelectedVendor(lookupForm.SelectedVendor);
            }
        }

        private void ApplySelectedVendor(Vendor v)
        {
            if (v == null) return;
            _selectedVendorID = v.VendorID;
            txtVendorNo.TextChanged -= TxtVendorNo_TextChanged;
            txtVendorNo.Text = v.VendorNo;
            txtVendorNo.TextChanged += TxtVendorNo_TextChanged;
            txtVendorName.Text = v.VendorName;
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

        private async void txtVendorNo_KeyDown(object? sender, KeyEventArgs e)
        {
            // 防呆處理：僅處理 Enter 鍵事件，且限制於編輯模式下觸發
            if (e.KeyCode != Keys.Enter || _currentState == FormState.Browse) return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            string inputNo = txtVendorNo.Text.Trim();
            if (string.IsNullOrEmpty(inputNo)) return;

            try
            {
                // 呼叫資料存取層：透過分頁引擎設定取回首筆資料，減少網路與資料庫負載
                var result = await _vendService.GetVendorsAsync(1, 1, false, inputNo);

                // 進行字串嚴格比對 (不區分大小寫)
                var match = result.Items.FirstOrDefault(c => c.VendorNo.Equals(inputNo, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    ApplySelectedVendor(match);

                    // 自動切換焦點至備註欄位，優化輸入流程
                }
                else
                {
                    MessageBox.Show("找不到此廠商代碼！", "查無資料", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // 防呆處理：若查無資料，清除選取狀態以防寫入錯誤關聯 ID
                    _selectedVendorID = 0;
                    txtVendorName.Clear();
                    txtVendorNo.SelectAll();
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
                ucPagination.ResetToFirstPage();
                await SearchDataAsync();
            }
            finally { btnRefresh.Enabled = true; }
        }

        private async Task SearchDataAsync()
        {
            string keyword = txtKeyword.Text.Trim();
            bool showVoided = chkShowVoided.Checked;

            long? lastSelectedId = _bsMaster.Current is PurchaseMaster currentMaster ? currentMaster.PurchaseID : null;

            int pageSize = ucPagination.PageSize;
            int currentPage = ucPagination.CurrentPage;

            try
            {
                var result = await _purchService.GetPurchaseOrdersAsync(currentPage, pageSize, keyword, showVoided);

                // 若當前頁碼因資料刪除等原因導致越界，重新計算最後頁碼並再次執行查詢
                if (result.Items.Count == 0 && result.TotalCount > 0)
                {
                    int correctLastPage = (int)Math.Ceiling((double)result.TotalCount / pageSize);
                    ucPagination.ForceCurrentPage(correctLastPage);
                    result = await _purchService.GetPurchaseOrdersAsync(correctLastPage, pageSize, keyword, showVoided);
                }

                _bsMaster.CurrentChanged -= BsMaster_CurrentChanged;

                _masterBindingList.Clear();
                _masterBindingList.AddRange(result.Items);

                if (_bsMaster.Count > 0)
                {
                    var targetMaster = _masterBindingList.FirstOrDefault(m => m.PurchaseID == lastSelectedId);
                    int targetIndex = targetMaster != null ? _bsMaster.IndexOf(targetMaster) : 0;
                    _bsMaster.Position = targetIndex;

                    targetMaster = (PurchaseMaster)_bsMaster.Current!;

                    // 因已暫停 CurrentChanged 事件，需手動呼叫 UI 更新
                    BindMasterUI(targetMaster);
                    await LoadDetailDataAsync(targetMaster.PurchaseID);
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

            if (_bsMaster.Current is PurchaseMaster master)
            {
                BindMasterUI(master);
                await LoadDetailDataAsync(master.PurchaseID);
            }
        }

        private async Task LoadDetailDataAsync(long purchaseId)
        {
            try
            {
                var details = await _purchService.GetPurchaseDetailsAsync(purchaseId);
                _detailBindingList.Clear();
                _detailBindingList.AddRange(details);
                RecalculateTotalAmount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"明細載入失敗：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindMasterUI(PurchaseMaster m)
        {
            if (m == null)
            {
                MessageBox.Show("系統無法取得當前操作的單據！", "狀態異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetUIState(FormState.Browse);
                return;
            }

            splitRight.Panel1.SuspendLayout();

            _selectedVendorID = m.VendorID;
            txtPurchaseNo.Text = m.PurchaseNo;
            dtpPurchaseDate.Value = m.PurchaseDate;
            txtVendorNo.Text = m.VendorNo_Display;
            txtVendorName.Text = m.VendorName_Display;

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

            if (m.PurchaseID > 0)
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
            _selectedVendorID = 0;
            txtPurchaseNo.Text = "[儲存後自動配發]";
            dtpPurchaseDate.Value = DateTime.Now;
            txtVendorNo.Clear();
            txtVendorName.Clear();
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
            dgvPurchaseDetail.SuspendLayout();

            foreach (DataGridViewRow row in dgvPurchaseDetail.Rows)
            {
                // 略過允許新增資料時最末端之空白列
                if (row.IsNewRow) continue;

                row.HeaderCell.Value = (row.Index + 1).ToString();
            }

            dgvPurchaseDetail.ResumeLayout(true);
        }

        // =====================================================================
        // 明細資料編輯與即時試算功能
        // =====================================================================
        private void DgvPurchaseDetail_DefaultValuesNeeded(object? sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells["Qty"].Value = 1;
            e.Row.Cells["UnitPrice"].Value = 0m;
        }

        private async void DgvPurchaseDetail_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _currentState == FormState.Browse) return;

            var colName = dgvPurchaseDetail.Columns[e.ColumnIndex].Name;

            // 處理商品代碼輸入，向後端查詢對應資訊並回填明細欄位
            if (colName == "ProductNo")
            {
                string? inputNo = dgvPurchaseDetail.Rows[e.RowIndex].Cells["ProductNo"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(inputNo))
                {
                    try
                    {
                        var product = await _prodService.GetProductByNoAsync(inputNo);
                        if (product != null)
                        {
                            dgvPurchaseDetail.Rows[e.RowIndex].Cells["ProductNo"].Value = product.ProductNo;
                            dgvPurchaseDetail.Rows[e.RowIndex].Cells["ProductID"].Value = product.ProductID;
                            dgvPurchaseDetail.Rows[e.RowIndex].Cells["ProductName"].Value = product.ProductName;
                            dgvPurchaseDetail.Rows[e.RowIndex].Cells["UnitPrice"].Value = product.PurchasePrice;
                        }
                        else
                        {
                            MessageBox.Show("查無此商品代碼！");
                            dgvPurchaseDetail.Rows[e.RowIndex].Cells["ProductNo"].Value = string.Empty;
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("查詢商品失敗：" + ex.Message); }
                }
            }

            // 監聽數量或單價變動，觸發結束編輯模式並重新計算總計
            if (colName == "Qty" || colName == "UnitPrice" || colName == "ProductNo")
            {
                dgvPurchaseDetail.EndEdit();
                RecalculateTotalAmount();
                dgvPurchaseDetail.InvalidateRow(e.RowIndex); // 強制重繪當前列，更新小計顯示
            }
        }

        // =====================================================================
        // 資料列視覺樣式自訂 (Visual Distinction)
        // =====================================================================
        private void dgvPurchaseMaster_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // 確保索引範圍正確
            if (e.RowIndex >= 0 && e.RowIndex < dgvPurchaseMaster.Rows.Count)
            {
                var purchaseMaster = dgvPurchaseMaster.Rows[e.RowIndex].DataBoundItem as PurchaseMaster;

                // 若單據狀態為已註銷 (3) 或已作廢 (4)，變更列樣式以提醒使用者
                if (purchaseMaster != null && (purchaseMaster.Status == 3 || purchaseMaster.Status == 4) && e.CellStyle != null)
                {
                    e.CellStyle.ForeColor = System.Drawing.Color.DarkGray;
                    e.CellStyle.Font = new System.Drawing.Font(dgvPurchaseMaster.Font, System.Drawing.FontStyle.Strikeout);
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

            var master = _bsMaster.Current as PurchaseMaster;
            // 權限防呆控制：僅草稿狀態且處於編輯模式下開放資料異動
            bool isDraft = (state == FormState.Add) || (master != null && master.Status == (byte)DocumentStatus.Draft);
            bool canEditFields = isEditing && isDraft;

            txtPurchaseNo.ReadOnly = true;
            txtVendorNo.ReadOnly = !canEditFields;
            btnLookupVendor.Enabled = canEditFields;
            txtRemark.ReadOnly = !canEditFields;
            dtpPurchaseDate.Enabled = canEditFields;

            // 明細 Grid 狀態控制
            dgvPurchaseDetail.ReadOnly = !canEditFields;
            dgvPurchaseDetail.AllowUserToAddRows = canEditFields;
            dgvPurchaseDetail.AllowUserToDeleteRows = canEditFields;

            btnMoveUp.Enabled = canEditFields;
            btnMoveDown.Enabled = canEditFields;

            dgvPurchaseMaster.Enabled = !isEditing;
            pnlSearch.Enabled = !isEditing;

            // 基礎 CRUD 工具列控制
            btnAdd.Enabled = !isEditing;
            btnEdit.Enabled = !isEditing && master != null && isDraft; // 僅限草稿允許修改
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
                txtVendorNo.Focus();
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
            if (_bsMaster.Current is PurchaseMaster master)
            {
                BindMasterUI(master);
                // 放棄修改時重新載入明細，還原畫面與資料庫同步狀態
                _ = LoadDetailDataAsync(master.PurchaseID);
            }
            else { ClearMasterUI(); }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            // 強制結束 DataGridView 編輯狀態，確保數值推入資料繫結集合
            dgvPurchaseDetail.EndEdit();
            _bsDetail.EndEdit();

            // 基礎資料檢核
            if (string.IsNullOrWhiteSpace(txtVendorNo.Text) || _selectedVendorID <= 0)
            {
                MessageBox.Show("請輸入並確認有效的廠商代碼！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 過濾並剃除未填寫商品代碼或數量之無效明細列
            var validDetails = _detailBindingList.Where(d => d.ProductID > 0 && d.Qty > 0).ToList();
            if (validDetails.Count == 0)
            {
                MessageBox.Show("進貨單至少需要輸入一筆有效的商品明細 (數量不可為 0)！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSave.Enabled = false;
            btnCancel.Enabled = false;

            var currentMaster = _currentState == FormState.Add ? new PurchaseMaster() : (_bsMaster.Current as PurchaseMaster);
            if (currentMaster == null) return;

            currentMaster.PurchaseDate = dtpPurchaseDate.Value;
            currentMaster.VendorID = _selectedVendorID;
            currentMaster.Remark = string.IsNullOrWhiteSpace(txtRemark.Text) ? null : txtRemark.Text.Trim();
            currentMaster.UpdateUser = SessionContext.CurrentAccountID;

            // 依序指派明細行號
            for (int i = 0; i < validDetails.Count; i++) validDetails[i].LineNo = i + 1;

            bool success = await SafeExecuteAsync(async () =>
            {
                if (_currentState == FormState.Add)
                {
                    // 執行分散式交易與 TVP 批次寫入
                    currentMaster = await _purchService.CreatePurchaseOrderAsync(currentMaster, validDetails, SessionContext.CurrentAccountID);
                }
                else if (_currentState == FormState.Edit)
                {
                    // 執行樂觀鎖檢查與草稿更新作業
                    byte[] newRowVersion = await _purchService.UpdatePurchaseOrderDraftAsync(currentMaster, validDetails, SessionContext.CurrentAccountID);
                }
            },
            reloadDataAction: async () => await SearchDataAsync());

            if (success)
            {
                string actionName = _currentState == FormState.Add ? "新增" : "更新";
                MessageBox.Show($"{actionName}成功！單號：{currentMaster.PurchaseNo}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (_currentState == FormState.Add)
                {
                    txtKeyword.Clear();
                    ucPagination.ResetToFirstPage();
                }
                SetUIState(FormState.Browse);
                await SearchDataAsync();
                _bsMaster.LocateTo<PurchaseMaster>(m => m.PurchaseNo == currentMaster.PurchaseNo);
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
            var master = _bsMaster.Current as PurchaseMaster;
            if (master == null) return;

            byte expected = master.Status;
            byte target = expected == (byte)DocumentStatus.Draft ? (byte)DocumentStatus.Cancelled : (byte)DocumentStatus.Voided;
            string action = expected == (byte)DocumentStatus.Draft ? "註銷草稿" : "作廢單據 (財務沖銷)";

            await ChangeStatusAsync(action, expected, target);
        }
        private async Task ChangeStatusAsync(string actionName, byte expectedStatus, byte targetStatus)
        {
            var currentMaster = _bsMaster.Current as PurchaseMaster;
            if (currentMaster == null) return;

            if (MessageBox.Show($"確定要將單據 [{currentMaster.PurchaseNo}] 執行【{actionName}】嗎？\n此操作不可逆轉！",
                "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                bool success = await SafeExecuteAsync(async () =>
                {
                    // 執行單據狀態切換與樂觀鎖防禦
                    _ = await _purchService.ChangeOrderStatusAsync(
                        currentMaster.PurchaseID,
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
                    _bsMaster.LocateTo<PurchaseMaster>(m => m.PurchaseNo == currentMaster.PurchaseNo);
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