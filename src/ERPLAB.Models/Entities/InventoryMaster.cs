//using ERPLAB.Models.Entities.Contracts;namespace ERPLAB.Models.Entities
using ERPLAB.Models.Entities;

{
    /// <summary>
    /// 庫存盤點主檔實體 (Entity)。
    /// 映射資料庫 [InventoryMaster] 資料表。盤點單屬核心交易單據，故無邏輯刪除 (Soft Delete) 設計；
    /// 草稿階段採實體刪除 (Hard Delete)，單據審核過帳後受資料庫 Trigger 約束保護，確保財務與庫存稽核之不可變性 (Immutability)。
    /// </summary>
    public class InventoryMaster : IErpAuditable, IConcurrencyAware
{
    // 系統主鍵 (Primary Key)
    public long InventoryID { get; set; }

    // 業務編號 (Business Key)，由系統共用取號引擎自動配發
    public string InventoryNo { get; set; } = string.Empty;

    public DateTime InventoryDate { get; set; } = DateTime.Now;

    // 關聯至負責盤點之員工主檔外鍵 (Foreign Key)
    public int EmployeeID { get; set; }

    public string? Remark { get; set; }

    // 單據狀態 (例如：1 = 草稿/盤點中，2 = 審核過帳)
    public byte Status { get; set; } = 1;

    // =====================================================================
    // 實作 IErpAuditable 介面：系統統一之稽核軌跡 (Audit Trail)
    // =====================================================================
    public DateTime CreateTime { get; set; }
    public int CreateUser { get; set; }
    public DateTime UpdateTime { get; set; }
    public int UpdateUser { get; set; }

    // =====================================================================
    // 實作 IConcurrencyAware 介面：樂觀鎖 (Optimistic Concurrency) 防禦標記
    // =====================================================================
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // =====================================================================
    // 擴充顯示屬性 (UI Display Properties)
    // 不參與實體資料庫寫入，專供 Repository 透過 JOIN 查詢帶出之關聯顯示資料。
    // 採用唯讀設計 (init-only setter)，確保資料自資料庫載入後之不可變性。
    // =====================================================================
    public string? EmployeeNo_Display { get; init; }
    public string? EmployeeName_Display { get; init; }
    public string? CreateUserNo_Display { get; init; }
    public string? UpdateUserNo_Display { get; init; }
}
}