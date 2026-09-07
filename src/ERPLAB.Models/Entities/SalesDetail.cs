namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 銷貨單明細實體 (Entity)。
    /// 映射資料庫 [SalesDetail] 資料表。作為 Master-Detail 架構之子表，承載單筆銷貨項目資訊。
    /// 其資料生命週期 (寫入與抹除) 完全由主檔交易與 TVP (Table-Valued Parameter) 批次引擎統一控管。
    /// </summary>
    public class SalesDetail
    {
        // 系統主鍵 (Primary Key)
        public long SalesDID { get; set; }

        // 關聯至銷貨單主檔之外鍵 (Foreign Key)
        public long SalesID { get; set; }

        // 明細行號，控制單據明細之呈現順序
        public int LineNo { get; set; }

        public int ProductID { get; set; }

        // 實際銷貨單價
        public decimal UnitPrice { get; set; }

        // 銷貨過帳當下之移動平均成本 (Unit Cost) 快照，供後續銷售毛利之歷史追溯與財報結算使用
        public decimal UnitCost { get; set; }

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