using ERPLAB.BLL.Services;
using ERPLAB.Models.Entities;
using ERPLAB.UI.Core;
// using Microsoft.Data.SqlClient;

namespace ERPLAB.UI.Views.BaseData
{
    /// <summary>
    /// 商品基本檔維護頁面 (List-Detail Pattern)。
    /// 繼承自 BasePage，負責處理商品資料之 CRUD 操作。
    /// 實作狀態機 UI 控制、樂觀鎖 (Optimistic Concurrency) 防禦機制，以及機敏成本欄位之存取權限控管。
    /// </summary>
    public partial class ProductPage : BasePage
    {
        // =====================================================================
        // 服務注入與全域狀態宣告
        // =====================================================================
        private readonly ProductService _prodService;

        private BindingSource _bsProducts;
        private ExtendedBindingList<Product> _productBindingList;

        // 表單狀態列舉，用於控制 UI 互動模式與唯讀限制
        private FormState _currentState = FormState.Browse;

        // 記憶體實體快取：保留 RowVersion 供存檔時進行併發比對
        //private Product _currentProduct;

        public ProductPage()
        {
            InitializeComponent();

            // 套用擴充方法開啟雙重緩衝，改善 DataGridView 渲染效能與滾動卡頓
            dgvProducts.EnableDoubleBuffering(true);

            _prodService = new ProductService();

            // 初始化資料綁定來源
            _bsProducts = new BindingSource();
            _productBindingList = new ExtendedBindingList<Product>();

            // 統一於建構子掛載生命週期與控制項事件，確保執行順序
            this.Load += ProductPage_Load;

            // 透過 BindingSource 之 CurrentChanged 事件監聽焦點轉移
            _bsProducts.CurrentChanged += BsProducts_CurrentChanged;

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
            btnToggleStatus.Click += btnToggleStatus_Click;

            txtKeyword.KeyDown += TxtKeyword_KeyDown;

            // 綁定過濾條件切換事件：變更時立即重新載入資料
            chkShowInactive.CheckedChanged += async (s, e) => await SearchDataAsync();

            // 綁定 Grid 繪圖事件：處理下架/停用資料之視覺提示
            dgvProducts.CellFormatting += DgvProducts_CellFormatting;

            // 訂閱分頁控制項事件，觸發資料重新查詢
            ucPagination.PageChanged += async (s, e) => await SearchDataAsync();
        }

        private async void ProductPage_Load(object? sender, EventArgs e)
        {
            // =====================================================================
            // 權限檢核 (RBAC)：依據使用者授權動態顯示操作按鈕與機敏資訊
            // 呼叫 BasePage 提供之 RequirePermission 進行控制項的實體隱藏
            // =====================================================================
            RequirePermission("ACT_PROD_ADD", btnAdd);
            RequirePermission("ACT_PROD_EDIT", btnEdit);
            RequirePermission("ACT_PROD_EDIT", btnToggleStatus);
            RequirePermission("ACT_PROD_VIEW_COST", txtMovingAverageCost);
            RequirePermission("ACT_PROD_VIEW_COST", lblMovingAverageCost);

            // 根據新增或修改之授權，決定儲存與取消按鈕之可見性
            bool canWrite = SessionContext.HasPermission("ACT_PROD_ADD") || SessionContext.HasPermission("ACT_PROD_EDIT");
            btnSave.Visible = canWrite;
            btnCancel.Visible = canWrite;

            SetupGridColumns();
            _bsProducts.DataSource = _productBindingList;
            dgvProducts.DataSource = _bsProducts;

            await SearchDataAsync();
            SetUIState(FormState.Browse);
        }

        private void SetupGridColumns()
        {
            dgvProducts.AutoGenerateColumns = false;
            if (dgvProducts.Columns.Count == 0)
            {
                dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductNo", HeaderText = "商品編號", Width = 140 });
                dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "商品名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
                dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PurchasePrice", HeaderText = "參考進貨單價", Width = 100 });
                dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CurrentStock", HeaderText = "帳面庫存量", Width = 120 });
            }
        }

