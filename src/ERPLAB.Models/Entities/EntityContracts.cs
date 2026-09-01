namespace ERPLAB.Models.Entities
{
    public interface ISoftDeletable
    {
        bool IsActive { get; set; }
    }

    // ==========================================
    // 高併發防禦合約 (Optimistic Concurrency)
    // ==========================================
    /// <summary>
    /// 標示具備樂觀鎖機制之實體，嚴格對應 SQL Server 的 TIMESTAMP (ROWVERSION) 型別
    /// </summary>
    public interface IConcurrencyAware
    {
        byte[] RowVersion { get; set; }
    }

    // ==========================================
    // 資料庫底層審計 (DB Audit) 介面群
    // 💡 因應 UserRoles 等對照表僅有 Create，進行介面隔離
    // ==========================================
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

    // 供 Account, Roles 等主檔使用的組合介面
    public interface IDbAuditable : IDbCreateAuditable, IDbUpdateAuditable
    {
    }

    // ==========================================
    // ERP 應用層審計 (ERP Audit) 介面
    // 💡 業務主檔與單據皆為四欄位同進同出，保持單一介面
    // ==========================================
    public interface IErpAuditable
    {
        DateTime CreateTime { get; set; }
        int CreateUser { get; set; }
        DateTime UpdateTime { get; set; }
        int UpdateUser { get; set; }
    }

    // ==========================================
    // 業務特徵共用介面
    // ==========================================
    public interface ITaxPayable
    {
        string? TaxID { get; set; }
    }
}