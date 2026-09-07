using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 員工資料存取層 (Repository)
    /// 負責處理 Employee 實體的資料庫 I/O 操作，包含多重結果集分頁查詢、
    /// 實體狀態與 Enum 的強型別對應，以及樂觀鎖 (Optimistic Concurrency) 併發控制。
    /// </summary>
    public class EmployeeRepository
    {
        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================

        /// <summary>
        /// 取得員工清單 (分頁查詢)
        /// 採用單次 Batch 執行多段 SQL，同步取得總筆數與分頁明細以最佳化 I/O 效能。
        /// </summary>
        public async Task<(List<Employee> Items, int TotalCount)> GetEmployeesAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            var list = new List<Employee>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            // 將總筆數計算與分頁實體查詢合併為單一批次 (Batch) 執行，減少資料庫往返 (Round-trip) 成本
            var sqlBuilder = new System.Text.StringBuilder(@"
                -- 語句 1：計算符合條件的總筆數
                SELECT COUNT(1) 
                FROM [dbo].[Employee] e
                WHERE (@IncludeInactive = 1 OR e.[IsActive] = 1) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(@" AND (
                    e.[EmployeeNo] LIKE @Keyword OR 
                    e.[EmployeeName] LIKE @Keyword OR 
                    e.[PhoneNumber] LIKE @Keyword OR 
                    e.[Email] LIKE @Keyword) ");
            }

            sqlBuilder.Append(@"
                ;
                -- 語句 2：分頁撈取實體資料
                SELECT 
                    e.[EmployeeID], e.[EmployeeNo], e.[EmployeeName], e.[JobStatus], 
                    e.[JobTitle], e.[Gender], e.[PhoneNumber], 
                    e.[DistrictID], e.[CustomZipCode], e.[Address], e.[Email], 
                    e.[CreateTime], e.[CreateUser], e.[UpdateTime], e.[UpdateUser], 
                    e.[IsActive], e.[RowVersion],
                    
                    -- 透過 Accounts 關聯至 Employee 主檔，取得建檔者與異動者的實際工號 (EmployeeNo)
                    empCreate.[EmployeeNo] AS CreateUserNo_Display,
                    empUpdate.[EmployeeNo] AS UpdateUserNo_Display

                FROM [dbo].[Employee] e
                LEFT JOIN [dbo].[Accounts] accCreate ON e.[CreateUser] = accCreate.[AccountID]
                LEFT JOIN [dbo].[Employee] empCreate ON accCreate.[EmployeeID] = empCreate.[EmployeeID]
                LEFT JOIN [dbo].[Accounts] accUpdate ON e.[UpdateUser] = accUpdate.[AccountID]
                LEFT JOIN [dbo].[Employee] empUpdate ON accUpdate.[EmployeeID] = empUpdate.[EmployeeID]
                
                WHERE (@IncludeInactive = 1 OR e.[IsActive] = 1) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(@" AND (
                    e.[EmployeeNo] LIKE @Keyword OR 
                    e.[EmployeeName] LIKE @Keyword OR 
                    e.[PhoneNumber] LIKE @Keyword OR 
                    e.[Email] LIKE @Keyword) ");

                // SQL 參數於同一個 Command 批次中可跨 SELECT 語句重複綁定
                cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Keyword", $"%{keyword.Trim()}%", 50));
            }

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IncludeInactive", includeInactive));

            // 預設採用 ID 遞減排序 (確保最新到職的資料優先呈現)
            sqlBuilder.Append(" ORDER BY e.[EmployeeID] DESC ");

            // =====================================================================
            // 分頁邏輯與參數邊界驗證
            // =====================================================================
            if (pageSize > 0)
            {
                // 標準 OFFSET-FETCH 分頁模式
                sqlBuilder.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@Offset", offset));
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@PageSize", pageSize));
            }
            else if (pageSize == 0)
            {
                // 特殊情境：關閉分頁進行全資料撈取 (如匯出 Excel 報表)
                sqlBuilder.Append(";");
            }
            else
            {
                // Fail-Fast 機制：阻擋無效的負數分頁參數
                throw new ArgumentOutOfRangeException(nameof(pageSize), "分頁筆數 (pageSize) 必須大於或等於 0！");
            }

            cmd.CommandText = sqlBuilder.ToString();

            // 處理雙重結果集 (Multiple Result Sets)
            using var reader = await cmd.ExecuteReaderAsync();

            // 讀取第一段結果：符合條件之總筆數
            if (await reader.ReadAsync())
            {
                totalCount = reader.GetInt32(0);
            }

            // 推進至第二段結果：分頁實體資料
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    list.Add(new Employee
                    {
                        EmployeeID = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                        EmployeeNo = reader.GetString(reader.GetOrdinal("EmployeeNo")),
                        EmployeeName = reader.GetString(reader.GetOrdinal("EmployeeName")),

                        JobStatus = (EmployeeJobStatus)reader.GetByte(reader.GetOrdinal("JobStatus")),
                        Gender = (GenderType)reader.GetByte(reader.GetOrdinal("Gender")),

                        JobTitle = reader.GetString(reader.GetOrdinal("JobTitle")),
                        PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),

                        DistrictID = reader.GetInt32(reader.GetOrdinal("DistrictID")),
                        CustomZipCode = reader.GetString(reader.GetOrdinal("CustomZipCode")),
                        Address = reader.GetString(reader.GetOrdinal("Address")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),

                        CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                        CreateUser = reader.GetInt32(reader.GetOrdinal("CreateUser")),
                        UpdateTime = reader.GetDateTime(reader.GetOrdinal("UpdateTime")),
                        UpdateUser = reader.GetInt32(reader.GetOrdinal("UpdateUser")),

                        CreateUserNo_Display = reader.IsDBNull(reader.GetOrdinal("CreateUserNo_Display")) ? null : reader.GetString(reader.GetOrdinal("CreateUserNo_Display")),
                        UpdateUserNo_Display = reader.IsDBNull(reader.GetOrdinal("UpdateUserNo_Display")) ? null : reader.GetString(reader.GetOrdinal("UpdateUserNo_Display")),

                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        RowVersion = (byte[])reader["RowVersion"]
                    });
                }
            }

            return (list, totalCount);
        }

        // =====================================================================
        // 資料交易服務 (Command)
        // =====================================================================

        /// <summary>
        /// 新增員工資料。
        /// 透過 OUTPUT 子句同步取回資料庫生成的 ID 與 Timestamp (RowVersion)。
        /// </summary>
        public async Task<Employee> CreateAsync(Employee entity)
        {
            string sql = @"
                INSERT INTO [dbo].[Employee] 
                ([EmployeeNo], [EmployeeName], [JobStatus], [JobTitle], [Gender], 
                 [PhoneNumber], [DistrictID], [CustomZipCode], [Address], [Email], 
                 [CreateUser], [UpdateUser], [IsActive])
                OUTPUT INSERTED.EmployeeID, INSERTED.RowVersion
                VALUES 
                (@EmployeeNo, @EmployeeName, @JobStatus, @JobTitle, @Gender, 
                 @PhoneNumber, @DistrictID, @CustomZipCode, @Address, @Email, 
                 @CreateUser, @UpdateUser, @IsActive);";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@EmployeeNo", entity.EmployeeNo, 20));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@EmployeeName", entity.EmployeeName, 50));

            cmd.Parameters.Add(SqlParameterFactory.CreateTinyInt("@JobStatus", (byte)entity.JobStatus));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@JobTitle", entity.JobTitle, 50));
            cmd.Parameters.Add(SqlParameterFactory.CreateTinyInt("@Gender", (byte)entity.Gender));

            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@PhoneNumber", entity.PhoneNumber, 20));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@DistrictID", entity.DistrictID));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@CustomZipCode", entity.CustomZipCode, 6));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Address", entity.Address, 200));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@Email", entity.Email, 100));

            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@CreateUser", entity.CreateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", entity.UpdateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", entity.IsActive));

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                entity.EmployeeID = reader.GetInt32(0);
                entity.RowVersion = (byte[])reader[1];
            }
            return entity;
        }

        /// <summary>
        /// 更新員工資料。
        /// 實作樂觀鎖防禦機制，若資料於讀取後遭他人異動，將拋出 DBConcurrencyException。
        /// </summary>
        public async Task<byte[]> UpdateAsync(Employee entity)
        {
            // 業務規則：員工編號 (EmployeeNo) 為業務主鍵，建檔後不允許修改，故排除於 UPDATE 語句外。
            // 系統連動：若 JobStatus 變更為離職，資料庫 Trigger 會自動連動停用其關聯之登入帳號 (IsActive = 0)。
            string sql = @"
                UPDATE [dbo].[Employee] 
                SET [EmployeeName] = @EmployeeName,
                    [JobStatus] = @JobStatus,
                    [JobTitle] = @JobTitle,
                    [Gender] = @Gender,
                    [PhoneNumber] = @PhoneNumber,
                    [DistrictID] = @DistrictID,
                    [CustomZipCode] = @CustomZipCode,
                    [Address] = @Address,
                    [Email] = @Email,
                    [IsActive] = @IsActive,
                    [UpdateTime] = GETDATE(),
                    [UpdateUser] = @UpdateUser
                OUTPUT INSERTED.RowVersion
                WHERE [EmployeeID] = @EmployeeID AND [RowVersion] = @RowVersion;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@EmployeeName", entity.EmployeeName, 50));
            cmd.Parameters.Add(SqlParameterFactory.CreateTinyInt("@JobStatus", (byte)entity.JobStatus));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@JobTitle", entity.JobTitle, 50));
            cmd.Parameters.Add(SqlParameterFactory.CreateTinyInt("@Gender", (byte)entity.Gender));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@PhoneNumber", entity.PhoneNumber, 20));

            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@DistrictID", entity.DistrictID));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@CustomZipCode", entity.CustomZipCode, 6));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Address", entity.Address, 200));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@Email", entity.Email, 100));

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", entity.IsActive));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", entity.UpdateUser));

            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@EmployeeID", entity.EmployeeID));
            cmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", entity.RowVersion));

            var result = await cmd.ExecuteScalarAsync();

            // 若回傳值為 null，代表受影響筆數為 0，表示資料已被刪除或發生樂觀鎖衝突
            if (result == null)
            {
                throw new DBConcurrencyException("此員工資料已被其他使用者異動，請重新載入最新資料後再試。");
            }

            return (byte[])result;
        }
    }
}