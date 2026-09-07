namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 庫存盤點明細實體 (Entity)。
    /// 映射資料庫 [InventoryDetail] 資料表。核心職責為封裝單一商品的盤點紀錄，
    /// 包含盤點當下的系統庫存快照、實盤數量與成本資訊，以確保後續盤盈虧結算之基準一致性。
    /// </summary>
    public class InventoryDetail
    {
        public long InventoryDID { get; set; }
        public long InventoryID { get; set; }
        public int LineNo { get; set; }
        public int ProductID { get; set; }

        // 盤點單建立時的系統帳面庫存快照 (Snapshot)，作為盤盈虧差異計算之基準
        public int SystemStock { get; set; }

        // 實際盤點數量 (Physical Count)
        public int ActualStock { get; set; }

        // 盤點當下之移動平均成本快照，供後續盤盈虧之財務成本結算使用
        public decimal StockPrice { get; set; }

        public string? Remark { get; set; }

        // =====================================================================
        // 擴充顯示屬性 (UI Display Properties)
        // 供 Repository 透過 JOIN 查詢帶出之關聯顯示資料。
        // 採用唯讀設計 (init-only setter)，確保資料自資料庫載入後之不可變性。
        // =====================================================================
        public string? ProductNo_Display { get; init; }
        public string? ProductName_Display { get; init; }

        // =====================================================================
        // 唯讀運算屬性 (Computed Properties)
        // 供前端即時綁定與顯示，不參與實體資料庫儲存。
        // =====================================================================

        // 盤盈虧差異數量 (實際盤點數 - 帳面庫存數)
        public int DiffQty_Display => ActualStock - SystemStock;

        // 盤盈虧差異總成本
        public decimal DiffAmount_Display => DiffQty_Display * StockPrice;
    }
}