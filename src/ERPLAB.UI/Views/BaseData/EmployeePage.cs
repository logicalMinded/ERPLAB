using ERPLAB.BLL.Services;
//*using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using ERPLAB.UI.Core;
using System.Data;


namespace ERPLAB.UI.Views.BaseData
{
    /// <summary>
    /// 員工基本檔維護模組 (List-Detail 模式)。
    /// 核心展示：地理資料快取連動、3+3 郵遞區號虛擬化、狀態機鎖定與樂觀鎖 (Optimistic Locking) 防禦。
    /// </summary>
    public partial class EmployeePage : BasePage
    {
        // =====================================================================
        // 💡 倉儲與全域狀態快取
        // =====================================================================
        private readonly EmployeeService _empService;
        private readonly GeographyService _geoService;

        private BindingSource _bsEmployees;
        private ExtendedBindingList<Employee> _EmployeeBindingList;

        private List<Base_City> _cityList = new();
        private List<Base_District> _allDistrictList = new(); // 全台行政區快取，供本機 O(1) 過濾

        // 狀態機定義：精確控制畫面行為與防呆
        private FormState _currentState = FormState.Browse;

        // 記憶體實體快取：保留 RowVersion 供存檔時進行併發比對
        //private Employee _currentEmployee;

        public EmployeePage()
        {
            InitializeComponent();
            // 💡 物理優化：強制開啟 Grid 的雙重緩衝，解決捲動與載入時的渲染延遲
            dgvEmployees.EnableDoubleBuffering(true);

            _empService = new EmployeeService();
            _geoService = new GeographyService();
            // 💡 初始化 BindingSource
            _bsEmployees = new BindingSource();
            _EmployeeBindingList = new ExtendedBindingList<Employee>();

            // 💡 綁定生命週期與按鈕事件 (統一於建構子掛載，確保執行順序)
            this.Load += EmployeePage_Load;
            // 💡 用 BindingSource 的 CurrentChanged 來監聽焦點轉移
            _bsEmployees.CurrentChanged += BsEmployees_CurrentChanged;

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
            // 💡 綁定搜尋事件
            txtKeyword.KeyDown += TxtKeyword_KeyDown;
            // 💡 綁定過濾條件切換事件：打勾或取消時，立刻重新撈取資料
            chkShowInactive.CheckedChanged += async (s, e) => await SearchDataAsync();
            // 💡 綁定 Grid 繪圖事件：用來處理停用資料的視覺特效
            dgvEmployees.CellFormatting += DgvEmployees_CellFormatting;
            // 💡 訂閱分頁器的廣播：當有人翻頁，我就去撈資料
            ucPagination.PageChanged += async (s, e) => await SearchDataAsync();

            cmbCity.SelectedIndexChanged += CmbCity_SelectedIndexChanged;
            cmbDistrict.SelectedIndexChanged += CmbDistrict_SelectedIndexChanged;
        }

        private async void EmployeePage_Load(object? sender, EventArgs e)
        {
            // =====================================================================
            // 🛡️ [防禦引擎 A] UI 啟動瞬間發動 RBAC 物理斷路
            // 向父類別 BasePage 註冊機敏按鈕，若 SessionContext 無權限，按鈕將物理消失
            // =====================================================================
            RequirePermission("ACT_CUST_ADD", btnAdd);
            RequirePermission("ACT_CUST_EDIT", btnEdit);
            // 不靠狀態機控制 
            cmbJobStatus.Enabled = SessionContext.HasPermission("ACT_EMP_EDIT");


            // 💡 綜合判定「存檔」與「取消」的物理可見性
            // 只要具備「新增」或「修改」任何一項權限，就讓文境按鈕顯示在畫面上，否則徹底隱藏
            bool canWrite = SessionContext.HasPermission("ACT_CUST_ADD") || SessionContext.HasPermission("ACT_CUST_EDIT");
            btnSave.Visible = canWrite;
            btnCancel.Visible = canWrite;

            // ⚙️ [靜態配置] 透過擴充方法，將 Enums 綁定至下拉選單
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

            // 💡 優先載入靜態地理字典，再載入員工業務資料，確保連動邏輯不拋錯
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
        // 🌍 [地理連動引擎] 縣市 -> 鄉鎮 -> 郵遞區號 O(1) 篩選
        // =====================================================================
        private async Task LoadGeographyDataAsync()
        {
            try
            {
                _cityList.Clear();
                _cityList.AddRange(await _geoService.GetActiveCitiesAsync());
                _allDistrictList.Clear();
                _allDistrictList.AddRange(await _geoService.GetAllActiveDistrictsAsync());

                cmbCity.SelectedIndexChanged -= CmbCity_SelectedIndexChanged; // 暫時脫鉤防連動報錯
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

            // 在本機記憶體中極速過濾該縣市的行政區，零資料庫 I/O
            var filteredDistricts = _allDistrictList
                .Where(d => d.CityID == cityId)
                .OrderBy(d => d.SortSeq)
                .ToList();

            cmbDistrict.SelectedIndexChanged -= CmbDistrict_SelectedIndexChanged;
            cmbDistrict.DataSource = filteredDistricts;
            cmbDistrict.DisplayMember = "DistrictName";
            cmbDistrict.ValueMember = "DistrictID";
            cmbDistrict.SelectedIndex = -1; // 強迫使用者重選
            cmbDistrict.SelectedIndexChanged += CmbDistrict_SelectedIndexChanged;

            txtZipFront.Clear();
        }

        private void CmbDistrict_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // 自動帶出該區的 3 碼官方郵遞區號 (填入唯讀欄位)
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
        // 支援 Enter 鍵檢索
        // =====================================================================
        private async void TxtKeyword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true; // 消除按下 Enter 時惱人的 Windows "叮" 警告音
                await SearchDataAsync();
            }
        }