        // =====================================================================
        // 搜尋功能支援 (Enter 鍵觸發)
        // =====================================================================
        private async void TxtKeyword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true; // 消除系統預設之警告音效
                await SearchDataAsync();
            }
        }

        // =====================================================================
        // 資料查詢與綁定作業
        // =====================================================================
        private async Task SearchDataAsync()
        {
            string keyword = txtKeyword.Text.Trim();
            bool includeInactive = chkShowInactive.Checked;
            int? lastSelectedId = _bsProducts.Current is Product currentProduct ? currentProduct.ProductID : null;

            // 取回當前分頁參數
            int pageSize = ucPagination.PageSize;
            int currentPage = ucPagination.CurrentPage;

            try
            {
                var result = await _prodService.GetProductsAsync(currentPage, pageSize, includeInactive, keyword);

                // =====================================================================
                // 若當前頁碼因資料刪除等原因導致越界，重新計算最後頁碼並再次執行查詢
                // =====================================================================
                if (result.Items.Count == 0 && result.TotalCount > 0)
                {
                    // 重算正確之最後一頁
                    int correctLastPage = (int)Math.Ceiling((double)result.TotalCount / pageSize);
                    ucPagination.ForceCurrentPage(correctLastPage);

                    // 修正頁碼後重新查詢，避免遞迴呼叫
                    result = await _prodService.GetProductsAsync(correctLastPage, pageSize, includeInactive, keyword);
                }
                _bsProducts.CurrentChanged -= BsProducts_CurrentChanged;

                // 透過 ExtendedBindingList 之 AddRange 進行批次更新，維持 DataSource 結構不被破壞
                _productBindingList.Clear();
                _productBindingList.AddRange(result.Items);

                // 處理明細資料連動與游標定位
                if (_bsProducts.Count > 0)
                {
                    // 1. 於底層資料集合中尋找目標實體
                    var targetProduct = _productBindingList.FirstOrDefault(c => c.ProductID == lastSelectedId);

                    // 2. 取得目標實體於 BindingSource 中的索引值 (若無則預設指向首筆)
                    int targetIndex = targetProduct != null ? _bsProducts.IndexOf(targetProduct) : 0;

                    // 3. 更新 BindingSource 游標，自動連動 UI 焦點
                    _bsProducts.Position = targetIndex;

                    targetProduct = (Product)_bsProducts.Current!;

                    // 因已暫時解除 CurrentChanged 事件，需手動執行明細綁定
                    BindDetail(targetProduct);
                }
                else
                {
                    ClearDetail();
                    SetUIState(_currentState);
                }

                _bsProducts.CurrentChanged += BsProducts_CurrentChanged;

                // 更新分頁控制項狀態
                ucPagination.BindTotalCount(result.TotalCount);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"資料載入失敗：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 監聽 BindingSource，確保實體游標一致性
        private void BsProducts_CurrentChanged(object? sender, EventArgs e)
        {
            if (_currentState != FormState.Browse) return;

            if (_bsProducts.Current is Product current)
            {
                BindDetail(current);
            }
        }

        private void BindDetail(Product currentProduct)
        {
            if (currentProduct == null)
            {
                MessageBox.Show("系統無法取得當前操作的資料！這可能是資料已被其他使用者刪除，請重新操作。", "狀態異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetUIState(FormState.Browse);
                return;
            }
            splitContainerMain.Panel2.SuspendLayout();

            txtProductNo.Text = currentProduct.ProductNo;
            txtProductName.Text = currentProduct.ProductName;
            txtPurchasePrice.Text = currentProduct.PurchasePrice.ToString("N2");
            txtSalesPrice.Text = currentProduct.SalesPrice.ToString("N2");
            txtCurrentStock.Text = currentProduct.CurrentStock.ToString("N0"); ;
            txtDescription.Text = currentProduct.Description;
            txtRemark.Text = currentProduct.Remark;

            // 判斷權限以決定是否顯示移動平均成本，無權限則以星號遮蔽
            txtMovingAverageCost.Text = SessionContext.HasPermission("ACT_PROD_VIEW_COST") ? currentProduct.MovingAverageCost.ToString("N4") : "***";

            // =====================================================================
            // 狀態徽章 (Status Badge) 與按鈕文字之動態渲染
            // =====================================================================
            if (currentProduct.IsActive)
            {
                // 渲染徽章
                lblStatusBadge.Text = "✅ 狀態：已上架 (啟用)";
                lblStatusBadge.ForeColor = System.Drawing.Color.Green;

                btnToggleStatus.Text = "🚫 下架/停用";
                btnToggleStatus.ForeColor = System.Drawing.Color.Red;
            }
            else
            {
                // 渲染徽章
                lblStatusBadge.Text = "🚫 狀態：已下架 (停用)";
                lblStatusBadge.ForeColor = System.Drawing.Color.Red;

                btnToggleStatus.Text = "✅ 上架/啟用";
                btnToggleStatus.ForeColor = System.Drawing.Color.Green;
            }

            // 稽核軌跡 (Audit Trail) 資訊顯示
            if (currentProduct.ProductID > 0)
            {
                string creatorNo = currentProduct.CreateUserNo_Display ?? "未知";
                string updaterNo = currentProduct.UpdateUserNo_Display ?? "未知";

                lblAuditTrail.Text = $"建檔：{creatorNo} ({currentProduct.CreateTime:yyyy/MM/dd HH:mm}) ｜ " +
                                     $"最後異動：{updaterNo} ({currentProduct.UpdateTime:yyyy/MM/dd HH:mm})";
                lblAuditTrail.Visible = true;
            }
            else
            {
                // 新增模式時隱藏稽核區塊
                lblAuditTrail.Visible = false;
            }

            // 重新觸發狀態機評估
            SetUIState(_currentState);

            splitContainerMain.Panel2.ResumeLayout(true);
        }

        private void ClearDetail()
        {
            txtProductNo.Text = "[儲存後自動配發]";
            txtProductName.Clear();
            txtPurchasePrice.Clear();
            txtSalesPrice.Clear();
            txtCurrentStock.Clear();
            txtDescription.Clear();
            txtRemark.Clear();

            // 新增模式時，預設狀態顯示
            lblStatusBadge.Text = "✅ 狀態：上架/啟用 (新資料)";
            lblStatusBadge.ForeColor = System.Drawing.Color.Green;

            btnToggleStatus.Text = "狀態操作";
            btnToggleStatus.ForeColor = System.Drawing.Color.Black;
        }

        // =====================================================================
        // 資料列視覺樣式自訂 (Visual Distinction)
        // =====================================================================
        private void DgvProducts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // 確保索引在合法範圍內
            if (e.RowIndex >= 0 && e.RowIndex < dgvProducts.Rows.Count)
            {
                // 取得該列綁定的實體物件
                var product = dgvProducts.Rows[e.RowIndex].DataBoundItem as Product;

                // 下架/停用資料之視覺提示：套用刪除線與灰階色彩
                if (product != null && !product.IsActive && e.CellStyle != null)
                {
                    e.CellStyle.ForeColor = System.Drawing.Color.DarkGray;
                    e.CellStyle.Font = new System.Drawing.Font(dgvProducts.Font, System.Drawing.FontStyle.Strikeout);
                }
            }
        }

        // =====================================================================
        // 狀態機 (State Machine) 控制邏輯
        // 依據當前操作模式動態切換控制項之啟用與唯讀屬性
        // =====================================================================
        private void SetUIState(FormState state)
        {
            _currentState = state;
            bool isEditing = (state == FormState.Add || state == FormState.Edit);
            bool isBrowse = (state == FormState.Browse);
            var currentProduct = _bsProducts.Current as Product;

            // 右側明細區狀態切換
            txtProductNo.ReadOnly = true; // 自動取號保持唯讀
            txtCurrentStock.ReadOnly = true; // 帳面庫存保持唯讀

            // 依據編輯狀態切換輸入欄位
            txtProductName.ReadOnly = !isEditing;
            txtPurchasePrice.ReadOnly = !isEditing;
            txtSalesPrice.ReadOnly = !isEditing;
            txtDescription.ReadOnly = !isEditing;
            txtRemark.ReadOnly = !isEditing;

            // 搜尋區控制項限制
            txtKeyword.Enabled = !isEditing;
            btnSearch.Enabled = !isEditing;
            chkShowInactive.Enabled = !isEditing;

            // 左側清單防呆：編輯時禁止切換資料
            dgvProducts.Enabled = !isEditing;

            // 工具列按鈕狀態切換
            btnAdd.Enabled = !isEditing;
            btnEdit.Enabled = !isEditing && currentProduct != null && currentProduct.ProductID > 0 && currentProduct.IsActive == true;
            btnSave.Enabled = isEditing;
            btnCancel.Enabled = isEditing;
            btnToggleStatus.Enabled = !isEditing && currentProduct != null && currentProduct.ProductID > 0;
            btnRefresh.Enabled = !isEditing;

            if (state == FormState.Add)
            {
                ClearDetail();
                txtProductName.Focus();
            }

            // 分頁控制項狀態切換
            ucPagination.SetUIState(state == FormState.Browse);
        }

        // =====================================================================
        // 資料異動作業 (新增、修改、狀態切換與樂觀鎖處理)
        // =====================================================================
        private void BtnAdd_Click(object? sender, EventArgs e) => SetUIState(FormState.Add);
        private void BtnEdit_Click(object? sender, EventArgs e) => SetUIState(FormState.Edit);

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            SetUIState(FormState.Browse);
            // 放棄修改，還原為 DataGridView 中當前選取之實體資料
            if (dgvProducts.SelectedRows.Count > 0 &&
                    dgvProducts.SelectedRows[0].DataBoundItem is Product product)
            {
                BindDetail(product);
            }
            else { ClearDetail(); }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var currentProduct = _currentState == FormState.Add ? new Product() : (_bsProducts.Current as Product);

            // 前端基礎必填與邏輯檢核
            if (string.IsNullOrWhiteSpace(txtProductName.Text)
                || string.IsNullOrWhiteSpace(txtPurchasePrice.Text)
                || string.IsNullOrWhiteSpace(txtSalesPrice.Text))
            {
                MessageBox.Show("藍色欄位為必填欄位！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureValid(SystemValidator.ValidatePrice(txtPurchasePrice.Text, lblPurchasePrice.Text), txtPurchasePrice))
                return;
            if (!EnsureValid(SystemValidator.ValidatePrice(txtSalesPrice.Text, lblSalesPrice.Text), txtSalesPrice))
                return;

            if (currentProduct == null)
            {
                MessageBox.Show("系統無法取得當前操作的資料！這可能是資料已被其他使用者刪除，請重新操作。", "狀態異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetUIState(FormState.Browse);
                return;
            }

            // =====================================================================
            // 鎖定操作按鈕，防止非同步處理期間發生重複送出 (Double Click)
            // =====================================================================
            btnSave.Enabled = false;
            btnCancel.Enabled = false;

            // 將 UI 畫面資料對映回記憶體實體 (DTO Mapping)
            currentProduct.ProductName = txtProductName.Text.Trim();
            currentProduct.PurchasePrice = decimal.Parse(txtPurchasePrice.Text.Trim());
            currentProduct.SalesPrice = decimal.Parse(txtSalesPrice.Text.Trim());
            currentProduct.CurrentStock = 0;
            currentProduct.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
            currentProduct.Remark = string.IsNullOrWhiteSpace(txtRemark.Text) ? null : txtRemark.Text.Trim();

            // 寫入系統稽核參數 (操作者 ID)
            // currentProduct.UpdateUser = SessionContext.CurrentAccountID;

            bool success = await SafeExecuteAsync(async () =>
            {
                if (_currentState == FormState.Add)
                {
                    currentProduct = await _prodService.CreateProductAsync(currentProduct, SessionContext.CurrentAccountID); // 包含 INSERTED.ProductID 與 RowVersion 的回傳
                }
                else if (_currentState == FormState.Edit)
                {
                    // 發動樂觀鎖更新
                    byte[] newRowVersion = await _prodService.UpdateProductAsync(currentProduct, SessionContext.CurrentAccountID);
                }
            },
            reloadDataAction: async () => await SearchDataAsync());

            if (success)
            {
                string actionName = _currentState == FormState.Add ? "新增" : "更新";
                MessageBox.Show($"{actionName}成功！", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (_currentState == FormState.Add) ucPagination.ResetToFirstPage();
                SetUIState(FormState.Browse);
                txtKeyword.Clear();
                await SearchDataAsync();

                // 定位至剛異動完的實體
                _bsProducts.LocateTo<Product>(c => c.ProductNo == currentProduct.ProductNo);
            }

            // 若因檢核未過或 SQL 例外導致存檔失敗，強制解鎖按鈕以利使用者修正後重試
            if (_currentState == FormState.Add || _currentState == FormState.Edit)
            {
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
            }
        }

        private async void btnToggleStatus_Click(object? sender, EventArgs e)
        {
            var currentProduct = _bsProducts.Current as Product;
            if (currentProduct == null || currentProduct.ProductID == 0)
            {
                MessageBox.Show("系統無法取得當前操作的資料！這可能是資料已被其他使用者刪除，請重新操作。", "狀態異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetUIState(FormState.Browse);
                return;
            }

            string actionName = currentProduct.IsActive ? "下架/停用" : "上架/啟用";

            if (MessageBox.Show($"確定要對 [{currentProduct.ProductName}] 執行 {actionName} 嗎？",
                "狀態變更確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bool success = await SafeExecuteAsync(async () =>
                {
                    byte[] newRowVersion = await _prodService.UpdateProductStatusAsync(
                        currentProduct.ProductID,
                        currentProduct.IsActive,
                        currentProduct.RowVersion,
                        SessionContext.CurrentAccountID);
                },
                reloadDataAction: async () => await SearchDataAsync());

                if (success)
                {
                    MessageBox.Show($"已成功 {actionName}。", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await SearchDataAsync();
                }
            }
        }

        private async void btnSearch_Click(object? sender, EventArgs e)
        {
            // 鎖定按鈕避免連點產生併發請求
            btnSearch.Enabled = false;
            if (string.IsNullOrWhiteSpace(txtKeyword.Text))
            {
                MessageBox.Show("請輸入有效的關鍵字！");
                btnSearch.Enabled = true;
                return;
            }
            try
            {
                ucPagination.ResetToFirstPage(); // 執行新查詢時重置為第一頁
                await SearchDataAsync();
            }
            finally
            {
                btnSearch.Enabled = true;
            }
        }

        // =====================================================================
        // 畫面重置與資料更新
        // =====================================================================
        private async void btnRefresh_Click(object? sender, EventArgs e)
        {
            // 鎖定按鈕避免連點
            btnRefresh.Enabled = false;

            try
            {
                txtKeyword.Clear();
                ucPagination.ResetToFirstPage(); // 重整時重置為第一頁

                // 重新載入無過濾條件之資料
                await SearchDataAsync();
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }
    }
}