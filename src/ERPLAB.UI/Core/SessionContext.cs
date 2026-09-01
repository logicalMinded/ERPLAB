using ERPLAB.Models.Entities;

namespace ERPLAB.UI.Core
{
    /// <summary>
    /// 全域會話狀態管理中心 (Singleton)
    /// </summary>
    public static class SessionContext
    {
        public static int CurrentAccountID { get; private set; }
        public static int CurrentEmployeeID { get; private set; }
        public static string Username { get; private set; } = string.Empty;

        // 💡 登出生命週期控制旗標，供 Program.cs 判定是否重啟 LoginForm
        public static bool IsLogoutRequested { get; set; } = false;

        // 💡 暫存登入時撈出的授權節點，供 MainForm 生成動態選單與反射掛載使用
        public static List<SystemNode> AuthorizedNodes { get; set; } = new List<SystemNode>();

        // 💡 核心防線：使用 HashSet 達成 O(1) 的物理極速權限查核，避免高頻 UI 渲染卡頓
        private static readonly HashSet<string> _authorizedPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 登入成功時寫入全域狀態快取
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
        /// 登出時徹底清空機敏記憶體快取
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
        /// 權限查核引擎：供 BasePage 與各業務表單即時驗證控制項是否應予顯示
        /// </summary>
        public static bool HasPermission(string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode))
                return false;

            return _authorizedPermissions.Contains(permissionCode);
        }
    }
}