using ERPLAB.BLL.Services;
//*using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using ERPLAB.UI.Core;
using System.Data;


namespace ERPLAB.UI.Views.BaseData
{
    /// <summary>
    /// 員工基本檔維護頁面 (List-Detail Pattern)。
    /// 繼承自 BasePage，負責處理員工資料之 CRUD 操作。
    /// 實作地理圖資連動、郵遞區號拆解、狀態機 UI 控制與樂觀鎖 (Optimistic Concurrency) 防禦機制。
    /// </summary>
    public partial class EmployeePage : BasePage
    {
        // =====================================================================
        // 服務注入與全域狀態宣告
        // =====================================================================
        private readonly EmployeeService _empService;
        private readonly GeographyService _geoService;

        private BindingSource _bsEmployees;
        private ExtendedBindingList<Employee> _EmployeeBindingList;

        private List<Base_City> _cityList = new();
        private List<Base_District> _allDistrictList = new(); // 記憶體快取：全台行政區，供本機 O(1) 篩選使用

        // 表單狀態列舉，用於控制 UI 互動模式與唯讀限制
        private FormState _currentState = FormState.Browse;

        // 記憶體實體快取：保留 RowVersion 供存檔時進行併發比對
        //private Employee _currentEmployee;

        public EmployeePage()
        {
            InitializeComponent();

            // 套用擴充方法開啟雙重緩衝，改善 DataGridView 渲染效能與滾動卡頓
            dgvEmployees.EnableDoubleBuffering(true);

            _empService = new EmployeeService();
            _geoService = new GeographyService();

            // 初始化資料綁定來源
            _bsEmployees = new BindingSource();
            _EmployeeBindingList = new ExtendedBindingList<Employee>();

            // 統一於建構子掛載生命週期與控制項事件，確保執行順序
            this.Load += EmployeePage_Load;

            // 透過 BindingSource 之 CurrentChanged 事件監聽焦點轉移
            _bsEmployees.CurrentChanged += BsEmployees_CurrentChanged;

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;

            txtKeyword.KeyDown += TxtKeyword_KeyDown;

            // 綁定過濾條件切換事件：變更時立即重新載入資料
            chkShowInactive.CheckedChanged += async (s, e) => await SearchDataAsync();

            // 綁定 Grid 繪圖事件：處理停用資料之視覺提示
            dgvEmployees.CellFormatting += DgvEmployees_CellFormatting;

            // 訂閱分頁控制項事件，觸發資料重新查詢
            ucPagination.PageChanged += async (s, e) => await SearchDataAsync();

            cmbCity.SelectedIndexChanged += CmbCity_SelectedIndexChanged;
            cmbDistrict.SelectedIndexChanged += CmbDistrict_SelectedIndexChanged;
        }

        private async void EmployeePage_Load(object? sender, EventArgs e)
        {
            // =====================================================================
            // 權限檢核 (RBAC)：依據使用者授權動態顯示操作按鈕
            // 呼叫 BasePage 提供之 RequirePermission 進行控制項的實體隱藏
            // =====================================================================
            RequirePermission("ACT_CUST_ADD", btnAdd);
            RequirePermission("ACT_CUST_EDIT", btnEdit);

            // 職位狀態不受一般狀態機控制，獨立進行權限檢核
            cmbJobStatus.Enabled = SessionContext.HasPermission("ACT_EMP_EDIT");

            // 根據新增或修改之授權，決定儲存與取消按鈕之可見性
            bool canWrite = SessionContext.HasPermission("ACT_CUST_ADD") || SessionContext.HasPermission("ACT_CUST_EDIT");
            btnSave.Visible = canWrite;
            btnCancel.Visible = canWrite;

            // 透過擴充方法，將列舉動態綁定至下拉選單
            cmbGender.BindToEnum<GenderType>();
            cmbJobStatus.BindToEnum<EmployeeJobStatus>();
            cmbJobTitle.DataSource = new List<string>
            {
                "總經理", "財務經理", "業務經理", "採購經理", "倉儲主管", "系統管理員", "系統工程師",
                "會計專員", "行政助理", "業務專員", "採購專員", "行銷企劃", "倉管人員", "數據分析師",
                "業務助理"
            };
            cmbJobTitle.SelectedIndex = -1;

            SetupGridColumns();
            _bsEmployees.DataSource = _EmployeeBindingList;
            dgvEmployees.DataSource = _bsEmployees;

            // 優先載入靜態地理字典，再載入業務資料，確保連動邏輯順利執行
            await LoadGeographyDataAsync();
            await SearchDataAsync();
            SetUIState(FormState.Browse);
        }