        // =====================================================================
        // 🔄 [資料流引擎] 響應式綁定機制
        // =====================================================================
        private async Task SearchDataAsync()
        {
            string keyword = txtKeyword.Text.Trim();
            bool includeInactive = chkShowInactive.Checked;
            int? lastSelectedId = _bsEmployees.Current is Employee currentEmployee ? currentEmployee.EmployeeID : null;
            //  直接向分頁列要參數
            int pageSize = ucPagination.PageSize;
            int currentPage = ucPagination.CurrentPage;

            try
            {
                var result = await _empService.GetEmployeesAsync(currentPage, pageSize, includeInactive, keyword);

                // =====================================================================
                // 在原地重新計算頁碼並單獨重撈一次資料，保持執行流的絕對平整。
                // =====================================================================
                if (result.Items.Count == 0 && result.TotalCount > 0)
                {
                    // 計算正確的最後一頁
                    int correctLastPage = (int)Math.Ceiling((double)result.TotalCount / pageSize);
                    ucPagination.ForceCurrentPage(correctLastPage);

                    // 💡 物理防線：直接再打一次資料庫，不呼叫自己 (零遞迴)
                    result = await _empService.GetEmployeesAsync(correctLastPage, pageSize, includeInactive, keyword);
                }
                _bsEmployees.CurrentChanged -= BsEmployees_CurrentChanged; // 防觸發

                // 💡 透過 AddRange 極速批次更新綁定清單，不再破壞 DataSource 結構
                _EmployeeBindingList.Clear();
                _EmployeeBindingList.AddRange(result.Items);

                // 若無資料，清空明細
                if (_bsEmployees.Count > 0)
                {
                    // 1. 從「底層資料」尋找目標物件，而非走訪「UI 列」
                    var targetEmployee = _EmployeeBindingList.FirstOrDefault(c => c.EmployeeID == lastSelectedId);

                    // 2. 取得該物件在 BindingSource 中的索引值（若找不到則退回第 0 筆）
                    int targetIndex = targetEmployee != null ? _bsEmployees.IndexOf(targetEmployee) : 0;

                    // 3. 直接改變 BindingSource 的資料游標，UI (DataGridView) 會自動連動反白與轉移焦點
                    _bsEmployees.Position = targetIndex;

                    targetEmployee = (Employee)_bsEmployees.Current;

                    // 己解綁 _bsEmployees.CurrentChanged 己解綁，需手動更新明細
                    BindDetail(targetEmployee);
                }
                else
                {
                    ClearDetail();
                    SetUIState(_currentState);
                }

                _bsEmployees.CurrentChanged += BsEmployees_CurrentChanged; // 防觸發後重新綁定

                // 總筆數交給分頁列，自動計算總分頁並設定為預設狀態
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

            #region💡 [郵遞區號拆解引擎] 讀取時將 DB 的 VARCHAR(6) 虛擬拆為 3+3 顯示
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

            // 💡 [地理逆向推導] 透過 DistrictID 反查 CityID，確保連動下拉選單精確顯示
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
            // 💡 [視覺引擎] 狀態徽章 (Status Badge) 動態渲染
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

            // 💡 [審計軌跡渲染] 組合 4 個欄位，提供安靜且透明的內控資訊
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

            // 💡 重新觸發狀態機評估
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
            // 新增模式時，預設顯示為正常交易
            lblStatusBadge.Text = "✅ 狀態：正常登入 (新資料)";
            lblStatusBadge.ForeColor = System.Drawing.Color.Green;
        }
        // =====================================================================
        // 👁️ [視覺引擎] 停用資料的物理識別 (Visual Distinction)
        // =====================================================================
        private void DgvEmployees_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // 確保索引在合法範圍內
            if (e.RowIndex >= 0 && e.RowIndex < dgvEmployees.Rows.Count)
            {
                // 取得該列綁定的實體物件
                var Employee = dgvEmployees.Rows[e.RowIndex].DataBoundItem as Employee;

                // 若該員工已被停用 (IsActive == false)
                if (Employee != null && !Employee.IsActive && e.CellStyle != null)
                {
                    // 💡 字體顏色改為深灰色
                    e.CellStyle.ForeColor = System.Drawing.Color.DarkGray;

                    // 💡 加上物理刪除線 (Strikeout)，產生強烈的視覺斷層，警告使用者此為無效資料
                    e.CellStyle.Font = new System.Drawing.Font(dgvEmployees.Font, System.Drawing.FontStyle.Strikeout);
                }
            }
        }

