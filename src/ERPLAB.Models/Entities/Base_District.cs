namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 鄉鎮市區基礎字典實體。
    /// 核心職責：與縣市形成實體強外鍵連動，並精確提供 3 碼郵遞區號。
    /// </summary>
    public class Base_District : ISoftDeletable
    {
        public int DistrictID { get; set; }

        // 關聯 Base_City 的強外鍵
        public int CityID { get; set; }

        // 台灣標準 3 碼郵遞區號 (對齊資料庫 VARCHAR(3) 嚴格限制)
        public string ZipCode { get; set; } = string.Empty;

        public string DistrictName { get; set; } = string.Empty;

        public int SortSeq { get; set; }

        // 實作 ISoftDeletable 介面
        public bool IsActive { get; set; } = true;
    }
}