namespace ERPLAB.Models.Entities
{
    public class Account : ISoftDeletable, IDbAuditable, IConcurrencyAware
    {
        public int AccountID { get; set; }

        // 關聯員工主檔的強外鍵
        public int EmployeeID { get; set; }

        // 系統登入帳號 (VARCHAR 50)
        public string Username { get; set; } = string.Empty;

        // 嚴格對應 VARBINARY(64) 的密碼與動態鹽值
        public string PasswordHash { get; set; } = string.Empty;

        // 帳號鎖定狀態與錯誤計數
        public bool IsLocked { get; set; }
        public byte FailedCount { get; set; } // 對應資料庫 TINYINT

        public DateTime? LastLogin { get; set; }

        // --- 實作 IDbAuditable 介面 (資料庫連線層審計) ---
        public DateTime DbCreateTime { get; set; }
        public string DbCreateUser { get; set; } = string.Empty;
        public DateTime DbUpdateTime { get; set; }
        public string DbUpdateUser { get; set; } = string.Empty;

        // --- 實作 ISoftDeletable 介面 (全系統統一軟刪除線) ---
        public bool IsActive { get; set; } = true;

        // --- 實作 IConcurrencyAware 介面，宣告 RowVersion 屬性
        // 對應 SQL Server 的 TIMESTAMP 欄位，專供 ADO.NET 更新時作為樂觀鎖比對參數
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}