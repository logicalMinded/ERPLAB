using ERPLAB.DataAccess.Repositories;
using ERPLAB.UI.Core;
namespace ERPLAB.UI.Views.Reports
{
    /// <summary>
    /// 銷售戰情儀表板頁面 (Executive Dashboard)。
    /// 繼承自 BasePage，負責呈現高階決策支援所需之營運數據。
    /// 透過資料存取層 (DAL) 取得聚合查詢結果，展示關鍵績效指標 (KPI) 與銷售排行，並提供動態區間查詢功能。
    /// </summary>
    public partial class SalesDashboardPage : BasePage
    {
        private readonly SalesAnalysisRepository _analysisRepo;

        public SalesDashboardPage()
        {
            InitializeComponent();

            // 套用擴充方法開啟雙重緩衝，改善 DataGridView 渲染效能與資料載入時之畫面閃爍問題
            dgvTopProducts.EnableDoubleBuffering(true);
            dgvTopCustomers.EnableDoubleBuffering(true);

            _analysisRepo = new SalesAnalysisRepository();

            this.Load += SalesDashboardPage_Load;

            // 事件綁定 (Event Binding)：配置自訂搜尋與快速查詢區間操作
            btnSearch.Click += async (s, e) => await LoadDashboardDataAsync();

            btnThisMonth.Click += async (s, e) =>
            {
                var today = DateTime.Today;
                dtpStartDate.Value = new DateTime(today.Year, today.Month, 1);
                dtpEndDate.Value = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
                await LoadDashboardDataAsync();
            };

            btnThisYear.Click += async (s, e) =>
            {
                var today = DateTime.Today;
                dtpStartDate.Value = new DateTime(today.Year, 1, 1);
                dtpEndDate.Value = new DateTime(today.Year, 12, 31);
                await LoadDashboardDataAsync();
            };
        }

        private async void SalesDashboardPage_Load(object? sender, EventArgs e)
        {
            // 初始化查詢條件：預設查詢區間設定為當月
            var today = DateTime.Today;
            dtpStartDate.Value = new DateTime(today.Year, today.Month, 1);
            dtpEndDate.Value = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

            // 初始化 DataGridView 欄位配置
            SetupGridColumns();

            // 觸發初始非同步資料載入
            await LoadDashboardDataAsync();
        }

        private void SetupGridColumns()
        {
            // 動態建構熱銷商品排行榜欄位
            dgvTopProducts.AutoGenerateColumns = false;
            dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductNo", HeaderText = "商品代碼", Width = 100 });
            dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "商品名稱", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalQtySold", HeaderText = "銷售量", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" } });
            dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalRevenue", HeaderText = "貢獻營業額", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C0" } });

            // 動態建構貢獻客戶排行榜欄位
            dgvTopCustomers.AutoGenerateColumns = false;
            dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerNo", HeaderText = "客戶編號", Width = 100 });
            dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "客戶名稱", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OrderCount", HeaderText = "訂單數", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" } });
            dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalRevenue", HeaderText = "貢獻營業額", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C0", ForeColor = System.Drawing.Color.MediumVioletRed } });
        }

        private async Task LoadDashboardDataAsync()
        {
            // 鎖定查詢控制項，防範非同步執行期間產生重複觸發 (Double Click) 或併發請求
            btnSearch.Enabled = false;
            btnThisMonth.Enabled = false;
            btnThisYear.Enabled = false;

            try
            {
                // 呼叫資料存取層執行聚合查詢。
                // 透過單次請求 (如配合 MARS 機制) 取回所有儀表板所需數據，降低資料庫連線與 I/O 負載。
                var result = await _analysisRepo.GetDashboardDataAsync(dtpStartDate.Value, dtpEndDate.Value);

                // =====================================================================
                // 關鍵績效指標 (KPI) 渲染與數值格式化
                // =====================================================================
                lblRevenueValue.Text = result.Summary.TotalRevenue.ToString("C0");

                // 依據毛利率正負值動態切換文字色彩，提供直覺之視覺警示 (綠色為正，紅色為負)
                lblGrossProfitValue.Text = $"{result.Summary.GrossProfit:C0}\n({result.Summary.GrossMarginRatio:P1})";
                lblGrossProfitValue.ForeColor = result.Summary.GrossProfit >= 0 ? System.Drawing.Color.Green : System.Drawing.Color.Red;

                lblOrdersValue.Text = $"{result.Summary.TotalOrders:N0} 張";
                lblAovValue.Text = result.Summary.AverageOrderValue.ToString("C0");

                // =====================================================================
                // 排行榜資料綁定 (Data Binding)
                // =====================================================================
                dgvTopProducts.DataSource = result.TopProducts;
                dgvTopCustomers.DataSource = result.TopCustomers;

                dgvTopProducts.ClearSelection();
                dgvTopCustomers.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"產生戰情報表時發生異常：\n{ex.Message}", "資料載入失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 查詢結束，解除控制項鎖定
                btnSearch.Enabled = true;
                btnThisMonth.Enabled = true;
                btnThisYear.Enabled = true;
            }
        }
    }
}