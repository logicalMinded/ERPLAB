using ERPLAB.BLL.Services;
using ERPLAB.Models.Entities;
using ERPLAB.UI.Core;
using System.Data;

namespace ERPLAB.UI.Views.BaseData
{
    /// <summary>
    /// 廠商基本檔維護頁面 (List-Detail Pattern)。
    /// 繼承自 BasePage，負責處理廠商資料之 CRUD 操作。
    /// 實作地理圖資連動、郵遞區號拆解、狀態機 UI 控制與樂觀鎖 (Optimistic Concurrency) 防禦機制。
    /// </summary>
    public partial class VendorPage : BasePage
    {
        // =====================================================================
        // 服務注入與全域狀態宣告
        // =====================================================================
        private readonly VendorService _vendorService;
        private readonly GeographyService _geoService;

        private BindingSource _bsVendors;
        private ExtendedBindingList<Vendor> _vendorBindingList;

        private List<Base_City> _cityList = new();
        private List<Base_District> _allDistrictList = new(); // 記憶體快取：全台行政區，供本機 O(1) 篩選使用

        // 表單狀態列舉，用於控制 UI 互動模式與唯讀限制
        private FormState _currentState = FormState.Browse;

        // 記憶體實體快取：保留 RowVersion 供存檔時進行併發比對
        //private Vendor _currentVendor;

        public VendorPage()
        {
            InitializeComponent();

            // 套用擴充方法開啟雙重緩衝，改善 DataGridView 渲染效能與滾動卡頓
            dgvVendors.EnableDoubleBuffering(true);

            _vendorService = new VendorService();
            _geoService = new GeographyService();

            // 初始化資料綁定來源
            _bsVendors = new BindingSource();
            _vendorBindingList = new ExtendedBindingList<Vendor>();

            // 統一於建構子掛載生命週期與控制項事件，確保執行順序
            this.Load += VendorPage_Load;

            // 透過 BindingSource 之 CurrentChanged 事件監聽焦點轉移
            _bsVendors.CurrentChanged += BsVendors_CurrentChanged;

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
            btnToggleStatus.Click += btnToggleStatus_Click;

            txtKeyword.KeyDown += TxtKeyword_KeyDown;

            // 綁定過濾條件切換事件：變更時立即重新載入資料
            chkShowInactive.CheckedChanged += async (s, e) => await SearchDataAsync();

            // 綁定 Grid 繪圖事件：處理停用資料之視覺提示
            dgvVendors.CellFormatting += DgvVendors_CellFormatting;

            // 訂閱分頁控制項事件，觸發資料重新查詢
            ucPagination.PageChanged += async (s, e) => await SearchDataAsync();

            cmbCity.SelectedIndexChanged += CmbCity_SelectedIndexChanged;
            cmbDistrict.SelectedIndexChanged += CmbDistrict_SelectedIndexChanged;
        }

        private async void VendorPage_Load(object? sender, EventArgs e)
        {
            // =====================================================================
            // 權限檢核 (RBAC)：依據使用者授權動態顯示操作按鈕
            // 呼叫 BasePage 提供之 RequirePermission 進行控制項的實體隱藏
            // =====================================================================
            RequirePermission("ACT_VEND_ADD", btnAdd);
            RequirePermission("ACT_VEND_EDIT", btnEdit);
            RequirePermission("ACT_VEND_EDIT", btnToggleStatus);

            // 根據新增或修改之授權，決定儲存與取消按鈕之可見性
            bool canWrite = SessionContext.HasPermission("ACT_VEND_ADD") || SessionContext.HasPermission("ACT_VEND_EDIT");
            btnSave.Visible = canWrite;
            btnCancel.Visible = canWrite;

            SetupGridColumns();
            _bsVendors.DataSource = _vendorBindingList;
            dgvVendors.DataSource = _bsVendors;

            // 優先載入靜態地理字典，再載入業務資料，確保連動邏輯順利執行
            await LoadGeographyDataAsync();
            await SearchDataAsync();
            SetUIState(FormState.Browse);
        }

