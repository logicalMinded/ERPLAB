namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 銷貨主檔實體。
    /// 💡 物理特性：廢除 IsActive 軟刪除，全權由 4 維狀態機 (Status) 控管生命週期。
    /// </summary>
    public class SalesMaster : IErpAuditable, IConcurrencyAware
    {
        public long SalesID { get; set; } // 單據流水號為 BIGINT
        public string SalesNo { get; set; } = string.Empty;
        public DateTime SalesDate { get; set; } = DateTime.Now;
        public int ShipDistrictID { get; set; }
        public string ShipZipCode { get; set; } = string.Empty;
        public string ShipAddress { get; set; } = string.Empty;

        public int CustomerID { get; set; }

        // 反正規化快取 (由後端重算，不信任前端)
        public decimal TotalAmount { get; set; }
        public string? Remark { get; set; }

        // =====================================================================
        // 💡 4 維狀態機核心：
        // 1=未過帳(草稿), 2=已過帳(唯讀/扣庫存), 3=已註銷(未過帳作廢), 4=已作廢(已過帳沖銷)
        // =====================================================================
        public byte Status { get; set; } = 1;

        // 實作 IErpAuditable
        public DateTime CreateTime { get; set; }
        public int CreateUser { get; set; }
        public DateTime UpdateTime { get; set; }
        public int UpdateUser { get; set; }

        // 實作 IConcurrencyAware (樂觀鎖)
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        #region [UI 唯讀輔助屬性] 供 DataGridView 顯示使用 (不寫入 DB)
        public string? CustomerNo_Display { get; init; }
        public string? CustomerName_Display { get; init; }
        public string? CreateUserNo_Display { get; init; }
        public string? UpdateUserNo_Display { get; init; }
        #endregion

        // 💡 封裝商業邏輯：提供一個明確的方法，讓外部把明細丟進來，由主檔自己算總和 - 微充血
        public void RecalculateTotalAmount(IEnumerable<SalesDetail> details)
        {
            this.TotalAmount = details.Sum(d => d.UnitPrice * d.Qty);
        }
    }
}