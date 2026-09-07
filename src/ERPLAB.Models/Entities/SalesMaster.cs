namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 銷貨單主檔實體 (Entity)。
    /// 映射資料庫 [SalesMaster] 資料表，採 Master-Detail 架構設計。
    /// 交易單據生命週期捨棄邏輯刪除 (Soft Delete)，全面改由狀態機 (State Machine) 控管，確保帳務與庫存稽核軌跡之完整性。
    /// </summary>
    public class SalesMaster : IErpAuditable, IConcurrencyAware
    {
        // 系統主鍵 (Primary Key)
        public long SalesID { get; set; }

        // 業務編號 (Business Key)，由系統取號引擎自動配發
        public string SalesNo { get; set; } = string.Empty;

        public DateTime SalesDate { get; set; } = DateTime.Now;

        // 送貨地理資訊與聯絡地址
        public int ShipDistrictID { get; set; }
        public string ShipZipCode { get; set; } = string.Empty;
        public string ShipAddress { get; set; } = string.Empty;

        // 關聯至客戶主檔之外鍵 (Foreign Key)
        public int CustomerID { get; set; }

        // 單據總金額 (反正規化欄位)。
        // 基於後端零信任原則 (Zero Trust)，此數值必須由後端依據明細重新計算，嚴禁直接信任前端傳入之數值。
        public decimal TotalAmount { get; set; }

        public string? Remark { get; set; }

        // =====================================================================
        // 交易單據狀態機 (State Machine) 定義：
        // 1 = 草稿/未過帳 (Draft)
        // 2 = 已審核過帳 (Posted) - 觸發扣減庫存與成本快照
        // 3 = 已註銷 (Canceled) - 針對未過帳之草稿進行作廢
        // 4 = 已作廢 (Voided) - 針對已過帳單據進行庫存與帳務沖銷
        // =====================================================================
        public byte Status { get; set; } = 1;

        // =====================================================================
        // 實作 IErpAuditable 介面：系統統一之稽核軌跡 (Audit Trail)
        // =====================================================================
        public DateTime CreateTime { get; set; }
        public int CreateUser { get; set; }
        public DateTime UpdateTime { get; set; }
        public int UpdateUser { get; set; }

        // =====================================================================
        // 實作 IConcurrencyAware 介面：樂觀鎖 (Optimistic Concurrency) 防禦標記
        // =====================================================================
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        #region 擴充顯示屬性 (UI Display Properties)
        // 供 Repository 透過 JOIN 查詢帶出之關聯客戶與員工顯示資料。
        // 採用唯讀設計 (init-only setter)，確保資料自資料庫載入後之不可變性 (Immutability)。
        public string? CustomerNo_Display { get; init; }
        public string? CustomerName_Display { get; init; }
        public string? CreateUserNo_Display { get; init; }
        public string? UpdateUserNo_Display { get; init; }
        #endregion

        /// <summary>
        /// 重新計算單據總金額。
        /// 領域驅動設計 (DDD) 概念應用：將總金額計算之商業邏輯封裝於實體內部，
        /// 確保主檔總金額與明細小計之資料一致性，避免外部不當賦值。
        /// </summary>
        public void RecalculateTotalAmount(IEnumerable<SalesDetail> details)
        {
            this.TotalAmount = details.Sum(d => d.UnitPrice * d.Qty);
        }
    }
}