namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 銷貨明細實體。
    /// 💡 物理特性：極簡 POCO，生命週期完全由主檔與 TVP 引擎接管。
    /// </summary>
    public class SalesDetail
    {
        public long SalesDID { get; set; }
        public long SalesID { get; set; }
        public int LineNo { get; set; }
        public int ProductID { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public int Qty { get; set; }
        public string? Remark { get; set; }

        // =====================================================================
        // 💡 [UI 唯讀輔助屬性] 供 Grid 顯示商品名稱與單列小計 (不寫入此張 DB 表)
        // =====================================================================
        public string? ProductNo_Display { get; init; }
        public string? ProductName_Display { get; init; }

        // C# 記憶體即時運算屬性，方便 UI 直接綁定顯示小計
        public decimal SubTotal_Display => UnitPrice * Qty;
    }
}