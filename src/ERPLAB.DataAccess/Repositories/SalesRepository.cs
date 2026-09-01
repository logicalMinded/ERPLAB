using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 銷貨單倉儲 (Master-Detail 架構)。
    /// 核心防線：微交易取號、TVP 批次寫入、分散式交易控制 (SqlTransaction)、反正規化零信任計算、狀態機物理鎖定。
    /// </summary>
    public class SalesRepository
    {
        // =====================================================================
        // 🔍 [檢索引擎] 支援分頁 (Pagination) 與 JOIN 輔助欄位 (身分鑑識)
        // =====================================================================
        public async Task<(List<SalesMaster> Items, int TotalCount)> GetSalesOrdersAsync(int pageNumber, int pageSize, string keyword = "", bool showVoided = false)
        {
            var list = new List<SalesMaster>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            // 💡 MARS 多重結果集：同時要回總筆數與分頁明細
            var sqlBuilder = new System.Text.StringBuilder(@"
                -- 語句 1：計算總筆數
                SELECT COUNT(1) 
                FROM [dbo].[SalesMaster] sm
                LEFT JOIN [dbo].[Customer] c ON sm.[CustomerID] = c.[CustomerID]
                WHERE 1=1 AND (@ShowVoided = 1 OR sm.[Status] IN (1, 2)) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(" AND (sm.[SalesNo] LIKE @Keyword OR c.[CustomerName] LIKE @Keyword) ");
            }

            sqlBuilder.Append(@"
                ;
                -- 語句 2：分頁撈取主檔實體，並 JOIN 帶出廠商與審計資訊
                SELECT 
                    sm.[SalesID], sm.[SalesNo], sm.[SalesDate], sm.[ShipDistrictID], sm.[ShipZipCode], sm.[ShipAddress], 
                    sm.[CustomerID], sm.[TotalAmount], sm.[Remark], sm.[Status],
                    sm.[CreateTime], sm.[CreateUser], sm.[UpdateTime], sm.[UpdateUser], sm.[RowVersion],
                    
                    c.[CustomerNo] AS CustomerNo_Display, 
                    c.[CustomerName] AS CustomerName_Display,

                    -- 💡 跨表身分鑑識
                    empCreate.[EmployeeNo] AS CreateUserNo_Display,
                    empUpdate.[EmployeeNo] AS UpdateUserNo_Display

                FROM [dbo].[SalesMaster] sm
                LEFT JOIN [dbo].[Customer] c ON sm.[CustomerID] = c.[CustomerID]
                LEFT JOIN [dbo].[Accounts] accCreate ON sm.[CreateUser] = accCreate.[AccountID]
                LEFT JOIN [dbo].[Employee] empCreate ON accCreate.[EmployeeID] = empCreate.[EmployeeID]
                LEFT JOIN [dbo].[Accounts] accUpdate ON sm.[UpdateUser] = accUpdate.[AccountID]
                LEFT JOIN [dbo].[Employee] empUpdate ON accUpdate.[EmployeeID] = empUpdate.[EmployeeID]
                WHERE 1=1 AND (@ShowVoided = 1 OR sm.[Status] IN (1, 2)) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(" AND (sm.[SalesNo] LIKE @Keyword OR c.[CustomerName] LIKE @Keyword) ");
                cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Keyword", $"%{keyword.Trim()}%", 50));
            }

            sqlBuilder.Append(" ORDER BY sm.[SalesID] DESC ");

            if (pageSize > 0)
            {
                sqlBuilder.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@Offset", offset));
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@PageSize", pageSize));
            }
            else if (pageSize == 0) sqlBuilder.Append(";");
            else throw new ArgumentOutOfRangeException(nameof(pageSize), "分頁筆數必須大於或等於 0！");

            cmd.Parameters.Add(SqlParameterFactory.CreateBit("@ShowVoided", showVoided));
            cmd.CommandText = sqlBuilder.ToString();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync()) totalCount = reader.GetInt32(0);

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var sm = new SalesMaster
                    {
                        SalesID = reader.GetInt64(reader.GetOrdinal("SalesID")),
                        SalesNo = reader.GetString(reader.GetOrdinal("SalesNo")),
                        SalesDate = reader.GetDateTime(reader.GetOrdinal("SalesDate")),
                        ShipDistrictID = reader.GetInt32(reader.GetOrdinal("ShipDistrictID")),
                        ShipZipCode = reader.GetString(reader.GetOrdinal("ShipZipCode")),
                        ShipAddress = reader.GetString(reader.GetOrdinal("ShipAddress")),
                        CustomerID = reader.GetInt32(reader.GetOrdinal("CustomerID")),
                        TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                        Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),
                        Status = reader.GetByte(reader.GetOrdinal("Status")),

                        CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                        CreateUser = reader.GetInt32(reader.GetOrdinal("CreateUser")),
                        UpdateTime = reader.GetDateTime(reader.GetOrdinal("UpdateTime")),
                        UpdateUser = reader.GetInt32(reader.GetOrdinal("UpdateUser")),
                        RowVersion = (byte[])reader["RowVersion"],

                        CustomerNo_Display = reader.IsDBNull(reader.GetOrdinal("CustomerNo_Display")) ? null : reader.GetString(reader.GetOrdinal("CustomerNo_Display")),
                        CustomerName_Display = reader.IsDBNull(reader.GetOrdinal("CustomerName_Display")) ? null : reader.GetString(reader.GetOrdinal("CustomerName_Display")),
                        CreateUserNo_Display = reader.IsDBNull(reader.GetOrdinal("CreateUserNo_Display")) ? null : reader.GetString(reader.GetOrdinal("CreateUserNo_Display")),
                        UpdateUserNo_Display = reader.IsDBNull(reader.GetOrdinal("UpdateUserNo_Display")) ? null : reader.GetString(reader.GetOrdinal("UpdateUserNo_Display"))
                    };
                    list.Add(sm);
                }
            }
            return (list, totalCount);
        }

        /// <summary>
        /// 取得單張銷貨單的所有明細
        /// </summary>
        public async Task<List<SalesDetail>> GetSalesDetailsAsync(long salesId)
        {
            var list = new List<SalesDetail>();

            // 💡 JOIN 商品基本檔以取得代碼與品名，供 UI 顯示使用
            string sql = @"
                SELECT 
                    sd.[SalesDID], sd.[SalesID], sd.[LineNo], sd.[ProductID], 
                    sd.[UnitPrice], sd.[Qty], sd.[Remark],
                    p.[ProductNo] AS ProductNo_Display, 
                    p.[ProductName] AS ProductName_Display
                FROM [dbo].[SalesDetail] sd
                LEFT JOIN [dbo].[Product] p ON sd.[ProductID] = p.[ProductID]
                WHERE sd.[SalesID] = @SalesID
                ORDER BY sd.[LineNo] ASC;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@SalesID", SqlDbType.BigInt) { Value = salesId });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new SalesDetail
                {
                    SalesDID = reader.GetInt64(reader.GetOrdinal("SalesDID")),
                    SalesID = reader.GetInt64(reader.GetOrdinal("SalesID")),
                    LineNo = reader.GetInt32(reader.GetOrdinal("LineNo")),
                    ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                    UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    Qty = reader.GetInt32(reader.GetOrdinal("Qty")),
                    Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),

                    ProductNo_Display = reader.IsDBNull(reader.GetOrdinal("ProductNo_Display")) ? null : reader.GetString(reader.GetOrdinal("ProductNo_Display")),
                    ProductName_Display = reader.IsDBNull(reader.GetOrdinal("ProductName_Display")) ? null : reader.GetString(reader.GetOrdinal("ProductName_Display"))
                });
            }
            return list;
        }

        // =====================================================================
        // ➕ [交易引擎] 建立新單據 (Master-Detail TVP 寫入)
        // =====================================================================
        public async Task<SalesMaster> CreateSalesOrderAsync(SalesMaster master, List<SalesDetail> details)
        {
            details ??= new List<SalesDetail>();

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            // 💡 開啟分散式交易
            using var tx = conn.BeginTransaction();
            try
            {
                // 💡 寫入主檔，並利用 OUTPUT 瞬間取回資料庫配發的 BIGINT 主鍵與樂觀鎖
                string masterSql = @"
                    INSERT INTO [dbo].[SalesMaster] 
                    ([SalesNo], [SalesDate], [ShipDistrictID], [ShipZipCode], [ShipAddress], [CustomerID], 
                     [TotalAmount], [Remark], [Status], [CreateUser], [UpdateUser])
                    OUTPUT INSERTED.SalesID, INSERTED.RowVersion
                    VALUES 
                    (@SalesNo, @SalesDate, @ShipDistrictID, @ShipZipCode, @ShipAddress, @CustomerID, 
                     @TotalAmount, @Remark, @Status, @CreateUser, @UpdateUser);";

                using var cmdMaster = new SqlCommand(masterSql, conn, tx);
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateVarChar("@SalesNo", master.SalesNo, 20));
                cmdMaster.Parameters.Add(new SqlParameter("@SalesDate", SqlDbType.DateTime) { Value = master.SalesDate });
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@ShipDistrictID", master.ShipDistrictID));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateVarChar("@ShipZipCode", master.ShipZipCode, 6));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateNVarChar("@ShipAddress", master.ShipAddress, 200));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@CustomerID", master.CustomerID));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateDecimal("@TotalAmount", master.TotalAmount));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", master.Remark, 500));

                // 新增單據必定為草稿 (未過帳)
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateTinyInt("@Status", (byte)DocumentStatus.Draft));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@CreateUser", master.CreateUser));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", master.UpdateUser));

                using (var reader = await cmdMaster.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        master.SalesID = reader.GetInt64(0);
                        master.RowVersion = (byte[])reader[1];
                    }
                }

                // 💡 TVP 批次寫入明細 (1 次 I/O 寫入全表明細)
                if (details != null && details.Count > 0)
                {
                    string detailSql = @"
                        INSERT INTO [dbo].[SalesDetail] ([SalesID], [LineNo], [ProductID], [UnitPrice], [Qty], [Remark])
                        SELECT @SalesID, [LineNo], [ProductID], [UnitPrice], [Qty], [Remark]
                        FROM @DetailsTvp;";

                    using var cmdDetail = new SqlCommand(detailSql, conn, tx);
                    cmdDetail.Parameters.Add(new SqlParameter("@SalesID", SqlDbType.BigInt) { Value = master.SalesID });

                    // 💡 將 List 轉為 DataTable 並指定為 Structured (結構化) 參數
                    cmdDetail.Parameters.Add(new SqlParameter("@DetailsTvp", SqlDbType.Structured)
                    {
                        TypeName = "dbo.SalesDetailType",
                        Value = TvpHelper.CreateSalesDetailTvp(details)
                    });

                    await cmdDetail.ExecuteNonQueryAsync();
                }

                tx.Commit(); // 物理提交，寫入硬碟
                return master;
            }
            catch
            {
                tx.Rollback(); // 遭遇任何錯誤 (包含約束違反)，全數退回
                throw;
            }
        }

        // =====================================================================
        // 📝 [更新引擎] 樂觀鎖防禦 + 明細砍掉重練 (The Purge & Replace Pattern)
        // =====================================================================
        public async Task<byte[]> UpdateSalesOrderDraftAsync(SalesMaster master, List<SalesDetail> details)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1. 更新主檔 (掛載樂觀鎖與狀態防呆)
                // 🚨 物理阻斷：狀態必須為 Draft(1) 才允許修改草稿！若為其他狀態直接引發 0 筆更新例外
                string masterSql = @"
                    UPDATE [dbo].[SalesMaster] 
                    SET [SalesDate] = @SalesDate,
                        [ShipZipCode] = @ShipZipCode,
                        [ShipAddress] = @ShipAddress,
                        [CustomerID] = @CustomerID,
                        [TotalAmount] = @TotalAmount,
                        [Remark] = @Remark,
                        [UpdateTime] = GETDATE(),
                        [UpdateUser] = @UpdateUser
                    OUTPUT INSERTED.RowVersion
                    WHERE [SalesID] = @SalesID 
                      AND [RowVersion] = @RowVersion 
                      AND [Status] = @DraftStatus;";

                using var cmdMaster = new SqlCommand(masterSql, conn, tx);
                cmdMaster.Parameters.Add(new SqlParameter("@SalesDate", SqlDbType.DateTime) { Value = master.SalesDate });
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@ShipDistrictID", master.ShipDistrictID));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateVarChar("@ShipZipCode", master.ShipZipCode, 6));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateNVarChar("@ShipAddress", master.ShipAddress, 200));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@CustomerID", master.CustomerID));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateDecimal("@TotalAmount", master.TotalAmount));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", master.Remark, 500));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", master.UpdateUser));

                cmdMaster.Parameters.Add(new SqlParameter("@SalesID", SqlDbType.BigInt) { Value = master.SalesID });
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", master.RowVersion));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateTinyInt("@DraftStatus", (byte)DocumentStatus.Draft));

                var result = await cmdMaster.ExecuteScalarAsync();
                if (result == null)
                {
                    throw new DBConcurrencyException("此單據已被異動，或已改變狀態 (如：已審核過帳)，無法修改草稿！請重新載入資料。");
                }

                // 2. 🧹 物理抹除舊明細 (免除狀態追蹤地獄)
                string deleteSql = "DELETE FROM [dbo].[SalesDetail] WHERE [SalesID] = @SalesID;";
                using var cmdDelete = new SqlCommand(deleteSql, conn, tx);
                cmdDelete.Parameters.Add(new SqlParameter("@SalesID", SqlDbType.BigInt) { Value = master.SalesID });
                await cmdDelete.ExecuteNonQueryAsync();

                // 3. 🚀 TVP 重新寫入新明細 (與 Create 邏輯相同)
                if (details != null && details.Count > 0)
                {
                    string detailSql = @"
                        INSERT INTO [dbo].[SalesDetail] ([SalesID], [LineNo], [ProductID], [UnitPrice], [Qty], [Remark])
                        SELECT @SalesID, [LineNo], [ProductID], [UnitPrice], [Qty], [Remark]
                        FROM @DetailsTvp;";

                    using var cmdDetail = new SqlCommand(detailSql, conn, tx);
                    cmdDetail.Parameters.Add(new SqlParameter("@SalesID", SqlDbType.BigInt) { Value = master.SalesID });
                    cmdDetail.Parameters.Add(new SqlParameter("@DetailsTvp", SqlDbType.Structured)
                    {
                        TypeName = "dbo.SalesDetailType",
                        Value = TvpHelper.CreateSalesDetailTvp(details)
                    });

                    await cmdDetail.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return (byte[])result; // 回傳最新的時間戳記供 UI 同步
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // =====================================================================
        // 🔄 [狀態機推進引擎] 單向變更狀態 (如：審核過帳、註銷、作廢)
        // 業界實務：修改狀態時，絕對不允許同時修改單據內容，確保內控獨立性。
        // =====================================================================
        public async Task<byte[]> UpdateOrderStatusAsync(long salesId, byte expectedCurrentStatus, byte targetStatus, byte[] rowVersion, int updateUser)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();

            // 💡 物理防線：狀態切換與庫存異動必須「同生共死」
            using var tx = conn.BeginTransaction();

            try
            {
                // 1. 執行狀態機單向推進與樂觀鎖防禦
                string statusSql = @"
                    UPDATE [dbo].[SalesMaster]
                    SET [Status] = @TargetStatus,
                        [UpdateTime] = GETDATE(),
                        [UpdateUser] = @UpdateUser
                    OUTPUT INSERTED.RowVersion
                    WHERE [SalesID] = @SalesID 
                      AND [RowVersion] = @RowVersion 
                      AND [Status] = @ExpectedCurrentStatus;";

                using var cmdStatus = new SqlCommand(statusSql, conn, tx);
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateTinyInt("@TargetStatus", targetStatus));
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", updateUser));
                cmdStatus.Parameters.Add(new SqlParameter("@SalesID", SqlDbType.BigInt) { Value = salesId });
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", rowVersion));
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateTinyInt("@ExpectedCurrentStatus", expectedCurrentStatus));

                var result = await cmdStatus.ExecuteScalarAsync();

                if (result == null)
                {
                    throw new DBConcurrencyException("單據狀態已發生變更 (可能已被其他主管審核或作廢)，請重新載入後再試！");
                }

                // =====================================================================
                // 📦 2. [物理庫存連動引擎] 依據狀態決定「扣庫存」或「加庫存」
                // =====================================================================
                string stockUpdateSql = string.Empty;

                // 狀態 1 -> 2 (審核過帳)：扣除庫存 (使用 - 號)，寫入成本快照
                if (targetStatus == (byte)DocumentStatus.Posted)
                {
                    // 過帳瞬間，強制將 Product 當下成本快照寫死進 SalesDetail
                    string snapshotSql = @"
                        UPDATE sd
                        SET sd.[UnitCost] = p.[MovingAverageCost]
                        FROM [dbo].[SalesDetail] sd
                        INNER JOIN [dbo].[Product] p ON sd.[ProductID] = p.[ProductID]
                        WHERE sd.[SalesID] = @SalesID;";

                    using var cmdSnapshot = new SqlCommand(snapshotSql, conn, tx);
                    cmdSnapshot.Parameters.Add(new SqlParameter("@SalesID", SqlDbType.BigInt) { Value = salesId });
                    await cmdSnapshot.ExecuteNonQueryAsync();

                    // 💡 聚合更新防線：必須先 SUM(Qty)，防範同一張單據內輸入重複商品導致漏扣！
                    stockUpdateSql = @"
                        UPDATE p
                        SET p.[CurrentStock] = p.[CurrentStock] - agg.[TotalQty],
                            p.[UpdateTime] = GETDATE(),
                            p.[UpdateUser] = @UpdateUser
                        FROM [dbo].[Product] p
                        INNER JOIN (
                            SELECT [ProductID], SUM([Qty]) AS TotalQty
                            FROM [dbo].[SalesDetail]
                            WHERE [SalesID] = @SalesID
                            GROUP BY [ProductID]
                        ) agg ON p.[ProductID] = agg.[ProductID];";
                }
                // 狀態 2 -> 4 (作廢沖銷)：加回庫存 (使用 + 號)
                else if (targetStatus == (byte)DocumentStatus.Voided)
                {
                    stockUpdateSql = @"
                        UPDATE p
                        SET 
                            p.[MovingAverageCost] = 
                                CASE 
                                    WHEN (p.[CurrentStock] + agg.[TotalQty]) <= 0 THEN p.[MovingAverageCost]
                                    ELSE ROUND(((p.[CurrentStock] * p.[MovingAverageCost]) + agg.[TotalCostSnapshot]) / (p.[CurrentStock] + agg.[TotalQty]), 4)
                                END,
                            p.[CurrentStock] = p.[CurrentStock] + agg.[TotalQty],
                            p.[UpdateTime] = GETDATE(),
                            p.[UpdateUser] = @UpdateUser
                        FROM [dbo].[Product] p
                        INNER JOIN (
                            -- 提取快照成本參與重算
                            SELECT [ProductID], SUM([Qty]) AS TotalQty, SUM([Qty] * [UnitCost]) AS TotalCostSnapshot
                            FROM [dbo].[SalesDetail]
                            WHERE [SalesID] = @SalesID
                            GROUP BY [ProductID]
                        ) agg ON p.[ProductID] = agg.[ProductID];";
                }

                // 執行庫存異動
                if (!string.IsNullOrEmpty(stockUpdateSql))
                {
                    using var cmdStock = new SqlCommand(stockUpdateSql, conn, tx);
                    cmdStock.Parameters.Add(new SqlParameter("@SalesID", SqlDbType.BigInt) { Value = salesId });
                    cmdStock.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", updateUser));

                    // 🚨 這裡可能會觸發 CK_Product_CurrentStock (庫存不可為負數) 的 Error 547！
                    await cmdStock.ExecuteNonQueryAsync();
                }

                tx.Commit(); // 狀態與庫存異動雙雙落地
                return (byte[])result;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}