using ERPLAB.BLL.Services;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using ERPLAB.UI.Core;
using ERPLAB.UI.Views.BaseData;
using System.Data;

namespace ERPLAB.UI.Views.Inventory
{
    /// <summary>
    /// 庫存盤點單維護模組 (Master-Detail)
    /// 核心展示：雙軌庫存比對、一鍵載入引擎、實體硬刪除 (Hard Delete)、TVP 批次寫入。
    /// </summary>
    public partial class InventoryPage : BasePage
    {
        // =====================================================================
        // 💡 倉儲與主明細狀態快取
        // =====================================================================
        private readonly InventoryService _invService;
        private readonly EmployeeService _empService;
        private readonly ProductService _prodService;

        private BindingSource _bsMaster;
        private BindingSource _bsDetail;
        private ExtendedBindingList<InventoryMaster> _masterBindingList;
        private ExtendedBindingList<InventoryDetail> _detailBindingList;

        private FormState _currentState = FormState.Browse;
        private int _selectedEmployeeID = 0; // 追蹤負責盤點的員工 ID

        public InventoryPage()
        {
            InitializeComponent();

            // 物理優化：強制開啟 Grid 的雙重緩衝
            dgvInventoryMaster.EnableDoubleBuffering(true);
            dgvInventoryDetail.EnableDoubleBuffering(true);

            _invService = new InventoryService();
            _empService = new EmployeeService();
            _prodService = new ProductService();

            _bsMaster = new BindingSource();
            _bsDetail = new BindingSource();
            _masterBindingList = new ExtendedBindingList<InventoryMaster>();
            _detailBindingList = new ExtendedBindingList<InventoryDetail>();

            // 生命週期綁定
            this.Load += InventoryPage_Load;
            _bsMaster.CurrentChanged += BsMaster_CurrentChanged;

            // 明細盲打試算與渲染事件
            dgvInventoryDetail.CellEndEdit += DgvInventoryDetail_CellEndEdit;
            dgvInventoryDetail.RowsRemoved += (s, e) => RecalculateDiffAmount();
            dgvInventoryDetail.DefaultValuesNeeded += DgvInventoryDetail_DefaultValuesNeeded;

            // 行號自繪與寬度自適應
            dgvInventoryDetail.DataBindingComplete += (s, e) => UpdateRowHeaderNumbers();
            dgvInventoryDetail.RowsAdded += (s, e) => UpdateRowHeaderNumbers();
            dgvInventoryDetail.RowsRemoved += (s, e) => UpdateRowHeaderNumbers();

            // 主檔工具列
            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
            btnDelete.Click += BtnDelete_Click; // 💡 盤點專屬：實體刪除草稿
            btnPost.Click += BtnPost_Click;

            // 檢索與分頁
            txtKeyword.KeyDown += txtKeyword_KeyDown;
            btnSearch.Click += btnSearch_Click;
            btnRefresh.Click += btnRefresh_Click;
            ucPagination.PageChanged += async (s, e) => await SearchDataAsync();

            // 盤點人員速查引擎
            txtEmployeeNo.KeyDown += txtEmployeeNo_KeyDown;
            txtEmployeeNo.TextChanged += txtEmployeeNo_TextChanged;
            btnLookupEmployee.Click += BtnLookupEmployee_Click;

            // 💡 明細專屬工具列 (一鍵載入與清空)
            btnLoadSystemStock.Click += BtnLoadSystemStock_Click;
            btnClearDetails.Click += BtnClearDetails_Click;
        }

        private async void InventoryPage_Load(object? sender, EventArgs e)
        {
            // 1. RBAC 權限物理斷路
            RequirePermission("ACT_INV_ADD", btnAdd);
            RequirePermission("ACT_INV_EDIT", btnEdit);
            RequirePermission("ACT_INV_DEL", btnDelete);
            RequirePermission("ACT_INV_APPROVE", btnPost);

            bool canWrite = SessionContext.HasPermission("ACT_INV_ADD") || SessionContext.HasPermission("ACT_INV_EDIT");
            btnSave.Visible = canWrite;
            btnCancel.Visible = canWrite;

            // 明細專屬按鈕也受寫入權限管制
            btnLoadSystemStock.Visible = canWrite;
            btnClearDetails.Visible = canWrite;

            SetupMasterGridColumns();
            SetupDetailGridColumns();

            _bsMaster.DataSource = _masterBindingList;
            dgvInventoryMaster.DataSource = _bsMaster;

            _bsDetail.DataSource = _detailBindingList;
            dgvInventoryDetail.DataSource = _bsDetail;

            await SearchDataAsync();
            SetUIState(FormState.Browse);
        }

