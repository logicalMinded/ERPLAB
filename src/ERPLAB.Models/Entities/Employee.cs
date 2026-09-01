using ERPLAB.Models.Enums;

namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 員工基本檔實體。
    /// 核心職責：純粹的資料承載物件 (POCO)，完美對應 SQL Server 的物理資料表結構。
    /// </summary>
    public class Employee : ISoftDeletable, IErpAuditable, IConcurrencyAware
    {
        public int EmployeeID { get; set; }

        // 業務鍵 (絕對唯一，不因離職而回收重複使用)
        public string EmployeeNo { get; set; } = string.Empty;

        public string EmployeeName { get; set; } = string.Empty;

        // =====================================================================
        // 💡 員工特有領域欄位 (Domain Specific Fields)
        // =====================================================================

        // 人事真實業務狀態 (0:離職, 1:留職停薪, 2:在職)
        public EmployeeJobStatus JobStatus { get; set; } = EmployeeJobStatus.Active;

        public string JobTitle { get; set; } = string.Empty;

        // 性別 (0:其他, 1:男, 2:女)
        public GenderType Gender { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        // =====================================================================
        // 🌍 擴充的地理與聯絡資訊 (與 Customer / Vendor 規格 100% 對齊)
        // =====================================================================
        public int DistrictID { get; set; }
        public string CustomZipCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Email { get; set; } // 允許 NULL，但由資料庫條件過濾確保不重複

        // =====================================================================
        // 🛡️ 實作防禦合約 (Contracts)
        // =====================================================================

        // 實作 IErpAuditable (由應用層寫入)
        public DateTime CreateTime { get; set; }
        public int CreateUser { get; set; }
        public DateTime UpdateTime { get; set; }
        public int UpdateUser { get; set; }

        // 實作 IConcurrencyAware (樂觀鎖)
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // 實作 ISoftDeletable (系統實體狀態，受 JobStatus 連動支配)
        public bool IsActive { get; set; } = true;

        // =====================================================================
        // 💡 [UI 唯讀輔助屬性] 供 DataGridView 與明細區顯示審計資訊使用
        // 使用 init 確保資料從 DAL 撈出後，在 UI 記憶體中絕對不可被竄改
        // =====================================================================
        public string? CreateUserNo_Display { get; init; }
        public string? UpdateUserNo_Display { get; init; }
    }
}