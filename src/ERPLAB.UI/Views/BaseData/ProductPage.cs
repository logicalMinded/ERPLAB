using ERPLAB.BLL.Services;
using ERPLAB.Models.Entities;
using ERPLAB.UI.Core;
// using Microsoft.Data.SqlClient;

namespace ERPLAB.UI.Views.BaseData
{
    /// <summary>
    /// 廠商基本檔維護模組 (List-Detail 模式)。
    /// 核心展示：地理資料快取連動、3+3 郵遞區號虛擬化、狀態機鎖定與樂觀鎖 (Optimistic Locking) 防禦。
    /// </summary>
    public partial class ProductPage : BasePage
    {
        // =====================================================================
        // 💡 倉儲與全域狀態快取
        // =====================================================================
        private readonly ProductService _prodService;

        private BindingSource _bsProducts;
        private ExtendedBindingList<Product> _productBindingList;

        // 狀態機定義：精確控制畫面行為與防呆
        private FormState _currentState = FormState.Browse;

        // 記憶體實體快取：保留 RowVersion 供存檔時進行併發比對
        //private Product _currentProduct;

        public ProductPage()
        {
            InitializeComponent();
            // 💡 物理優化：強制開啟 Grid 的雙重緩衝，解決捲動與載入時的渲染延遲
            dgvProducts.EnableDoubleBuffering(true);

            _prodService = new ProductService();
            // 💡 初始化 BindingSource
            _bsProducts = new BindingSource();
            _productBindingList = new ExtendedBindingList<Product>();

            // 💡 綁定生命週期與按鈕事件 (統一於建構子掛載，確保執行順序)
            this.Load += ProductPage_Load;
            // 💡 用 BindingSource 的 CurrentChanged 來監聽焦點轉移
            _bsProducts.CurrentChanged += BsProducts_CurrentChanged;

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
            btnToggleStatus.Click += btnToggleStatus_Click;
            // 💡 綁定搜尋事件
            txtKeyword.KeyDown += TxtKeyword_KeyDown;
            // 💡 綁定過濾條件切換事件：打勾或取消時，立刻重新撈取資料
            chkShowInactive.CheckedChanged += async (s, e) => await SearchDataAsync();
            // 💡 綁定 Grid 繪圖事件：用來處理停用資料的視覺特效
            dgvProducts.CellFormatting += DgvProducts_CellFormatting;
            // 💡 訂閱分頁器的廣播：當有人翻頁，我就去撈資料
            ucPagination.PageChanged += async (s, e) => await SearchDataAsync();
        }

        private async void ProductPage_Load(object? sender, EventArgs e)
        {
            // =====================================================================
            // 🛡️ [防禦引擎 A] UI 啟動瞬間發動 RBAC 物理斷路
            // 向父類別 BasePage 註冊機敏按鈕，若 SessionContext 無權限，按鈕將物理消失
            // =====================================================================
            RequirePermission("ACT_PROD_ADD", btnAdd);
            RequirePermission("ACT_PROD_EDIT", btnEdit);
            RequirePermission("ACT_PROD_EDIT", btnToggleStatus);
            RequirePermission("ACT_PROD_VIEW_COST", txtMovingAverageCost);
            RequirePermission("ACT_PROD_VIEW_COST", lblMovingAverageCost);

            // 💡 綜合判定「存檔」與「取消」的物理可見性
            // 只要具備「新增」或「修改」任何一項權限，就讓文境按鈕顯示在畫面上，否則徹底隱藏
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
        // 支援 Enter 鍵檢索
        // =====================================================================
        private async void TxtKeyword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true; // 消除按下 Enter 時惱人的 Windows "叮" 警告音
                await SearchDataAsync();
            }
        }