        private void SetupGridColumns()
        {
            dgvVendors.AutoGenerateColumns = false;
            if (dgvVendors.Columns.Count == 0)
            {
                dgvVendors.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "VendorNo", HeaderText = "廠商編號", Width = 140 });
                dgvVendors.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "VendorName", HeaderText = "廠商名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
                dgvVendors.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TaxID", HeaderText = "統一編號", Width = 100 });
                dgvVendors.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PhoneNumber", HeaderText = "聯絡電話", Width = 120 });
            }
        }

        // =====================================================================
        // 地理資料連動機制 (縣市 -> 鄉鎮市區 -> 郵遞區號)
        // =====================================================================
        private async Task LoadGeographyDataAsync()
        {
            try
            {
                _cityList.Clear();
                _cityList.AddRange(await _geoService.GetActiveCitiesAsync());
                _allDistrictList.Clear();
                _allDistrictList.AddRange(await _geoService.GetAllActiveDistrictsAsync());

                cmbCity.SelectedIndexChanged -= CmbCity_SelectedIndexChanged; // 暫時解除綁定以防觸發異常
                cmbCity.DataSource = _cityList;
                cmbCity.DisplayMember = "CityName";
                cmbCity.ValueMember = "CityID";
                cmbCity.SelectedIndex = -1;
                cmbCity.SelectedIndexChanged += CmbCity_SelectedIndexChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入地理字典檔失敗：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CmbCity_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbCity.SelectedValue == null || !int.TryParse(cmbCity.SelectedValue.ToString(), out int cityId))
            {
                cmbDistrict.DataSource = null;
                txtZipFront.Clear();
                return;
            }

            // 於記憶體中進行 O(1) 篩選，避免頻繁的資料庫 I/O 請求
            var filteredDistricts = _allDistrictList
                .Where(d => d.CityID == cityId)
                .OrderBy(d => d.SortSeq)
                .ToList();

            cmbDistrict.SelectedIndexChanged -= CmbDistrict_SelectedIndexChanged;
            cmbDistrict.DataSource = filteredDistricts;
            cmbDistrict.DisplayMember = "DistrictName";
            cmbDistrict.ValueMember = "DistrictID";
            cmbDistrict.SelectedIndex = -1; // 強制重選
            cmbDistrict.SelectedIndexChanged += CmbDistrict_SelectedIndexChanged;

            txtZipFront.Clear();
        }

        private void CmbDistrict_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // 連動帶出所選行政區之 3 碼郵遞區號
            if (cmbDistrict.SelectedItem is Base_District selectedDistrict)
            {
                txtZipFront.Text = selectedDistrict.ZipCode;
            }
            else
            {
                txtZipFront.Clear();
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
            int? lastSelectedId = _bsVendors.Current is Vendor currentVendor ? currentVendor.VendorID : null;

            // 取回當前分頁參數
            int pageSize = ucPagination.PageSize;
            int currentPage = ucPagination.CurrentPage;

            try
            {
                var result = await _vendorService.GetVendorsAsync(currentPage, pageSize, includeInactive, keyword);

                // =====================================================================
                // 若當前頁碼因資料刪除等原因導致越界，重新計算最後頁碼並再次執行查詢
                // =====================================================================
                if (result.Items.Count == 0 && result.TotalCount > 0)
                {
                    // 重算正確之最後一頁
                    int correctLastPage = (int)Math.Ceiling((double)result.TotalCount / pageSize);
                    ucPagination.ForceCurrentPage(correctLastPage);

                    // 修正頁碼後重新查詢，避免遞迴呼叫
                    result = await _vendorService.GetVendorsAsync(correctLastPage, pageSize, includeInactive, keyword);
                }
                _bsVendors.CurrentChanged -= BsVendors_CurrentChanged;

                // 透過 ExtendedBindingList 之 AddRange 進行批次更新，維持 DataSource 結構不被破壞
                _vendorBindingList.Clear();
                _vendorBindingList.AddRange(result.Items);

                // 處理明細資料連動與游標定位
                if (_bsVendors.Count > 0)
                {
                    // 1. 於底層資料集合中尋找目標實體
                    var targetVendor = _vendorBindingList.FirstOrDefault(c => c.VendorID == lastSelectedId);

                    // 2. 取得目標實體於 BindingSource 中的索引值 (若無則預設指向首筆)
                    int targetIndex = targetVendor != null ? _bsVendors.IndexOf(targetVendor) : 0;

                    // 3. 更新 BindingSource 游標，自動連動 UI 焦點
                    _bsVendors.Position = targetIndex;

                    targetVendor = (Vendor)_bsVendors.Current!;

                    // 因已暫時解除 CurrentChanged 事件，需手動執行明細綁定
                    BindDetail(targetVendor);
                }
                else
                {
                    ClearDetail();
                    SetUIState(_currentState);
                }

                _bsVendors.CurrentChanged += BsVendors_CurrentChanged;

                // 更新分頁控制項狀態
                ucPagination.BindTotalCount(result.TotalCount);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"資料載入失敗：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 監聽 BindingSource，確保實體游標一致性
        private void BsVendors_CurrentChanged(object? sender, EventArgs e)
        {
            if (_currentState != FormState.Browse) return;

            if (_bsVendors.Current is Vendor current)
            {
                BindDetail(current);
            }
        }

        private void BindDetail(Vendor currentVendor)
        {
            if (currentVendor == null)
            {
                MessageBox.Show("系統無法取得當前操作的資料！這可能是資料已被其他使用者刪除，請重新操作。", "狀態異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetUIState(FormState.Browse);
                return;
            }
            splitContainerMain.Panel2.SuspendLayout();

            txtVendorNo.Text = currentVendor.VendorNo;
            txtVendorName.Text = currentVendor.VendorName;
            txtTaxID.Text = currentVendor.TaxID;
            txtContactPerson.Text = currentVendor.ContactPerson;
            txtPhoneNumber.Text = currentVendor.PhoneNumber;
            txtAddress.Text = currentVendor.Address;
            txtEmail.Text = currentVendor.Email;
            txtRemark.Text = currentVendor.Remark;

            // =====================================================================
            // 郵遞區號顯示處理：將資料庫儲存之 VARCHAR(6) 拆分為前端 3+3 格式
            // =====================================================================
            string zip = currentVendor.CustomZipCode ?? string.Empty;
            if (zip.Length >= 3)
            {
                txtZipFront.Text = zip.Substring(0, 3);
                txtZipRear.Text = zip.Length == 6 ? zip.Substring(3, 3) : string.Empty;
            }
            else
            {
                txtZipFront.Clear();
                txtZipRear.Clear();
            }

            // 地理資料反向連動：透過 DistrictID 推導 CityID，維持下拉選單選項正確性
            if (currentVendor.DistrictID > 0 && _allDistrictList != null)
            {
                var district = _allDistrictList.FirstOrDefault(d => d.DistrictID == currentVendor.DistrictID);
                if (district != null)
                {
                    cmbCity.SelectedValue = district.CityID;     // 自動觸發過濾行政區
                    cmbDistrict.SelectedValue = currentVendor.DistrictID;    // 自動帶出前 3 碼
                }
            }
            else
            {
                cmbCity.SelectedIndex = -1;
            }

            // =====================================================================
            // 狀態徽章 (Status Badge) 與按鈕文字之動態渲染
            // =====================================================================
            if (currentVendor.IsActive)
            {
                // 渲染徽章
                lblStatusBadge.Text = "✅ 狀態：正常交易";
                lblStatusBadge.ForeColor = System.Drawing.Color.Green;

                btnToggleStatus.Text = "🚫 終止交易 (停用)";
                btnToggleStatus.ForeColor = System.Drawing.Color.Red;
            }
            else
            {
                // 渲染徽章
                lblStatusBadge.Text = "🚫 狀態：已終止 (停用)";
                lblStatusBadge.ForeColor = System.Drawing.Color.Red;

                btnToggleStatus.Text = "✅ 恢復交易 (啟用)";
                btnToggleStatus.ForeColor = System.Drawing.Color.Green;
            }

            // 稽核軌跡 (Audit Trail) 資訊顯示
            if (currentVendor.VendorID > 0)
            {
                string creatorNo = currentVendor.CreateUserNo_Display ?? "未知";
                string updaterNo = currentVendor.UpdateUserNo_Display ?? "未知";

                lblAuditTrail.Text = $"建檔：{creatorNo} ({currentVendor.CreateTime:yyyy/MM/dd HH:mm}) ｜ " +
                                     $"最後異動：{updaterNo} ({currentVendor.UpdateTime:yyyy/MM/dd HH:mm})";
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
            txtVendorNo.Text = "[儲存後自動配發]";
            txtVendorName.Clear();
            txtTaxID.Clear();
            txtContactPerson.Clear();
            txtPhoneNumber.Clear();
            txtZipFront.Clear();
            txtZipRear.Clear();
            txtAddress.Clear();
            txtEmail.Clear();
            txtRemark.Clear();
            cmbCity.SelectedIndex = -1;

            // 新增模式時，預設狀態顯示
            lblStatusBadge.Text = "✅ 狀態：正常交易 (新資料)";
            lblStatusBadge.ForeColor = System.Drawing.Color.Green;

            btnToggleStatus.Text = "狀態操作";
            btnToggleStatus.ForeColor = System.Drawing.Color.Black;
        }

        // =====================================================================
        // 資料列視覺樣式自訂 (Visual Distinction)
        // =====================================================================
        private void DgvVendors_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // 確保索引在合法範圍內
            if (e.RowIndex >= 0 && e.RowIndex < dgvVendors.Rows.Count)
            {
                // 取得該列綁定的實體物件
                var vendor = dgvVendors.Rows[e.RowIndex].DataBoundItem as Vendor;

                // 停用資料之視覺提示：套用刪除線與灰階色彩
                if (vendor != null && !vendor.IsActive && e.CellStyle != null)
                {
                    e.CellStyle.ForeColor = System.Drawing.Color.DarkGray;
                    e.CellStyle.Font = new System.Drawing.Font(dgvVendors.Font, System.Drawing.FontStyle.Strikeout);
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
            var currentVendor = _bsVendors.Current as Vendor;

            // 右側明細區狀態切換
            txtVendorNo.ReadOnly = true; // 自動取號保持唯讀

            txtZipFront.ReadOnly = true; // 受行政區連動，維持唯讀

            // 依據編輯狀態切換輸入欄位
            txtZipRear.ReadOnly = !isEditing;
            txtVendorName.ReadOnly = !isEditing;
            txtTaxID.ReadOnly = !isEditing;
            txtPhoneNumber.ReadOnly = !isEditing;
            txtAddress.ReadOnly = !isEditing;
            txtEmail.ReadOnly = !isEditing;
            txtRemark.ReadOnly = !isEditing;

            cmbCity.Enabled = isEditing;
            cmbDistrict.Enabled = isEditing;

            // 搜尋區控制項限制
            txtKeyword.Enabled = !isEditing;
            btnSearch.Enabled = !isEditing;
            chkShowInactive.Enabled = !isEditing;

            // 左側清單防呆：編輯時禁止切換資料
            dgvVendors.Enabled = !isEditing;

            // 工具列按鈕狀態切換
            btnAdd.Enabled = !isEditing;
            btnEdit.Enabled = !isEditing && currentVendor != null && currentVendor.VendorID > 0 && currentVendor.IsActive == true;
            btnSave.Enabled = isEditing;
            btnCancel.Enabled = isEditing;
            btnToggleStatus.Enabled = !isEditing && currentVendor != null && currentVendor.VendorID > 0;
            btnRefresh.Enabled = !isEditing;

            if (state == FormState.Add)
            {
                ClearDetail();
                txtVendorName.Focus();
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
            if (dgvVendors.SelectedRows.Count > 0 &&
                   dgvVendors.SelectedRows[0].DataBoundItem is Vendor vendor)
            {
                BindDetail(vendor);
            }
            else { ClearDetail(); }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            // 前端基礎必填與邏輯檢核
            if (string.IsNullOrWhiteSpace(txtVendorName.Text)
                || string.IsNullOrWhiteSpace(txtPhoneNumber.Text)
                || string.IsNullOrWhiteSpace(txtAddress.Text)
                || cmbDistrict.SelectedValue == null)
            {
                MessageBox.Show("藍色欄位為必填欄位！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureValid(SystemValidator.ValidateEmail(txtEmail.Text), txtEmail))
                return;
            if (!EnsureValid(SystemValidator.ValidatePhone(txtPhoneNumber.Text), txtPhoneNumber))
                return;
            if (!EnsureValid(SystemValidator.ValidateZipRear(txtZipRear.Text), txtZipRear))
                return;

            var currentVendor = _currentState == FormState.Add ? new Vendor() : (_bsVendors.Current as Vendor);
            if (currentVendor == null)
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

            // =====================================================================
            // 郵遞區號合併處理：將 UI 之 3+3 格式組合為 VARCHAR(6) 寫入實體
            // =====================================================================
            string front = txtZipFront.Text.Trim();
            string rear = txtZipRear.Text.Trim();
            currentVendor.CustomZipCode = string.IsNullOrEmpty(rear) ? front : front + rear;

            // 將 UI 畫面資料對映回記憶體實體 (DTO Mapping)
            currentVendor.VendorName = txtVendorName.Text.Trim();
            currentVendor.TaxID = string.IsNullOrWhiteSpace(txtTaxID.Text) ? null : txtTaxID.Text.Trim();
            currentVendor.ContactPerson = txtContactPerson.Text.Trim();
            currentVendor.PhoneNumber = txtPhoneNumber.Text.Trim();
            currentVendor.DistrictID = (int)cmbDistrict.SelectedValue;
            currentVendor.Address = txtAddress.Text.Trim();
            currentVendor.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
            currentVendor.Remark = string.IsNullOrWhiteSpace(txtRemark.Text) ? null : txtRemark.Text.Trim();

            // 寫入系統稽核參數 (操作者 ID)
            // currentVendor.UpdateUser = SessionContext.CurrentAccountID;

            bool success = await SafeExecuteAsync(async () =>
            {
                if (_currentState == FormState.Add)
                {
                    currentVendor = await _vendorService.CreateVendorAsync(currentVendor, SessionContext.CurrentAccountID);
                }
                else if (_currentState == FormState.Edit)
                {
                    _ = await _vendorService.UpdateVendorAsync(currentVendor, SessionContext.CurrentAccountID);
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
                _bsVendors.LocateTo<Vendor>(c => c.VendorNo == currentVendor.VendorNo);
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
            var currentVendor = _bsVendors.Current as Vendor;
            if (currentVendor == null || currentVendor.VendorID == 0)
            {
                MessageBox.Show("系統無法取得當前操作的資料！這可能是資料已被其他使用者刪除，請重新操作。", "狀態異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetUIState(FormState.Browse);
                return;
            }

            string actionName = currentVendor.IsActive ? "終止交易 (停用)" : "恢復交易 (啟用)";

            if (MessageBox.Show($"確定要對 [{currentVendor.VendorName}] 執行 {actionName} 嗎？",
                "狀態變更確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bool success = await SafeExecuteAsync(async () =>
                {
                    byte[] newRowVersion = await _vendorService.UpdateVendorStatusAsync(
                        currentVendor.VendorID,
                        currentVendor.IsActive,
                        currentVendor.RowVersion,
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