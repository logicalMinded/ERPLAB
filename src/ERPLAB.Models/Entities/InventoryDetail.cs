namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 盤點明細實體。
    /// 💡 物理特性：包含快照與實盤的雙軌庫存紀錄。
    /// </summary>
    public class InventoryDetail
    {
        public long InventoryDID { get; set; }
        public long InventoryID { get; set; }
        public int LineNo { get; set; }
        public int ProductID { get; set; }

        // 💡 建立單據當下的系統庫存快照 (凍結時間點)
        public int SystemStock { get; set; }

        // 💡 實際盤點輸入的數量
        public int ActualStock { get; set; }

        // 盤點當下的單位成本快照 (供盤盈虧財報計算使用)
        public decimal StockPrice { get; set; }

        public string? Remark { get; set; }

        // [UI 唯讀輔助屬性]
        public string? ProductNo_Display { get; init; }
        public string? ProductName_Display { get; init; }

        // 💡 [即時運算輔助] 盤盈虧差異數量 (實際 - 帳面)
        public int DiffQty_Display => ActualStock - SystemStock;
        // 💡 [即時運算輔助] 盤盈虧差異金額
        public decimal DiffAmount_Display => DiffQty_Display * StockPrice;
    }
}