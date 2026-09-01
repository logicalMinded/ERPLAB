using System.ComponentModel;

namespace ERPLAB.UI.Core
{
    /// <summary>
    /// 支援點擊標題列排序與高效能 AddRange 的擴充綁定清單
    /// </summary>
    public class ExtendedBindingList<T> : BindingList<T>
    {
        private bool _isSorted;
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;
        private PropertyDescriptor _sortProperty;

        public ExtendedBindingList() : base() { }

        public ExtendedBindingList(IEnumerable<T> enumeration) : base(new List<T>(enumeration)) { }

        // =====================================================================
        // 💡 高效能批次加入引擎
        // =====================================================================
        public void AddRange(IEnumerable<T> items)
        {
            this.RaiseListChangedEvents = false; // 物理凍結：暫停觸發重繪事件
            foreach (var item in items)
            {
                this.Add(item);
            }
            this.RaiseListChangedEvents = true;  // 恢復機制
            this.ResetBindings();                // 僅觸發一次全域重繪
        }

        // =====================================================================
        // 💡 覆寫排序機制 (支援 DataGridView 點擊欄位標題自動排序)
        // =====================================================================
        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => _isSorted;
        protected override ListSortDirection SortDirectionCore => _sortDirection;
        protected override PropertyDescriptor SortPropertyCore => _sortProperty;

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            var items = this.Items as List<T>;
            if (items == null) return;

            if (direction == ListSortDirection.Ascending)
                items.Sort((x, y) => Comparer<object>.Default.Compare(prop.GetValue(x), prop.GetValue(y)));
            else
                items.Sort((x, y) => Comparer<object>.Default.Compare(prop.GetValue(y), prop.GetValue(x)));

            _isSorted = true;
            _sortDirection = direction;
            _sortProperty = prop;

            this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        protected override void RemoveSortCore()
        {
            _isSorted = false;
            this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
    }
}