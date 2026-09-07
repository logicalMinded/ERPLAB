namespace ERPLAB.Models.DTOs
{
    /// <summary>
    /// 營業摘要資料傳輸物件 (DTO)。
    /// 專供前端儀表板 (Dashboard) 呈現總體營運指標快照。
    /// 採用 C# 9.0 唯讀屬性 (init-only) 設計，確保資料傳遞過程的不可變性 (Immutability)。
    /// </summary>
    public class SalesSummaryDto
    {
        public int TotalOrders { get; init; }         // 有效訂單總數
        public decimal TotalRevenue { get; init; }    // 總營業額
        public decimal TotalCost { get; init; }       // 總成本 (依據出庫明細快照計算：SUM(Qty * UnitCost))

        // 透過唯讀運算屬性 (Computed Property) 即時計算毛利，避免實體儲存冗餘欄位
        public decimal GrossProfit => TotalRevenue - TotalCost;

        // 封裝毛利率計算邏輯，並實作除以零 (Divide by Zero) 之邊界防禦
        public decimal GrossMarginRatio => TotalRevenue == 0 ? 0 : (GrossProfit / TotalRevenue);

        // 封裝客單價計算邏輯，並實作除以零之邊界防禦
        public decimal AverageOrderValue => TotalOrders == 0 ? 0 : (TotalRevenue / TotalOrders);
    }

    /// <summary>
    /// 暢銷商品排行資料傳輸物件 (DTO)。
    /// 負責封裝由資料庫層級完成運算的商品銷售聚合 (Aggregation) 數據。
    /// </summary>
    public class TopProductDto
    {
        public string ProductNo { get; init; } = string.Empty;
        public string ProductName { get; init; } = string.Empty;
        public int TotalQtySold { get; init; }
        public decimal TotalRevenue { get; init; }
        public decimal GrossProfit { get; init; }
    }

    /// <summary>
    /// 客戶貢獻排行資料傳輸物件 (DTO)。
    /// 負責封裝由資料庫層級完成運算的客戶訂單與營收聚合數據。
    /// </summary>
    public class TopCustomerDto
    {
        public string CustomerNo { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public int OrderCount { get; init; }
        public decimal TotalRevenue { get; init; }
    }
}