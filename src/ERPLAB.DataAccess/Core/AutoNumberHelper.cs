using Microsoft.Data.SqlClient;
namespace ERPLAB.DataAccess.Core
{
    /// <summary>
    /// 自動編碼產生器 (AutoNumberHelper)
    /// 負責處理單據流水號的原子性 (Atomic) 取號作業，
    /// 透過獨立連線避免與主業務交易產生鎖定爭用 (Lock Contention)。
    /// </summary>
    public static class AutoNumberHelper
    {
        /// <summary>
        /// 取得下一個單據編號 (例如: SO202607130001)
        /// </summary>
        /// <param name="docType">單據類型代碼 (例如: SO, PO, INV)</param>
        public static async Task<string> GetNextSequenceAsync(string docType)
        {
            // 建立獨立資料庫連線：使取號操作與主業務交易分離，
            // 確保 UPDATE 執行後立即釋放資源，降低資料表鎖定 (Locking) 的影響。
            using var conn = await DbConnectionFactory.GetConnectionAsync();

            string sql = @"
                DECLARE @NextSeq INT;
                DECLARE @Today DATE = CAST(GETDATE() AS DATE);

                UPDATE [dbo].[AutoNumber]
                SET 
                    @NextSeq = [LastSeq] = CASE WHEN [CurrentDate] = @Today THEN [LastSeq] + 1 ELSE 1 END,
                    [CurrentDate] = @Today
                WHERE [DocType] = @DocType;

                SELECT @NextSeq AS Seq, @Today AS DbDate;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@DocType", docType, 5));

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int seqIndex = reader.GetOrdinal("Seq");
                int dateIndex = reader.GetOrdinal("DbDate");

                int nextSeq = reader.GetInt32(seqIndex);
                DateTime dbDate = reader.GetDateTime(dateIndex);

                // 組合單據編號格式：單據字首 (DocType) + 資料庫當前日期 (yyyyMMdd) + 4碼流水號
                return $"{docType}{dbDate:yyyyMMdd}{nextSeq:D4}";
            }

            throw new InvalidOperationException($"單據取號引擎發生異常：找不到 DocType = '{docType}' 的編碼規則，請確認種子資料是否已建立。");
        }
    }
}