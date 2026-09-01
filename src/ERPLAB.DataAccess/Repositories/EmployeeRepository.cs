using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 員工基本檔倉儲。
    /// 核心防線：精確映射人事狀態與性別 Enum、完整支援 3+3 郵遞區號擴充，以及身份鑑識雙重 LEFT JOIN。
    /// </summary>
    public class EmployeeRepository
    {
        // =====================================================================
        // 🔍 [檢索引擎] 支援動態過濾停用資料、多欄位模糊搜尋與極速分頁
        // =====================================================================
        public async Task<(List<Employee> Items, int TotalCount)> GetEmployeesAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            var list = new List<Employee>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            // 💡 效能亮點：MARS (多重結果集) 查詢
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
                    e.[Email] LIKE @Keyword) "); // 💡 擴充 Email 搜尋
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
                    
                    -- 💡 [身分鑑識引擎] 透過 Accounts 表橋接，還原建檔與修改者的工號
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

                // 參數化模糊搜尋
                cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Keyword", $"%{keyword.Trim()}%", 50));
            }

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IncludeInactive", includeInactive));

            // 預設採用 ID 遞減排序 (最新到職的排在最前)
            sqlBuilder.Append(" ORDER BY e.[EmployeeID] DESC ");

            // =====================================================================
            // 💡 [分頁引擎開關與絕對邊界防禦] 
            // =====================================================================
            if (pageSize > 0)
            {
                sqlBuilder.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@Offset", offset));
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@PageSize", pageSize));
            }
            else if (pageSize == 0)
            {
                sqlBuilder.Append(";");
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "分頁筆數 (pageSize) 必須大於或等於 0！");
            }

            cmd.CommandText = sqlBuilder.ToString();

            using var reader = await cmd.ExecuteReaderAsync();

            // 讀取總筆數
            if (await reader.ReadAsync())
            {
                totalCount = reader.GetInt32(0);
            }

            // 讀取分頁資料
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    list.Add(new Employee
                    {
                        EmployeeID = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                        EmployeeNo = reader.GetString(reader.GetOrdinal("EmployeeNo")),
                        EmployeeName = reader.GetString(reader.GetOrdinal("EmployeeName")),

                        // 💡 雙重強型別 Enum 轉換 (Downcasting)
                        JobStatus = (EmployeeJobStatus)reader.GetByte(reader.GetOrdinal("JobStatus")),
                        Gender = (GenderType)reader.GetByte(reader.GetOrdinal("Gender")),

                        JobTitle = reader.GetString(reader.GetOrdinal("JobTitle")),
                        PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),

                        // 💡 擴充的地理與聯絡欄位映射
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
        // ➕ [寫入引擎] 
        // =====================================================================
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

            // 💡 Enum 轉回位元組 (Upcasting to Value Type)
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

        // =====================================================================
        // 📝 [更新引擎] 樂觀鎖防禦
        // =====================================================================
        public async Task<byte[]> UpdateAsync(Employee entity)
        {
            // 💡 [架構師提醒] EmployeeNo 屬於業務主鍵，建檔後不允許修改
            // 若修改 JobStatus 為離職，SQL Trigger 會自動連動砍掉此人的帳號 IsActive 權限
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

            // 💡 補齊擴充的地理與聯絡欄位參數
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@DistrictID", entity.DistrictID));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@CustomZipCode", entity.CustomZipCode, 6));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Address", entity.Address, 200));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@Email", entity.Email, 100));

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", entity.IsActive));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", entity.UpdateUser));

            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@EmployeeID", entity.EmployeeID));
            cmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", entity.RowVersion));

            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
            {
                throw new DBConcurrencyException("此員工資料已被其他使用者異動，請重新載入最新資料後再試。");
            }

            return (byte[])result;
        }
    }
}