using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 商品基本檔倉儲。
    /// 核心防線：Update 時絕對隔離庫存欄位，防範透過基本檔維護畫面竄改庫存數量的重大內控漏洞。
    /// </summary>
    public class ProductRepository
    {
        // =====================================================================
        // 🔍 [檢索引擎] 支援動態過濾停用資料、多欄位模糊搜尋與極速分頁
        // =====================================================================
        public async Task<(List<Product> Items, int TotalCount)> GetProductsAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            var list = new List<Product>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

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
                -- 語句 2：分頁撈取實體資料與身分鑑識
                SELECT 
                    p.[ProductID], p.[ProductNo], p.[ProductName], p.[MovingAverageCost],
                    p.[PurchasePrice], p.[SalesPrice], p.[CurrentStock], 
                    p.[Description], p.[ImageName], p.[Remark],
                    p.[CreateTime], p.[CreateUser], p.[UpdateTime], p.[UpdateUser], 
                    p.[IsActive], p.[RowVersion],
                    
                    empCreate.[EmployeeNo] AS CreateUserNo_Display,
                    empUpdate.[EmployeeNo] AS UpdateUserNo_Display

                FROM [dbo].[Product] p
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

            sqlBuilder.Append(" ORDER BY p.[ProductID] DESC ");

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

                        // 財務數值讀取
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

        // =====================================================================
        // ➕ [寫入引擎] 
        // =====================================================================
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

            // 新增時，庫存預設寫入 0 或前端給定的初始值
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

        // =====================================================================
        // 📝 [更新引擎] 樂觀鎖防禦與物理隔離
        // =====================================================================
        public async Task<byte[]> UpdateAsync(Product entity)
        {
            // 🚨 物理隔離防線：UPDATE 語法中「絕對禁止」出現 [CurrentStock]！
            // 庫存只能由進銷存單據過帳時，透過交易發動增減。基本檔維護畫面無權干涉。
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

            if (result == null)
            {
                throw new DBConcurrencyException("此商品資料已被異動，請重新載入最新資料後再試。");
            }

            return (byte[])result;
        }

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

        public async Task<Product?> GetProductByNoAsync(string productNo)
        {
            // 💡 物理優化：只撈取打單當下「絕對必要」的欄位，將網路 I/O 封包體積壓縮到極限
            string sql = @"
                SELECT 
                    [ProductID], [ProductNo], [ProductName], [SalesPrice], [PurchasePrice]
                FROM [dbo].[Product]
                WHERE [ProductNo] = @ProductNo AND [IsActive] = 1;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            // 嚴格綁定 VARCHAR 防禦隱式轉換，觸發 SQL Server 的 Index Seek (索引尋結)
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

            return null; // 找不到或已停用，回傳 null 供 UI 攔截
        }
    }
}