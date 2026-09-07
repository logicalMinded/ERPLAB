using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 進貨單資料存取層 (Repository)
    /// 負責處理 Purchase 實體的資料庫 I/O 操作，包含分頁查詢、TVP 批次寫入、樂觀鎖併發控制，
    /// 以及進貨單過帳/作廢時的核心連動邏輯：商品庫存數量與移動平均成本 (Moving Average Cost) 運算。
    /// </summary>
    public class PurchaseRepository
    {
        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================

        /// <summary>
        /// 取得進貨單清單 (分頁查詢)。
        /// 採用單次 Batch 執行多段 SQL，同步取得總筆數與分頁明細以最佳化 I/O 效能。
        /// </summary>
        public async Task<(List<PurchaseMaster> Items, int TotalCount)> GetPurchaseOrdersAsync(int pageNumber, int pageSize, string keyword = "", bool showVoided = false)
        {
            var list = new List<PurchaseMaster>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            // 將總筆數計算與分頁查詢合併為單一批次 (Batch) 執行，減少網路往返 (Round-trip)
            var sqlBuilder = new System.Text.StringBuilder(@"
                -- 語句 1：計算總筆數
                SELECT COUNT(1) 
                FROM [dbo].[PurchaseMaster] pm
                LEFT JOIN [dbo].[Vendor] v ON pm.[VendorID] = v.[VendorID]
                WHERE 1=1 AND (@ShowVoided = 1 OR pm.[Status] IN (1, 2)) ");

            if (!string.IsNullOrWhiteSpace(keyword))
                sqlBuilder.Append(" AND (pm.[PurchaseNo] LIKE @Keyword OR v.[VendorName] LIKE @Keyword) ");

            sqlBuilder.Append(@"
                ;
                -- 語句 2：分頁撈取實體資料
                SELECT 
                    pm.[PurchaseID], pm.[PurchaseNo], pm.[PurchaseDate], 
                    pm.[VendorID], pm.[TotalAmount], pm.[Remark], pm.[Status],
                    pm.[CreateTime], pm.[CreateUser], pm.[UpdateTime], pm.[UpdateUser], pm.[RowVersion],
                    
                    v.[VendorNo] AS VendorNo_Display, 
                    v.[VendorName] AS VendorName_Display,

                    empCreate.[EmployeeNo] AS CreateUserNo_Display,
                    empUpdate.[EmployeeNo] AS UpdateUserNo_Display
                FROM [dbo].[PurchaseMaster] pm
                
                -- 關聯取得廠商顯示名稱與人員顯示工號
                LEFT JOIN [dbo].[Vendor] v ON pm.[VendorID] = v.[VendorID]
                LEFT JOIN [dbo].[Accounts] accCreate ON pm.[CreateUser] = accCreate.[AccountID]
                LEFT JOIN [dbo].[Employee] empCreate ON accCreate.[EmployeeID] = empCreate.[EmployeeID]
                LEFT JOIN [dbo].[Accounts] accUpdate ON pm.[UpdateUser] = accUpdate.[AccountID]
                LEFT JOIN [dbo].[Employee] empUpdate ON accUpdate.[EmployeeID] = empUpdate.[EmployeeID]
                
                WHERE 1=1 AND (@ShowVoided = 1 OR pm.[Status] IN (1, 2)) ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(" AND (pm.[PurchaseNo] LIKE @Keyword OR v.[VendorName] LIKE @Keyword) ");
                cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Keyword", $"%{keyword.Trim()}%", 50));
            }

            // 預設依單據 ID 遞減排序 (最新建立者優先)
            sqlBuilder.Append(" ORDER BY pm.[PurchaseID] DESC ");

            // 分頁邊界防禦與參數處理
            if (pageSize > 0)
            {
                sqlBuilder.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@Offset", offset));
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@PageSize", pageSize));
            }
            else if (pageSize == 0) sqlBuilder.Append(";"); // 關閉分頁撈取全表
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
                    list.Add(new PurchaseMaster
                    {
                        PurchaseID = reader.GetInt64(reader.GetOrdinal("PurchaseID")),
                        PurchaseNo = reader.GetString(reader.GetOrdinal("PurchaseNo")),
                        PurchaseDate = reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                        VendorID = reader.GetInt32(reader.GetOrdinal("VendorID")),
                        TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                        Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),
                        Status = reader.GetByte(reader.GetOrdinal("Status")),

                        CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                        CreateUser = reader.GetInt32(reader.GetOrdinal("CreateUser")),
                        UpdateTime = reader.GetDateTime(reader.GetOrdinal("UpdateTime")),
                        UpdateUser = reader.GetInt32(reader.GetOrdinal("UpdateUser")),
                        RowVersion = (byte[])reader["RowVersion"],

                        VendorNo_Display = reader.IsDBNull(reader.GetOrdinal("VendorNo_Display")) ? null : reader.GetString(reader.GetOrdinal("VendorNo_Display")),
                        VendorName_Display = reader.IsDBNull(reader.GetOrdinal("VendorName_Display")) ? null : reader.GetString(reader.GetOrdinal("VendorName_Display")),
                        CreateUserNo_Display = reader.IsDBNull(reader.GetOrdinal("CreateUserNo_Display")) ? null : reader.GetString(reader.GetOrdinal("CreateUserNo_Display")),
                        UpdateUserNo_Display = reader.IsDBNull(reader.GetOrdinal("UpdateUserNo_Display")) ? null : reader.GetString(reader.GetOrdinal("UpdateUserNo_Display"))
                    });
                }
            }
            return (list, totalCount);
        }

        /// <summary>
        /// 取得指定進貨單之明細資料，並關聯商品主檔取得顯示名稱。
        /// </summary>
        public async Task<List<PurchaseDetail>> GetPurchaseDetailsAsync(long purchaseId)
        {
            var list = new List<PurchaseDetail>();
            string sql = @"
                SELECT 
                    pd.[PurchaseDID], pd.[PurchaseID], pd.[LineNo], pd.[ProductID], 
                    pd.[UnitPrice], pd.[Qty], pd.[Remark],
                    p.[ProductNo] AS ProductNo_Display, 
                    p.[ProductName] AS ProductName_Display
                FROM [dbo].[PurchaseDetail] pd
                LEFT JOIN [dbo].[Product] p ON pd.[ProductID] = p.[ProductID]
                WHERE pd.[PurchaseID] = @PurchaseID
                ORDER BY pd.[LineNo] ASC;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@PurchaseID", SqlDbType.BigInt) { Value = purchaseId });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PurchaseDetail
                {
                    PurchaseDID = reader.GetInt64(reader.GetOrdinal("PurchaseDID")),
                    PurchaseID = reader.GetInt64(reader.GetOrdinal("PurchaseID")),
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
        /// 建立進貨草稿單據。
        /// 使用 SqlTransaction 確保主檔與明細檔寫入之原子性 (Atomicity)，明細部分透過 TVP 進行批次寫入。
        /// </summary>
        public async Task<PurchaseMaster> CreatePurchaseOrderAsync(PurchaseMaster master, List<PurchaseDetail> details)
        {
            details ??= new List<PurchaseDetail>();

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                string masterSql = @"
                    INSERT INTO [dbo].[PurchaseMaster] 
                    ([PurchaseNo], [PurchaseDate], [VendorID], [TotalAmount], [Remark], [Status], [CreateUser], [UpdateUser])
                    OUTPUT INSERTED.PurchaseID, INSERTED.RowVersion
                    VALUES 
                    (@PurchaseNo, @PurchaseDate, @VendorID, @TotalAmount, @Remark, @Status, @CreateUser, @UpdateUser);";

                using var cmdMaster = new SqlCommand(masterSql, conn, tx);
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateVarChar("@PurchaseNo", master.PurchaseNo, 20));
                cmdMaster.Parameters.Add(new SqlParameter("@PurchaseDate", SqlDbType.DateTime) { Value = master.PurchaseDate });
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@VendorID", master.VendorID));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateDecimal("@TotalAmount", master.TotalAmount));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", master.Remark, 500));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateTinyInt("@Status", (byte)DocumentStatus.Draft));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@CreateUser", master.CreateUser));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", master.UpdateUser));

                // 透過 OUTPUT 子句取得自動生成的 ID 與 Timestamp
                using (var reader = await cmdMaster.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        master.PurchaseID = reader.GetInt64(0);
                        master.RowVersion = (byte[])reader[1];
                    }
                }

                if (details.Count > 0)
                {
                    string detailSql = @"
                        INSERT INTO [dbo].[PurchaseDetail] ([PurchaseID], [LineNo], [ProductID], [UnitPrice], [Qty], [Remark])
                        SELECT @PurchaseID, [LineNo], [ProductID], [UnitPrice], [Qty], [Remark]
                        FROM @DetailsTvp;";

                    using var cmdDetail = new SqlCommand(detailSql, conn, tx);
                    cmdDetail.Parameters.Add(new SqlParameter("@PurchaseID", SqlDbType.BigInt) { Value = master.PurchaseID });
                    cmdDetail.Parameters.Add(new SqlParameter("@DetailsTvp", SqlDbType.Structured)
                    {
                        TypeName = "dbo.PurchaseDetailType",
                        Value = TvpHelper.CreatePurchaseDetailTvp(details)
                    });

                    await cmdDetail.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return master;
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>
        /// 更新進貨草稿單據。
        /// 結合 RowVersion 與 ExpectedStatus 檢核樂觀鎖。明細檔採先刪後增 (Delete-then-Insert) 模式配合 TVP 寫入。
        /// </summary>
        public async Task<byte[]> UpdatePurchaseOrderDraftAsync(PurchaseMaster master, List<PurchaseDetail> details)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                string masterSql = @"
                    UPDATE [dbo].[PurchaseMaster] 
                    SET [PurchaseDate] = @PurchaseDate,
                        [VendorID] = @VendorID,
                        [TotalAmount] = @TotalAmount,
                        [Remark] = @Remark,
                        [UpdateTime] = GETDATE(),
                        [UpdateUser] = @UpdateUser
                    OUTPUT INSERTED.RowVersion
                    WHERE [PurchaseID] = @PurchaseID 
                      AND [RowVersion] = @RowVersion 
                      AND [Status] = @DraftStatus;";

                using var cmdMaster = new SqlCommand(masterSql, conn, tx);
                cmdMaster.Parameters.Add(new SqlParameter("@PurchaseDate", SqlDbType.DateTime) { Value = master.PurchaseDate });
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@VendorID", master.VendorID));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateDecimal("@TotalAmount", master.TotalAmount));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", master.Remark, 500));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", master.UpdateUser));
                cmdMaster.Parameters.Add(new SqlParameter("@PurchaseID", SqlDbType.BigInt) { Value = master.PurchaseID });
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", master.RowVersion));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateTinyInt("@DraftStatus", (byte)DocumentStatus.Draft));

                // 樂觀鎖驗證：確認狀態未變動且 Timestamp 吻合
                var result = await cmdMaster.ExecuteScalarAsync();
                if (result == null) throw new DBConcurrencyException("此單據已被異動，或已改變狀態，無法修改草稿！請重新載入資料。");

                string deleteSql = "DELETE FROM [dbo].[PurchaseDetail] WHERE [PurchaseID] = @PurchaseID;";
                using var cmdDelete = new SqlCommand(deleteSql, conn, tx);
                cmdDelete.Parameters.Add(new SqlParameter("@PurchaseID", SqlDbType.BigInt) { Value = master.PurchaseID });
                await cmdDelete.ExecuteNonQueryAsync();

                if (details != null && details.Count > 0)
                {
                    string detailSql = @"
                        INSERT INTO [dbo].[PurchaseDetail] ([PurchaseID], [LineNo], [ProductID], [UnitPrice], [Qty], [Remark])
                        SELECT @PurchaseID, [LineNo], [ProductID], [UnitPrice], [Qty], [Remark]
                        FROM @DetailsTvp;";

                    using var cmdDetail = new SqlCommand(detailSql, conn, tx);
                    cmdDetail.Parameters.Add(new SqlParameter("@PurchaseID", SqlDbType.BigInt) { Value = master.PurchaseID });
                    cmdDetail.Parameters.Add(new SqlParameter("@DetailsTvp", SqlDbType.Structured)
                    {
                        TypeName = "dbo.PurchaseDetailType",
                        Value = TvpHelper.CreatePurchaseDetailTvp(details)
                    });

                    await cmdDetail.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return (byte[])result;
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>
        /// 進貨單狀態推進 (過帳或作廢)。
        /// 過帳時將執行庫存增加並重新計算移動平均成本 (Moving Average Cost)；
        /// 作廢時將執行庫存扣除，並反推移動平均成本，同時受底層 CHECK Constraint 防禦超扣。
        /// </summary>
        public async Task<byte[]> UpdateOrderStatusAsync(long purchaseId, byte expectedCurrentStatus, byte targetStatus, byte[] rowVersion, int updateUser)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                string statusSql = @"
                    UPDATE [dbo].[PurchaseMaster]
                    SET [Status] = @TargetStatus,
                        [UpdateTime] = GETDATE(),
                        [UpdateUser] = @UpdateUser
                    OUTPUT INSERTED.RowVersion
                    WHERE [PurchaseID] = @PurchaseID 
                      AND [RowVersion] = @RowVersion 
                      AND [Status] = @ExpectedCurrentStatus;";

                using var cmdStatus = new SqlCommand(statusSql, conn, tx);
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateTinyInt("@TargetStatus", targetStatus));
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", updateUser));
                cmdStatus.Parameters.Add(new SqlParameter("@PurchaseID", SqlDbType.BigInt) { Value = purchaseId });
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", rowVersion));
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateTinyInt("@ExpectedCurrentStatus", expectedCurrentStatus));

                var result = await cmdStatus.ExecuteScalarAsync();
                if (result == null) throw new DBConcurrencyException("單據狀態已發生變更，請重新載入後再試！");

                // =====================================================================
                // 庫存與移動平均成本 (Moving Average Cost) 運算引擎
                // =====================================================================
                string stockUpdateSql = string.Empty;

                if (targetStatus == (byte)DocumentStatus.Posted)
                {
                    // 過帳：增加庫存並重算平均成本
                    stockUpdateSql = @"
                        UPDATE p
                        SET 
                            -- 避免除以零異常：若更新後總庫存小於等於 0 (異常防禦)，則維持原成本
                            p.[MovingAverageCost] = 
                                CASE 
                                    WHEN (p.[CurrentStock] + agg.[TotalQty]) <= 0 THEN p.[MovingAverageCost]
                                    ELSE ROUND(((p.[CurrentStock] * p.[MovingAverageCost]) + agg.[TotalCost]) / (p.[CurrentStock] + agg.[TotalQty]), 4)
                                END,
                            p.[CurrentStock] = p.[CurrentStock] + agg.[TotalQty],
                            p.[UpdateTime] = GETDATE(),
                            p.[UpdateUser] = @UpdateUser
                        FROM [dbo].[Product] p
                        INNER JOIN (
                            SELECT [ProductID], SUM([Qty]) AS TotalQty, SUM([Qty] * [UnitPrice]) AS TotalCost
                            FROM [dbo].[PurchaseDetail]
                            WHERE [PurchaseID] = @PurchaseID
                            GROUP BY [ProductID]
                        ) agg ON p.[ProductID] = agg.[ProductID];";
                }
                else if (targetStatus == (byte)DocumentStatus.Voided)
                {
                    // 作廢：扣除庫存並反推還原平均成本
                    stockUpdateSql = @"
                        UPDATE p
                        SET 
                            -- 避免除以零異常：若作廢後庫存歸零或小於 0，成本喪失數學意義，直接維持原成本
                            p.[MovingAverageCost] = 
                                CASE 
                                    WHEN (p.[CurrentStock] - agg.[TotalQty]) <= 0 THEN p.[MovingAverageCost]
                                    ELSE ROUND(((p.[CurrentStock] * p.[MovingAverageCost]) - agg.[TotalCost]) / (p.[CurrentStock] - agg.[TotalQty]), 4)
                                END,
                            p.[CurrentStock] = p.[CurrentStock] - agg.[TotalQty],
                            p.[UpdateTime] = GETDATE(),
                            p.[UpdateUser] = @UpdateUser
                        FROM [dbo].[Product] p
                        INNER JOIN (
                            SELECT [ProductID], SUM([Qty]) AS TotalQty, SUM([Qty] * [UnitPrice]) AS TotalCost
                            FROM [dbo].[PurchaseDetail]
                            WHERE [PurchaseID] = @PurchaseID
                            GROUP BY [ProductID]
                        ) agg ON p.[ProductID] = agg.[ProductID];";
                }

                if (!string.IsNullOrEmpty(stockUpdateSql))
                {
                    using var cmdStock = new SqlCommand(stockUpdateSql, conn, tx);
                    cmdStock.Parameters.Add(new SqlParameter("@PurchaseID", SqlDbType.BigInt) { Value = purchaseId });
                    cmdStock.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", updateUser));

                    // 若作廢時發生扣減庫存數量大於現有庫存量，資料庫底層的 CHECK Constraint 將會觸發 Error 547 拋出異常，中斷交易。
                    await cmdStock.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return (byte[])result;
            }
            catch { tx.Rollback(); throw; }
        }
    }
}