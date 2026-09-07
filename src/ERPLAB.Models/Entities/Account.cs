namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 系統帳號實體 (Entity)。
    /// 映射資料庫 [Accounts] 資料表，封裝帳號登入狀態與安全防護欄位，
    /// 並透過介面實作統一規範稽核軌跡 (Audit Trail)、邏輯刪除 (Soft Delete) 與樂觀鎖控制機制。
    /// </summary>
    public class Account : ISoftDeletable, IDbAuditable, IConcurrencyAware
    {
        public int AccountID { get; set; }

        // 關聯至員工主檔之外鍵 (Foreign Key)，確保人事資料與系統帳號之資料一致性
        public int EmployeeID { get; set; }

        public string Username { get; set; } = string.Empty;

        // 儲存相容於 ASP.NET Core Identity V3 規範之 Base64 密碼雜湊值 (內含 Salt 與 PBKDF2 參數)
        public string PasswordHash { get; set; } = string.Empty;

        // 登入安全防護機制：記錄連續登入失敗次數與實體鎖定狀態，防範暴力破解
        public bool IsLocked { get; set; }
        public byte FailedCount { get; set; }

        public DateTime? LastLogin { get; set; }

        // =====================================================================
        // 實作 IDbAuditable 介面：由底層資料庫 (Default Constraints / Trigger) 自動維護之稽核軌跡
        // =====================================================================
        public DateTime DbCreateTime { get; set; }
        public string DbCreateUser { get; set; } = string.Empty;
        public DateTime DbUpdateTime { get; set; }
        public string DbUpdateUser { get; set; } = string.Empty;

        // =====================================================================
        // 實作 ISoftDeletable 介面：系統統一之邏輯刪除 (Soft Delete) 與權限停用標記
        // =====================================================================
        public bool IsActive { get; set; } = true;

        // =====================================================================
        // 實作 IConcurrencyAware 介面：樂觀鎖 (Optimistic Concurrency) 防禦標記
        // 精確映射 SQL Server 之 ROWVERSION (TIMESTAMP) 欄位，供 ADO.NET 寫入時進行併發驗證
        // =====================================================================
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}