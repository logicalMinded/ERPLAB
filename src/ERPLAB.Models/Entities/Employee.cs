using ERPLAB.Models.Enums;
namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 員工基本檔實體 (Entity)。
    /// 映射資料庫 [Employee] 資料表，並透過介面實作系統共用規範：
    /// 包含邏輯刪除 (ISoftDeletable)、稽核軌跡 (IErpAuditable) 與樂觀鎖控制 (IConcurrencyAware)。
    /// </summary>
    public class Employee : ISoftDeletable, IErpAuditable, IConcurrencyAware
    {
        // 系統主鍵 (Primary Key)
        public int EmployeeID { get; set; }

        // 業務編號 (Business Key)，具備全域唯一性，人員離職後亦不回收重複使用
        public string EmployeeNo { get; set; } = string.Empty;

        public string EmployeeName { get; set; } = string.Empty;

        // =====================================================================
        // 人事業務屬性
        // =====================================================================

        // 實際人事狀態 (例如：0:離職, 1:留職停薪, 2:在職)
        public EmployeeJobStatus JobStatus { get; set; } = EmployeeJobStatus.Active;

        public string JobTitle { get; set; } = string.Empty;

        public GenderType Gender { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        // =====================================================================
        // 地理與聯絡資訊
        // =====================================================================
        public int DistrictID { get; set; }
        public string CustomZipCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // 允許為 Null，並由資料庫 Unique Filtered Index 確保非空值之唯一性
        public string? Email { get; set; }

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
        // 實作 ISoftDeletable 介面：邏輯刪除 (Soft Delete) 標記。
        // 實務上，員工之系統權限與啟用狀態將受人事狀態 (JobStatus) 異動連動支配。
        // =====================================================================
        public bool IsActive { get; set; } = true;

        // =====================================================================
        // 擴充顯示屬性 (UI Display Properties)
        // 供 Repository 透過 JOIN 查詢帶出之關聯顯示資料。
        // 採用唯讀設計 (init-only setter)，確保資料自資料庫載入後之不可變性 (Immutability)。
        // =====================================================================
        public string? CreateUserNo_Display { get; init; }
        public string? UpdateUserNo_Display { get; init; }
    }
}