namespace ERPLAB.UI.Core
{
    /// <summary>
    /// 共用分頁底座控制項
    /// 核心職責：封裝頁碼計算、越界防禦、UI 物理鎖死，並對外曝露單一的 PageChanged 事件。
    /// </summary>
    public partial class PaginationControl : UserControl
    {
        // =====================================================================
        // 📢 [對外通訊合約] 
        // =====================================================================
        /// <summary>
        /// 當使用者要求翻頁、或更改每頁筆數時觸發
        /// </summary>
        public event EventHandler PageChanged;

        public int CurrentPage { get; private set; } = 1;

        public int PageSize => cmbPageSize.SelectedValue != null ? (int)cmbPageSize.SelectedValue : 10;

        // 內部狀態記憶
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
            // 初始化陣列綁定
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
                CurrentPage = 1; // 改筆數強迫回第一頁
                PageChanged?.Invoke(this, EventArgs.Empty);
            };

            // 手動跳頁防呆
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
                    MessageBox.Show($"請輸入 1 到 {_totalPages} 之間的有效頁碼！", "越界", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            PageChanged?.Invoke(this, EventArgs.Empty); // 💡 通知外部 (如 CustomerPage) 去撈資料
        }

        // =====================================================================
        // 🧮 [外部呼叫 API] 供宿主表單控制分頁器
        // =====================================================================

        /// <summary>
        /// 宿主表單撈完資料後，呼叫此方法將總筆數餵給分頁器，自動計算並重繪 UI
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
        /// 宿主表單狀態改變時，通知分頁器鎖定或解鎖
        /// </summary>
        public void SetUIState(bool isBrowseMode)
        {
            _isBrowseMode = isBrowseMode;
            UpdateUIState();
        }

        /// <summary>
        /// 供宿主表單在「搜尋」或「重整」時，強迫頁碼歸零
        /// </summary>
        public void ResetToFirstPage()
        {
            CurrentPage = 1;
        }

        /// <summary>
        /// 供宿主表單在發生「幽靈頁碼踩空」時，強迫物理修正頁碼
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