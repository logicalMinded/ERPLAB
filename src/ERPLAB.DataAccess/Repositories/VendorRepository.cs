using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 廠商資料存取層 (Repository)
    /// 負責處理 Vendor 實體的資料庫 I/O 操作，包含多重結果集分頁查詢與樂觀鎖 (Optimistic Concurrency) 併發控制。
    /// </summary>
    public class VendorRepository
    {
        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================

        /// <summary>
        /// 取得廠商清單 (分頁查詢)。
        /// 採用單次 Batch 執行多段 SQL，同步取得總筆數與分頁明細以最佳化 I/O 效能。
        /// </summary>
        public async Task<(List<Vendor> Items, int TotalCount)> GetVendorsAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            var list = new List<Vendor>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            // 將總筆數計算與分頁實體查詢合併為單一批次 (Batch) 執行，減少網路往返 (Round-trip) 成本
            var sqlBuilder = new System.Text.StringBuilder(@"
                -- 語句 1：計算符合條件的總筆數
                SELECT COUNT(1) 
                FROM [dbo].[Vendor] v
                WHERE (@IncludeInactive = 1 OR v.[IsActive] = 1) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(@" AND (
                    v.[VendorNo] LIKE @Keyword OR 
                    v.[VendorName] LIKE @Keyword OR 
                    v.[TaxID] LIKE @Keyword OR 
                    v.[PhoneNumber] LIKE @Keyword) ");
            }

            sqlBuilder.Append(@"
                ;
                -- 語句 2：分頁撈取實體資料與人員顯示資訊
                SELECT 
                    v.[VendorID], v.[VendorNo], v.[VendorName], v.[TaxID], v.[ContactPerson], 
                    v.[PhoneNumber], v.[DistrictID], v.[CustomZipCode], v.[Address], v.[Email], 
                    v.[Remark],
                    v.[CreateTime], v.[CreateUser], v.[UpdateTime], v.[UpdateUser], 
                    v.[IsActive], v.[RowVersion],
                    
                    empCreate.[EmployeeNo] AS CreateUserNo_Display,
                    empUpdate.[EmployeeNo] AS UpdateUserNo_Display
                FROM [dbo].[Vendor] v
                
                -- 透過 Accounts 關聯至 Employee 主檔，取得建檔者與異動者的實際工號
                LEFT JOIN [dbo].[Accounts] accCreate ON v.[CreateUser] = accCreate.[AccountID]
                LEFT JOIN [dbo].[Employee] empCreate ON accCreate.[EmployeeID] = empCreate.[EmployeeID]
                LEFT JOIN [dbo].[Accounts] accUpdate ON v.[UpdateUser] = accUpdate.[AccountID]
                LEFT JOIN [dbo].[Employee] empUpdate ON accUpdate.[EmployeeID] = empUpdate.[EmployeeID]
                
                WHERE (@IncludeInactive = 1 OR v.[IsActive] = 1) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(@" AND (
                    v.[VendorNo] LIKE @Keyword OR 
                    v.[VendorName] LIKE @Keyword OR 
                    v.[TaxID] LIKE @Keyword OR 
                    v.[PhoneNumber] LIKE @Keyword) ");

                // SQL 參數於同一個 Command 批次中可跨 SELECT 語句重複綁定
                cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Keyword", $"%{keyword.Trim()}%", 50));
            }

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IncludeInactive", includeInactive));

            // 預設依廠商 ID 遞減排序 (最新建立者優先)
            sqlBuilder.Append(" ORDER BY v.[VendorID] DESC ");

            // 分頁邊界防禦與參數處理
            if (pageSize > 0)
            {
                sqlBuilder.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@Offset", offset));
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@PageSize", pageSize));
            }
            else if (pageSize == 0)
            {
                sqlBuilder.Append(";"); // 關閉分頁撈取全表
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "分頁筆數 (pageSize) 必須大於或等於 0！");
            }

            cmd.CommandText = sqlBuilder.ToString();

            // 處理雙重結果集 (Multiple Result Sets)
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync()) totalCount = reader.GetInt32(0);

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    list.Add(new Vendor
                    {
                        VendorID = reader.GetInt32(reader.GetOrdinal("VendorID")),
                        VendorNo = reader.GetString(reader.GetOrdinal("VendorNo")),
                        VendorName = reader.GetString(reader.GetOrdinal("VendorName")),
                        TaxID = reader.IsDBNull(reader.GetOrdinal("TaxID")) ? null : reader.GetString(reader.GetOrdinal("TaxID")),
                        ContactPerson = reader.GetString(reader.GetOrdinal("ContactPerson")),
                        PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        DistrictID = reader.GetInt32(reader.GetOrdinal("DistrictID")),
                        CustomZipCode = reader.GetString(reader.GetOrdinal("CustomZipCode")),
                        Address = reader.GetString(reader.GetOrdinal("Address")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),

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
        /// 新增廠商資料。
        /// 透過 OUTPUT 子句同步取回資料庫生成的 ID 與 Timestamp (RowVersion)。
        /// </summary>
        public async Task<Vendor> CreateAsync(Vendor entity)
        {
            string sql = @"
                INSERT INTO [dbo].[Vendor] 
                ([VendorNo], [VendorName], [TaxID], [ContactPerson], [PhoneNumber], 
                 [DistrictID], [CustomZipCode], [Address], [Email], [Remark], 
                 [CreateUser], [UpdateUser], [IsActive])
                OUTPUT INSERTED.VendorID, INSERTED.RowVersion
                VALUES 
                (@VendorNo, @VendorName, @TaxID, @ContactPerson, @PhoneNumber, 
                 @DistrictID, @CustomZipCode, @Address, @Email, @Remark, 
                 @CreateUser, @UpdateUser, @IsActive);";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@VendorNo", entity.VendorNo, 20));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@VendorName", entity.VendorName, 100));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@TaxID", entity.TaxID, 8));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@ContactPerson", entity.ContactPerson, 50));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@PhoneNumber", entity.PhoneNumber, 20));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@DistrictID", entity.DistrictID));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@CustomZipCode", entity.CustomZipCode, 6));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Address", entity.Address, 200));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@Email", entity.Email, 100));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", entity.Remark, 500));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@CreateUser", entity.CreateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", entity.UpdateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", entity.IsActive));

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                entity.VendorID = reader.GetInt32(0);
                entity.RowVersion = (byte[])reader[1];
            }
            return entity;
        }

        /// <summary>
        /// 更新廠商資料。
        /// 實作樂觀鎖 (Optimistic Concurrency) 防禦機制，並將業務主鍵 (VendorNo) 排除於更新欄位之外，確保資料完整性。
        /// </summary>
        public async Task<byte[]> UpdateAsync(Vendor entity)
        {
            string sql = @"
                UPDATE [dbo].[Vendor] 
                SET [VendorName] = @VendorName,
                    [TaxID] = @TaxID,
                    [ContactPerson] = @ContactPerson,
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
                WHERE [VendorID] = @VendorID AND [RowVersion] = @RowVersion;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@VendorName", entity.VendorName, 100));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@TaxID", entity.TaxID, 8));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@ContactPerson", entity.ContactPerson, 50));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@PhoneNumber", entity.PhoneNumber, 20));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@DistrictID", entity.DistrictID));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@CustomZipCode", entity.CustomZipCode, 6));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Address", entity.Address, 200));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@Email", entity.Email, 100));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", entity.Remark, 500));
            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", entity.IsActive));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", entity.UpdateUser));

            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@VendorID", entity.VendorID));
            cmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", entity.RowVersion));

            var result = await cmd.ExecuteScalarAsync();

            // 若回傳值為 null，代表受影響筆數為 0，表示資料已被刪除或發生樂觀鎖衝突
            if (result == null)
            {
                throw new DBConcurrencyException("此廠商資料已被其他使用者異動，請重新載入最新資料後再試。");
            }
            return (byte[])result;
        }

        /// <summary>
        /// 更新廠商啟用/停用狀態。
        /// 針對狀態與審計欄位進行局部更新，降低高併發情境下的資料鎖定衝突。
        /// </summary>
        public async Task<byte[]> UpdateStatusAsync(int vendorId, bool targetActiveState, byte[] rowVersion, int updateUser)
        {
            string sql = @"
                UPDATE [dbo].[Vendor] 
                SET [IsActive] = @IsActive,
                    [UpdateTime] = GETDATE(),
                    [UpdateUser] = @UpdateUser
                OUTPUT INSERTED.RowVersion
                WHERE [VendorID] = @VendorID AND [RowVersion] = @RowVersion;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", targetActiveState));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", updateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@VendorID", vendorId));
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