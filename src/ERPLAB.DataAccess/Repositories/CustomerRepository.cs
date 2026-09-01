using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    public class CustomerRepository
    {
        // =====================================================================
        // 🔍 [檢索引擎] 支援動態過濾停用資料、多欄位模糊搜尋與極速分頁
        // =====================================================================

        /// <summary>
        /// 取得廠商清單 (分頁模式)
        /// </summary>
        /// <param name="pageNumber">當前頁碼 (自 1 起算)</param>
        /// <param name="pageSize">每頁筆數 (傳入 0 代表全撈)</param>
        /// <param name="includeInactive">是否包含已停用廠商</param>
        /// <param name="keyword">搜尋關鍵字</param>
        public async Task<(List<Customer> Items, int TotalCount)> GetCustomersAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            var list = new List<Customer>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            // 一次網路 I/O，同時撈取「符合條件的總筆數」與「當頁明細」
            var sqlBuilder = new System.Text.StringBuilder(@"
                -- 語句 1：計算符合條件的總筆數
                SELECT COUNT(1) 
                FROM [dbo].[Customer]
                WHERE (@IncludeInactive = 1 OR [IsActive] = 1) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(@" AND (
                    [CustomerNo] LIKE @Keyword OR 
                    [CustomerName] LIKE @Keyword OR 
                    [TaxID] LIKE @Keyword OR 
                    [PhoneNumber] LIKE @Keyword) ");
            }

            sqlBuilder.Append(@"
                ;
                -- 語句 2：分頁撈取實體資料
                SELECT 
                    c.[CustomerID], c.[CustomerNo], c.[CustomerName], c.[TaxID], c.[Gender], 
                    c.[PhoneNumber], c.[DistrictID], c.[CustomZipCode], c.[Address], c.[Email], 
                    c.[Interests], c.[Remark], c.[ImageName],
                    c.[CreateTime], c.[CreateUser], c.[UpdateTime], c.[UpdateUser], 
                    c.[IsActive], c.[RowVersion],
                    
                    -- 💡 [審計軌跡快照] 跨表抓出實體員工工號
                    empCreate.[EmployeeNo] AS CreateUserNo_Display,
                    empUpdate.[EmployeeNo] AS UpdateUserNo_Display

                FROM [dbo].[Customer] c

                -- 💡 [身分鑑識引擎] 建檔者：透過 Accounts 實體橋接至 Employee
                LEFT JOIN [dbo].[Accounts] accCreate ON c.[CreateUser] = accCreate.[AccountID]
                LEFT JOIN [dbo].[Employee] empCreate ON accCreate.[EmployeeID] = empCreate.[EmployeeID]
                
                -- 💡 [身分鑑識引擎] 異動者：透過 Accounts 實體橋接至 Employee
                LEFT JOIN [dbo].[Accounts] accUpdate ON c.[UpdateUser] = accUpdate.[AccountID]
                LEFT JOIN [dbo].[Employee] empUpdate ON accUpdate.[EmployeeID] = empUpdate.[EmployeeID]

                WHERE (@IncludeInactive = 1 OR c.[IsActive] = 1) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(@" AND (
                    c.[CustomerNo] LIKE @Keyword OR 
                    c.[CustomerName] LIKE @Keyword OR 
                    c.[TaxID] LIKE @Keyword OR 
                    c.[PhoneNumber] LIKE @Keyword) ");

                // 💡 參數只需加入一次，同一個 Batch 內的兩段 SELECT 皆可共用此變數
                cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Keyword", $"%{keyword.Trim()}%", 50));
            }

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IncludeInactive", includeInactive));

            // 預設採用 ID 遞減排序 (最新建立的排在最前)
            sqlBuilder.Append(" ORDER BY c.[CustomerID] DESC ");

            // =====================================================================
            // 💡 [分頁引擎開關與絕對邊界防禦] 
            // =====================================================================
            if (pageSize > 0)
            {
                // 標準分頁模式
                sqlBuilder.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@Offset", offset));
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@PageSize", pageSize));
            }
            else if (pageSize == 0)
            {
                // 特例：關閉分頁，全撈 (如匯出 Excel 或下拉選單使用)
                sqlBuilder.Append(";");
            }
            else
            {
                // 🚨 物理防線：徹底封殺負數等不合法的髒參數，觸發 Fail-Fast
                throw new ArgumentOutOfRangeException(nameof(pageSize), "分頁筆數 (pageSize) 必須大於或等於 0！");
            }

            cmd.CommandText = sqlBuilder.ToString();

            // =====================================================================
            // 🔄 雙重結果集讀取 (Multiple Result Sets)
            // =====================================================================
            using var reader = await cmd.ExecuteReaderAsync();

            // 讀取第一段結果：總筆數
            if (await reader.ReadAsync())
            {
                totalCount = reader.GetInt32(0);
            }

            // 跳躍至第二段結果：分頁實體資料
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var c = new Customer
                    {
                        CustomerID = reader.GetInt32(reader.GetOrdinal("CustomerID")),
                        CustomerNo = reader.GetString(reader.GetOrdinal("CustomerNo")),
                        CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                        TaxID = reader.IsDBNull(reader.GetOrdinal("TaxID")) ? null : reader.GetString(reader.GetOrdinal("TaxID")),

                        // 注意：此處已改為 Enum 強制轉型
                        Gender = (GenderType)reader.GetByte(reader.GetOrdinal("Gender")),

                        PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        DistrictID = reader.GetInt32(reader.GetOrdinal("DistrictID")),
                        CustomZipCode = reader.GetString(reader.GetOrdinal("CustomZipCode")),
                        Address = reader.GetString(reader.GetOrdinal("Address")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        Interests = reader.IsDBNull(reader.GetOrdinal("Interests")) ? null : reader.GetString(reader.GetOrdinal("Interests")),
                        Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),
                        ImageName = reader.IsDBNull(reader.GetOrdinal("ImageName")) ? null : reader.GetString(reader.GetOrdinal("ImageName")),

                        CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                        CreateUser = reader.GetInt32(reader.GetOrdinal("CreateUser")),
                        UpdateTime = reader.GetDateTime(reader.GetOrdinal("UpdateTime")),
                        UpdateUser = reader.GetInt32(reader.GetOrdinal("UpdateUser")),

                        CreateUserNo_Display = reader.IsDBNull(reader.GetOrdinal("CreateUserNo_Display")) ? null : reader.GetString(reader.GetOrdinal("CreateUserNo_Display")),
                        UpdateUserNo_Display = reader.IsDBNull(reader.GetOrdinal("UpdateUserNo_Display")) ? null : reader.GetString(reader.GetOrdinal("UpdateUserNo_Display")),

                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        RowVersion = (byte[])reader["RowVersion"]
                    };
                    list.Add(c);
                }
            }

            return (list, totalCount);
        }
        /// <summary>
        /// 💡 寫入廠商並同時取回 ID 與 RowVersion (降低往返 I/O)
        /// </summary>
        public async Task<Customer> CreateAsync(Customer entity)
        {
            string sql = @"
                INSERT INTO [dbo].[Customer] 
                ([CustomerNo], [CustomerName], [TaxID], [Gender], [PhoneNumber], 
                 [DistrictID], [CustomZipCode], [Address], [Email], [Remark], 
                 [CreateUser], [UpdateUser])
                OUTPUT INSERTED.CustomerID, INSERTED.RowVersion
                VALUES 
                (@CustomerNo, @CustomerName, @TaxID, @Gender, @PhoneNumber, 
                 @DistrictID, @CustomZipCode, @Address, @Email, @Remark, 
                 @CreateUser, @UpdateUser);";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            // 嚴格參數綁定：隔離 VARCHAR 與 NVARCHAR
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@CustomerNo", entity.CustomerNo, 20));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@CustomerName", entity.CustomerName, 50));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@TaxID", entity.TaxID, 8));
            cmd.Parameters.Add(SqlParameterFactory.CreateTinyInt("@Gender", (byte)entity.Gender));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@PhoneNumber", entity.PhoneNumber, 20));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@DistrictID", entity.DistrictID));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@CustomZipCode", entity.CustomZipCode, 6));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Address", entity.Address, 200));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@Email", entity.Email, 100));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", entity.Remark, 500));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@CreateUser", entity.CreateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", entity.UpdateUser));

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                entity.CustomerID = reader.GetInt32(0);
                entity.RowVersion = (byte[])reader[1];
            }
            return entity;
        }

        /// <summary>
        /// 💡 更新廠商 (發動樂觀鎖防禦)
        /// </summary>
        public async Task<byte[]> UpdateAsync(Customer entity)
        {
            // (CustomerNo 為業務編號，依企業內控常理，建檔後通常不允許隨意修改，故不列入 UPDATE)
            string sql = @"
                UPDATE [dbo].[Customer] 
                SET [CustomerName] = @CustomerName,
                    [TaxID] = @TaxID,
                    [Gender] = @Gender,
                    [PhoneNumber] = @PhoneNumber,
                    [DistrictID] = @DistrictID,
                    [CustomZipCode] = @CustomZipCode,
                    [Address] = @Address,
                    [Email] = @Email,
                    [Remark] = @Remark,
                    [IsActive] = @IsActive,
                    [UpdateTime] = GETDATE(),
                    [UpdateUser] = @UpdateUser
                OUTPUT INSERTED.RowVersion
                WHERE [CustomerID] = @CustomerID AND [RowVersion] = @RowVersion;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@CustomerName", entity.CustomerName, 50));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@TaxID", entity.TaxID, 8));
            cmd.Parameters.Add(SqlParameterFactory.CreateTinyInt("@Gender", (byte)entity.Gender));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@PhoneNumber", entity.PhoneNumber, 20));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@DistrictID", entity.DistrictID));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@CustomZipCode", entity.CustomZipCode, 6));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Address", entity.Address, 200));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@Email", entity.Email, 100));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", entity.Remark, 500));
            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", entity.IsActive));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", entity.UpdateUser));

            // 併發防禦條件
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@CustomerID", entity.CustomerID));
            cmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", entity.RowVersion));

            var result = await cmd.ExecuteScalarAsync();

            // 若 result 為 null，代表 rowsAffected == 0，樂觀鎖觸發或資料已被刪除
            if (result == null)
            {
                throw new DBConcurrencyException("此廠商資料已被其他使用者異動，請重新載入最新資料後再試。");
            }

            return (byte[])result; // 回傳最新版時間戳記供 UI 更新
        }

        public async Task<byte[]> UpdateStatusAsync(int customerId, bool targetActiveState, byte[] rowVersion, int updateUser)
        {
            // 💡 絕對純潔的 SQL：只動 IsActive、時間與人員。其他業務資料 100% 免疫。
            string sql = @"
                UPDATE [dbo].[Customer] 
                SET [IsActive] = @IsActive,
                    [UpdateTime] = GETDATE(),
                    [UpdateUser] = @UpdateUser
                OUTPUT INSERTED.RowVersion
                WHERE [CustomerID] = @CustomerID AND [RowVersion] = @RowVersion;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", targetActiveState));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", updateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@CustomerID", customerId));
            cmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", rowVersion));

            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
            {
                throw new DBConcurrencyException("此廠商狀態已被其他使用者異動，請重新載入最新資料後再試。");
            }

            return (byte[])result;
        }
    }
}