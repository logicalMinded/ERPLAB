using ERPLAB.Models.Enums;

namespace ERPLAB.Models.Entities
{
    // 💡 新增實作 INotifyPropertyChanged
    public class Customer : ISoftDeletable, IErpAuditable, IConcurrencyAware, ITaxPayable
    {
        public int CustomerID { get; set; }

        public string CustomerNo { get; set; } = string.Empty;

        // (省略其他未改變的欄位... 維持使用 public { get; set; } 即可，除非您希望它能在 Grid 即時連動)
        public string CustomerName { get; set; } = string.Empty;
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