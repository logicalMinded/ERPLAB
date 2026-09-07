using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 客戶資料存取層 (Repository)
    /// 負責處理 Customer 實體的資料庫 I/O 操作，包含多重結果集分頁查詢與樂觀鎖 (Optimistic Concurrency) 併發控制。
    /// </summary>
    public class CustomerRepository
    {
        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================

        /// <summary>
        /// 取得客戶清單 (分頁查詢)
        /// 採用單次 Batch 執行多段 SQL，同步取得總筆數與分頁明細以最佳化效能。
        /// </summary>
        /// <param name="pageNumber">當前頁碼 (自 1 起算)</param>
        /// <param name="pageSize">每頁筆數 (傳入 0 代表取消分頁撈取全部)</param>
        /// <param name="includeInactive">是否包含已停用之客戶</param>
        /// <param name="keyword">搜尋關鍵字</param>
        public async Task<(List<Customer> Items, int TotalCount)> GetCustomersAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            var list = new List<Customer>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            // 將總筆數計算與分頁實體查詢合併為單一批次 (Batch) 執行，減少一次資料庫往返 (Round-trip) 成本
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
                    
                    -- 審計軌跡所需之顯示欄位
                    empCreate.[EmployeeNo] AS CreateUserNo_Display,
                    empUpdate.[EmployeeNo] AS UpdateUserNo_Display

                FROM [dbo].[Customer] c

                -- 透過 Accounts 關聯至 Employee 主檔，取得建檔者與異動者的實際工號 (EmployeeNo)
                LEFT JOIN [dbo].[Accounts] accCreate ON c.[CreateUser] = accCreate.[AccountID]
                LEFT JOIN [dbo].[Employee] empCreate ON accCreate.[EmployeeID] = empCreate.[EmployeeID]
                
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

                // SQL 參數於同一個 Command 批次中可跨 SELECT 語句重複綁定
                cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Keyword", $"%{keyword.Trim()}%", 50));
            }

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IncludeInactive", includeInactive));

            // 預設採用 ID 遞減排序 (確保最新建立的資料優先呈現)
            sqlBuilder.Append(" ORDER BY c.[CustomerID] DESC ");

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
                // 特殊情境：關閉分頁進行全資料撈取 (如匯出 Excel 或綁定下拉選單)
                sqlBuilder.Append(";");
            }
            else
            {
                // Fail-Fast 機制：阻擋無效的負數分頁參數
                throw new ArgumentOutOfRangeException(nameof(pageSize), "分頁筆數 (pageSize) 必須大於或等於 0！");
            }

            cmd.CommandText = sqlBuilder.ToString();

            // =====================================================================
            // 處理雙重結果集 (Multiple Result Sets)
            // =====================================================================
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

        // =====================================================================
        // 資料交易服務 (Command)
        // =====================================================================

        /// <summary>
        /// 新增客戶資料。
        /// 透過 OUTPUT 子句同步取回資料庫生成的 ID 與 Timestamp (RowVersion)，避免額外的 SELECT 查詢。
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

            // 嚴格參數綁定：明確定義 VARCHAR 與 NVARCHAR 以避免隱式轉型效能耗損
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
        /// 更新客戶資料。
        /// 實作樂觀鎖防禦機制，若資料於讀取後遭他人異動，將拋出 DBConcurrencyException。
        /// </summary>
        public async Task<byte[]> UpdateAsync(Customer entity)
        {
            // 依據內控原則，客戶業務編號 (CustomerNo) 經建檔後不允許修改，故排除於 UPDATE 語句之外。
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

            // 樂觀鎖驗證條件
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@CustomerID", entity.CustomerID));
            cmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", entity.RowVersion));

            var result = await cmd.ExecuteScalarAsync();

            // 若 OUTPUT 未回傳值 (result 為 null)，代表受影響筆數為 0，表示資料已被刪除或發生樂觀鎖衝突
            if (result == null)
            {
                throw new DBConcurrencyException("此客戶資料已被其他使用者異動，請重新載入最新資料後再試。");
            }

            return (byte[])result; // 回傳最新版 RowVersion 供呼叫端更新狀態
        }

        /// <summary>
        /// 更新客戶停用/啟用狀態。
        /// </summary>
        public async Task<byte[]> UpdateStatusAsync(int customerId, bool targetActiveState, byte[] rowVersion, int updateUser)
        {
            // 獨立的狀態更新語句：僅異動狀態與審計欄位 (時間/人員)，避免影響其他業務欄位並降低併發衝突機率。
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
                throw new DBConcurrencyException("此客戶狀態已被其他使用者異動，請重新載入最新資料後再試。");
            }

            return (byte[])result;
        }
    }
}