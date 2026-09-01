using ERPLAB.DataAccess.Repositories;
using ERPLAB.UI.Core;

namespace ERPLAB.UI
{
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
        /// 登入事件 (支援非同步 I/O 與 CPU 算力下放)
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

            // 💡 鎖定 UI：物理防止雙擊導致的併發發送
            btnLogin.Enabled = false;
            btnLogin.Text = "驗證中...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // 1. 呼叫 DAL 驗證引擎 (底層 PBKDF2 算力已被 Task.Run 隔離)
                var (isSuccess, accountData, message) = await _accountRepo.VerifyLoginAsync(username, password);

                if (!isSuccess || accountData == null)
                {
                    MessageBox.Show(message, "登入失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 2. 登入成功：立刻撈取該使用者授權的「系統節點與權限清單」
                var authorizedNodes = await _systemNodeRepo.GetAuthorizedNodesAsync(accountData.AccountID);

                // 從節點中萃取出不為空的 PermissionCode 集合
                var permissions = authorizedNodes
                    .Where(n => !string.IsNullOrWhiteSpace(n.PermissionCode))
                    .Select(n => n.PermissionCode!)
                    .ToList();

                // 3. 寫入全域狀態快取
                SessionContext.Login(
                    accountData.AccountID,
                    accountData.EmployeeID,
                    accountData.Username,
                    permissions,
                    authorizedNodes);

                // 4. 設定對話框結果並關閉自身，交由 Program.cs 啟動主畫面
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"系統發生不可預期之例外：\n{ex.Message}", "系統崩潰防禦", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 確保 UI 狀態復原
                btnLogin.Enabled = true;
                btnLogin.Text = "登入";
                this.Cursor = Cursors.Default;
            }
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // 阻止系統發出 "叮" 的錯誤警告音
                e.SuppressKeyPress = true;

                // 依照控制項的 TabIndex 順序，自動尋找並將焦點移動到下一個控制項
                // 參數：(目前控制項, 是否往前尋找, 是否停留在同一個容器, 是否跳過未啟用的控制項, 是否循環尋找)
                this.SelectNextControl(this.ActiveControl, true, true, true, true);
            }
        }
    }
}