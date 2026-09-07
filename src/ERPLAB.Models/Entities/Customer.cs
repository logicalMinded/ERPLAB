using ERPLAB.Models.Enums;
namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 客戶基本檔實體 (Entity)。
    /// 映射資料庫 [Customer] 資料表，並透過多重介面實作系統共用規範：
    /// 包含邏輯刪除 (ISoftDeletable)、稽核軌跡 (IErpAuditable)、樂觀鎖控制 (IConcurrencyAware) 與稅籍約束驗證 (ITaxPayable)。
    /// </summary>
    public class Customer : ISoftDeletable, IErpAuditable, IConcurrencyAware, ITaxPayable
    {
        // 系統主鍵 (Primary Key)
        public int CustomerID { get; set; }

        // 業務編號 (Business Key)，建檔後通常不允許修改
        public string CustomerNo { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        // 統一編號，對應 ITaxPayable 介面以進行格式與邏輯驗證
        public string? TaxID { get; set; }

        public GenderType Gender { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public int DistrictID { get; set; }
        public string CustomZipCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Interests { get; set; }
        public string? Remark { get; set; }
        public string? ImageName { get; set; }

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
        // 實作 ISoftDeletable 介面：邏輯刪除 (Soft Delete) 標記
        // =====================================================================
        public bool IsActive { get; set; } = true;

        // =====================================================================
        // 擴充顯示屬性 (UI Display Properties)
        // 供 Repository 透過 JOIN 查詢帶出之關聯顯示資料。
        // 採用唯讀設計 (init-only setter)，確保資料自資料庫載入後不被非預期竄改。
        // =====================================================================
        public string? CreateUserNo_Display { get; init; }
        public string? UpdateUserNo_Display { get; init; }
    }
}