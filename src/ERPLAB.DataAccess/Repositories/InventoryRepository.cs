using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    public class InventoryRepository
    {
        // =====================================================================
        // 🔍 [檢索引擎] 
        // 物理特性：無 IsActive，全表撈取 (可額外傳入狀態過濾)
        // =====================================================================
        public async Task<(List<InventoryMaster> Items, int TotalCount)> GetInventoryOrdersAsync(int pageNumber, int pageSize, string keyword = "")
        {
            var list = new List<InventoryMaster>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            var sqlBuilder = new System.Text.StringBuilder(@"
                SELECT COUNT(1) 
                FROM [dbo].[InventoryMaster] im
                LEFT JOIN [dbo].[Employee] e ON im.[EmployeeID] = e.[EmployeeID]
                WHERE 1=1 ");

            if (!string.IsNullOrWhiteSpace(keyword))
                sqlBuilder.Append(" AND (im.[InventoryNo] LIKE @Keyword OR e.[EmployeeName] LIKE @Keyword) ");

            sqlBuilder.Append(@"
                ;
                SELECT 
                    im.[InventoryID], im.[InventoryNo], im.[InventoryDate], 
                    im.[EmployeeID], im.[Remark], im.[Status],
                    im.[CreateTime], im.[CreateUser], im.[UpdateTime], im.[UpdateUser], im.[RowVersion],
                    
                    e.[EmployeeNo] AS EmployeeNo_Display, 
                    e.[EmployeeName] AS EmployeeName_Display,

                    empCreate.[EmployeeNo] AS CreateUserNo_Display,
                    empUpdate.[EmployeeNo] AS UpdateUserNo_Display
                FROM [dbo].[InventoryMaster] im
                LEFT JOIN [dbo].[Employee] e ON im.[EmployeeID] = e.[EmployeeID]
                LEFT JOIN [dbo].[Accounts] accCreate ON im.[CreateUser] = accCreate.[AccountID]
                LEFT JOIN [dbo].[Employee] empCreate ON accCreate.[EmployeeID] = empCreate.[EmployeeID]
                LEFT JOIN [dbo].[Accounts] accUpdate ON im.[UpdateUser] = accUpdate.[AccountID]
                LEFT JOIN [dbo].[Employee] empUpdate ON accUpdate.[EmployeeID] = empUpdate.[EmployeeID]
                WHERE 1=1 ");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sqlBuilder.Append(" AND (im.[InventoryNo] LIKE @Keyword OR e.[EmployeeName] LIKE @Keyword) ");
                cmd.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Keyword", $"%{keyword.Trim()}%", 50));
            }

            sqlBuilder.Append(" ORDER BY im.[InventoryID] DESC ");

            if (pageSize > 0)
            {
                sqlBuilder.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@Offset", offset));
                cmd.Parameters.Add(SqlParameterFactory.CreateInt("@PageSize", pageSize));
            }
            else if (pageSize == 0) sqlBuilder.Append(";");
            else throw new ArgumentOutOfRangeException(nameof(pageSize));

            cmd.CommandText = sqlBuilder.ToString();

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync()) totalCount = reader.GetInt32(0);

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    list.Add(new InventoryMaster
                    {
                        InventoryID = reader.GetInt64(reader.GetOrdinal("InventoryID")),
                        InventoryNo = reader.GetString(reader.GetOrdinal("InventoryNo")),
                        InventoryDate = reader.GetDateTime(reader.GetOrdinal("InventoryDate")),
                        EmployeeID = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                        Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),
                        Status = reader.GetByte(reader.GetOrdinal("Status")),

                        CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                        CreateUser = reader.GetInt32(reader.GetOrdinal("CreateUser")),
                        UpdateTime = reader.GetDateTime(reader.GetOrdinal("UpdateTime")),
                        UpdateUser = reader.GetInt32(reader.GetOrdinal("UpdateUser")),
                        RowVersion = (byte[])reader["RowVersion"],

                        EmployeeNo_Display = reader.IsDBNull(reader.GetOrdinal("EmployeeNo_Display")) ? null : reader.GetString(reader.GetOrdinal("EmployeeNo_Display")),
                        EmployeeName_Display = reader.IsDBNull(reader.GetOrdinal("EmployeeName_Display")) ? null : reader.GetString(reader.GetOrdinal("EmployeeName_Display")),
                        CreateUserNo_Display = reader.IsDBNull(reader.GetOrdinal("CreateUserNo_Display")) ? null : reader.GetString(reader.GetOrdinal("CreateUserNo_Display")),
                        UpdateUserNo_Display = reader.IsDBNull(reader.GetOrdinal("UpdateUserNo_Display")) ? null : reader.GetString(reader.GetOrdinal("UpdateUserNo_Display"))
                    });
                }
            }
            return (list, totalCount);
        }

        public async Task<List<InventoryDetail>> GetInventoryDetailsAsync(long inventoryId)
        {
            var list = new List<InventoryDetail>();
            string sql = @"
                SELECT 
                    id.[InventoryDID], id.[InventoryID], id.[LineNo], id.[ProductID], 
                    id.[SystemStock], id.[ActualStock], id.[StockPrice], id.[Remark],
                    p.[ProductNo] AS ProductNo_Display, 
                    p.[ProductName] AS ProductName_Display
                FROM [dbo].[InventoryDetail] id
                LEFT JOIN [dbo].[Product] p ON id.[ProductID] = p.[ProductID]
                WHERE id.[InventoryID] = @InventoryID
                ORDER BY id.[LineNo] ASC;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@InventoryID", SqlDbType.BigInt) { Value = inventoryId });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new InventoryDetail
                {
                    InventoryDID = reader.GetInt64(reader.GetOrdinal("InventoryDID")),
                    InventoryID = reader.GetInt64(reader.GetOrdinal("InventoryID")),
                    LineNo = reader.GetInt32(reader.GetOrdinal("LineNo")),
                    ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                    SystemStock = reader.GetInt32(reader.GetOrdinal("SystemStock")),
                    ActualStock = reader.GetInt32(reader.GetOrdinal("ActualStock")),
                    StockPrice = reader.GetDecimal(reader.GetOrdinal("StockPrice")),
                    Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),
                    ProductNo_Display = reader.IsDBNull(reader.GetOrdinal("ProductNo_Display")) ? null : reader.GetString(reader.GetOrdinal("ProductNo_Display")),
                    ProductName_Display = reader.IsDBNull(reader.GetOrdinal("ProductName_Display")) ? null : reader.GetString(reader.GetOrdinal("ProductName_Display"))
                });
            }
            return list;
        }

        // =====================================================================
        // ➕ [交易引擎] 建立草稿 (TVP 寫入)
        // =====================================================================
        public async Task<InventoryMaster> CreateInventoryOrderAsync(InventoryMaster master, List<InventoryDetail> details)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                string masterSql = @"
                    INSERT INTO [dbo].[InventoryMaster] 
                    ([InventoryNo], [InventoryDate], [EmployeeID], [Remark], [Status], [CreateUser], [UpdateUser])
                    OUTPUT INSERTED.InventoryID, INSERTED.RowVersion
                    VALUES 
                    (@InventoryNo, @InventoryDate, @EmployeeID, @Remark, @Status, @CreateUser, @UpdateUser);";

                using var cmdMaster = new SqlCommand(masterSql, conn, tx);
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateVarChar("@InventoryNo", master.InventoryNo, 20));
                cmdMaster.Parameters.Add(new SqlParameter("@InventoryDate", SqlDbType.DateTime) { Value = master.InventoryDate });
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@EmployeeID", master.EmployeeID));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", master.Remark, 500));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateTinyInt("@Status", (byte)DocumentStatus.Draft));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@CreateUser", master.CreateUser));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", master.UpdateUser));

                using (var reader = await cmdMaster.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        master.InventoryID = reader.GetInt64(0);
                        master.RowVersion = (byte[])reader[1];
                    }
                }

                if (details != null && details.Count > 0)
                {
                    string detailSql = @"
                        INSERT INTO [dbo].[InventoryDetail] ([InventoryID], [LineNo], [ProductID], [SystemStock], [ActualStock], [StockPrice], [Remark])
                        SELECT @InventoryID, [LineNo], [ProductID], [SystemStock], [ActualStock], [StockPrice], [Remark]
                        FROM @DetailsTvp;";

                    using var cmdDetail = new SqlCommand(detailSql, conn, tx);
                    cmdDetail.Parameters.Add(new SqlParameter("@InventoryID", SqlDbType.BigInt) { Value = master.InventoryID });
                    cmdDetail.Parameters.Add(new SqlParameter("@DetailsTvp", SqlDbType.Structured)
                    {
                        TypeName = "dbo.InventoryDetailType",
                        Value = TvpHelper.CreateInventoryDetailTvp(details)
                    });

                    await cmdDetail.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return master;
            }
            catch { tx.Rollback(); throw; }
        }

        // =====================================================================
        // 📝 [更新引擎] 樂觀鎖與明細砍掉重練
        // =====================================================================
        public async Task<byte[]> UpdateInventoryOrderDraftAsync(InventoryMaster master, List<InventoryDetail> details)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                string masterSql = @"
                    UPDATE [dbo].[InventoryMaster] 
                    SET [InventoryDate] = @InventoryDate,
                        [EmployeeID] = @EmployeeID,
                        [Remark] = @Remark,
                        [UpdateTime] = GETDATE(),
                        [UpdateUser] = @UpdateUser
                    OUTPUT INSERTED.RowVersion
                    WHERE [InventoryID] = @InventoryID 
                      AND [RowVersion] = @RowVersion 
                      AND [Status] = @ExpectedStatus;";

                using var cmdMaster = new SqlCommand(masterSql, conn, tx);
                cmdMaster.Parameters.Add(new SqlParameter("@InventoryDate", SqlDbType.DateTime) { Value = master.InventoryDate });
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@EmployeeID", master.EmployeeID));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateNVarChar("@Remark", master.Remark, 500));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", master.UpdateUser));
                cmdMaster.Parameters.Add(new SqlParameter("@InventoryID", SqlDbType.BigInt) { Value = master.InventoryID });
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", master.RowVersion));
                cmdMaster.Parameters.Add(SqlParameterFactory.CreateTinyInt("@ExpectedStatus", (byte)DocumentStatus.Draft));

                var result = await cmdMaster.ExecuteScalarAsync();
                if (result == null) throw new DBConcurrencyException("此盤點單已被異動，或已改變狀態，無法修改！請重新載入資料。");

                string deleteSql = "DELETE FROM [dbo].[InventoryDetail] WHERE [InventoryID] = @InventoryID;";
                using var cmdDelete = new SqlCommand(deleteSql, conn, tx);
                cmdDelete.Parameters.Add(new SqlParameter("@InventoryID", SqlDbType.BigInt) { Value = master.InventoryID });
                await cmdDelete.ExecuteNonQueryAsync();

                if (details != null && details.Count > 0)
                {
                    string detailSql = @"
                        INSERT INTO [dbo].[InventoryDetail] ([InventoryID], [LineNo], [ProductID], [SystemStock], [ActualStock], [StockPrice], [Remark])
                        SELECT @InventoryID, [LineNo], [ProductID], [SystemStock], [ActualStock], [StockPrice], [Remark]
                        FROM @DetailsTvp;";

                    using var cmdDetail = new SqlCommand(detailSql, conn, tx);
                    cmdDetail.Parameters.Add(new SqlParameter("@InventoryID", SqlDbType.BigInt) { Value = master.InventoryID });
                    cmdDetail.Parameters.Add(new SqlParameter("@DetailsTvp", SqlDbType.Structured)
                    {
                        TypeName = "dbo.InventoryDetailType",
                        Value = TvpHelper.CreateInventoryDetailTvp(details)
                    });

                    await cmdDetail.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return (byte[])result;
            }
            catch { tx.Rollback(); throw; }
        }

        // =====================================================================
        // 🗑️ [物理抹除引擎] 僅限草稿狀態可發動
        // =====================================================================
        public async Task DeleteDraftAsync(long inventoryId, byte[] rowVersion)
        {
            // 💡 依賴資料庫 DDL 中的 ON DELETE CASCADE (明細會自動被連帶刪除)
            // 💡 依賴 Trigger [TR_InventoryMaster_ProtectDelete] 進行過帳防禦攔截
            string sql = @"
                DELETE FROM [dbo].[InventoryMaster] 
                WHERE [InventoryID] = @InventoryID 
                  AND [RowVersion] = @RowVersion 
                  AND [Status] = @ExpectedStatus;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@InventoryID", SqlDbType.BigInt) { Value = inventoryId });
            cmd.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", rowVersion));
            cmd.Parameters.Add(SqlParameterFactory.CreateTinyInt("@ExpectedStatus", (byte)DocumentStatus.Draft));

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
            {
                throw new DBConcurrencyException("單據已被異動或狀態改變，無法刪除！");
            }
        }

        // =====================================================================
        // 🔐 [狀態推進引擎] 差異沖平算法 (The Delta Adjustment Pattern)
        // =====================================================================
        public async Task<byte[]> UpdateOrderStatusAsync(long inventoryId, byte[] rowVersion, int updateUser)
        {
            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                string statusSql = @"
                    UPDATE [dbo].[InventoryMaster]
                    SET [Status] = @TargetStatus,
                        [UpdateTime] = GETDATE(),
                        [UpdateUser] = @UpdateUser
                    OUTPUT INSERTED.RowVersion
                    WHERE [InventoryID] = @InventoryID 
                      AND [RowVersion] = @RowVersion 
                      AND [Status] = @ExpectedCurrentStatus;";

                using var cmdStatus = new SqlCommand(statusSql, conn, tx);
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateTinyInt("@TargetStatus", (byte)DocumentStatus.Posted));
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", updateUser));
                cmdStatus.Parameters.Add(new SqlParameter("@InventoryID", SqlDbType.BigInt) { Value = inventoryId });
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateTimestamp("@RowVersion", rowVersion));
                cmdStatus.Parameters.Add(SqlParameterFactory.CreateTinyInt("@ExpectedCurrentStatus", (byte)DocumentStatus.Draft));

                var result = await cmdStatus.ExecuteScalarAsync();
                if (result == null) throw new DBConcurrencyException("單據狀態已發生變更，請重新載入後再試！");

                // =====================================================================
                // 📦 💡 [差異沖平算法] 
                // 絕對禁止直接將庫存覆寫為 ActualStock！
                // 必須計算 (實盤 - 帳面) 得到盤盈虧差異值，將此差異「加」回當前實體庫存。
                // 如此方能免疫「盤點草稿期間，發生其他進銷貨交易」的併發覆寫地雷！
                // =====================================================================
                string stockUpdateSql = @"
                        UPDATE p
                        SET p.[CurrentStock] = p.[CurrentStock] + agg.[DiffQty],
                            p.[UpdateTime] = GETDATE(),
                            p.[UpdateUser] = @UpdateUser
                        FROM [dbo].[Product] p
                        INNER JOIN (
                            SELECT [ProductID], SUM([ActualStock] - [SystemStock]) AS DiffQty
                            FROM [dbo].[InventoryDetail]
                            WHERE [InventoryID] = @InventoryID
                            GROUP BY [ProductID]
                        ) agg ON p.[ProductID] = agg.[ProductID];";

                using var cmdStock = new SqlCommand(stockUpdateSql, conn, tx);
                cmdStock.Parameters.Add(new SqlParameter("@InventoryID", SqlDbType.BigInt) { Value = inventoryId });
                cmdStock.Parameters.Add(SqlParameterFactory.CreateInt("@UpdateUser", updateUser));
                await cmdStock.ExecuteNonQueryAsync();

                tx.Commit();
                return (byte[])result;
            }
            catch { tx.Rollback(); throw; }
        }
    }
}