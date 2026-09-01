using Microsoft.Data.SqlClient;

namespace ERPLAB.DataAccess.Core
{
    /// <summary>
    /// 全域自動編碼取號引擎。
    /// 核心職責：提供高併發下的極速原子級 (Atomic) 取號，物理隔離於各業務單據的龐大交易之外。
    /// </summary>
    public static class AutoNumberHelper
    {
        /// <summary>
        /// 取得下一個單據編號 (如: SO202607130001)
        /// </summary>
        /// <param name="docType">單據類型代碼 (如: SO, PO, INV)</param>
        public static async Task<string> GetNextSequenceAsync(string docType)
        {
            // 💡 物理隔離：自己開一條獨立連線。
            // 執行完 UPDATE 瞬間釋放，絕對不跟後續 Master-Detail 的長時間寫入搶資源。
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

                // 💡 共用字串組裝邏輯：動態組合傳入的 DocType + 系統日期 + 4碼流水號
                return $"{docType}{dbDate:yyyyMMdd}{nextSeq:D4}";
            }

            throw new InvalidOperationException($"單據取號引擎發生異常：找不到 DocType = '{docType}' 的編碼規則，請確認種子資料是否已建立。");
        }
    }
}