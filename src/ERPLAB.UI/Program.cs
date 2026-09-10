using ERPLAB.UI.Core;
namespace ERPLAB.UI
{
    /// <summary>
    /// 應用程式進入點 (Application Entry Point)。
    /// 負責配置 WinForms 視覺樣式，並控管系統登入與主框架之生命週期 (Lifecycle)。
    /// 實作登出重啟機制，允許使用者於不終止應用程式進程 (Process) 之情況下重新驗證身分。
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // =====================================================================
            // 應用程式生命週期迴圈 (Application Lifecycle Loop)
            // 透過迴圈控制登入視窗與主視窗之流轉，實作登出後重新載入登入介面之機制。
            // =====================================================================
            while (true)
            {
                using (var loginForm = new LoginForm())
                {
                    // 顯示登入對話方塊。若使用者取消登入或直接關閉視窗 (非 DialogResult.OK)，則中斷迴圈終止程式。
                    if (loginForm.ShowDialog() != DialogResult.OK)
                    {
                        break;
                    }
                }

                // 登入驗證成功，啟動系統主框架視窗。
                // Application.Run 將阻塞當前執行緒 (Thread)，直至主視窗關閉為止。
                using (var mainForm = new MainForm())
                {
                    Application.Run(mainForm);
                }

                // 評估主視窗關閉之原因：
                // 若全域狀態顯示並未觸發登出 (例如使用者點擊視窗右上角關閉按鈕)，則中斷迴圈，結束應用程式。
                if (!SessionContext.IsLogoutRequested)
                {
                    break;
                }

                // 若為登出觸發，則重置生命週期控制旗標，讓迴圈重新實例化 LoginForm 進入下一輪身分驗證。
                SessionContext.IsLogoutRequested = false;
            }
        }
    }
}