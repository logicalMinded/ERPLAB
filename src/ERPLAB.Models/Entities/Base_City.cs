namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 縣市基礎資料實體 (Entity)。
    /// 作為系統共用之地理參照資料，主要供前端介面 (UI) 下拉選單進行資料綁定。
    /// 因屬低頻異動之靜態字典檔，故未實作完整稽核軌跡 (Audit Trail)，僅保留邏輯刪除 (Soft Delete) 機制。
    /// </summary>
    public class Base_City : ISoftDeletable
    {
        public int CityID { get; set; }

        // 官方行政區代碼，保留未來與政府開放資料 (Open Data) 服務介接之擴充性
        public string CityNo { get; set; } = string.Empty;

        public string CityName { get; set; } = string.Empty;

        // 自訂排序權重 (Sorting Weight)，供前端優化使用者體驗 (例如：將六都設置為較高權重以置頂顯示)
        public int SortSeq { get; set; }

        // =====================================================================
        // 實作 ISoftDeletable 介面：邏輯刪除 (Soft Delete) 標記。
        // 用於控制該縣市是否顯示於前端選單，並避免實體刪除導致歷史單據之關聯錯誤。
        // =====================================================================
        public bool IsActive { get; set; } = true;
    }
}