        // =====================================================================
        // 🔄 [資料流引擎] 響應式綁定機制
        // =====================================================================
        private async Task SearchDataAsync()
        {
            string keyword = txtKeyword.Text.Trim();
            bool includeInactive = chkShowInactive.Checked;
            int? lastSelectedId = _bsProducts.Current is Product currentProduct ? currentProduct.ProductID : null;
            //  直接向分頁列要參數
            int pageSize = ucPagination.PageSize;
            int currentPage = ucPagination.CurrentPage;

            try
            {
                var result = await _prodService.GetProductsAsync(currentPage, pageSize, includeInactive, keyword);

                // =====================================================================
                // 在原地重新計算頁碼並單獨重撈一次資料，保持執行流的絕對平整。
                // =====================================================================
                if (result.Items.Count == 0 && result.TotalCount > 0)
                {
                    // 計算正確的最後一頁
                    int correctLastPage = (int)Math.Ceiling((double)result.TotalCount / pageSize);
                    ucPagination.ForceCurrentPage(correctLastPage);

                    // 💡 物理防線：直接再打一次資料庫，不呼叫自己 (零遞迴)
                    result = await _prodService.GetProductsAsync(correctLastPage, pageSize, includeInactive, keyword);
                }
                _bsProducts.CurrentChanged -= BsProducts_CurrentChanged; // 防觸發

                // 💡 透過 AddRange 極速批次更新綁定清單，不再破壞 DataSource 結構
                _productBindingList.Clear();
                _productBindingList.AddRange(result.Items);

                // 若無資料，清空明細
                if (_bsProducts.Count > 0)
                {
                    // 1. 從「底層資料」尋找目標物件，而非走訪「UI 列」
                    var targetProduct = _productBindingList.FirstOrDefault(c => c.ProductID == lastSelectedId);

                    // 2. 取得該物件在 BindingSource 中的索引值（若找不到則退回第 0 筆）
                    int targetIndex = targetProduct != null ? _bsProducts.IndexOf(targetProduct) : 0;

                    // 3. 直接改變 BindingSource 的資料游標，UI (DataGridView) 會自動連動反白與轉移焦點
                    _bsProducts.Position = targetIndex;

                    targetProduct = (Product)_bsProducts.Current;

                    // 己解綁 _bsProducts.CurrentChanged 己解綁，需手動更新明細
                    BindDetail(targetProduct);
                }
                else
                {
                    ClearDetail();
                    SetUIState(_currentState);
                }

                _bsProducts.CurrentChanged += BsProducts_CurrentChanged; // 防觸發後重新綁定

                // 總筆數交給分頁列，自動計算總分頁並設定為預設狀態
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

            txtMovingAverageCost.Text = SessionContext.HasPermission("ACT_PROD_VIEW_COST") ? currentProduct.MovingAverageCost.ToString("N4") : "***";

            // =====================================================================
            // 💡 [視覺引擎] 狀態徽章 (Status Badge) 動態渲染
            // =====================================================================
            if (currentProduct.IsActive)
            {
                // 渲染徽章
                lblStatusBadge.Text = "✅ 狀態：已上架 (啟用)";
                lblStatusBadge.ForeColor = System.Drawing.Color.Green;

                // 渲染工具列快捷按鈕 (先前的邏輯)
                btnToggleStatus.Text = "🚫 下架/停用";
                btnToggleStatus.ForeColor = System.Drawing.Color.Red;
            }
            else
            {
                // 渲染徽章
                lblStatusBadge.Text = "🚫 狀態：已下架 (停用)";
                lblStatusBadge.ForeColor = System.Drawing.Color.Red;

                // 渲染工具列快捷按鈕 (先前的邏輯)
                btnToggleStatus.Text = "✅ 上架/啟用";
                btnToggleStatus.ForeColor = System.Drawing.Color.Green;
            }

            // 💡 [審計軌跡渲染] 組合 4 個欄位，提供安靜且透明的內控資訊
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
                // 新增模式時隱藏
                lblAuditTrail.Visible = false;
            }

            // 💡 重新觸發狀態機評估
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
            // 新增模式時，預設顯示為正常交易
            lblStatusBadge.Text = "✅ 狀態：上架/啟用 (新資料)";
            lblStatusBadge.ForeColor = System.Drawing.Color.Green;

            btnToggleStatus.Text = "狀態操作";
            btnToggleStatus.ForeColor = System.Drawing.Color.Black;
        }
        // =====================================================================
        // 👁️ [視覺引擎] 停用資料的物理識別 (Visual Distinction)
        // =====================================================================
        private void DgvProducts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // 確保索引在合法範圍內
            if (e.RowIndex >= 0 && e.RowIndex < dgvProducts.Rows.Count)
            {
                // 取得該列綁定的實體物件
                var product = dgvProducts.Rows[e.RowIndex].DataBoundItem as Product;

                // 若該廠商已被停用 (IsActive == false)
                if (product != null && !product.IsActive && e.CellStyle != null)
                {
                    // 💡 字體顏色改為深灰色
                    e.CellStyle.ForeColor = System.Drawing.Color.DarkGray;

                    // 💡 加上物理刪除線 (Strikeout)，產生強烈的視覺斷層，警告使用者此為無效資料
                    e.CellStyle.Font = new System.Drawing.Font(dgvProducts.Font, System.Drawing.FontStyle.Strikeout);
                }
            }
        }

        // =====================================================================
        // ⚙️ [狀態機引擎] UI 動態鎖定與解鎖管理
        // =====================================================================
        private void SetUIState(FormState state)
        {
            _currentState = state;
            bool isEditing = (state == FormState.Add || state == FormState.Edit);
            bool isBrowse = (state == FormState.Browse);
            var currentProduct = _bsProducts.Current as Product;

            // 右側明細區解鎖/鎖定
            // 保持唯讀
            txtProductNo.ReadOnly = true;
            txtCurrentStock.ReadOnly = true;

            // 其餘欄位依據 isEditing 切換
            txtProductName.ReadOnly = !isEditing;
            txtPurchasePrice.ReadOnly = !isEditing;
            txtSalesPrice.ReadOnly = !isEditing;
            txtDescription.ReadOnly = !isEditing;
            txtRemark.ReadOnly = !isEditing;

            // 搜尋區控制項
            txtKeyword.Enabled = !isEditing;
            btnSearch.Enabled = !isEditing;
            chkShowInactive.Enabled = !isEditing;

            // 左側清單防呆 (編輯時禁止切換資料)
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
            // 分頁列狀態切換
            ucPagination.SetUIState(state == FormState.Browse);
        }

        // =====================================================================
        // 💾 [交易引擎] 增刪改、資料合併與樂觀鎖防禦
        // =====================================================================
        private void BtnAdd_Click(object? sender, EventArgs e) => SetUIState(FormState.Add);
        private void BtnEdit_Click(object? sender, EventArgs e) => SetUIState(FormState.Edit);

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            SetUIState(FormState.Browse);
            // 放棄修改，將畫面復原為 Grid 中當前選取的實體狀態
            if (dgvProducts.SelectedRows.Count > 0)
                BindDetail((Product)dgvProducts.SelectedRows[0].DataBoundItem);
            else { ClearDetail(); }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var currentProduct = _currentState == FormState.Add ? new Product() : (_bsProducts.Current as Product);

            // 1. 前端物理防呆、長度與邏輯攔截
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
            // 2 物理防連點 (驗證通過了，準備開始漫長的存檔，這時才把按鈕鎖死)
            // =====================================================================
            btnSave.Enabled = false;
            btnCancel.Enabled = false;

            // 3. 將 UI 畫面資料推回記憶體實體 (DTO Mapping)
            currentProduct.ProductName = txtProductName.Text.Trim();
            currentProduct.PurchasePrice = decimal.Parse(txtPurchasePrice.Text.Trim());
            currentProduct.SalesPrice = decimal.Parse(txtSalesPrice.Text.Trim());
            currentProduct.CurrentStock = 0;
            currentProduct.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
            currentProduct.Remark = string.IsNullOrWhiteSpace(txtRemark.Text) ? null : txtRemark.Text.Trim();

            // 寫入 ERP 應用層審計 (取出目前登入者的 EmployeeID)
            // currentProduct.UpdateUser = SessionContext.CurrentAccountID;

            bool success = await SafeExecuteAsync(async () =>
            {
                if (_currentState == FormState.Add)
                {
                    currentProduct = await _prodService.CreateProductAsync(currentProduct, SessionContext.CurrentAccountID); // 包含 INSERTED.ProductID 與 RowVersion 的回傳
                }
                else if (_currentState == FormState.Edit)
                {
                    // 🚨 發動樂觀鎖更新
                    byte[] newRowVersion = await _prodService.UpdateProductAsync(currentProduct, SessionContext.CurrentAccountID);
                }
            },
            reloadDataAction: async () => await SearchDataAsync());

            if (success)
            {
                string actionName = _currentState == FormState.Add ? "新增" : "更新";
                MessageBox.Show($"{actionName}成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (_currentState == FormState.Add) ucPagination.ResetToFirstPage();
                SetUIState(FormState.Browse);
                txtKeyword.Clear();
                await SearchDataAsync();
                _bsProducts.LocateTo<Product>(c => c.ProductNo == currentProduct.ProductNo);
            }

            // 按鈕已為了保險關閉，確保離開時，若存檔失敗 (如檢核未過、SQL 例外)
            // ，狀態仍停留在 Add/Edit，則在此強制解鎖按鈕，確保使用者能修改資料後再次重試。
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
                    MessageBox.Show($"已成功 {actionName}。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await SearchDataAsync();
                }
            }
        }

        private async void btnSearch_Click(object? sender, EventArgs e)
        {
            // 物理防呆：鎖定按鈕避免連點
            btnSearch.Enabled = false;
            if (string.IsNullOrWhiteSpace(txtKeyword.Text))
            {
                MessageBox.Show("請輸入有效的關鍵字！");
                // 解鎖按鈕
                btnSearch.Enabled = true;
                return;
            }
            try
            {
                ucPagination.ResetToFirstPage(); // 搜尋必須回到第一頁
                await SearchDataAsync();
            }
            finally
            {
                // 解鎖按鈕
                btnSearch.Enabled = true;
            }
        }

        // =====================================================================
        // 🔄 [重整引擎] 恢復初始狀態
        // 核心職責：清空關鍵字與 CheckBox，並防止事件連鎖觸發引發的 I/O 浪費
        // =====================================================================
        private async void btnRefresh_Click(object? sender, EventArgs e)
        {
            // 物理防呆：鎖定按鈕避免連點引發併發查詢
            btnRefresh.Enabled = false;

            try
            {
                // 1清空文字框
                txtKeyword.Clear();
                ucPagination.ResetToFirstPage(); // 重整必須回到第一頁
                // 呼叫核心資料流引擎，向資料庫要求最新、無過濾的乾淨資料
                await SearchDataAsync();
            }
            finally
            {
                // 解鎖按鈕
                btnRefresh.Enabled = true;
            }
        }
    }
}