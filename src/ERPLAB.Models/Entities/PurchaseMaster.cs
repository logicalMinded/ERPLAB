namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 進貨單主檔實體 (Entity)。
    /// 映射資料庫 [PurchaseMaster] 資料表，採 Master-Detail 架構設計。
    /// 實作系統共用之稽核軌跡 (IErpAuditable) 與樂觀鎖控制 (IConcurrencyAware)，確保交易單據之資料完整性與併發安全。
    /// </summary>
    public class PurchaseMaster : IErpAuditable, IConcurrencyAware
    {
        // 系統主鍵 (Primary Key)
        public long PurchaseID { get; set; }

        // 業務編號 (Business Key)，由系統取號引擎自動配發
        public string PurchaseNo { get; set; } = string.Empty;

        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        // 關聯至廠商主檔之外鍵 (Foreign Key)
        public int VendorID { get; set; }

        // 單據總金額 (由進貨明細加總計算而得)
        public decimal TotalAmount { get; set; }

        public string? Remark { get; set; }

        // 單據狀態 (例如：1 = 草稿/未過帳，2 = 審核過帳，3 = 作廢)
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
        // 供 Repository 透過 JOIN 查詢帶出之關聯廠商與員工顯示資料。
        // 採用唯讀設計 (init-only setter)，確保資料自資料庫載入後之不可變性 (Immutability)。
        // =====================================================================
        public string? VendorNo_Display { get; init; }
        public string? VendorName_Display { get; init; }
        public string? CreateUserNo_Display { get; init; }
        public string? UpdateUserNo_Display { get; init; }

        /// <summary>
        /// 重新計算單據總金額。
        /// 領域驅動設計 (DDD) 概念應用：將總金額計算之商業邏輯封裝於實體內部，
        /// 確保主檔總金額與明細小計之資料一致性，避免外部不當賦值。
        /// </summary>
        public void RecalculateTotalAmount(IEnumerable<PurchaseDetail> details)
        {
            this.TotalAmount = details?.Sum(d => d.UnitPrice * d.Qty) ?? 0m;
        }
    }
}