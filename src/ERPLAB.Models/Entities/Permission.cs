namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 系統權限實體 (Entity)。
    /// 映射資料庫 [Permissions] 資料表，定義系統內所有可被授權之功能節點或操作行為，
    /// 供 RBAC (Role-Based Access Control) 權限控制架構進行身分驗證與攔截使用。
    /// </summary>
    public class Permission : ISoftDeletable
    {
        // 權限代碼：作為自然主鍵 (Natural Key)，例如：SALES_ORDER_VOID。
        // 供前端介面與後端商業邏輯層進行權限比對之唯一識別碼。
        public string PermissionCode { get; set; } = string.Empty;

        // 權限顯示名稱：供使用者於授權設定介面中識別使用。
        public string PermissionName { get; set; } = string.Empty;

        // =====================================================================
        // 實作 ISoftDeletable 介面：全域權限之啟用/停用開關 (Kill Switch)。
        // 當設定為停用 (false) 時，將強制中斷該功能之所有授權存取，
        // 提供系統管理者於緊急情況下直接封鎖特定操作之能力。
        // =====================================================================
        public bool IsActive { get; set; } = true;
    }
}