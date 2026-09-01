using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using Microsoft.Data.SqlClient;

namespace ERPLAB.DataAccess.Repositories
{
    public class SystemNodeRepository
    {
        /// <summary>
        /// 取得指定帳號在目前啟用狀態下，所擁有的全部系統節點 (供動態生成選單與前端斷路使用)
        /// </summary>
        public async Task<List<SystemNode>> GetAuthorizedNodesAsync(int accountId)
        {
            var nodes = new List<SystemNode>();

            // 💡 查詢策略：
            // 1. 僅撈取 IsActive = 1 的啟用節點。
            // 2. PermissionCode IS NULL 代表純結構目錄 (如模組節點)，預設全撈 (後續由 UI 負責修剪空樹枝)。
            // 3. 有綁定 PermissionCode 的節點，必須存在於 vw_Account_ActivePermissions 檢視表中。
            string sql = @"
                SELECT 
                    sn.[NodeID], 
                    sn.[NodeName], 
                    sn.[NodeType], 
                    sn.[ParentNodeID], 
                    sn.[SortSeq], 
                    sn.[FormClassPath], 
                    sn.[PermissionCode]
                FROM [dbo].[SystemNodes] sn
                WHERE sn.[IsActive] = 1
                  AND (
                      sn.[PermissionCode] IS NULL 
                      OR EXISTS (
                          SELECT 1 
                          FROM [dbo].[vw_Account_ActivePermissions] vp 
                          WHERE vp.[AccountID] = @AccountID 
                            AND vp.[PermissionCode] = sn.[PermissionCode]
                      )
                  )
                ORDER BY sn.[ParentNodeID] ASC, sn.[SortSeq] ASC;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            // 綁定查詢參數
            cmd.Parameters.Add(SqlParameterFactory.CreateInt("@AccountID", accountId));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var node = new SystemNode
                {
                    NodeID = reader.GetInt32(reader.GetOrdinal("NodeID")),
                    NodeName = reader.GetString(reader.GetOrdinal("NodeName")),
                    NodeType = reader.GetByte(reader.GetOrdinal("NodeType")),
                    SortSeq = reader.GetInt32(reader.GetOrdinal("SortSeq")),

                    // 處理可為 Null 的欄位
                    ParentNodeID = reader.IsDBNull(reader.GetOrdinal("ParentNodeID"))
                                   ? null
                                   : reader.GetInt32(reader.GetOrdinal("ParentNodeID")),

                    FormClassPath = reader.IsDBNull(reader.GetOrdinal("FormClassPath"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("FormClassPath")),

                    PermissionCode = reader.IsDBNull(reader.GetOrdinal("PermissionCode"))
                                     ? null
                                     : reader.GetString(reader.GetOrdinal("PermissionCode")),

                    IsActive = true // 查詢條件已卡死 IsActive = 1
                };

                nodes.Add(node);
            }

            return nodes;
        }
    }
}