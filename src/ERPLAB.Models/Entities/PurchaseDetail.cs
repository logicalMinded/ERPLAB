namespace ERPLAB.Models.Entities
{
    public class PurchaseDetail
    {
        public long PurchaseDID { get; set; }
        public long PurchaseID { get; set; }
        public int LineNo { get; set; }
        public int ProductID { get; set; }
        public decimal UnitPrice { get; set; } // 💡 此處的單價代表「進貨成本」
        public int Qty { get; set; }
        public string? Remark { get; set; }

        public string? ProductNo_Display { get; init; }
        public string? ProductName_Display { get; init; }
        public decimal SubTotal_Display => UnitPrice * Qty;
    }
}