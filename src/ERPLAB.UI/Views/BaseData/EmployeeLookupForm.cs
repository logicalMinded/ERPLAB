using ERPLAB.BLL.Services;
using ERPLAB.Models.Entities;
using ERPLAB.UI.Core;
namespace ERPLAB.UI.Views.BaseData
{
    /// <summary>
    /// 員工查詢對話方塊 (Lookup Dialog)。
    /// 以強制回應視窗 (Modal Form) 形式提供共用之資料查詢與選取介面。
    /// 透過封裝獨立之查詢邏輯，降低與宿主表單 (Host Form) 之耦合度，提升模組重用性。
    /// </summary>
    public partial class EmployeeLookupForm : Form
    {
        private readonly EmployeeService _empService;

        // 唯讀公開屬性。作為對外暴露之資料交換合約，供宿主表單取得使用者最終選定之員工實體 (Entity)。
        public Employee SelectedEmployee { get; private set; } = new();

        public EmployeeLookupForm()
        {
            InitializeComponent();
            _empService = new EmployeeService();

            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "請選擇員工";

            dgvList.AutoGenerateColumns = false;

            // 透過程式碼動態建構 DataGridView 欄位，降低對視覺化設計工具 (Designer) 之依賴，以利版本控制與後續維護。
            if (dgvList.Columns.Count == 0)
            {
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EmployeeNo", HeaderText = "員工編號", Width = 140 });
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EmployeeName", HeaderText = "員工名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            }

            dgvList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvList.MultiSelect = false;
            dgvList.ReadOnly = true;

            // 套用擴充方法開啟雙重緩衝 (Double Buffering)，優化資料渲染效能。
            dgvList.EnableDoubleBuffering(true);

            // 綁定使用者操作事件：支援滑鼠雙擊選取與 Enter 鍵觸發查詢。
            dgvList.CellDoubleClick += (s, e) => ConfirmSelection();
            btnSearch.Click += async (s, e) => await DoSearchAsync();
            txtKeyword.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await DoSearchAsync(); } };
        }

        private async void EmployeeLookupForm_Load(object sender, EventArgs e)
        {
            txtKeyword.PlaceholderText = "請在此輸入搜尋關鍵字";
            this.ActiveControl = txtKeyword;

            // 實務上若資料量龐大，初始載入應搭配分頁機制，或保留空白交由使用者主動輸入條件觸發查詢。
            //await DoSearchAsync();
        }

        private async Task DoSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(txtKeyword.Text)) return;
            try
            {
                // UI 狀態鎖定：查詢期間停用按鈕，防範連續點擊造成非預期之非同步併發請求。
                btnSearch.Enabled = false;
                string kw = txtKeyword.Text.Trim();

                // 業務過濾條件：僅查詢狀態為啟用 (IsActive = true) 之員工，
                // 確保前端交易單據 (如進銷存單據) 不會引用已遭邏輯刪除或離職之無效實體。
                var result = await _empService.GetEmployeesAsync(1, 0, includeInactive: false, keyword: kw);

                dgvList.DataSource = result.Items;
            }
            catch (Exception ex)
            {
                MessageBox.Show("查詢失敗：" + ex.Message);
            }
            finally
            {
                btnSearch.Enabled = true;
            }
        }

        private void ConfirmSelection()
        {
            if (dgvList.CurrentRow != null && dgvList.CurrentRow.DataBoundItem is Employee c)
            {
                SelectedEmployee = c;

                // 設定對話方塊回傳值為 OK，觸發視窗關閉並將控制權與選定資料交還予宿主表單。
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}