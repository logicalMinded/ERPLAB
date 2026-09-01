namespace ERPLAB.Models.Entities
{
    public class Product : ISoftDeletable, IErpAuditable, IConcurrencyAware
    {
        public int ProductID { get; set; }
        public string ProductNo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        // 💡 高精度財務欄位 (對應 DECIMAL(18,2))
        public decimal PurchasePrice { get; set; }
        public decimal SalesPrice { get; set; }

        public decimal MovingAverageCost { get; set; }

        // 🚨 庫存快取：UI 端必須設為 ReadOnly，僅允許單據過帳時由 SQL Server 異動
        public int CurrentStock { get; set; }

        public string? Description { get; set; }
        public string? ImageName { get; set; }
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