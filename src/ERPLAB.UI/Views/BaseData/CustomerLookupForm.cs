using ERPLAB.BLL.Services;
using ERPLAB.Models.Entities;
using ERPLAB.UI.Core;
namespace ERPLAB.UI.Views.BaseData
{
    /// <summary>
    /// 客戶查詢對話方塊 (Lookup Dialog)。
    /// 以強制回應視窗 (Modal Form) 形式提供共用之資料查詢與選取介面。
    /// 透過封裝獨立之查詢邏輯，降低與宿主表單 (Host Form) 之耦合度，提升模組重用性。
    /// </summary>
    public partial class CustomerLookupForm : Form
    {
        private readonly CustomerService _customerService;

        // 唯讀公開屬性。作為對外暴露之資料交換合約，供宿主表單取得使用者最終選定之客戶實體 (Entity)。
        public Customer SelectedCustomer { get; private set; } = new();

        public CustomerLookupForm()
        {
            InitializeComponent();
            _customerService = new CustomerService();

            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "請選擇客戶";

            dgvList.AutoGenerateColumns = false;

            // 透過程式碼動態建構 DataGridView 欄位，避免過度依賴視覺化設計工具 (Designer)，以利版本控制與維護。
            if (dgvList.Columns.Count == 0)
            {
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerNo", HeaderText = "客戶編號", Width = 140 });
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "客戶名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TaxID", HeaderText = "統一編號", Width = 100 });
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PhoneNumber", HeaderText = "聯絡電話", Width = 120 });
            }

            dgvList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvList.MultiSelect = false;
            dgvList.ReadOnly = true;
            dgvList.EnableDoubleBuffering(true);

            // 綁定使用者操作事件：支援滑鼠雙擊選取與 Enter 鍵觸發查詢。
            dgvList.CellDoubleClick += (s, e) => ConfirmSelection();
            btnSearch.Click += async (s, e) => await DoSearchAsync();
            txtKeyword.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await DoSearchAsync(); } };
        }

        private async void CustomerLookupForm_Load(object sender, EventArgs e)
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

                // 業務過濾條件：僅查詢狀態為啟用 (IsActive = true) 之客戶，
                // 確保前端交易單據 (如銷貨單、報價單) 不會引用已遭邏輯刪除之無效實體。
                var result = await _customerService.GetCustomersAsync(1, 0, includeInactive: false, keyword: kw);

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
            if (dgvList.CurrentRow != null && dgvList.CurrentRow.DataBoundItem is Customer c)
            {
                SelectedCustomer = c;

                // 設定對話方塊回傳值為 OK，觸發視窗關閉並將控制權與選定資料交還予宿主表單。
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}