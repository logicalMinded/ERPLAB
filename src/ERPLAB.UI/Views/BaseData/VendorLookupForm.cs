using ERPLAB.BLL.Services;
using ERPLAB.Models.Entities;
using ERPLAB.UI.Core;

namespace ERPLAB.UI.Views.BaseData
{
    /// <summary>
    /// 共用廠商開窗查詢模組 (Lookup Dialog)
    /// </summary>
    public partial class VendorLookupForm : Form
    {
        private readonly VendorService _vendorService;

        // 💡 唯一對外暴露的屬性：回傳使用者選定的廠商實體
        public Vendor SelectedVendor { get; private set; } = new();

        public VendorLookupForm()
        {
            InitializeComponent();
            _vendorService = new VendorService();

            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "請選擇廠商";

            dgvList.AutoGenerateColumns = false;

            if (dgvList.Columns.Count == 0)
            {
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "VendorNo", HeaderText = "廠商編號", Width = 140 });
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "VendorName", HeaderText = "廠商名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TaxID", HeaderText = "統一編號", Width = 100 });
                dgvList.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PhoneNumber", HeaderText = "聯絡電話", Width = 120 });
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

        private async void VendorLookupForm_Load(object sender, EventArgs e)
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

                // 💡 僅撈取啟用中 (IsActive=1) 的廠商供打單使用
                var result = await _vendorService.GetVendorsAsync(1, 0, includeInactive: false, keyword: kw);

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
            if (dgvList.CurrentRow != null && dgvList.CurrentRow.DataBoundItem is Vendor c)
            {
                SelectedVendor = c;
                this.DialogResult = DialogResult.OK; // 觸發成功信號並關閉視窗
            }
        }
    }
}