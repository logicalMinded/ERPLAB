namespace ERPLAB.UI.Core
{
    /// <summary>
    /// 共用分頁控制項 (Pagination Control)。
    /// 繼承自 UserControl，負責封裝分頁運算、邊界檢核 (Boundary Checking) 與 UI 狀態管理。
    /// 透過對外暴露 PageChanged 事件與宿主表單 (Host Form) 解耦，實現標準化之分頁操作介面。
    /// </summary>
    public partial class PaginationControl : UserControl
    {
        // =====================================================================
        // 事件合約 (Event Contracts)
        // =====================================================================

        /// <summary>
        /// 分頁狀態變更事件。
        /// 當使用者觸發翻頁操作或變更每頁顯示筆數時觸發，通知宿主表單重新載入資料。
        /// </summary>
        public event EventHandler? PageChanged;

        public int CurrentPage { get; private set; } = 1;

        public int PageSize => cmbPageSize.SelectedValue != null ? (int)cmbPageSize.SelectedValue : 10;

        // 內部狀態 (Internal State)
        private int _totalCount = 0;
        private int _totalPages = 1;
        private bool _isBrowseMode = true;

        public PaginationControl()
        {
            InitializeComponent();
            InitControls();
            BindEvents();
        }

        private void InitControls()
        {
            cmbPageSize.DataSource = new int[] { 10, 50, 100, 200 };
            cmbPageSize.SelectedIndex = 0; // 預設 10 筆
        }

        private void BindEvents()
        {
            btnFirstPage.Click += (s, e) => RequestPageChange(1);
            btnPrevPage.Click += (s, e) => RequestPageChange(CurrentPage - 1);
            btnNextPage.Click += (s, e) => RequestPageChange(CurrentPage + 1);
            btnLastPage.Click += (s, e) => RequestPageChange(_totalPages);

            cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                if (!_isBrowseMode) return;

                // 變更分頁筆數時，重置為第一頁
                CurrentPage = 1;
                PageChanged?.Invoke(this, EventArgs.Empty);
            };

            // 頁碼輸入框之防呆與邊界驗證
            txtCurrentPage.KeyDown += TxtCurrentPage_KeyDown;
            txtCurrentPage.Leave += (s, e) => txtCurrentPage.Text = CurrentPage.ToString();
        }

        private void TxtCurrentPage_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            if (!_isBrowseMode) return;

            if (int.TryParse(txtCurrentPage.Text.Trim(), out int inputPage))
            {
                if (inputPage >= 1 && inputPage <= _totalPages)
                {
                    RequestPageChange(inputPage);
                }
                else
                {
                    MessageBox.Show($"請輸入 1 到 {_totalPages} 之間的有效頁碼！", "輸入越界", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCurrentPage.Text = CurrentPage.ToString();
                }
            }
            else
            {
                txtCurrentPage.Text = CurrentPage.ToString();
            }
        }

        private void RequestPageChange(int targetPage)
        {
            if (!_isBrowseMode || targetPage < 1 || targetPage > _totalPages || targetPage == CurrentPage)
                return;

            CurrentPage = targetPage;

            // 觸發事件，通知宿主表單執行資料查詢
            PageChanged?.Invoke(this, EventArgs.Empty);
        }

        // =====================================================================
        // 公開方法 (Public API)
        // 供宿主表單呼叫，以進行狀態同步與 UI 更新。
        // =====================================================================

        /// <summary>
        /// 綁定總資料筆數。
        /// 由宿主表單於資料查詢後呼叫，用以自動重新計算總頁數，並觸發 UI 狀態更新。
        /// </summary>
        public void BindTotalCount(int totalCount)
        {
            _totalCount = totalCount;

            if (_totalCount == 0)
            {
                _totalPages = 1;
                CurrentPage = 1;
                lblPageInfo.Text = " / 1 頁 (共 0 筆)";
            }
            else
            {
                _totalPages = (int)Math.Ceiling((double)_totalCount / PageSize);
                if (CurrentPage > _totalPages) CurrentPage = _totalPages;
                lblPageInfo.Text = $" / {_totalPages} 頁 (共 {_totalCount} 筆)";
            }

            txtCurrentPage.Text = CurrentPage.ToString();
            UpdateUIState();
        }

        /// <summary>
        /// 設定 UI 互動狀態。
        /// 供宿主表單於編輯模式或特定作業中，統一鎖定或解鎖分頁控制項之操作權限。
        /// </summary>
        public void SetUIState(bool isBrowseMode)
        {
            _isBrowseMode = isBrowseMode;
            UpdateUIState();
        }

        /// <summary>
        /// 重置為第一頁。
        /// 供宿主表單於執行新查詢條件或重新整理時呼叫。
        /// </summary>
        public void ResetToFirstPage()
        {
            CurrentPage = 1;
        }

        /// <summary>
        /// 強制修正當前頁碼。
        /// 處理極端情境 (如：刪除該頁最後一筆資料時導致當前頁碼超出總頁數)，
        /// 供宿主表單強制校正頁碼，避免資料查詢越界。
        /// </summary>
        public void ForceCurrentPage(int page)
        {
            CurrentPage = page < 1 ? 1 : page;
        }

        private void UpdateUIState()
        {
            btnFirstPage.Enabled = _isBrowseMode && (CurrentPage > 1);
            btnPrevPage.Enabled = _isBrowseMode && (CurrentPage > 1);
            btnNextPage.Enabled = _isBrowseMode && (CurrentPage < _totalPages);
            btnLastPage.Enabled = _isBrowseMode && (CurrentPage < _totalPages);

            cmbPageSize.Enabled = _isBrowseMode;
            txtCurrentPage.ReadOnly = !_isBrowseMode;
        }
    }
}