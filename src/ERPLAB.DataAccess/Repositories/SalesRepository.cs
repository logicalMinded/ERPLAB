using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 銷貨單資料存取層 (Repository)。
    /// 負責處理 Sales 實體 (Master-Detail 架構) 的資料庫 I/O 操作。
    /// 核心實作包含 TVP 批次寫入、樂觀鎖併發控制 (Optimistic Concurrency)，
    /// 以及銷貨過帳/作廢時連動商品庫存增減與移動平均成本 (Moving Average Cost) 之還原運算。
    /// </summary>
    public class SalesRepository
    {
        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================

        /// <summary>
        /// 取得銷貨單清單 (分頁查詢)。
        /// 採用單次 Batch 執行多段 SQL，同步取得總筆數與分頁明細以最佳化 I/O 效能。
        /// </summary>
        public async Task<(List<SalesMaster> Items, int TotalCount)> GetSalesOrdersAsync(int pageNumber, int pageSize, string keyword = "", bool showVoided = false)
        {
            var list = new List<SalesMaster>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            // 將總筆數計算與分頁查詢合併為單一批次 (Batch) 執行，減少網路往返 (Round-trip)
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
                -- 語句 2：分頁撈取主檔實體，並關聯客戶與審計人員資訊
                SELECT 
                    sm.[SalesID], sm.[SalesNo], sm.[SalesDate], sm.[ShipDistrictID], sm.[ShipZipCode], sm.[ShipAddress], 
                    sm.[CustomerID], sm.[TotalAmount], sm.[Remark], sm.[Status],
                    sm.[CreateTime], sm.[CreateUser], sm.[UpdateTime], sm.[UpdateUser], sm.[RowVersion],
                    
                    c.[CustomerNo] AS CustomerNo_Display, 
                    c.[CustomerName] AS CustomerName_Display,

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

            // 預設依單據 ID 遞減排序 (最新建立者優先)
            sqlBuilder.Append(" ORDER BY sm.[SalesID] DESC ");

            // 分頁邊界防禦與參數處理
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

            // 處理雙重結果集 (Multiple Result Sets)
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
        /// 取得指定銷貨單之明細資料，並關聯商品主檔取得顯示名稱。
        /// </summary>
        public async Task<List<SalesDetail>> GetSalesDetailsAsync(long salesId)
        {
            var list = new List<SalesDetail>();

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
        // 資料交易服務 (Command)
        // =====================================================================

        /// <summary>
        /// 建立銷貨草稿單據。
        /// 使用 SqlTransaction 確保主檔與明細檔寫入之原子性 (Atomicity)，明細部分透過 TVP 進行批次寫入。
        /// </summary>
        public async Task<SalesMaster> CreateSalesOrderAsync(SalesMaster master, List<SalesDetail> details)
        {
            details ??= new List<SalesDetail>();

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                // 寫入主檔，並利用 OUTPUT 同步取回資料庫配發的主鍵 (SalesID) 與 Timestamp (RowVersion)
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

                // 將明細集合轉換為 TVP 格式批次寫入
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
                return master;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 更新銷貨草稿單據。
        /// 結合 RowVersion 與 DraftStatus 檢核樂觀鎖。明細檔採先刪後增 (Delete-then-Insert) 模式配合 TVP 寫入。
        /// </summary>
        public async Task<byte[]> UpdateSalesOrderDraftAsync(SalesMaster master, List<SalesDetail> details)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var tx = conn.BeginTransaction();

            try
            {
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

                // 樂觀鎖驗證：確認單據狀態為草稿且 Timestamp 吻合
                var result = await cmdMaster.ExecuteScalarAsync();
                if (result == null)
                {
                    throw new DBConcurrencyException("此單據已被異動，或已改變狀態 (如：已審核過帳)，無法修改草稿！請重新載入資料。");
                }

                // 物理抹除舊有明細資料
                string deleteSql = "DELETE FROM [dbo].[SalesDetail] WHERE [SalesID] = @SalesID;";
                using var cmdDelete = new SqlCommand(deleteSql, conn, tx);
                cmdDelete.Parameters.Add(new SqlParameter("@SalesID", SqlDbType.BigInt) { Value = master.SalesID });
                await cmdDelete.ExecuteNonQueryAsync();

                // 批次寫入新明細
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
                return (byte[])result;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 銷貨單狀態推進 (過帳或作廢)。
        /// 狀態變更與實體庫存異動必須在同一 Transaction 內完成，確保業務邏輯之 ACID 特性。
        /// 過帳時將執行庫存扣減，並將商品當下之移動平均成本寫入銷貨明細做為快照；作廢時則將商品庫存加回。
        /// </summary>
        public async Task<byte[]> UpdateOrderStatusAsync(long salesId, byte expectedCurrentStatus, byte targetStatus, byte[] rowVersion, int updateUser)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1. 更新主檔狀態與樂觀鎖防禦
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
                // 2. 庫存與移動平均成本 (Moving Average Cost) 運算引擎
                // =====================================================================
                string stockUpdateSql = string.Empty;

                if (targetStatus == (byte)DocumentStatus.Posted)
                {
                    // 過帳瞬間，將 Product 當下成本寫入 SalesDetail 作為歷史快照
                    string snapshotSql = @"
                        UPDATE sd
                        SET sd.[UnitCost] = p.[MovingAverageCost]
                        FROM [dbo].[SalesDetail] sd
                        INNER JOIN [dbo].[Product] p ON sd.[ProductID] = p.[ProductID]
                        WHERE sd.[SalesID] = @SalesID;";

                    using var cmdSnapshot = new SqlCommand(snapshotSql, conn, tx);
                    cmdSnapshot.Parameters.Add(new SqlParameter("@SalesID", SqlDbType.BigInt) { Value = salesId });
                    await cmdSnapshot.ExecuteNonQueryAsync();

                    // 扣除庫存 (銷貨出庫)
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
                else if (targetStatus == (byte)DocumentStatus.Voided)
                {
                    // 作廢：加回庫存並透過明細中的 UnitCost 快照反推還原移動平均成本
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

                    // 若過帳扣減時庫存不足，將觸發底層 CK_Product_CurrentStock 拋出 Error 547 例外並中斷交易
                    await cmdStock.ExecuteNonQueryAsync();
                }

                tx.Commit();
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