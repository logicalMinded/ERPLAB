using System.ComponentModel;
using System.Reflection;

namespace ERPLAB.UI.Core
{
    /// <summary>
    /// UI 渲染底層優化擴充方法
    /// </summary>
    public static class UIExtensions
    {
        /// <summary>
        /// 透過反射強制開啟控制項的雙重緩衝，徹底消滅 DataGridView 的渲染閃爍與卡頓
        /// </summary>
        public static void EnableDoubleBuffering(this Control control, bool enable = true)
        {
            var propertyInfo = typeof(Control).GetProperty(
                "DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);

            propertyInfo?.SetValue(control, enable, null);
        }

        // =====================================================================
        // 💡 [資料綁定引擎] 列舉 (Enum) 自動綁定器
        // 核心職責：透過反射讀取 Enum 的 [Description] 作為顯示文字，
        // 並將底層數值轉為 byte 綁定至 ComboBox，徹底消滅 UI 端的硬編碼。
        // =====================================================================
        public static void BindToEnum<TEnum>(this ComboBox comboBox) where TEnum : struct, Enum // 泛型標記 + 擴充方法 + 泛型條件約束
        {
            // 使用強型別 KeyValuePair 避免匿名型別的反射效能損耗
            var items = new List<KeyValuePair<string, byte>>();

            foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
            {
                // 1. 取得 Enum 欄位的反射資訊
                string description = enumValue.ToString(); //預防 descriptionAttribute == NULL
                FieldInfo fieldInfo = typeof(TEnum).GetField(description);

                // 2. 提取 [Description] 標籤內容
                var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttribute != null)
                {
                    description = descriptionAttribute.Description;
                }

                // 3. 物理轉型：因為本專案 Enum 皆嚴格宣告為 : byte，此處進行雙重轉型拆箱
                byte numericValue = (byte)(object)enumValue;

                items.Add(new KeyValuePair<string, byte>(description, numericValue));
            }

            // 4. 執行物理綁定
            comboBox.DataSource = items;
            comboBox.DisplayMember = "Key";   // KeyValuePair 的 Key (字串)
            comboBox.ValueMember = "Value";   // KeyValuePair 的 Value (數字)
            comboBox.SelectedIndex = -1;      // 預設不選取防呆
        }

        /// <summary>
        /// 在 BindingSource 中搜尋符合條件的實體，並將指標定位到該紀錄。
        /// </summary>
        /// <typeparam name="T">資料源的實體型別</typeparam>
        /// <param name="source">要操作的 BindingSource</param>
        /// <param name="predicate">比對條件 (Lambda 運算式)</param>
        public static void LocateTo<T>(this BindingSource source, Func<T, bool> predicate)
        {
            // 1. 防禦性檢查：若未綁定資料、沒有資料或未傳入條件，則直接返回
            if (source == null || source.Count == 0 || predicate == null)
                return;

            // 2. 透過 LINQ 與傳入的條件尋找目標物件
            var targetItem = source.Cast<T>().FirstOrDefault(predicate);

            // 3. 若找到物件，則更新 Position 指標
            if (targetItem != null)
            {
                source.Position = source.IndexOf(targetItem);
            }
        }
    }
}