        private void SetupMasterGridColumns()
        {
            dgvInventoryMaster.AutoGenerateColumns = false;
            if (dgvInventoryMaster.Columns.Count == 0)
            {
                dgvInventoryMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InventoryNo", HeaderText = "盤點單號", Width = 160 });
                dgvInventoryMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InventoryDate", HeaderText = "盤點日期", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
                dgvInventoryMaster.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EmployeeName_Display", HeaderText = "盤點人員", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            }
        }

        private void SetupDetailGridColumns()
        {
            dgvInventoryDetail.AutoGenerateColumns = false;
            dgvInventoryDetail.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            if (dgvInventoryDetail.Columns.Count == 0)
            {
                // 💡 [雙軌庫存版型] 明確區分帳面數量(唯讀)與實盤數量(可改)
                dgvInventoryDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductNo", DataPropertyName = "ProductNo_Display", HeaderText = "商品代碼 (輸入)", Width = 150 });
                dgvInventoryDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", DataPropertyName = "ProductName_Display", HeaderText = "商品名稱", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });

                dgvInventoryDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SystemStock", DataPropertyName = "SystemStock", HeaderText = "帳面庫存", Width = 90, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, BackColor = Color.WhiteSmoke } });
                dgvInventoryDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualStock", DataPropertyName = "ActualStock", HeaderText = "實盤數量", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Color.Blue } });

                // 差異視覺化 (正數為盈，負數為虧)
                dgvInventoryDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffQty_Display", DataPropertyName = "DiffQty_Display", HeaderText = "差異數量", Width = 90, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, BackColor = Color.WhiteSmoke, Font = new Font("微軟正黑體", 10, FontStyle.Bold) } });

                dgvInventoryDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockPrice", DataPropertyName = "StockPrice", HeaderText = "單位成本", Width = 100, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvInventoryDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffAmount_Display", DataPropertyName = "DiffAmount_Display", HeaderText = "盤盈虧金額", Width = 120, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight, BackColor = Color.WhiteSmoke } });

                dgvInventoryDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "Remark", DataPropertyName = "Remark", HeaderText = "差異原因備註", Width = 150 });

                dgvInventoryDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", DataPropertyName = "ProductID", Visible = false });
            }
        }

        // =====================================================================
        // 🔍 [員工速查引擎] (比照客戶速查邏輯)
        // =====================================================================
        private async void txtEmployeeNo_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter || _currentState == FormState.Browse) return;
            e.SuppressKeyPress = true;

            string inputNo = txtEmployeeNo.Text.Trim();
            if (string.IsNullOrEmpty(inputNo)) return;

            try
            {
                var result = await _empService.GetEmployeesAsync(1, 1, false, inputNo);
                var match = result.Items.FirstOrDefault(emp => emp.EmployeeNo.Equals(inputNo, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    ApplySelectedEmployee(match);
                    txtRemark.Focus();
                }
                else
                {
                    MessageBox.Show("找不到此員工代碼！", "查無資料", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _selectedEmployeeID = 0;
                    txtEmployeeName.Clear();
                    txtEmployeeNo.SelectAll();
                }
            }
            catch (Exception ex) { MessageBox.Show($"查詢員工發生異常：{ex.Message}"); }
        }

        private void txtEmployeeNo_TextChanged(object? sender, EventArgs e)
        {
            if (_currentState == FormState.Browse) return;
            _selectedEmployeeID = 0;
            txtEmployeeName.Clear();
        }

        private void BtnLookupEmployee_Click(object? sender, EventArgs e)
        {
            if (_currentState == FormState.Browse) return;

            using (var lookupForm = new EmployeeLookupForm())
            {
                if (lookupForm.ShowDialog() == DialogResult.OK)
                    ApplySelectedEmployee(lookupForm.SelectedEmployee);
            }
        }

        private void ApplySelectedEmployee(Employee emp)
        {
            if (emp == null) return;

            txtEmployeeNo.TextChanged -= txtEmployeeNo_TextChanged;
            _selectedEmployeeID = emp.EmployeeID;
            txtEmployeeNo.Text = emp.EmployeeNo;
            txtEmployeeName.Text = emp.EmployeeName;
            txtEmployeeNo.TextChanged += txtEmployeeNo_TextChanged;
        }

        // =====================================================================
        // 🔄 [資料流引擎] 搜尋與主檔綁定
        // =====================================================================
        private async void txtKeyword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; await SearchDataAsync(); }
        }

        private async void btnSearch_Click(object? sender, EventArgs e)
        {
            btnSearch.Enabled = false;
            try
            {
                if (string.IsNullOrWhiteSpace(txtKeyword.Text)) { MessageBox.Show("請輸入關鍵字！"); return; }
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
            }
            finally { btnRefresh.Enabled = true; }
        }

        private async Task SearchDataAsync()
        {
            string keyword = txtKeyword.Text.Trim();
            long? lastSelectedId = _bsMaster.Current is InventoryMaster currentMaster ? currentMaster.InventoryID : null;

            int pageSize = ucPagination.PageSize;
            int currentPage = ucPagination.CurrentPage;

            try
            {
                var result = await _invService.GetInventoryOrdersAsync(currentPage, pageSize, keyword); // 實務請傳入 showVoided

                if (result.Items.Count == 0 && result.TotalCount > 0)
                {
                    int correctLastPage = (int)Math.Ceiling((double)result.TotalCount / pageSize);
                    ucPagination.ForceCurrentPage(correctLastPage);
                    result = await _invService.GetInventoryOrdersAsync(correctLastPage, pageSize, keyword);
                }

                _bsMaster.CurrentChanged -= BsMaster_CurrentChanged;

                _masterBindingList.Clear();
                _masterBindingList.AddRange(result.Items);

                if (_bsMaster.Count > 0)
                {
                    var targetMaster = _masterBindingList.FirstOrDefault(m => m.InventoryID == lastSelectedId) ?? _masterBindingList[0];
                    _bsMaster.Position = _bsMaster.IndexOf(targetMaster);

                    targetMaster = (InventoryMaster)_bsMaster.Current;
                    BindMasterUI(targetMaster);
                    await LoadDetailDataAsync(targetMaster.InventoryID);
                }
                else
                {
                    ClearMasterUI();
                }

                _bsMaster.CurrentChanged += BsMaster_CurrentChanged;
                ucPagination.BindTotalCount(result.TotalCount);
                SetUIState(_currentState);
            }
            catch (Exception ex) { MessageBox.Show($"資料載入失敗：{ex.Message}"); }
        }

        private async void BsMaster_CurrentChanged(object? sender, EventArgs e)
        {
            if (_currentState != FormState.Browse) return;

            if (_bsMaster.Current is InventoryMaster master)
            {
                BindMasterUI(master);
                await LoadDetailDataAsync(master.InventoryID);
            }
        }

        private async Task LoadDetailDataAsync(long inventoryId)
        {
            try
            {
                var details = await _invService.GetInventoryDetailsAsync(inventoryId);
                _detailBindingList.Clear();
                _detailBindingList.AddRange(details);
                RecalculateDiffAmount();
            }
            catch (Exception ex) { MessageBox.Show($"明細載入失敗：{ex.Message}"); }
        }

        private void BindMasterUI(InventoryMaster m)
        {
            if (m == null)
            {
                MessageBox.Show("系統無法取得當前操作的單據！", "狀態異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetUIState(FormState.Browse);
                return;
            }

            splitRight.Panel1.SuspendLayout();

            _selectedEmployeeID = m.EmployeeID;
            txtInventoryNo.Text = m.InventoryNo;
            dtpInventoryDate.Value = m.InventoryDate;

            txtEmployeeNo.TextChanged -= txtEmployeeNo_TextChanged;
            txtEmployeeNo.Text = m.EmployeeNo_Display;
            txtEmployeeName.Text = m.EmployeeName_Display;
            txtEmployeeNo.TextChanged += txtEmployeeNo_TextChanged;

            txtRemark.Text = m.Remark;

            // 狀態徽章渲染
            switch (m.Status)
            {
                case (byte)DocumentStatus.Draft:
                    lblStatusBadge.Text = "📝 未過帳 (草稿)";
                    lblStatusBadge.ForeColor = Color.DarkOrange;
                    break;
                case (byte)DocumentStatus.Posted:
                    lblStatusBadge.Text = "🔒 已過帳 (正式)";
                    lblStatusBadge.ForeColor = Color.Green;
                    break;
                case (byte)DocumentStatus.Cancelled:
                    lblStatusBadge.Text = "❌ 已註銷";
                    lblStatusBadge.ForeColor = Color.Gray;
                    break;
                case (byte)DocumentStatus.Voided:
                    lblStatusBadge.Text = "🚫 已作廢 (沖銷)";
                    lblStatusBadge.ForeColor = Color.Red;
                    break;
            }

            if (m.InventoryID > 0)
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
            _selectedEmployeeID = SessionContext.CurrentEmployeeID; // 預設為當前登入者
            txtInventoryNo.Text = "[儲存後自動配發]";
            dtpInventoryDate.Value = DateTime.Now;

            // 實務上這裡可以直接帶入 SessionContext 記錄的工號與名稱
            txtEmployeeNo.Clear();
            txtEmployeeName.Clear();

            txtRemark.Clear();
            _detailBindingList.Clear();
            lblTotalDiffAmount.Text = "盤盈虧總計：$0";

            lblStatusBadge.Text = "📝 新增草稿";
            lblStatusBadge.ForeColor = Color.Blue;
            lblAuditTrail.Visible = false;
        }

        // =====================================================================
        // ⚡ [盤點專屬] 系統庫存載入引擎與盲打試算
        // =====================================================================
        private async void BtnLoadSystemStock_Click(object? sender, EventArgs e)
        {
            if (_detailBindingList.Count > 0)
            {
                if (MessageBox.Show("載入系統庫存將會清空目前畫面上的所有明細，確定執行？", "確認覆寫", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }

            btnLoadSystemStock.Enabled = false;
            try
            {
                // 呼叫 ProductRepository 全撈啟用中的商品
                var result = await _prodService.GetProductsAsync(1, 0, false); // pageSize 0 = 全撈

                // 物理凍結 Grid，防止巨量資料引發閃爍
                _detailBindingList.RaiseListChangedEvents = false;
                _detailBindingList.Clear();

                foreach (var p in result.Items)
                {
                    _detailBindingList.Add(new InventoryDetail
                    {
                        ProductID = p.ProductID,
                        ProductNo_Display = p.ProductNo,
                        ProductName_Display = p.ProductName,
                        SystemStock = p.CurrentStock,   // 💡 絕對帳面快照
                        ActualStock = p.CurrentStock,   // 💡 省力盤點法：預設等同帳面，只改差異
                        StockPrice = p.MovingAverageCost    // 💡 採用進價作為成本基準
                    });
                }

                _detailBindingList.RaiseListChangedEvents = true;
                _bsDetail.ResetBindings(false); // 瞬間重繪
                RecalculateDiffAmount();
            }
            catch (Exception ex) { MessageBox.Show($"載入系統庫存失敗：{ex.Message}"); }
            finally { btnLoadSystemStock.Enabled = true; }
        }

        private void BtnClearDetails_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("確定要清空所有明細嗎？", "確認清空", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _detailBindingList.Clear();
                RecalculateDiffAmount();
            }
        }

        private void DgvInventoryDetail_DefaultValuesNeeded(object? sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells["SystemStock"].Value = 0;
            e.Row.Cells["ActualStock"].Value = 0;
            e.Row.Cells["StockPrice"].Value = 0m;
        }

        private async void DgvInventoryDetail_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _currentState == FormState.Browse) return;

            var colName = dgvInventoryDetail.Columns[e.ColumnIndex].Name;

            // 盲打查詢
            if (colName == "ProductNo")
            {
                string? inputNo = dgvInventoryDetail.Rows[e.RowIndex].Cells["ProductNo"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(inputNo))
                {
                    try
                    {
                        var product = await _prodService.GetProductByNoAsync(inputNo);
                        if (product != null)
                        {
                            dgvInventoryDetail.Rows[e.RowIndex].Cells["ProductID"].Value = product.ProductID;
                            dgvInventoryDetail.Rows[e.RowIndex].Cells["ProductName"].Value = product.ProductName;

                            // 💡 盲打帶入時，瞬間擷取系統庫存快照
                            dgvInventoryDetail.Rows[e.RowIndex].Cells["SystemStock"].Value = product.CurrentStock;
                            dgvInventoryDetail.Rows[e.RowIndex].Cells["StockPrice"].Value = product.PurchasePrice;
                        }
                        else
                        {
                            MessageBox.Show("查無此商品代碼！");
                            dgvInventoryDetail.Rows[e.RowIndex].Cells["ProductNo"].Value = string.Empty;
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("查詢商品失敗：" + ex.Message); }
                }
            }

            // 即時試算 (注意此處只監聽實盤數量的改變)
            if (colName == "ActualStock" || colName == "ProductID")
            {
                dgvInventoryDetail.EndEdit();
                RecalculateDiffAmount();
                dgvInventoryDetail.InvalidateRow(e.RowIndex);
            }
        }

        private void RecalculateDiffAmount()
        {
            // 💡 記憶體極速運算總盤差
            decimal totalDiff = _detailBindingList.Sum(d => d.DiffAmount_Display);

            lblTotalDiffAmount.Text = $"盤盈虧總計：{totalDiff:N0}";
            // 視覺防呆：盤盈綠色，盤虧紅色
            lblTotalDiffAmount.ForeColor = totalDiff >= 0 ? Color.Green : Color.Red;
        }

        private void UpdateRowHeaderNumbers()
        {
            dgvInventoryDetail.SuspendLayout();
            foreach (DataGridViewRow row in dgvInventoryDetail.Rows)
            {
                if (row.IsNewRow) continue;
                row.HeaderCell.Value = (row.Index + 1).ToString();
            }
            dgvInventoryDetail.ResumeLayout(true);
        }

        // =====================================================================
        // ⚙️ [狀態機引擎] UI 鎖定管理
        // =====================================================================
        private void SetUIState(FormState state)
        {
            _currentState = state;
            bool isEditing = (state == FormState.Add || state == FormState.Edit);
            bool isBrowse = (state == FormState.Browse);

            var master = _bsMaster.Current as InventoryMaster;
            bool isDraft = (state == FormState.Add) || (master != null && master.Status == (byte)DocumentStatus.Draft);
            bool canEditFields = isEditing && isDraft;

            txtInventoryNo.ReadOnly = true;
            txtEmployeeNo.ReadOnly = !canEditFields;
            btnLookupEmployee.Enabled = canEditFields;
            dtpInventoryDate.Enabled = canEditFields;
            txtRemark.ReadOnly = !canEditFields;

            dgvInventoryDetail.ReadOnly = !canEditFields;
            // 💡 保護帳面資料不被手殘竄改
            if (dgvInventoryDetail.Columns["SystemStock"] != null) dgvInventoryDetail.Columns["SystemStock"].ReadOnly = true;
            if (dgvInventoryDetail.Columns["StockPrice"] != null) dgvInventoryDetail.Columns["StockPrice"].ReadOnly = true;

            dgvInventoryDetail.AllowUserToAddRows = canEditFields;
            dgvInventoryDetail.AllowUserToDeleteRows = canEditFields;

            btnLoadSystemStock.Enabled = canEditFields;
            btnClearDetails.Enabled = canEditFields;

            dgvInventoryMaster.Enabled = !isEditing;
            pnlSearch.Enabled = !isEditing;

            btnAdd.Enabled = !isEditing;
            btnEdit.Enabled = !isEditing && master != null && isDraft;

            // 💡 [特例] 盤點單專屬的硬刪除按鈕 (取代作廢)
            btnDelete.Enabled = !isEditing && master != null && isDraft;

            btnSave.Enabled = isEditing;
            btnCancel.Enabled = isEditing;
            btnRefresh.Enabled = !isEditing;

            btnPost.Enabled = isBrowse && master != null && master.Status == (byte)DocumentStatus.Draft;

            if (state == FormState.Add)
            {
                ClearMasterUI();
                txtEmployeeNo.Focus();
            }

            ucPagination.SetUIState(isBrowse);
        }

        // =====================================================================
        // 💾 [交易引擎] 存檔、硬刪除與過帳
        // =====================================================================
        private void BtnAdd_Click(object? sender, EventArgs e) => SetUIState(FormState.Add);
        private void BtnEdit_Click(object? sender, EventArgs e) => SetUIState(FormState.Edit);

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            SetUIState(FormState.Browse);
            if (_bsMaster.Current is InventoryMaster master)
            {
                BindMasterUI(master);
                _ = LoadDetailDataAsync(master.InventoryID);
            }
            else { ClearMasterUI(); }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            dgvInventoryDetail.EndEdit();
            _bsDetail.EndEdit();

            if (string.IsNullOrWhiteSpace(txtEmployeeNo.Text) || _selectedEmployeeID <= 0)
            {
                MessageBox.Show("請輸入並確認有效的盤點人員代碼！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var validDetails = _detailBindingList.Where(d => d.ProductID > 0).ToList();
            if (validDetails.Count < 0)
            {
                MessageBox.Show("盤點單至少需要一筆有效的明細 (數量不可為 0)！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSave.Enabled = false;
            btnCancel.Enabled = false;

            var master = _currentState == FormState.Add ? new InventoryMaster() : (_bsMaster.Current as InventoryMaster);
            if (master == null) return;

            master.InventoryDate = dtpInventoryDate.Value;
            master.EmployeeID = _selectedEmployeeID;
            master.Remark = string.IsNullOrWhiteSpace(txtRemark.Text) ? null : txtRemark.Text.Trim();

            for (int i = 0; i < validDetails.Count; i++) validDetails[i].LineNo = i + 1;

            bool success = await SafeExecuteAsync(async () =>
            {
                if (_currentState == FormState.Add)
                {
                    //master.CreateUser = SessionContext.CurrentAccountID;
                    master = await _invService.CreateInventoryOrderAsync(master, validDetails, SessionContext.CurrentAccountID);
                }
                else if (_currentState == FormState.Edit)
                {
                    master.RowVersion = await _invService.UpdateInventoryOrderDraftAsync(master, validDetails, SessionContext.CurrentAccountID);
                }
            },
            reloadDataAction: async () => await SearchDataAsync());

            if (success)
            {
                string actionName = _currentState == FormState.Add ? "新增" : "更新";
                MessageBox.Show($"{actionName}成功！單號：{master.InventoryNo}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (_currentState == FormState.Add) ucPagination.ResetToFirstPage();
                txtKeyword.Clear();
                SetUIState(FormState.Browse);
                await SearchDataAsync();
                _bsMaster.LocateTo<InventoryMaster>(m => m.InventoryNo == master.InventoryNo);
            }

            if (_currentState == FormState.Add || _currentState == FormState.Edit)
            {
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
            }
        }

        // 💡 盤點專屬：實體物理刪除 (Hard Delete)
        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            var master = _bsMaster.Current as InventoryMaster;
            if (master == null) return;

            if (MessageBox.Show($"確定要【徹底刪除】盤點草稿 [{master.InventoryNo}] 嗎？\n資料庫將物理抹除所有明細，此操作無法還原！",
                "危險警告", MessageBoxButtons.YesNo, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                bool success = await SafeExecuteAsync(
                    async () => await _invService.DeleteDraftAsync(master.InventoryID, master.RowVersion, master.Status),
                    reloadDataAction: async () => await SearchDataAsync()
                );

                if (success)
                {
                    MessageBox.Show("草稿已徹底刪除。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await SearchDataAsync(); // 畫面自動遞補下一筆
                }
            }
        }

        private async void BtnPost_Click(object? sender, EventArgs e)
        {
            var master = _bsMaster.Current as InventoryMaster;
            if (master == null) return;

            if (MessageBox.Show($"確定要將盤點單 [{master.InventoryNo}] 執行【審核過帳】嗎？\n系統將依照差異數量自動調整庫存，此操作不可逆轉！",
                "確認過帳", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                bool success = await SafeExecuteAsync(async () =>
                {
                    master.RowVersion = await _invService.ApproveOrderAsync(
                        master.InventoryID,
                        master.RowVersion,
                        master.Status,
                        SessionContext.CurrentAccountID);
                },
                reloadDataAction: async () => await SearchDataAsync());

                if (success)
                {
                    MessageBox.Show("盤點差異已成功過帳入庫！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await SearchDataAsync();
                    _bsMaster.LocateTo<InventoryMaster>(m => m.InventoryNo == master.InventoryNo);
                }
            }
        }
    }
}