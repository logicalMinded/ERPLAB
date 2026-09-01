namespace ERPLAB.Models.DTOs
{
    // =====================================================================
    // 💡 儀表板頂端：總體營運指標快照
    // =====================================================================
    public class SalesSummaryDto
    {
        public int TotalOrders { get; init; }         // 有效訂單數
        public decimal TotalRevenue { get; init; }    // 總營業額
        public decimal TotalCost { get; init; }       // 總成本 (SUM(Qty * UnitCost))
        public decimal GrossProfit => TotalRevenue - TotalCost; // 總毛利

        // 毛利率 (避免除以零的物理防呆)
        public decimal GrossMarginRatio => TotalRevenue == 0 ? 0 : (GrossProfit / TotalRevenue);
        public decimal AverageOrderValue => TotalOrders == 0 ? 0 : (TotalRevenue / TotalOrders); // 客單價
    }

    // =====================================================================
    // 💡 排行榜：熱銷商品 Top 10
    // =====================================================================
    public class TopProductDto
    {
        public string ProductNo { get; init; } = string.Empty;
        public string ProductName { get; init; } = string.Empty;
        public int TotalQtySold { get; init; }
        public decimal TotalRevenue { get; init; }
        public decimal GrossProfit { get; init; }
    }

    // =====================================================================
    // 💡 排行榜：VIP 客戶貢獻 Top 10
    // =====================================================================
    public class TopCustomerDto
    {
        public string CustomerNo { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public int OrderCount { get; init; }
        public decimal TotalRevenue { get; init; }
    }
}