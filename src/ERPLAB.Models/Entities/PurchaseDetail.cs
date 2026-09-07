namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 進貨單明細實體 (Entity)。
    /// 映射資料庫 [PurchaseDetail] 資料表。負責承載單筆進貨項目的數量與成本資訊，
    /// 於單據過帳 (Posting) 時，此處之數據將作為實體庫存增加與移動平均成本 (Moving Average Cost) 重新運算之依據。
    /// </summary>
    public class PurchaseDetail
    {
        // 系統主鍵 (Primary Key)
        public long PurchaseDID { get; set; }

        // 關聯至進貨單主檔之外鍵 (Foreign Key)
        public long PurchaseID { get; set; }

        // 明細行號，控制單據明細之呈現順序
        public int LineNo { get; set; }

        public int ProductID { get; set; }

        // 實際進貨單價 (進貨成本)
        public decimal UnitPrice { get; set; }

        public int Qty { get; set; }

        public string? Remark { get; set; }

        // =====================================================================
        // 擴充顯示屬性 (UI Display Properties)
        // 供 Repository 透過 JOIN 查詢帶出之關聯顯示資料。
        // 採用唯讀設計 (init-only setter)，確保資料自資料庫載入後之不可變性 (Immutability)。
        // =====================================================================
        public string? ProductNo_Display { get; init; }
        public string? ProductName_Display { get; init; }

        // =====================================================================
        // 唯讀運算屬性 (Computed Properties)
        // 明細小計金額。供前端介面即時綁定顯示，不參與實體資料庫儲存。
        // =====================================================================
        public decimal SubTotal_Display => UnitPrice * Qty;
    }
}