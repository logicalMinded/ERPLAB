namespace ERPLAB.Models.Entities
{
    public class Permission : ISoftDeletable
    {
        // 自然實體主鍵 (VARCHAR 100)，如 SALES_ORDER_VOID
        public string PermissionCode { get; set; } = string.Empty;

        // 權限語意描述 (NVARCHAR 50)
        public string PermissionName { get; set; } = string.Empty;

        // --- 實作 ISoftDeletable 介面 (0:斷路封鎖, 1:正常授權) ---
        public bool IsActive { get; set; } = true;
    }
}