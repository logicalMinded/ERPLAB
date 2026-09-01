using ERPLAB.BLL.Services;
using ERPLAB.Models.Entities;
using ERPLAB.UI.Core;

namespace ERPLAB.UI.Views.BaseData
{
    /// <summary>
    /// 共用員工開窗查詢模組 (Lookup Dialog)
    /// </summary>
    public partial class EmployeeLookupForm : Form
    {
        private readonly EmployeeService _empService;

        // 💡 唯一對外暴露的屬性：回傳使用者選定的員工實體
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

            if (dgvList.Columns.Count == 0)
            {
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EmployeeNo", HeaderText = "員工編號", Width = 140 });
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EmployeeName", HeaderText = "員工名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            }

            dgvList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvList.MultiSelect = false;
            dgvList.ReadOnly = true;
            dgvList.EnableDoubleBuffering(true); // 套用雙重緩衝擴充

            // 雙擊直接帶回資料
            dgvList.CellDoubleClick += (s, e) => ConfirmSelection();
            btnSearch.Click += async (s, e) => await DoSearchAsync();
            txtKeyword.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await DoSearchAsync(); } };
        }

        private async void EmployeeLookupForm_Load(object sender, EventArgs e)
        {
            txtKeyword.PlaceholderText = "請在此輸入搜尋關鍵字";
            this.ActiveControl = txtKeyword;

            // 預設載入前 50 筆 (實務上應配合分頁機制)
            //await DoSearchAsync();
        }

        private async Task DoSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(txtKeyword.Text)) return;
            try
            {
                btnSearch.Enabled = false;
                string kw = txtKeyword.Text.Trim();

                // 💡 僅撈取啟用中 (IsActive=1) 的員工供打單使用
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
                this.DialogResult = DialogResult.OK; // 觸發成功信號並關閉視窗
            }
        }
    }
}