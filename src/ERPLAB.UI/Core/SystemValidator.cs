using System.Text.RegularExpressions;
namespace ERPLAB.UI.Core
{
    /// <summary>
    /// 系統共用資料驗證模組 (Validation Utility)。
    /// 封裝全域共用之靜態資料檢核邏輯，確保前端輸入資料符合系統格式規範。
    /// 統一回傳 ValueTuple (IsValid, ErrorMsg) 資料結構，以利與基底表單 (BasePage.EnsureValid) 無縫整合，提供標準化之錯誤攔截與提示。
    /// </summary>
    public static class SystemValidator
    {
        /// <summary>
        /// 聯絡電話格式檢核。
        /// 驗證必填與基礎長度限制。
        /// </summary>
        public static (bool IsValid, string ErrorMsg) ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return (false, "聯絡電話不可為空白！");

            if (phone.Trim().Length < 7)
                return (false, "電話號碼長度過短，請輸入至少 7 碼！");

            return (true, string.Empty);
        }

        /// <summary>
        /// 擴充郵遞區號格式檢核。
        /// 允許為空值；若有輸入，則必須嚴格符合 3 碼之長度限制。
        /// </summary>
        public static (bool IsValid, string ErrorMsg) ValidateZipRear(string zipRear)
        {
            if (zipRear == null) return (true, "");
            string trimmed = zipRear.Trim();
            if (trimmed.Length != 0 && trimmed.Length != 3)
                return (false, "郵遞區號後碼若有填寫，必須為精確的 3 碼！");

            return (true, string.Empty);
        }

        /// <summary>
        /// 電子郵件格式檢核。
        /// 透過正規表達式 (Regular Expression) 驗證 Email 基礎結構。
        /// </summary>
        public static (bool IsValid, string ErrorMsg) ValidateEmail(string Email)
        {
            if (string.IsNullOrWhiteSpace(Email))
                return (true, string.Empty);

            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(Email, pattern, RegexOptions.IgnoreCase))
            {
                return (false, "Email 格式不符，請確認是否包含 '@' 與正確的網域！");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 金額數值邏輯檢核。
        /// 確保輸入字串可合法轉型為高精度小數 (Decimal)，且數值必須為正數。
        /// </summary>
        public static (bool IsValid, string ErrorMsg) ValidatePrice(string priceInput, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(priceInput))
                return (false, $"「{fieldName}」不可為空白！");

            if (!decimal.TryParse(priceInput, out decimal price))
                return (false, $"「{fieldName}」請輸入有效的數字格式！");

            if (price <= 0)
                return (false, $"「{fieldName}」必須大於 0！");

            return (true, string.Empty);
        }
    }
}