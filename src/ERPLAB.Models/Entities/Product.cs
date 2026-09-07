namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 商品基本檔實體 (Entity)。
    /// 映射資料庫 [Product] 資料表，並透過介面實作系統共用規範：
    /// 包含邏輯刪除 (ISoftDeletable)、稽核軌跡 (IErpAuditable) 與樂觀鎖控制 (IConcurrencyAware)。
    /// </summary>
    public class Product : ISoftDeletable, IErpAuditable, IConcurrencyAware
    {
        // 系統主鍵 (Primary Key)
        public int ProductID { get; set; }

        // 業務編號 (Business Key)，建檔後通常不允許修改
        public string ProductNo { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        // =====================================================================
        // 財務與庫存屬性
        // =====================================================================

        public decimal PurchasePrice { get; set; }
        public decimal SalesPrice { get; set; }

        // 移動平均成本 (Moving Average Cost)。
        // 供財務報表盤盈虧與毛利計算使用。此數值由進銷存單據過帳 (Posting) 或作廢時動態重算，不允許手動竄改。
        public decimal MovingAverageCost { get; set; }

        // 當前實體庫存數量。
        // 基於嚴格內控原則，此欄位嚴禁透過商品維護介面直接異動，必須完全由交易單據層級發動庫存增減。
        public int CurrentStock { get; set; }

        public string? Description { get; set; }
        public string? ImageName { get; set; }
        public string? Remark { get; set; }

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
        // 採用唯讀設計 (init-only setter)，確保資料自資料庫載入後之不可變性。
        // =====================================================================
        public string? CreateUserNo_Display { get; init; }
        public string? UpdateUserNo_Display { get; init; }
    }
}