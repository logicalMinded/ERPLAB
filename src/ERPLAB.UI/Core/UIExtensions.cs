using System.ComponentModel;
using System.Reflection;
namespace ERPLAB.UI.Core
{
    /// <summary>
    /// UI 控制項擴充方法 (Extension Methods) 工具類別。
    /// 封裝底層渲染優化、泛型列舉資料綁定與資料來源定位等共用邏輯，提升前端視窗元件之重用性與維護性。
    /// </summary>
    public static class UIExtensions
    {
        /// <summary>
        /// 強制啟用控制項雙重緩衝 (Double Buffering)。
        /// 透過反射 (Reflection) 寫入非公開屬性，解決 WinForms 控制項 (如 DataGridView) 於大量資料重繪時產生之畫面閃爍問題。
        /// </summary>
        public static void EnableDoubleBuffering(this Control control, bool enable = true)
        {
            var propertyInfo = typeof(Control).GetProperty(
                "DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);

            propertyInfo?.SetValue(control, enable, null);
        }

        // =====================================================================
        // 列舉 (Enum) 自動綁定機制
        // 透過反射讀取 Enum 之 [Description] 屬性作為 UI 顯示文字，
        // 並將列舉數值綁定至 ComboBox，降低前端介面之硬編碼 (Hardcoding)。
        // =====================================================================
        public static void BindToEnum<TEnum>(this ComboBox comboBox) where TEnum : struct, Enum
        {
            // 採用強型別 KeyValuePair，避免使用匿名型別造成資料綁定時的額外效能損耗
            var items = new List<KeyValuePair<string, byte>>();

            foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
            {
                // 1. 取得列舉欄位之反射資訊，並以列舉名稱作為預設顯示文字
                string description = enumValue.ToString();
                FieldInfo fieldInfo = typeof(TEnum).GetField(description);

                // 2. 嘗試提取 [Description] 屬性 (Attribute) 作為自訂顯示文字
                var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttribute != null)
                {
                    description = descriptionAttribute.Description;
                }

                // 3. 拆箱與轉型：配合專案資料庫規格，將列舉底層型別明確轉為 byte
                byte numericValue = (byte)(object)enumValue;

                items.Add(new KeyValuePair<string, byte>(description, numericValue));
            }

            // 4. 執行控制項資料綁定
            comboBox.DataSource = items;
            comboBox.DisplayMember = "Key";
            comboBox.ValueMember = "Value";
            comboBox.SelectedIndex = -1;      // 清除預設選取，避免觸發非預期之連動事件
        }

        /// <summary>
        /// 於 BindingSource 中搜尋符合條件之實體，並同步更新游標位置 (Position)。
        /// 供前端介面於新增或修改資料後，自動將焦點定位至該筆紀錄。
        /// </summary>
        /// <typeparam name="T">資料來源之實體型別</typeparam>
        /// <param name="source">目標 BindingSource</param>
        /// <param name="predicate">比對條件 (Lambda 運算式)</param>
        public static void LocateTo<T>(this BindingSource source, Func<T, bool> predicate)
        {
            // 防禦性檢查 (Defensive Programming)
            if (source == null || source.Count == 0 || predicate == null)
                return;

            // 透過 LINQ 查詢符合條件之目標物件
            var targetItem = source.Cast<T>().FirstOrDefault(predicate);

            // 更新 BindingSource 之內部指標，觸發 UI 畫面連動
            if (targetItem != null)
            {
                source.Position = source.IndexOf(targetItem);
            }
        }
    }
}