using ERPLAB.DataAccess.Core;
using ERPLAB.Models.DTOs;
using ERPLAB.Models.Enums;
using Microsoft.Data.SqlClient;
using System.Data;
namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 銷售數據分析倉儲 (Sales Analysis Repository)。
    /// 負責提供儀表板 (Dashboard) 所需之線上分析處理 (OLAP) 查詢服務。
    /// 核心架構：將繁重之資料聚合運算 (Aggregation，如 GROUP BY、SUM) 委派至資料庫引擎層執行，
    /// 僅回傳輕量化之資料傳輸物件 (DTO)，藉此降低網路頻寬消耗與應用程式伺服器之運算及記憶體負擔。
    /// </summary>
    public class SalesAnalysisRepository
    {
        /// <summary>
        /// 取得指定區間之銷售營運綜合數據。
        /// 透過單次資料庫往返 (Single Round-Trip) 執行包含多個查詢之 T-SQL 批次指令，
        /// 並利用 SqlDataReader.NextResultAsync 循序解析多重結果集 (Multiple Result Sets)，以達極致之 I/O 效能。
        /// </summary>
        public async Task<(SalesSummaryDto Summary, List<TopProductDto> TopProducts, List<TopCustomerDto> TopCustomers)> GetDashboardDataAsync(DateTime startDate, DateTime endDate)
        {
            var summary = new SalesSummaryDto();
            var topProducts = new List<TopProductDto>();
            var topCustomers = new List<TopCustomerDto>();

            // 業務邏輯約束：營運數據分析僅能採計已過帳 (Status = 2) 之正式交易單據，嚴格排除草稿與作廢資料。
            string sql = @"
                -- =========================================================
                -- 查詢一：總體營運指標 (總額與成本之 JOIN 聚合計算)
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
                -- 查詢二：熱銷商品排行 Top 10 (依銷售總數降冪排列)
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
                -- 查詢三：貢獻客戶排行 Top 10 (依營業總額降冪排列)
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

            // 參數綁定與邊界處理 (Boundary Handling)
            cmd.Parameters.Add(SqlParameterFactory.CreateTinyInt("@PostedStatus", (byte)DocumentStatus.Posted));
            cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = startDate.Date }); // 確保自 00:00:00 開始
            cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = endDate.Date.AddDays(1).AddTicks(-3) }); // 確保精確涵蓋至 23:59:59.997

            using var reader = await cmd.ExecuteReaderAsync();

            // 1. 讀取並對映總體營運指標
            if (await reader.ReadAsync())
            {
                summary = new SalesSummaryDto
                {
                    TotalOrders = reader.GetInt32(reader.GetOrdinal("TotalOrders")),
                    TotalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
                    TotalCost = reader.GetDecimal(reader.GetOrdinal("TotalCost"))
                };
            }

            // 2. 切換至第二個結果集，對映熱銷商品清單
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

            // 3. 切換至最後一個結果集，對映高貢獻客戶清單
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