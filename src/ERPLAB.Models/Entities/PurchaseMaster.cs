namespace ERPLAB.Models.Entities
{
    public class PurchaseMaster : IErpAuditable, IConcurrencyAware
    {
        public long PurchaseID { get; set; }
        public string PurchaseNo { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        public int VendorID { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Remark { get; set; }

        public byte Status { get; set; } = 1;

        public DateTime CreateTime { get; set; }
        public int CreateUser { get; set; }
        public DateTime UpdateTime { get; set; }
        public int UpdateUser { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // 💡 唯讀輔助屬性 (關聯廠商與員工)
        public string? VendorNo_Display { get; init; }
        public string? VendorName_Display { get; init; }
        public string? CreateUserNo_Display { get; init; }
        public string? UpdateUserNo_Display { get; init; }

        public void RecalculateTotalAmount(IEnumerable<PurchaseDetail> details)
        {
            this.TotalAmount = details?.Sum(d => d.UnitPrice * d.Qty) ?? 0m;
        }
    }
}