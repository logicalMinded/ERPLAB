using ERPLAB.DataAccess.Repositories;
using ERPLAB.UI.Core;

namespace ERPLAB.UI.Views.Reports
{
    /// <summary>
    /// 銷售戰情儀表板 (Executive Dashboard)
    /// 核心職責：從 OLAP 查詢引擎獲取聚合數據，提供高階主管決策支援。
    /// </summary>
    public partial class SalesDashboardPage : BasePage
    {
        private readonly SalesAnalysisRepository _analysisRepo;

        public SalesDashboardPage()
        {
            InitializeComponent();

            // 物理優化
            dgvTopProducts.EnableDoubleBuffering(true);
            dgvTopCustomers.EnableDoubleBuffering(true);

            _analysisRepo = new SalesAnalysisRepository();

            this.Load += SalesDashboardPage_Load;

            // 綁定事件
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
            // 1. 設定預設查詢區間 (本月)
            var today = DateTime.Today;
            dtpStartDate.Value = new DateTime(today.Year, today.Month, 1);
            dtpEndDate.Value = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

            // 2. 設定 Grid 白名單欄位
            SetupGridColumns();

            // 3. 發動非同步聚合查詢
            await LoadDashboardDataAsync();
        }

        private void SetupGridColumns()
        {
            // 商品排行榜欄位
            dgvTopProducts.AutoGenerateColumns = false;
            dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductNo", HeaderText = "商品代碼", Width = 100 });
            dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "商品名稱", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalQtySold", HeaderText = "銷售量", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" } });
            dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalRevenue", HeaderText = "貢獻營業額", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C0" } });

            // 客戶排行榜欄位
            dgvTopCustomers.AutoGenerateColumns = false;
            dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerNo", HeaderText = "客戶編號", Width = 100 });
            dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "客戶名稱", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OrderCount", HeaderText = "訂單數", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" } });
            dgvTopCustomers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalRevenue", HeaderText = "貢獻營業額", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C0", ForeColor = System.Drawing.Color.MediumVioletRed } });
        }

        private async Task LoadDashboardDataAsync()
        {
            btnSearch.Enabled = false;
            btnThisMonth.Enabled = false;
            btnThisYear.Enabled = false;

            try
            {
                // 💡 呼叫 DAL 層執行 MARS 聚合查詢 (1 次網路 I/O 帶回所有戰情數據)
                var result = await _analysisRepo.GetDashboardDataAsync(dtpStartDate.Value, dtpEndDate.Value);

                // =====================================================================
                // 📊 渲染 KPI 戰情卡片 (Formatting)
                // =====================================================================
                lblRevenueValue.Text = result.Summary.TotalRevenue.ToString("C0");

                // 毛利率若為負數，自動變換顏色顯示警示
                lblGrossProfitValue.Text = $"{result.Summary.GrossProfit:C0}\n({result.Summary.GrossMarginRatio:P1})";
                lblGrossProfitValue.ForeColor = result.Summary.GrossProfit >= 0 ? System.Drawing.Color.Green : System.Drawing.Color.Red;

                lblOrdersValue.Text = $"{result.Summary.TotalOrders:N0} 張";
                lblAovValue.Text = result.Summary.AverageOrderValue.ToString("C0");

                // =====================================================================
                // 🏆 渲染排行榜 (Data Binding)
                // =====================================================================
                dgvTopProducts.DataSource = result.TopProducts;
                dgvTopCustomers.DataSource = result.TopCustomers;

                dgvTopProducts.ClearSelection();
                dgvTopCustomers.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"產生戰情報表時發生異常：\n{ex.Message}", "資料庫錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSearch.Enabled = true;
                btnThisMonth.Enabled = true;
                btnThisYear.Enabled = true;
            }
        }
    }
}