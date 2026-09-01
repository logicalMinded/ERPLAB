using ERPLAB.UI.Core;

namespace ERPLAB.UI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 💡 建立企業級生命週期迴圈：支援無限次登出與重新登入
            while (true)
            {
                using (var loginForm = new LoginForm())
                {
                    // 若登入視窗不是回傳 OK (例如點擊 X 關閉)，則打破迴圈，徹底結束程式
                    if (loginForm.ShowDialog() != DialogResult.OK)
                    {
                        break;
                    }
                }

                // 登入成功，啟動主畫面 (透過 SessionContext 取出已快取的節點)
                // 這裡假設我們將 authorizedNodes 暫存在 LoginForm 或直接重新撈取，
                // 實務上建議透過靜態變數或建構子傳遞
                using (var mainForm = new MainForm())
                {
                    Application.Run(mainForm);
                }

                // 當主畫面關閉後，檢查是否為「登出」觸發。若是，則迴圈繼續，重新 new LoginForm()
                if (!SessionContext.IsLogoutRequested)
                {
                    break; // 若為正常關閉 (點擊 X)，則結束程式
                }

                // 重置狀態，準備迎接下一位登入者
                SessionContext.IsLogoutRequested = false;
            }
        }
    }
}