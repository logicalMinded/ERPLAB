using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    public class AccountRepository
    {
        /// <summary>
        /// 建立系統帳號。
        /// 密碼將透過 CryptoHelper 轉換為 Identity V3 相容之二進位雜湊格式後寫入。
        /// </summary>
        public async Task<int> CreateAccountAsync(int employeeId, string username, string plainPassword)
        {
            // 將明碼轉換為 Base64 密碼雜湊字串
            string hashString = CryptoHelper.HashPassword(plainPassword);

            // 審計欄位 (CreateTime/CreateUser 等) 委由資料庫 Default Constraint 與 Trigger 處理，確保資料一致性
            string sql = @"
                INSERT INTO [dbo].[Accounts] 
                ([EmployeeID], [Username], [PasswordHash])
                OUTPUT INSERTED.AccountID
                VALUES 
                (@EmployeeID, @Username, @PasswordHash);";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            // 透過 SqlParameterFactory 明確指定參數型別與長度，避免隱式轉型 (Implicit Conversion) 造成效能損耗
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@EmployeeID", employeeId));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@Username", username, 50));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@PasswordHash", hashString, 255));

            // 執行寫入並回傳由資料庫自動生成的 AccountID
            return (int)await cmd.ExecuteScalarAsync();
        }

        /// <summary>
        /// 帳號登入驗證作業。
        /// 整合帳號狀態檢核、CPU 密集型運算卸載 (Offloading)，以及樂觀鎖 (Optimistic Concurrency) 併發控制與重試機制。
        /// </summary>
        public async Task<(bool IsSuccess, Account? AccountData, string Message)> VerifyLoginAsync(string username, string plainPassword)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();

            // =====================================================================
            // 階段一：資料讀取 (取得帳號狀態與樂觀鎖版本號 RowVersion)
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

            // 帳號已被鎖定
            if (account.IsLocked)
                return (false, null, "您的帳號已因連續登入失敗被鎖定，請聯絡系統管理員。");

            // =====================================================================
            // 階段二：運算卸載 (將高耗時的密碼雜湊比對卸載至 Thread Pool，避免阻塞 UI 執行緒)
            // =====================================================================
            bool isPasswordValid = await Task.Run(() =>
                CryptoHelper.VerifyPassword(plainPassword, account.PasswordHash));

            // =====================================================================
            // 階段三：狀態更新與樂觀鎖防禦
            // =====================================================================
            if (!isPasswordValid)
            {
                bool updateSuccess = false;
                int retryCount = 0;
                byte currentFailedCount = account.FailedCount;
                byte[] currentRowVersion = account.RowVersion;
                bool lockAccount = false;

                // 實作寫入重試機制 (Retry Pattern)，最高嘗試 3 次以解決高併發下的更新衝突
                while (!updateSuccess && retryCount < 3)
                {
                    // 計算新狀態：滿 5 次即鎖定帳號
                    currentFailedCount++;
                    lockAccount = currentFailedCount >= 5;

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
                    // 透過 ExecuteNonQuery 回傳值 (RowsAffected) 判斷樂觀鎖是否發生衝突
                    // =====================================================================
                    int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                    if (rowsAffected == 1)
                    {
                        updateSuccess = true; // 寫入成功，跳出重試迴圈
                    }
                    else if (rowsAffected == 0)
                    {
                        // 寫入失敗 (發生併發衝突)：重新讀取最新的狀態與 RowVersion 以進行下一次重試
                        retryCount++;
                        string refreshSql = "SELECT [FailedCount], [RowVersion] FROM [dbo].[Accounts] WHERE [AccountID] = @AccountID AND [IsActive] = 1;";
                        using var refreshCmd = new SqlCommand(refreshSql, conn);
                        refreshCmd.Parameters.Add(SqlParameterFactory.CreateInt("@AccountID", account.AccountID));

                        using var reader = await refreshCmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            currentFailedCount = reader.GetByte(reader.GetOrdinal("FailedCount"));
                            currentRowVersion = (byte[])reader["RowVersion"];
                        }
                        else
                        {
                            // 資料已被物理刪除或停用，直接中斷重試
                            break;
                        }
                    }
                    else
                    {
                        // 攔截底層非預期行為：若回傳值小於 0 (如受 SET NOCOUNT ON 影響) 或大於 1，則拋出系統異常
                        throw new InvalidOperationException($"系統底層執行異常：預期更新 1 筆，但實際回傳值為 {rowsAffected}。請檢查 SQL 語法與 NOCOUNT 設定。");
                    }
                }

                if (!updateSuccess)
                {
                    // 超出重試次數上限仍無法寫入
                    return (false, null, "系統正忙碌中，請稍後再試。");
                }

                string errMsg = lockAccount ? "密碼錯誤次數過多，帳號已安全鎖定。" : $"密碼錯誤。剩餘嘗試次數：{5 - currentFailedCount}";
                return (false, null, errMsg);
            }

            // 登入成功：重置錯誤計數並更新最後登入時間
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
                    // 若登入成功寫入狀態時發生樂觀鎖衝突，拋出異常阻斷流程並要求使用者重試
                    throw new DBConcurrencyException("系統狀態異常：您的帳號資料於登入期間發生併發異動，請重新登入。");
                }
            }

            return (true, account, "登入成功");
        }

        // =====================================================================
        // 帳號狀態更新服務 (適用於人事異動如離職、復職之權限連動)
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