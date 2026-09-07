using System.ComponentModel;
namespace ERPLAB.UI.Core
{
    /// <summary>
    /// 擴充資料綁定清單 (Extended BindingList)。
    /// 繼承自 BindingList<T>，主要擴充批次新增 (AddRange) 功能以解決大量資料綁定時的 UI 效能瓶頸，
    /// 並實作底層排序核心邏輯，支援 DataGridView 點擊標題列進行自動排序。
    /// </summary>
    public class ExtendedBindingList<T> : BindingList<T>
    {
        private bool _isSorted;
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;
        private PropertyDescriptor _sortProperty;

        public ExtendedBindingList() : base() { }

        public ExtendedBindingList(IEnumerable<T> enumeration) : base(new List<T>(enumeration)) { }

        // =====================================================================
        // 批次資料載入最佳化 (Batch Loading Optimization)
        // =====================================================================

        /// <summary>
        /// 批次新增資料集合。
        /// 透過暫停與重啟事件觸發機制，避免逐筆新增時引發無謂的 UI 頻繁重繪 (Repaint)。
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            // 暫停觸發 ListChanged 事件，凍結前端控制項的重繪更新
            this.RaiseListChangedEvents = false;

            foreach (var item in items)
            {
                this.Add(item);
            }

            // 恢復事件觸發機制
            this.RaiseListChangedEvents = true;

            // 觸發單次全域資料重整，通知綁定的控制項進行畫面更新
            this.ResetBindings();
        }

        // =====================================================================
        // 實作資料排序機制 (Sorting Implementation)
        // 覆寫基底類別的核心排序屬性與方法，提供與 DataGridView 欄位標題點擊排序之原生整合。
        // =====================================================================
        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => _isSorted;
        protected override ListSortDirection SortDirectionCore => _sortDirection;
        protected override PropertyDescriptor SortPropertyCore => _sortProperty;

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            var items = this.Items as List<T>;
            if (items == null) return;

            // 依據指定的屬性 (PropertyDescriptor) 與方向進行集合排序
            if (direction == ListSortDirection.Ascending)
                items.Sort((x, y) => Comparer<object>.Default.Compare(prop.GetValue(x), prop.GetValue(y)));
            else
                items.Sort((x, y) => Comparer<object>.Default.Compare(prop.GetValue(y), prop.GetValue(x)));

            _isSorted = true;
            _sortDirection = direction;
            _sortProperty = prop;

            // 排序完成後觸發 ListChanged 事件通知 UI 更新排列順序
            this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        protected override void RemoveSortCore()
        {
            _isSorted = false;
            this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
    }
}