using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 商品資料存取層 (Repository)
    /// 負責處理 Product 實體的資料庫 I/O 操作。
    /// 架構重點：嚴格隔離基本檔與庫存系統的寫入權限，於 Update 作業中物理排除 CurrentStock 欄位，確保庫存異動僅能由進銷存交易單據驅動。
    /// </summary>
    public class ProductRepository
    {
        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================

        /// <summary>
        /// 取得商品清單 (分頁查詢)
        /// 採用單次 Batch 執行多段 SQL，同步取得總筆數與分頁明細，並透過 JOIN 取得建檔與異動人員資訊。
        /// </summary>
        public async Task<(List<Product> Items, int TotalCount)> GetProductsAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            var list = new List<Product>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            // 將總筆數計算與分頁實體查詢合併為單一批次 (Batch) 執行
            var sqlBuilder = new System.Text.StringBuilder(@"
                -- 語句 1：計算符合條件的總筆數
                SELECT COUNT(1) 
                FROM [dbo].[Product] p
                WHERE (@IncludeInactive = 1 OR p.[IsActive] = 1) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(@" AND (
                    p.[ProductNo] LIKE @Keyword OR 
                    p.[ProductName] LIKE @Keyword) ");
            }

            sqlBuilder.Append(@"
                ;
                -- 語句 2：分頁撈取實體資料與人員顯示資訊
                SELECT 
                    p.[ProductID], p.[ProductNo], p.[ProductName], p.[MovingAverageCost],
                    p.[PurchasePrice], p.[SalesPrice], p.[CurrentStock], 
                    p.[Description], p.[ImageName], p.[Remark],
                    p.[CreateTime], p.[CreateUser], p.[UpdateTime], p.[UpdateUser], 
                    p.[IsActive], p.[RowVersion],
                    
                    empCreate.[EmployeeNo] AS CreateUserNo_Display,
                    empUpdate.[EmployeeNo] AS UpdateUserNo_Display

                FROM [dbo].[Product] p
                
                -- 透過 Accounts 關聯至 Employee 主檔，取得建檔者與異動者的實際工號
                LEFT JOIN [dbo].[Accounts] accCreate ON p.[CreateUser] = accCreate.[AccountID]
                LEFT JOIN [dbo].[Employee] empCreate ON accCreate.[EmployeeID] = empCreate.[EmployeeID]
                LEFT JOIN [dbo].[Accounts] accUpdate ON p.[UpdateUser] = accUpdate.[AccountID]
                LEFT JOIN [dbo].[Employee] empUpdate ON accUpdate.[EmployeeID] = empUpdate.[EmployeeID]
                
                WHERE (@IncludeInactive = 1 OR p.[IsActive] = 1) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(@" AND (
                    p.[ProductNo] LIKE @Keyword OR 
                    p.[ProductName] LIKE @Keyword) ");

                cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Keyword", $"%{keyword.Trim()}%", 50));
            }

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IncludeInactive", includeInactive));

            // 預設依商品 ID 遞減排序 (最新建立者優先)
            sqlBuilder.Append(" ORDER BY p.[ProductID] DESC ");

            // 分頁邊界防禦與參數處理
            if (pageSize > 0)
            {
                sqlBuilder.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@Offset", offset));
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@PageSize", pageSize));
            }
            else if (pageSize == 0)
            {
                sqlBuilder.Append(";"); // 取消分頁撈取全部
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "分頁筆數 (pageSize) 必須大於或等於 0！");
            }

            cmd.CommandText = sqlBuilder.ToString();

            // 處理雙重結果集 (Multiple Result Sets)
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                totalCount = reader.GetInt32(0);
            }

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    list.Add(new Product
                    {
                        ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                        ProductNo = reader.GetString(reader.GetOrdinal("ProductNo")),
                        ProductName = reader.GetString(reader.GetOrdinal("ProductName")),

                        MovingAverageCost = reader.GetDecimal(reader.GetOrdinal("MovingAverageCost")),
                        PurchasePrice = reader.GetDecimal(reader.GetOrdinal("PurchasePrice")),
                        SalesPrice = reader.GetDecimal(reader.GetOrdinal("SalesPrice")),
                        CurrentStock = reader.GetInt32(reader.GetOrdinal("CurrentStock")),

                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                        ImageName = reader.IsDBNull(reader.GetOrdinal("ImageName")) ? null : reader.GetString(reader.GetOrdinal("ImageName")),
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

        /// <summary>
        /// 依據商品編號 (ProductNo) 查詢單筆商品資料。
        /// 針對交易單據(如銷貨單、進貨單)輸入商品編號時的連動查詢進行優化，僅撈取必要欄位以降低網路封包傳輸量。
        /// </summary>
        public async Task<Product?> GetProductByNoAsync(string productNo)
        {
            string sql = @"
                SELECT 
                    [ProductID], [ProductNo], [ProductName], [SalesPrice], [PurchasePrice]
                FROM [dbo].[Product]
                WHERE [ProductNo] = @ProductNo AND [IsActive] = 1;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            // 確保 Parameter 型別為 VARCHAR，避免引發隱式轉型導致無法利用 Index Seek (索引尋結)
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@ProductNo", productNo, 20));

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Product
                {
                    ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                    ProductNo = reader.GetString(reader.GetOrdinal("ProductNo")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    SalesPrice = reader.GetDecimal(reader.GetOrdinal("SalesPrice")),
                    PurchasePrice = reader.GetDecimal(reader.GetOrdinal("PurchasePrice"))
                };
            }

            return null;
        }

        // =====================================================================
        // 資料交易服務 (Command)
        // =====================================================================

        /// <summary>
        /// 新增商品資料。
        /// 透過 OUTPUT 子句同步取回資料庫生成的 ID 與 Timestamp (RowVersion)。
        /// </summary>
        public async Task<Product> CreateAsync(Product entity)
        {
            string sql = @"
                INSERT INTO [dbo].[Product] 
                ([ProductNo], [ProductName], [PurchasePrice], [SalesPrice], 
                 [CurrentStock], [Description], [ImageName], [Remark], 
                 [CreateUser], [UpdateUser], [IsActive])
                OUTPUT INSERTED.ProductID, INSERTED.RowVersion
                VALUES 
                (@ProductNo, @ProductName, @PurchasePrice, @SalesPrice, 
                 @CurrentStock, @Description, @ImageName, @Remark, 
                 @CreateUser, @UpdateUser, @IsActive);";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@ProductNo", entity.ProductNo, 20));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@ProductName", entity.ProductName, 100));
            cmd.Parameters.Add(SqlParameterFactory.CreateDecimal("@PurchasePrice", entity.PurchasePrice, 18, 2));
            cmd.Parameters.Add(SqlParameterFactory.CreateDecimal("@SalesPrice", entity.SalesPrice, 18, 2));

            // 建檔時允許賦予初始庫存值
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@CurrentStock", entity.CurrentStock));

            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Description", entity.Description, -1)); // MAX
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@ImageName", entity.ImageName, 255));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", entity.Remark, 500));

            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@CreateUser", entity.CreateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", entity.UpdateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", entity.IsActive));

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                entity.ProductID = reader.GetInt32(0);
                entity.RowVersion = (byte[])reader[1];
            }
            return entity;
        }

        /// <summary>
        /// 更新商品基本資料。
        /// 實作樂觀鎖 (Optimistic Concurrency) 驗證。特別排除了 CurrentStock 欄位，確保庫存一致性不被基本檔維護功能破壞。
        /// </summary>
        public async Task<byte[]> UpdateAsync(Product entity)
        {
            string sql = @"
                UPDATE [dbo].[Product] 
                SET [ProductName] = @ProductName,
                    [PurchasePrice] = @PurchasePrice,
                    [SalesPrice] = @SalesPrice,
                    [Description] = @Description,
                    [ImageName] = @ImageName,
                    [Remark] = @Remark,
                    [IsActive] = @IsActive,
                    [UpdateTime] = GETDATE(),
                    [UpdateUser] = @UpdateUser
                OUTPUT INSERTED.RowVersion
                WHERE [ProductID] = @ProductID AND [RowVersion] = @RowVersion;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@ProductName", entity.ProductName, 100));
            cmd.Parameters.Add(SqlParameterFactory.CreateDecimal("@PurchasePrice", entity.PurchasePrice, 18, 2));
            cmd.Parameters.Add(SqlParameterFactory.CreateDecimal("@SalesPrice", entity.SalesPrice, 18, 2));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Description", entity.Description, -1));
            cmd.Parameters.Add(SqlParameterFactory.CreateVarChar("@ImageName", entity.ImageName, 255));
            cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", entity.Remark, 500));
            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", entity.IsActive));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", entity.UpdateUser));

            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@ProductID", entity.ProductID));
            cmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", entity.RowVersion));

            var result = await cmd.ExecuteScalarAsync();

            // 若回傳值為 null，代表受影響筆數為 0，表示資料已被刪除或發生樂觀鎖衝突
            if (result == null)
            {
                throw new DBConcurrencyException("此商品資料已被異動，請重新載入最新資料後再試。");
            }

            return (byte[])result;
        }

        /// <summary>
        /// 更新商品啟用/停用狀態。
        /// </summary>
        public async Task<byte[]> UpdateStatusAsync(int productId, bool targetActiveState, byte[] rowVersion, int updateUser)
        {
            string sql = @"
                UPDATE [dbo].[Product] 
                SET [IsActive] = @IsActive,
                    [UpdateTime] = GETDATE(),
                    [UpdateUser] = @UpdateUser
                OUTPUT INSERTED.RowVersion
                WHERE [ProductID] = @ProductID AND [RowVersion] = @RowVersion;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@IsActive", targetActiveState));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", updateUser));
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@ProductID", productId));
            cmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", rowVersion));

            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
            {
                throw new DBConcurrencyException("此商品狀態已被其他使用者異動，請重新載入最新資料後再試。");
            }

            return (byte[])result;
        }
    }
}