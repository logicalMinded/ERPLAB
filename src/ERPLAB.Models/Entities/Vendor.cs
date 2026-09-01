namespace ERPLAB.Models.Entities
{
    public class Vendor : ISoftDeletable, IErpAuditable, IConcurrencyAware, ITaxPayable
    {
        public int VendorID { get; set; }
        public string VendorNo { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string? TaxID { get; set; }
        public string ContactPerson { get; set; } = string.Empty; // 💡 廠商特有欄位
        public string PhoneNumber { get; set; } = string.Empty;
        public int DistrictID { get; set; }
        public string CustomZipCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Remark { get; set; }

        public DateTime CreateTime { get; set; }
        public int CreateUser { get; set; }
        public DateTime UpdateTime { get; set; }
        public int UpdateUser { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public bool IsActive { get; set; } = true;

        // 顯示審計資訊用
        public string? CreateUserNo_Display { get; init; }
        public string? UpdateUserNo_Display { get; init; }
    }
}