namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 邏輯刪除 (Soft Delete) 介面。
    /// 供支援停用與啟用狀態之實體實作，以避免物理刪除 (Hard Delete) 破壞資料庫之歷史關聯完整性。
    /// </summary>
    public interface ISoftDeletable
    {
        bool IsActive { get; set; }
    }

    // =====================================================================
    // 併發控制合約 (Concurrency Control Contracts)
    // =====================================================================

    /// <summary>
    /// 樂觀鎖 (Optimistic Concurrency) 介面。
    /// 精確映射 SQL Server 之 ROWVERSION (TIMESTAMP) 型別，提供高併發環境下資料異動衝突之防禦機制。
    /// </summary>
    public interface IConcurrencyAware
    {
        byte[] RowVersion { get; set; }
    }

    // =====================================================================
    // 資料庫層級稽核 (Database Audit) 介面
    // 依循介面隔離原則 (Interface Segregation Principle, ISP)，將建立與更新職責分離，
    // 以支援僅具備寫入特性之資料表 (如多對多關聯之對照表)。
    // =====================================================================
    public interface IDbCreateAuditable
    {
        DateTime DbCreateTime { get; set; }
        string DbCreateUser { get; set; }
    }

    public interface IDbUpdateAuditable
    {
        DateTime DbUpdateTime { get; set; }
        string DbUpdateUser { get; set; }
    }

    /// <summary>
    /// 完整資料庫稽核介面。
    /// 供系統層級主檔 (如 Account, Role) 實作，由底層資料庫 Trigger 或 Default Constraints 寫入。
    /// </summary>
    public interface IDbAuditable : IDbCreateAuditable, IDbUpdateAuditable
    {
    }

    // =====================================================================
    // 應用層級稽核 (Application Audit) 介面
    // =====================================================================

    /// <summary>
    /// 系統業務稽核軌跡 (Audit Trail) 介面。
    /// 規範業務主檔與交易單據必須具備之追蹤欄位，並由應用程式層級負責寫入操作者 ID 與時間。
    /// </summary>
    public interface IErpAuditable
    {
        DateTime CreateTime { get; set; }
        int CreateUser { get; set; }
        DateTime UpdateTime { get; set; }
        int UpdateUser { get; set; }
    }

    // =====================================================================
    // 業務特徵共用介面 (Domain Feature Contracts)
    // =====================================================================

    /// <summary>
    /// 稅籍約束介面。
    /// 供具備統一編號之實體 (如廠商、客戶) 實作，以利進行跨實體的共用格式驗證與邏輯檢核。
    /// </summary>
    public interface ITaxPayable
    {
        string? TaxID { get; set; }
    }
}