        private void SetupGridColumns()
        {
            dgvEmployees.AutoGenerateColumns = false;
            if (dgvEmployees.Columns.Count == 0)
            {
                dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EmployeeNo", HeaderText = "員工編號", Width = 140 });
                dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EmployeeName", HeaderText = "員工名稱", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
                dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "JobTitle", HeaderText = "職稱", Width = 100 });
                dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PhoneNumber", HeaderText = "聯絡電話", Width = 120 });
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
            int? lastSelectedId = _bsEmployees.Current is Employee currentEmployee ? currentEmployee.EmployeeID : null;

            // 取回當前分頁參數
            int pageSize = ucPagination.PageSize;
            int currentPage = ucPagination.CurrentPage;

            try
            {
                var result = await _empService.GetEmployeesAsync(currentPage, pageSize, includeInactive, keyword);

                // =====================================================================
                // 若當前頁碼因資料刪除等原因導致越界，重新計算最後頁碼並再次執行查詢
                // =====================================================================
                if (result.Items.Count == 0 && result.TotalCount > 0)
                {
                    // 重算正確之最後一頁
                    int correctLastPage = (int)Math.Ceiling((double)result.TotalCount / pageSize);
                    ucPagination.ForceCurrentPage(correctLastPage);

                    // 修正頁碼後重新查詢，避免遞迴呼叫
                    result = await _empService.GetEmployeesAsync(correctLastPage, pageSize, includeInactive, keyword);
                }
                _bsEmployees.CurrentChanged -= BsEmployees_CurrentChanged;

                // 透過 ExtendedBindingList 之 AddRange 進行批次更新，維持 DataSource 結構不被破壞
                _EmployeeBindingList.Clear();
                _EmployeeBindingList.AddRange(result.Items);

                // 處理明細資料連動與游標定位
                if (_bsEmployees.Count > 0)
                {
                    // 1. 於底層資料集合中尋找目標實體
                    var targetEmployee = _EmployeeBindingList.FirstOrDefault(c => c.EmployeeID == lastSelectedId);

                    // 2. 取得目標實體於 BindingSource 中的索引值 (若無則預設指向首筆)
                    int targetIndex = targetEmployee != null ? _bsEmployees.IndexOf(targetEmployee) : 0;

                    // 3. 更新 BindingSource 游標，自動連動 UI 焦點
                    _bsEmployees.Position = targetIndex;

                    targetEmployee = (Employee)_bsEmployees.Current;

                    // 因已暫時解除 CurrentChanged 事件，需手動執行明細綁定
                    BindDetail(targetEmployee);
                }
                else
                {
                    ClearDetail();
                    SetUIState(_currentState);
                }

                _bsEmployees.CurrentChanged += BsEmployees_CurrentChanged;

                // 更新分頁控制項狀態
                ucPagination.BindTotalCount(result.TotalCount);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"資料載入失敗：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 監聽 BindingSource，確保實體游標一致性
        private void BsEmployees_CurrentChanged(object? sender, EventArgs e)
        {
            if (_currentState != FormState.Browse) return;

            if (_bsEmployees.Current is Employee current)
            {
                BindDetail(current);
            }
        }

        private void BindDetail(Employee currentEmployee)
        {
            if (currentEmployee == null)
            {
                MessageBox.Show("系統無法取得當前操作的資料！這可能是資料已被其他使用者刪除，請重新操作。", "狀態異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetUIState(FormState.Browse);
                return;
            }

            splitContainerMain.Panel2.SuspendLayout();

            txtEmployeeNo.Text = currentEmployee.EmployeeNo;
            txtEmployeeName.Text = currentEmployee.EmployeeName;
            txtEmail.Text = currentEmployee.Email;
            txtPhoneNumber.Text = currentEmployee.PhoneNumber;
            txtAddress.Text = currentEmployee.Address;

            #region 郵遞區號顯示處理：將資料庫儲存之 VARCHAR(6) 拆分為前端 3+3 格式
            // =====================================================================
            string zip = currentEmployee.CustomZipCode ?? string.Empty;
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
            if (currentEmployee.DistrictID > 0 && _allDistrictList != null)
            {
                var district = _allDistrictList.FirstOrDefault(d => d.DistrictID == currentEmployee.DistrictID);
                if (district != null)
                {
                    cmbCity.SelectedValue = district.CityID;     // 自動觸發過濾行政區
                    cmbDistrict.SelectedValue = currentEmployee.DistrictID;    // 自動觸發帶出前 3 碼
                }
            }
            else
            {
                cmbCity.SelectedIndex = -1;
            }
            #endregion

            // 處理下拉選單
            cmbGender.SelectedValue = (byte)currentEmployee.Gender;
            cmbJobTitle.SelectedItem = currentEmployee.JobTitle;
            cmbJobStatus.SelectedValue = (byte)currentEmployee.JobStatus;

            // =====================================================================
            // 狀態徽章 (Status Badge) 與按鈕文字之動態渲染
            // =====================================================================
            if (currentEmployee.IsActive)
            {
                // 渲染徽章
                lblStatusBadge.Text = "✅ 狀態：正常登入";
                lblStatusBadge.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                // 渲染徽章
                lblStatusBadge.Text = $"🚫 狀態：系統停權 ({currentEmployee.JobStatus.ToString()})";
                lblStatusBadge.ForeColor = System.Drawing.Color.Red;
            }

            // 稽核軌跡 (Audit Trail) 資訊顯示
            if (currentEmployee.EmployeeID > 0)
            {
                string creatorNo = currentEmployee.CreateUserNo_Display ?? "未知";
                string updaterNo = currentEmployee.UpdateUserNo_Display ?? "未知";

                lblAuditTrail.Text = $"建檔：{creatorNo} ({currentEmployee.CreateTime:yyyy/MM/dd HH:mm}) ｜ " +
                                     $"最後異動：{updaterNo} ({currentEmployee.UpdateTime:yyyy/MM/dd HH:mm})";
                lblAuditTrail.Visible = true;
            }
            else
            {
                // 新增模式時隱藏
                lblAuditTrail.Visible = false;
            }

            // 重新觸發狀態機評估
            SetUIState(_currentState);

            splitContainerMain.Panel2.ResumeLayout(true);
        }

        private void ClearDetail()
        {
            txtEmployeeNo.Text = "[儲存後自動配發]";
            txtEmployeeName.Clear();
            txtEmail.Clear();
            txtPhoneNumber.Clear();
            txtZipFront.Clear();
            txtZipRear.Clear();
            txtAddress.Clear();
            cmbJobStatus.SelectedValue = (byte)EmployeeJobStatus.Active;
            cmbJobTitle.SelectedIndex = -1;
            cmbGender.SelectedIndex = 0;
            cmbCity.SelectedIndex = -1;

            // 新增模式時，預設狀態顯示
            lblStatusBadge.Text = "✅ 狀態：正常登入 (新資料)";
            lblStatusBadge.ForeColor = System.Drawing.Color.Green;
        }
        // =====================================================================
        // 資料列視覺樣式自訂 (Visual Distinction)
        // =====================================================================
        private void DgvEmployees_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // 確保索引在合法範圍內
            if (e.RowIndex >= 0 && e.RowIndex < dgvEmployees.Rows.Count)
            {
                // 取得該列綁定的實體物件
                var Employee = dgvEmployees.Rows[e.RowIndex].DataBoundItem as Employee;

                // 停用資料之視覺提示：套用刪除線與灰階色彩
                if (Employee != null && !Employee.IsActive && e.CellStyle != null)
                {
                    e.CellStyle.ForeColor = System.Drawing.Color.DarkGray;
                    e.CellStyle.Font = new System.Drawing.Font(dgvEmployees.Font, System.Drawing.FontStyle.Strikeout);
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
            var currentEmployee = _bsEmployees.Current as Employee;

            // 右側明細區狀態切換
            txtEmployeeNo.ReadOnly = true; // 自動取號保持唯讀

            txtZipFront.ReadOnly = true; // 受行政區連動，維持唯讀

            // 依據編輯狀態切換輸入欄位
            txtZipRear.ReadOnly = !isEditing;
            txtEmployeeName.ReadOnly = !isEditing;
            txtEmail.ReadOnly = !isEditing;
            txtPhoneNumber.ReadOnly = !isEditing;
            txtAddress.ReadOnly = !isEditing;

            cmbJobStatus.Enabled = isEditing;
            cmbJobTitle.Enabled = isEditing;
            cmbGender.Enabled = isEditing;
            cmbCity.Enabled = isEditing;
            cmbDistrict.Enabled = isEditing;

            // 搜尋區控制項限制
            txtKeyword.Enabled = !isEditing;
            btnSearch.Enabled = !isEditing;
            chkShowInactive.Enabled = !isEditing;

            // 左側清單防呆：編輯時禁止切換資料
            dgvEmployees.Enabled = !isEditing;

            // 工具列按鈕狀態切換
            btnAdd.Enabled = !isEditing;
            btnEdit.Enabled = !isEditing && currentEmployee != null && currentEmployee.EmployeeID > 0;
            btnSave.Enabled = isEditing;
            btnCancel.Enabled = isEditing;
            btnRefresh.Enabled = !isEditing;

            if (state == FormState.Add)
            {
                ClearDetail();
                txtEmployeeName.Focus();
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
            if (dgvEmployees.SelectedRows.Count > 0)
                BindDetail((Employee)dgvEmployees.SelectedRows[0].DataBoundItem);
            else { ClearDetail(); }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            // 前端基礎必填與邏輯檢核
            if (string.IsNullOrWhiteSpace(txtEmployeeName.Text)
                || string.IsNullOrWhiteSpace(txtPhoneNumber.Text)
                || string.IsNullOrWhiteSpace(txtAddress.Text)
                || cmbDistrict.SelectedValue == null || cmbJobStatus.SelectedValue == null
                || cmbJobTitle.SelectedValue == null || cmbGender.SelectedValue == null)
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

            var currentEmployee = _currentState == FormState.Add ? new Employee() : (_bsEmployees.Current as Employee);
            if (currentEmployee == null)
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
            currentEmployee.CustomZipCode = string.IsNullOrEmpty(rear) ? front : front + rear;

            // 將 UI 畫面資料對映回記憶體實體 (DTO Mapping)
            currentEmployee.EmployeeName = txtEmployeeName.Text.Trim();
            currentEmployee.JobTitle = cmbJobTitle.SelectedValue?.ToString() ?? string.Empty;
            currentEmployee.Gender = (GenderType)(byte)(cmbGender.SelectedValue ?? (byte)0);
            currentEmployee.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
            currentEmployee.PhoneNumber = txtPhoneNumber.Text.Trim();
            currentEmployee.DistrictID = (int)cmbDistrict.SelectedValue;
            currentEmployee.Address = txtAddress.Text.Trim();

            if (cmbJobStatus.SelectedValue != null)
            {
                currentEmployee.JobStatus = (EmployeeJobStatus)(byte)cmbJobStatus.SelectedValue;
            }

            // 寫入系統稽核參數 (操作者 ID)
            //*currentEmployee.UpdateUser = SessionContext.CurrentAccountID;

            bool success = await SafeExecuteAsync(async () =>
            {
                if (_currentState == FormState.Add)
                {
                    currentEmployee = await _empService.CreateEmployeeAsync(currentEmployee, SessionContext.CurrentAccountID); // 包含 INSERTED.EmployeeID 與 RowVersion 的回傳
                }
                else if (_currentState == FormState.Edit)
                {
                    _ = await _empService.UpdateEmployeeAsync(currentEmployee, SessionContext.CurrentAccountID);
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
                _bsEmployees.LocateTo<Employee>(emp => emp.EmployeeNo == currentEmployee.EmployeeNo);
            }

            // 若因檢核未過或 SQL 例外導致存檔失敗，強制解鎖按鈕以利使用者修正後重試
            if (_currentState == FormState.Add || _currentState == FormState.Edit)
            {
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
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