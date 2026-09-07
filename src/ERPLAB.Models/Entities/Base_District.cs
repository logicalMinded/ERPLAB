namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 鄉鎮市區基礎資料實體 (Entity)。
    /// 作為系統共用之地理參照資料，與縣市主檔 (Base_City) 建立層級關聯，
    /// 主要供前端介面實作聯動下拉選單 (Cascading Dropdown) 並提供標準 3 碼郵遞區號。
    /// </summary>
    public class Base_District : ISoftDeletable
    {
        public int DistrictID { get; set; }

        // 關聯至縣市主檔之外鍵 (Foreign Key)，維持地理資料之關聯完整性
        public int CityID { get; set; }

        // 台灣標準 3 碼郵遞區號。嚴格對齊資料庫端 VARCHAR(3) 型別與長度限制，確保資料寫入之一致性
        public string ZipCode { get; set; } = string.Empty;

        public string DistrictName { get; set; } = string.Empty;

        // 自訂排序權重 (Sorting Weight)，供前端優化選單呈現順序
        public int SortSeq { get; set; }

        // =====================================================================
        // 實作 ISoftDeletable 介面：邏輯刪除 (Soft Delete) 標記。
        // 用於控制該行政區是否顯示於前端選單，避免實體刪除破壞歷史單據之地理關聯。
        // =====================================================================
        public bool IsActive { get; set; } = true;
    }
}