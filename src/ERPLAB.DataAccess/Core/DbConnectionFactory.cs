using Microsoft.Data.SqlClient;

namespace ERPLAB.DataAccess.Core
{
    public static class DbConnectionFactory
    {
        // 💡 階段七部署時將移至 App.config，此處預先配置連線池參數防禦高併發耗盡執行緒
        // Min Pool Size=5 保留基礎連線；Max Pool Size=100 限制連線上限
        private const string ConnectionString = "Server=.\\SQL2022;Database=ERPLAB2026;Trusted_Connection=True;TrustServerCertificate=True;Min Pool Size=5;Max Pool Size=100;";

        /// <summary>
        /// 取得並開啟新的 SQL 非同步連線 (推薦使用，避免卡死 UI)
        /// </summary>
        public static async Task<SqlConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        /// <summary>
        /// 取得並開啟新的 SQL 同步連線
        /// </summary>
        public static SqlConnection GetConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }
    }
}