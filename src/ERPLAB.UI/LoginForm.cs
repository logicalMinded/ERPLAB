using ERPLAB.DataAccess.Repositories;
using ERPLAB.UI.Core;
namespace ERPLAB.UI
{
    /// <summary>
    /// 系統登入視窗。
    /// 負責處理使用者身分驗證 (Authentication) 與權限載入 (Authorization)。
    /// 採非同步架構 (Async/Await) 處理資料庫 I/O 與密碼雜湊運算，確保登入過程前端介面不卡頓。
    /// </summary>
    public partial class LoginForm : Form
    {
        private readonly AccountRepository _accountRepo;
        private readonly SystemNodeRepository _systemNodeRepo;

        public LoginForm()
        {
            InitializeComponent();
            _accountRepo = new AccountRepository();
            _systemNodeRepo = new SystemNodeRepository();
        }

        /// <summary>
        /// 登入驗證事件。
        /// 透過非同步呼叫底層服務，避免運算與連線阻塞 UI 執行緒 (UI Thread)。
        /// </summary>
        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("請輸入帳號與密碼。", "驗證提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 鎖定 UI 狀態，防範使用者重複點擊引發非同步併發請求
            btnLogin.Enabled = false;
            btnLogin.Text = "驗證中...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // 1. 呼叫資料存取層 (DAL) 進行身分驗證
                // (底層密碼 PBKDF2 雜湊運算已封裝於背景執行緒處理)
                var (isSuccess, accountData, message) = await _accountRepo.VerifyLoginAsync(username, password);

                if (!isSuccess || accountData == null)
                {
                    MessageBox.Show(message, "登入失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 2. 驗證成功後，載入該使用者具備權限之系統節點與功能清單
                var authorizedNodes = await _systemNodeRepo.GetAuthorizedNodesAsync(accountData.AccountID);

                // 提取有效之權限代碼 (PermissionCode) 集合
                var permissions = authorizedNodes
                    .Where(n => !string.IsNullOrWhiteSpace(n.PermissionCode))
                    .Select(n => n.PermissionCode!)
                    .ToList();

                // 3. 將使用者資訊與權限集合寫入全域狀態快取 (SessionContext)
                SessionContext.Login(
                    accountData.AccountID,
                    accountData.EmployeeID,
                    accountData.Username,
                    permissions,
                    authorizedNodes);

                // 4. 設定對話方塊回傳值並關閉視窗，交由應用程式進入點 (Program.cs) 接手啟動主畫面
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                // 攔截未預期之系統或網路例外
                MessageBox.Show($"系統發生不可預期之例外：\n{ex.Message}", "系統異常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 確保登入結束後 (無論成功或失敗)，確實復原 UI 控制項狀態與游標樣式
                btnLogin.Enabled = true;
                btnLogin.Text = "登入";
                this.Cursor = Cursors.Default;
            }
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // 攔截 Enter 鍵預設行為，消除系統警告音效
                e.SuppressKeyPress = true;

                // 實作按 Enter 鍵切換輸入焦點之功能，依據控制項之 TabIndex 順序自動切換至下一個元件
                // 參數說明：(目前控制項, 是否往前尋找, 是否停留在同一個容器, 是否跳過未啟用的控制項, 是否循環尋找)
                this.SelectNextControl(this.ActiveControl, true, true, true, true);
            }
        }
    }
}