        // =====================================================================
        // ⚙️ [狀態機引擎] UI 動態鎖定與解鎖管理
        // =====================================================================
        private void SetUIState(FormState state)
        {
            _currentState = state;
            bool isEditing = (state == FormState.Add || state == FormState.Edit);
            bool isBrowse = (state == FormState.Browse);
            var currentEmployee = _bsEmployees.Current as Employee;

            // 右側明細區解鎖/鎖定
            // 自動取號保持唯讀
            txtEmployeeNo.ReadOnly = true;

            // 永遠唯讀 (受行政區連動)
            txtZipFront.ReadOnly = true;
            // 其餘欄位依據 isEditing 切換
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

            // 搜尋區控制項
            txtKeyword.Enabled = !isEditing;
            btnSearch.Enabled = !isEditing;
            chkShowInactive.Enabled = !isEditing;

            // 左側清單防呆 (編輯時禁止切換資料)
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
            // 分頁列狀態切換
            ucPagination.SetUIState(state == FormState.Browse);
        }

        // =====================================================================
        // 💾 [交易引擎] 增刪改、資料合併與樂觀鎖防禦
        // =====================================================================
        private void BtnAdd_Click(object? sender, EventArgs e) => SetUIState(FormState.Add);
        private void BtnEdit_Click(object? sender, EventArgs e) => SetUIState(FormState.Edit);

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            SetUIState(FormState.Browse);
            // 放棄修改，將畫面復原為 Grid 中當前選取的實體狀態
            if (dgvEmployees.SelectedRows.Count > 0)
                BindDetail((Employee)dgvEmployees.SelectedRows[0].DataBoundItem);
            else { ClearDetail(); }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            // 1. 前端物理防呆、長度與邏輯攔截
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
            // 💡 1.2 物理防連點 (驗證通過了，準備開始漫長的存檔，這時才把按鈕鎖死)
            // =====================================================================
            btnSave.Enabled = false;
            btnCancel.Enabled = false;

            // =====================================================================
            // 💡 2. 郵遞區號合併引擎：確保 UI 的 3+3 精準轉化為 DB 的 VARCHAR(6)
            // =====================================================================
            string front = txtZipFront.Text.Trim();
            string rear = txtZipRear.Text.Trim();
            currentEmployee.CustomZipCode = string.IsNullOrEmpty(rear) ? front : front + rear;

            // 3. 將 UI 畫面資料推回記憶體實體 (DTO Mapping)
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

            // 寫入 ERP 應用層審計 (取出目前登入者的 EmployeeID)
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
                MessageBox.Show($"{actionName}成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (_currentState == FormState.Add) ucPagination.ResetToFirstPage();
                SetUIState(FormState.Browse);
                txtKeyword.Clear();
                await SearchDataAsync();
                _bsEmployees.LocateTo<Employee>(emp => emp.EmployeeNo == currentEmployee.EmployeeNo);
            }

            // 按鈕已為了保險關閉，確保離開時，若存檔失敗 (如檢核未過、SQL 例外)
            // ，狀態仍停留在 Add/Edit，則在此強制解鎖按鈕，確保使用者能修改資料後再次重試。
            if (_currentState == FormState.Add || _currentState == FormState.Edit)
            {
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
            }
        }

        private async void btnSearch_Click(object? sender, EventArgs e)
        {
            // 物理防呆：鎖定按鈕避免連點
            btnSearch.Enabled = false;
            if (string.IsNullOrWhiteSpace(txtKeyword.Text))
            {
                MessageBox.Show("請輸入有效的關鍵字！");
                // 解鎖按鈕
                btnSearch.Enabled = true;
                return;
            }
            try
            {
                ucPagination.ResetToFirstPage(); // 搜尋必須回到第一頁
                await SearchDataAsync();
            }
            finally
            {
                // 解鎖按鈕
                btnSearch.Enabled = true;
            }
        }

        // =====================================================================
        // 🔄 [重整引擎] 恢復初始狀態
        // 核心職責：清空關鍵字與 CheckBox，並防止事件連鎖觸發引發的 I/O 浪費
        // =====================================================================
        private async void btnRefresh_Click(object? sender, EventArgs e)
        {
            // 物理防呆：鎖定按鈕避免連點引發併發查詢
            btnRefresh.Enabled = false;

            try
            {
                // 1清空文字框
                txtKeyword.Clear();
                ucPagination.ResetToFirstPage(); // 重整必須回到第一頁
                // 呼叫核心資料流引擎，向資料庫要求最新、無過濾的乾淨資料
                await SearchDataAsync();
            }
            finally
            {
                // 解鎖按鈕
                btnRefresh.Enabled = true;
            }
        }
    }
}