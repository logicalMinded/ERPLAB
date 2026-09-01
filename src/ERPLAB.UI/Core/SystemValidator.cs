using System.Text.RegularExpressions;

namespace ERPLAB.UI.Core
{
    public static class SystemValidator
    {
        // 💡 升級：回傳 Tuple (是否合法, 建議的錯誤訊息)
        public static (bool IsValid, string ErrorMsg) ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return (false, "聯絡電話不可為空白！");

            if (phone.Trim().Length < 7)
                return (false, "電話號碼長度過短，請輸入至少 7 碼！");

            return (true, string.Empty);
        }

        public static (bool IsValid, string ErrorMsg) ValidateZipRear(string zipRear)
        {
            if (zipRear == null) return (true, "");
            string trimmed = zipRear.Trim();
            if (trimmed.Length != 0 && trimmed.Length != 3)
                return (false, "郵遞區號後碼若有填寫，必須為精確的 3 碼！");

            return (true, string.Empty);
        }
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