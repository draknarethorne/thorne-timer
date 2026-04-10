using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ThorneTimer
{
    /// <summary>
    /// Provides a generic collection that supports data binding with single-column and
    /// multi-column sorting. Implements <see cref="IBindingListView"/> for advanced sorting.
    /// If the elements are IComparable it uses that; otherwise compares the ToString().
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class SortableBindingList<T> : BindingList<T>, IBindingListView where T : class
    {
        private bool _isSorted;
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;
        private PropertyDescriptor _sortProperty;
        private ListSortDescriptionCollection _sortDescriptions = new ListSortDescriptionCollection();
        private readonly Dictionary<string, Func<object, object>> _displayResolvers = new Dictionary<string, Func<object, object>>();

        /// <summary>
        /// Registers a function that converts a raw property value (e.g. a numeric ID) to
        /// the display value (e.g. a name string) used for sort comparison.
        /// </summary>
        public void RegisterDisplayResolver(string propertyName, Func<object, object> resolver)
        {
            _displayResolvers[propertyName] = resolver;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortableBindingList{T}"/> class.
        /// </summary>
        public SortableBindingList()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortableBindingList{T}"/> class.
        /// </summary>
        /// <param name="list">An <see cref="T:System.Collections.Generic.IList`1" /> of items to be contained in the <see cref="T:System.ComponentModel.BindingList`1" />.</param>
        public SortableBindingList(IList<T> list)
            : base(list)
        {
        }

        #region IBindingList — Single-column sorting

        /// <summary>
        /// Gets a value indicating whether the list supports sorting.
        /// </summary>
        protected override bool SupportsSortingCore
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the list is sorted.
        /// </summary>
        protected override bool IsSortedCore
        {
            get { return _isSorted; }
        }

        /// <summary>
        /// Gets the direction the list is sorted.
        /// </summary>
        protected override ListSortDirection SortDirectionCore
        {
            get { return _sortDirection; }
        }

        /// <summary>
        /// Gets the property descriptor that is used for sorting the list if sorting is implemented in a derived class; otherwise, returns null
        /// </summary>
        protected override PropertyDescriptor SortPropertyCore
        {
            get { return _sortProperty; }
        }

        /// <summary>
        /// Removes any sort applied with ApplySortCore if sorting is implemented
        /// </summary>
        protected override void RemoveSortCore()
        {
            _sortDirection = ListSortDirection.Ascending;
            _sortProperty = null;
            _sortDescriptions = new ListSortDescriptionCollection();
            _isSorted = false;
        }

        /// <summary>
        /// Sorts the items by a single column. Also used by the DataGridView default column header click.
        /// </summary>
        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            _sortProperty = prop;
            _sortDirection = direction;
            _sortDescriptions = new ListSortDescriptionCollection(
                new[] { new ListSortDescription(prop, direction) });

            ApplySortInternal();
        }

        #endregion

        #region IBindingListView — Multi-column sorting

        /// <summary>
        /// Gets a value indicating whether the list supports advanced (multi-column) sorting.
        /// </summary>
        bool IBindingListView.SupportsAdvancedSorting
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the current sort descriptions (all columns in the sort chain).
        /// </summary>
        ListSortDescriptionCollection IBindingListView.SortDescriptions
        {
            get { return _sortDescriptions; }
        }

        /// <summary>
        /// Applies a multi-column sort using the provided sort descriptions.
        /// </summary>
        void IBindingListView.ApplySort(ListSortDescriptionCollection sorts)
        {
            _sortDescriptions = sorts ?? new ListSortDescriptionCollection();

            if (_sortDescriptions.Count > 0)
            {
                _sortProperty = _sortDescriptions[0].PropertyDescriptor;
                _sortDirection = _sortDescriptions[0].SortDirection;
            }

            ApplySortInternal();
        }

        // Filtering is not supported but required by IBindingListView
        bool IBindingListView.SupportsFiltering { get { return false; } }
        string IBindingListView.Filter
        {
            get { return null; }
            set { throw new NotSupportedException("Filtering is not supported."); }
        }
        void IBindingListView.RemoveFilter() { }

        #endregion

        #region Public helpers for multi-column sort

        /// <summary>
        /// Gets the current sort descriptions (all columns in the sort chain).
        /// Convenience accessor so callers don't need to cast to IBindingListView.
        /// </summary>
        public ListSortDescriptionCollection SortDescriptions
        {
            get { return _sortDescriptions; }
        }

        /// <summary>
        /// Applies a multi-column sort. Each tuple is (propertyName, direction).
        /// Columns are sorted in the order provided — first column is the primary sort.
        /// </summary>
        /// <example>
        /// list.ApplyMultiSort(("Scope", ListSortDirection.Ascending), ("Class", ListSortDirection.Ascending));
        /// </example>
        public void ApplyMultiSort(params (string propertyName, ListSortDirection direction)[] sorts)
        {
            if (sorts == null || sorts.Length == 0)
            {
                RemoveSortCore();
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                return;
            }

            var props = TypeDescriptor.GetProperties(typeof(T));
            var descriptions = new ListSortDescription[sorts.Length];

            for (int i = 0; i < sorts.Length; i++)
            {
                var prop = props[sorts[i].propertyName];
                if (prop == null)
                    throw new ArgumentException($"Property '{sorts[i].propertyName}' not found on type {typeof(T).Name}.");
                descriptions[i] = new ListSortDescription(prop, sorts[i].direction);
            }

            _sortDescriptions = new ListSortDescriptionCollection(descriptions);
            _sortProperty = descriptions[0].PropertyDescriptor;
            _sortDirection = descriptions[0].SortDirection;

            ApplySortInternal();
        }

        /// <summary>
        /// Adds a column to the existing sort chain (or toggles its direction if already present).
        /// Used for Shift+Click column header behavior.
        /// </summary>
        public void AddOrToggleSortColumn(PropertyDescriptor prop)
        {
            var list = new List<ListSortDescription>();

            // Copy existing sort descriptions
            for (int i = 0; i < _sortDescriptions.Count; i++)
                list.Add(_sortDescriptions[i]);

            // Check if this column is already in the sort chain
            int existingIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].PropertyDescriptor == prop)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                // Toggle direction in place — keep position in the sort chain
                var toggled = list[existingIndex].SortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                list[existingIndex] = new ListSortDescription(prop, toggled);
            }
            else
            {
                list.Add(new ListSortDescription(prop, ListSortDirection.Ascending));
            }

            _sortDescriptions = new ListSortDescriptionCollection(list.ToArray());

            if (_sortDescriptions.Count > 0)
            {
                _sortProperty = _sortDescriptions[0].PropertyDescriptor;
                _sortDirection = _sortDescriptions[0].SortDirection;
            }

            ApplySortInternal();
        }

        /// <summary>
        /// Removes a column from the sort chain. Used for Ctrl+Click column header behavior.
        /// Returns true if the column was found and removed; false if it wasn't in the chain.
        /// </summary>
        public bool RemoveSortColumn(PropertyDescriptor prop)
        {
            var list = new List<ListSortDescription>();

            for (int i = 0; i < _sortDescriptions.Count; i++)
                list.Add(_sortDescriptions[i]);

            var existing = list.FirstOrDefault(d => d.PropertyDescriptor == prop);
            if (existing == null)
                return false;

            list.Remove(existing);

            if (list.Count == 0)
            {
                RemoveSortCore();
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                return true;
            }

            _sortDescriptions = new ListSortDescriptionCollection(list.ToArray());
            _sortProperty = _sortDescriptions[0].PropertyDescriptor;
            _sortDirection = _sortDescriptions[0].SortDirection;

            ApplySortInternal();
            return true;
        }

        /// <summary>
        /// Re-applies the current sort without changing the sort chain.
        /// Use after in-place data edits to restore correct row ordering.
        /// No-op if the list is not currently sorted.
        /// </summary>
        public void ReapplySort()
        {
            if (_isSorted && _sortDescriptions != null && _sortDescriptions.Count > 0)
                ApplySortInternal();
        }

        #endregion

        #region Sort implementation

        private void ApplySortInternal()
        {
            List<T> list = Items as List<T>;
            if (list == null) return;

            list.Sort(Compare);

            _isSorted = true;
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        private int Compare(T lhs, T rhs)
        {
            if (_sortDescriptions == null || _sortDescriptions.Count == 0)
                return 0;

            for (int i = 0; i < _sortDescriptions.Count; i++)
            {
                var desc = _sortDescriptions[i];
                object lhsVal = desc.PropertyDescriptor.GetValue(lhs);
                object rhsVal = desc.PropertyDescriptor.GetValue(rhs);

                if (_displayResolvers.TryGetValue(desc.PropertyDescriptor.Name, out var resolver))
                {
                    lhsVal = resolver(lhsVal);
                    rhsVal = resolver(rhsVal);
                }

                int result = CompareValues(lhsVal, rhsVal);

                if (desc.SortDirection == ListSortDirection.Descending)
                    result = -result;

                if (result != 0)
                    return result;
            }

            return 0;
        }

        private int CompareValues(object lhsValue, object rhsValue)
        {
            if (lhsValue == null)
            {
                return (rhsValue == null) ? 0 : -1; //nulls are equal
            }
            if (rhsValue == null)
            {
                return 1; //first has value, second doesn't
            }
            if (lhsValue is IComparable comparable)
            {
                return comparable.CompareTo(rhsValue);
            }
            if (lhsValue.Equals(rhsValue))
            {
                return 0; //both are the same
            }
            //not comparable, compare ToString
            return lhsValue.ToString().CompareTo(rhsValue.ToString());
        }

        #endregion
    }
}