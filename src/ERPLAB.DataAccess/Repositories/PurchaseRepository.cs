using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    public class PurchaseRepository
    {
        public async Task<(List<PurchaseMaster> Items, int TotalCount)> GetPurchaseOrdersAsync(int pageNumber, int pageSize, string keyword = "", bool showVoided = false)
        {
            var list = new List<PurchaseMaster>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            var sqlBuilder = new System.Text.StringBuilder(@"
                SELECT COUNT(1) 
                FROM [dbo].[PurchaseMaster] pm
                LEFT JOIN [dbo].[Vendor] v ON pm.[VendorID] = v.[VendorID]
                WHERE 1=1 AND (@ShowVoided = 1 OR pm.[Status] IN (1, 2)) ");

            if (!string.IsNullOrWhiteSpace(keyword))
                sqlBuilder.Append(" AND (pm.[PurchaseNo] LIKE @Keyword OR v.[VendorName] LIKE @Keyword) ");

            sqlBuilder.Append(@"
                ;
                SELECT 
                    pm.[PurchaseID], pm.[PurchaseNo], pm.[PurchaseDate], 
                    pm.[VendorID], pm.[TotalAmount], pm.[Remark], pm.[Status],
                    pm.[CreateTime], pm.[CreateUser], pm.[UpdateTime], pm.[UpdateUser], pm.[RowVersion],
                    
                    v.[VendorNo] AS VendorNo_Display, 
                    v.[VendorName] AS VendorName_Display,

                    empCreate.[EmployeeNo] AS CreateUserNo_Display,
                    empUpdate.[EmployeeNo] AS UpdateUserNo_Display
                FROM [dbo].[PurchaseMaster] pm
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

            sqlBuilder.Append(" ORDER BY pm.[PurchaseID] DESC ");

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
                // 📦 💡 [實體庫存反向引擎] 進貨單過帳：加庫存 (+) / 作廢：扣庫存 (-)
                // =====================================================================
                string stockUpdateSql = string.Empty;

                if (targetStatus == (byte)DocumentStatus.Posted)
                {
                    stockUpdateSql = @"
                        UPDATE p
                        SET 
                            -- 數學防線：防範除以零 (若更新後庫存為 0，成本維持原價不變)
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
                    stockUpdateSql = @"
                        UPDATE p
                        SET 
                            -- 數學防線：若扣除後庫存小於等於 0，成本喪失數學意義，維持原價
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
                    // 作廢時若庫存不足扣抵，會觸發 CK_Product_CurrentStock 的 Error 547
                    await cmdStock.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return (byte[])result;
            }
            catch { tx.Rollback(); throw; }
        }
    }
}