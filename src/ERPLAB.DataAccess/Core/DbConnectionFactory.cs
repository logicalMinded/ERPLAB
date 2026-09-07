using Microsoft.Data.SqlClient;
namespace ERPLAB.DataAccess.Core
{
    /// <summary>
    /// 資料庫連線工廠類別 (DbConnectionFactory)
    /// 負責集中建立與管理 SQL Server 資料庫連線，並統一配置連線池 (Connection Pool) 參數以最佳化資源運用。
    /// </summary>
    public static class DbConnectionFactory
    {
        // 說明：正式環境部署時，連線字串應抽離至組態設定檔 (如 appsettings.json 或 App.config)。
        // 預設配置連線池參數：Min Pool Size=5 用於維持基礎連線效能；Max Pool Size=100 限制最大連線數以避免資源耗盡。
        private const string ConnectionString = "Server=.\\SQL2022;Database=ERPLAB2026;Trusted_Connection=True;TrustServerCertificate=True;Min Pool Size=5;Max Pool Size=100;";

        /// <summary>
        /// 建立並開啟非同步的 SQL 資料庫連線。
        /// 適用於 I/O 密集型存取操作，可避免阻塞呼叫端的執行緒。
        /// </summary>
        public static async Task<SqlConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        /// <summary>
        /// 建立並開啟同步的 SQL 資料庫連線。
        /// </summary>
        public static SqlConnection GetConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }
    }
}