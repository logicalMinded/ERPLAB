using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    public class AccountRepository
    {
        /// <summary>
        /// 建立新帳號 (將明碼密碼透過二進位封裝加密後寫入資料庫)
        /// </summary>
        public async Task<int> CreateAccountAsync(int employeeId, string username, string plainPassword)
        {
            // 1. 呼叫密碼學引擎產出 84 字元的 Identity V3 格式 Base64 字串
            string hashString = CryptoHelper.HashPassword(plainPassword);

            // 2. 撰寫 T-SQL。
            // 💡 審計欄位 (DbCreateTime/User) 由 SQL 預設值與 Trigger 接管，絕對不在此處傳入。
            string sql = @"
                INSERT INTO [dbo].[Accounts] 
                ([EmployeeID], [Username], [PasswordHash])
                OUTPUT INSERTED.AccountID
                VALUES 
                (@EmployeeID, @Username, @PasswordHash);";

            // 3. 取得非同步連線 (使用 using 確保自動釋放資源)
            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            // 4. 透過參數防呆工廠精確綁定型別，根除隱式轉換地雷
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@EmployeeID", employeeId));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@Username", username, 50));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@PasswordHash", hashString, 255));

            // 5. 執行並取回由資料庫底層生成的 AccountID (IDENTITY)
            return (int)await cmd.ExecuteScalarAsync();
        }

        /// <summary>
        /// 系統登入驗證引擎 (具備防時序攻擊、非同步算力下放、動態鎖定與樂觀鎖防禦)
        /// </summary>
        public async Task<(bool IsSuccess, Account? AccountData, string Message)> VerifyLoginAsync(string username, string plainPassword)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();

            // =====================================================================
            // 階段 A：資料庫極速 I/O 讀取 (撈取帳號狀態與樂觀鎖)
            // =====================================================================
            string selectSql = @"
                SELECT [AccountID], [EmployeeID], [Username], [PasswordHash], 
                       [IsLocked], [FailedCount], [LastLogin], [RowVersion]
                FROM [dbo].[Accounts]
                WHERE [Username] = @Username AND [IsActive] = 1;";

            Account? account = null;

            using (var selectCmd = new SqlCommand(selectSql, conn))
            {
                selectCmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@Username", username, 50));
                using var reader = await selectCmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    account = new Account
                    {
                        AccountID = reader.GetInt32(reader.GetOrdinal("AccountID")),
                        EmployeeID = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                        IsLocked = reader.GetBoolean(reader.GetOrdinal("IsLocked")),
                        FailedCount = reader.GetByte(reader.GetOrdinal("FailedCount")),
                        RowVersion = (byte[])reader["RowVersion"],
                        LastLogin = reader.IsDBNull(reader.GetOrdinal("LastLogin"))
                                    ? null
                                    : reader.GetDateTime(reader.GetOrdinal("LastLogin"))
                    };
                }
            }

            // 帳號不存在或已停用
            if (account == null)
                return (false, null, "帳號不存在或已停用。");

            // 帳號已被物理鎖定
            if (account.IsLocked)
                return (false, null, "您的帳號已因連續登入失敗被鎖定，請聯絡系統管理員。");

            // =====================================================================
            // 階段 B：算力下放 (將 35 萬次 CPU 運算丟至背景執行緒< 這裡不是非同步，而是透過委派(必須透過委派)切換執行緒 >，確保 WinForms 不卡死)
            // =====================================================================
            bool isPasswordValid = await Task.Run(() =>
                CryptoHelper.VerifyPassword(plainPassword, account.PasswordHash));

            // =====================================================================
            // 階段 C：狀態更新與樂觀鎖防禦 (RowVersion)
            // =====================================================================
            if (!isPasswordValid)
            {
                bool updateSuccess = false;
                int retryCount = 0;
                byte currentFailedCount = account.FailedCount;
                byte[] currentRowVersion = account.RowVersion;
                bool lockAccount = false;

                // 💡 實作重試機制 (最多重試 3 次，防止無窮迴圈)
                while (!updateSuccess && retryCount < 3)
                {
                    // 計算新狀態
                    currentFailedCount++;
                    lockAccount = currentFailedCount >= 5; // 5 次鎖死

                    string updateFailSql = @"
                        UPDATE [dbo].[Accounts]
                        SET [FailedCount] = @FailedCount,
                            [IsLocked] = @IsLocked
                        WHERE [AccountID] = @AccountID AND [RowVersion] = @RowVersion;";

                    using var updateCmd = new SqlCommand(updateFailSql, conn);
                    updateCmd.Parameters.Add(SqlParameterFactory.CreateTinyInt("@FailedCount", currentFailedCount));
                    updateCmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsLocked", lockAccount));
                    updateCmd.Parameters.Add(SqlParameterFactory.CreateInt("@AccountID", account.AccountID));
                    updateCmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", currentRowVersion));
                    // =====================================================================
                    // 確實利用 rowsAffected 執行樂觀鎖防禦判斷
                    // =====================================================================
                    int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                    if (rowsAffected == 1)
                    {
                        updateSuccess = true; // 寫入成功，跳出迴圈
                    }
                    else if (rowsAffected == 0)
                    {
                        // 💡 寫入失敗 (併發衝突發生)：重新撈取最新的 RowVersion 與 FailedCount
                        retryCount++;
                        string refreshSql = "SELECT [FailedCount], [RowVersion] FROM [dbo].[Accounts] WHERE [AccountID] = @AccountID AND [IsActive] = 1;";
                        using var refreshCmd = new SqlCommand(refreshSql, conn);
                        refreshCmd.Parameters.Add(SqlParameterFactory.CreateInt("@AccountID", account.AccountID));

                        using var reader = await refreshCmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            // 更新迴圈變數，準備下一次嘗試
                            currentFailedCount = reader.GetByte(reader.GetOrdinal("FailedCount"));
                            currentRowVersion = (byte[])reader["RowVersion"];
                        }
                        else
                        {
                            // 若連撈都撈不到，代表帳號被物理刪除或停用，直接中斷
                            break;
                        }
                    }
                    else // rowsAffected < 0 (通常是 -1) 或意外的大於 1
                    {
                        // 💡 架構異常攔截：
                        // 如果回傳 -1，代表有人在 C# 偷加了 SET NOCOUNT ON，或是 ADO.NET 網路協定崩潰。
                        // 如果回傳 > 1，代表資料庫的主鍵約束被破壞，發生了毀滅性的資料污染。
                        throw new InvalidOperationException($"系統底層執行異常：預期更新 1 筆，但實際回傳值為 {rowsAffected}。請檢查 SQL 語法與 NOCOUNT 設定。");
                    }
                }

                if (!updateSuccess)
                {
                    // 即使重試 3 次仍被極端高頻率的併發干擾，回傳異常訊息
                    return (false, null, "系統正忙碌中，請稍後再試。");
                }

                string errMsg = lockAccount ? "密碼錯誤次數過多，帳號已安全鎖定。" : $"密碼錯誤。剩餘嘗試次數：{5 - currentFailedCount}";
                return (false, null, errMsg);
            }

            // 登入成功：清空錯誤計數並壓上最後登入時間
            string updateSuccessSql = @"
                UPDATE [dbo].[Accounts]
                SET [FailedCount] = 0,
                    [LastLogin] = GETDATE()
                WHERE [AccountID] = @AccountID AND [RowVersion] = @RowVersion;";

            using (var updateCmd = new SqlCommand(updateSuccessSql, conn))
            {
                updateCmd.Parameters.Add(SqlParameterFactory.CreateInt("@AccountID", account.AccountID));
                updateCmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", account.RowVersion));

                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    // 💡 樂觀鎖絕對防線：若成功登入的瞬間帳號被竄改，強制阻斷登入並要求重試
                    throw new DBConcurrencyException("系統狀態異常：您的帳號資料於登入期間發生併發異動，請重新登入。");
                }
            }

            return (true, account, "登入成功");
        }

        // =====================================================================
        // 🔄 [帳號狀態引擎] 專供因應人事異動 (離職/復職) 呼叫
        // =====================================================================
        public async Task UpdateIsActiveByEmployeeIdAsync(int employeeId, bool isActive, int updateUser)
        {
            string sql = @"
                UPDATE [dbo].[Accounts]
                SET [IsActive] = @IsActive,
                    [DbUpdateTime] = GETDATE(),
                    [DbUpdateUser] = @UpdateUser
                WHERE [EmployeeID] = @EmployeeID;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", isActive));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", updateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@EmployeeID", employeeId));

            await cmd.ExecuteNonQueryAsync();
        }
    }
}