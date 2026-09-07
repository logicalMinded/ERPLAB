using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using Microsoft.Data.SqlClient;
namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 系統節點倉儲 (Repository)
    /// 負責處理系統選單架構與權限節點的資料存取，支援動態選單生成與 RBAC (Role-Based Access Control) 權限檢核。
    /// </summary>
    public class SystemNodeRepository
    {
        /// <summary>
        /// 取得指定帳號獲授權之所有啟用節點。
        /// 供前端動態渲染導覽選單 (Navigation Menu) 及介面存取權限攔截使用。
        /// </summary>
        public async Task<List<SystemNode>> GetAuthorizedNodesAsync(int accountId)
        {
            var nodes = new List<SystemNode>();

            // =====================================================================
            // 權限過濾策略 (Authorization Strategy)：
            // 1. 僅讀取狀態為啟用 (IsActive = 1) 的節點。
            // 2. 結構型目錄 (PermissionCode IS NULL) 預設全數載入，後續交由 UI 遞迴處理，過濾無授權子節點之空目錄。
            // 3. 功能型節點 (具備 PermissionCode) 必須存在於使用者當前啟用的權限檢視表 (vw_Account_ActivePermissions) 中。
            // =====================================================================
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

                    // 實體映射：處理可為 Null 之資料庫欄位
                    ParentNodeID = reader.IsDBNull(reader.GetOrdinal("ParentNodeID"))
                                   ? null
                                   : reader.GetInt32(reader.GetOrdinal("ParentNodeID")),

                    FormClassPath = reader.IsDBNull(reader.GetOrdinal("FormClassPath"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("FormClassPath")),

                    PermissionCode = reader.IsDBNull(reader.GetOrdinal("PermissionCode"))
                                     ? null
                                     : reader.GetString(reader.GetOrdinal("PermissionCode")),

                    IsActive = true
                };

                nodes.Add(node);
            }

            return nodes;
        }
    }
}