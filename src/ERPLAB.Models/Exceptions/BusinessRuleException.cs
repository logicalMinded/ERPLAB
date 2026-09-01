namespace ERPLAB.Models.Exceptions
{
    /// <summary>
    /// 系統商業邏輯例外 (Business Rule Exception)
    /// 核心職責：封裝 BLL 翻譯後的客製化錯誤訊息，阻斷 SQL 錯誤直接穿透至 UI 層。
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }
}