//using ERPLAB.Models.Entities.Contracts;
namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 盤點主檔實體。
    /// 💡 物理特性：無 IsActive 欄位。草稿採物理刪除 (Hard Delete)；過帳後受 Trigger 實體鎖死保護。
    /// </summary>
    public class InventoryMaster : IErpAuditable, IConcurrencyAware
    {
        public long InventoryID { get; set; }
        public string InventoryNo { get; set; } = string.Empty;
        public DateTime InventoryDate { get; set; } = DateTime.Now;

        // 負責盤點的員工
        public int EmployeeID { get; set; }
        public string? Remark { get; set; }

        // 1=盤點中(草稿), 2=已確認過帳
        public byte Status { get; set; } = 1;

        public DateTime CreateTime { get; set; }
        public int CreateUser { get; set; }
        public DateTime UpdateTime { get; set; }
        public int UpdateUser { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // =====================================================================
        // 💡 [UI 唯讀輔助屬性] (不寫入 DB)
        // =====================================================================
        public string? EmployeeNo_Display { get; init; }
        public string? EmployeeName_Display { get; init; }
        public string? CreateUserNo_Display { get; init; }
        public string? UpdateUserNo_Display { get; init; }
    }
}