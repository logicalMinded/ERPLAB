using ERPLAB.Models.Entities;
namespace ERPLAB.UI.Core
{
    /// <summary>
    /// 全域會話狀態管理中心 (Session Context)。
    /// 以靜態類別 (Static Class) 實作應用程式層級之全域快取 (Global Cache)，
    /// 集中管理當前登入者資訊、授權節點與權限集合，提供 RBAC 權限架構於前端介面 (UI) 之高效能驗證機制。
    /// </summary>
    public static class SessionContext
    {
        public static int CurrentAccountID { get; private set; }
        public static int CurrentEmployeeID { get; private set; }
        public static string Username { get; private set; } = string.Empty;

        // 登出生命週期旗標。供應用程式進入點 (Program.cs) 判斷是否需重新初始化並顯示登入視窗。
        public static bool IsLogoutRequested { get; set; } = false;

        // 授權節點快取。儲存使用者配置之系統節點集合，供主表單動態產製導覽選單與進行類別反射 (Reflection) 實例化使用。
        public static List<SystemNode> AuthorizedNodes { get; set; } = new List<SystemNode>();

        // =====================================================================
        // 權限檢核快取機制
        // 採用 HashSet<string> 資料結構儲存扁平化之權限代碼，
        // 確保權限檢核之時間複雜度為 O(1)，避免高頻 UI 渲染與動態綁定時產生效能瓶頸。
        // =====================================================================
        private static readonly HashSet<string> _authorizedPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 寫入登入狀態。
        /// 於系統身分驗證成功後，將使用者基本資料、權限代碼與節點清單載入全域記憶體快取。
        /// </summary>
        public static void Login(int accountId, int employeeId, string username, IEnumerable<string> permissions, List<SystemNode> nodes)
        {
            CurrentAccountID = accountId;
            CurrentEmployeeID = employeeId;
            Username = username;
            AuthorizedNodes = nodes;

            _authorizedPermissions.Clear();
            foreach (var p in permissions)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    _authorizedPermissions.Add(p);
                }
            }
        }

        /// <summary>
        /// 清除登入狀態。
        /// 執行系統登出程序時，徹底清空全域狀態與授權記憶體快取，確保帳號切換之安全性。
        /// </summary>
        public static void Logout()
        {
            CurrentAccountID = 0;
            CurrentEmployeeID = 0;
            Username = string.Empty;
            AuthorizedNodes.Clear();
            _authorizedPermissions.Clear();
        }

        /// <summary>
        /// 權限查核。
        /// 供 BasePage 基底類別及各業務表單呼叫，即時驗證當前使用者是否具備指定之操作權限，
        /// 作為前端介面執行控制項隱藏與唯讀鎖定之依據。
        /// </summary>
        public static bool HasPermission(string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode))
                return false;

            return _authorizedPermissions.Contains(permissionCode);
        }
    }
}