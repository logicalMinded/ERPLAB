using ERPLAB.DataAccess.Core;
using ERPLAB.Models.DTOs;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 銷售數據分析倉儲 (OLAP 查詢引擎)。
    /// 核心職責：將繁重的 GROUP BY 與 SUM 運算留在 SQL Server 內部，壓榨底層算力，僅回傳極簡的 DTO 給 C#。
    /// </summary>
    public class SalesAnalysisRepository
    {
        /// <summary>
        /// 取得指定日期區間的銷售儀表板數據 (多重結果集一次性回傳)
        /// </summary>
        public async Task<(SalesSummaryDto Summary, List<TopProductDto> TopProducts, List<TopCustomerDto> TopCustomers)> GetDashboardDataAsync(DateTime startDate, DateTime endDate)
        {
            var summary = new SalesSummaryDto();
            var topProducts = new List<TopProductDto>();
            var topCustomers = new List<TopCustomerDto>();

            // 💡 物理防線：分析數據【絕對只能】包含已過帳 (Status = 2) 的單據！
            string sql = @"
                -- =========================================================
                -- 語句 1：總體營運指標 (總表與明細表的 JOIN 聚合)
                -- =========================================================
                SELECT 
                    COUNT(DISTINCT sm.SalesID) AS TotalOrders,
                    ISNULL(SUM(sd.UnitPrice * sd.Qty), 0) AS TotalRevenue,
                    ISNULL(SUM(sd.UnitCost * sd.Qty), 0) AS TotalCost
                FROM [dbo].[SalesMaster] sm
                INNER JOIN [dbo].[SalesDetail] sd ON sm.SalesID = sd.SalesID
                WHERE sm.[Status] = @PostedStatus
                  AND sm.[SalesDate] >= @StartDate 
                  AND sm.[SalesDate] <= @EndDate;

                -- =========================================================
                -- 語句 2：熱銷商品 Top 10 (依銷售數量降冪)
                -- =========================================================
                SELECT TOP 10
                    p.ProductNo,
                    p.ProductName,
                    SUM(sd.Qty) AS TotalQtySold,
                    SUM(sd.UnitPrice * sd.Qty) AS TotalRevenue,
                    SUM((sd.UnitPrice - sd.UnitCost) * sd.Qty) AS GrossProfit
                FROM [dbo].[SalesDetail] sd
                INNER JOIN [dbo].[SalesMaster] sm ON sd.SalesID = sm.SalesID
                INNER JOIN [dbo].[Product] p ON sd.ProductID = p.ProductID
                WHERE sm.[Status] = @PostedStatus
                  AND sm.[SalesDate] >= @StartDate 
                  AND sm.[SalesDate] <= @EndDate
                GROUP BY p.ProductNo, p.ProductName
                ORDER BY TotalQtySold DESC;

                -- =========================================================
                -- 語句 3：VIP 客戶貢獻 Top 10 (依總營業額降冪)
                -- =========================================================
                SELECT TOP 10
                    c.CustomerNo,
                    c.CustomerName,
                    COUNT(sm.SalesID) AS OrderCount,
                    SUM(sm.TotalAmount) AS TotalRevenue
                FROM [dbo].[SalesMaster] sm
                INNER JOIN [dbo].[Customer] c ON sm.CustomerID = c.CustomerID
                WHERE sm.[Status] = @PostedStatus
                  AND sm.[SalesDate] >= @StartDate 
                  AND sm.[SalesDate] <= @EndDate
                GROUP BY c.CustomerNo, c.CustomerName
                ORDER BY TotalRevenue DESC;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            // 參數綁定 (精確鎖定日期區間的邊界)
            cmd.Parameters.Add(SqlParameterFactory.CreateTinyInt("@PostedStatus", (byte)DocumentStatus.Posted));
            cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = startDate.Date }); // 確保從 00:00:00 開始
            cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = endDate.Date.AddDays(1).AddTicks(-3) }); // 確保涵蓋到 23:59:59.997

            using var reader = await cmd.ExecuteReaderAsync();

            // 1️⃣ 讀取總體指標
            if (await reader.ReadAsync())
            {
                summary = new SalesSummaryDto
                {
                    TotalOrders = reader.GetInt32(reader.GetOrdinal("TotalOrders")),
                    TotalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
                    TotalCost = reader.GetDecimal(reader.GetOrdinal("TotalCost"))
                };
            }

            // 2️⃣ 讀取熱銷商品
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    topProducts.Add(new TopProductDto
                    {
                        ProductNo = reader.GetString(reader.GetOrdinal("ProductNo")),
                        ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                        TotalQtySold = reader.GetInt32(reader.GetOrdinal("TotalQtySold")),
                        TotalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
                        GrossProfit = reader.GetDecimal(reader.GetOrdinal("GrossProfit"))
                    });
                }
            }

            // 3️⃣ 讀取 VIP 客戶
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    topCustomers.Add(new TopCustomerDto
                    {
                        CustomerNo = reader.GetString(reader.GetOrdinal("CustomerNo")),
                        CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                        OrderCount = reader.GetInt32(reader.GetOrdinal("OrderCount")),
                        TotalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"))
                    });
                }
            }

            return (summary, topProducts, topCustomers);
        }
    }
}