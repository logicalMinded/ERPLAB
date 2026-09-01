namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 縣市基礎字典實體。
    /// 核心職責：提供全域縣市下拉選單綁定，不含審計追蹤，僅實作軟刪除。
    /// </summary>
    public class Base_City : ISoftDeletable
    {
        public int CityID { get; set; }

        // 官方行政區代碼 (供未來介接政府 Open Data API 使用)
        public string CityNo { get; set; } = string.Empty;

        public string CityName { get; set; } = string.Empty;

        // UI 呈現權重 (例如：可將六都設為 0~5 優先排在選單最上方)
        public int SortSeq { get; set; }

        // 實作 ISoftDeletable 介面 (0:歷史停用, 1:啟用中)
        public bool IsActive { get; set; } = true;
    }
}