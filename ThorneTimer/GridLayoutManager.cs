using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Façade for per-grid layout persistence (column widths, fill weights,
    /// and multi-column sort state).  Bundles the FormMain-side wiring that
    /// loads a grid's persisted layout from the database and applies it.
    ///
    /// The underlying storage and SQL still live in <see cref="Database"/>;
    /// this class just removes the dictionary-iteration boilerplate from
    /// FormMain so each grid setup is a one-line call.
    /// </summary>
    internal class GridLayoutManager
    {
        private readonly SQLiteConnection con;

        public GridLayoutManager(SQLiteConnection con)
        {
            this.con = con;
        }

        /// <summary>
        /// Applies saved column widths and fill weights from the database to
        /// <paramref name="grid"/>.  Restores FillWeight for ALL columns so
        /// that Fill-mode recalculations preserve the user's proportions, and
        /// sets pixel Width for visible columns so non-Fill columns get their
        /// exact saved size.  Silently skips columns that no longer exist.
        /// </summary>
        public void LoadColumnWidths(string gridName, DataGridView grid)
        {
            try
            {
                Dictionary<string, int> widths = Database.GetColumnWidths(con, gridName);
                Dictionary<string, float> fillWeights = Database.GetColumnFillWeights(con, gridName);

                foreach (var kvp in fillWeights)
                {
                    if (grid.Columns.Contains(kvp.Key))
                    {
                        DataGridViewColumn col = grid.Columns[kvp.Key];
                        if (kvp.Value > 0)
                        {
                            col.FillWeight = kvp.Value;
                        }
                    }
                }

                foreach (var kvp in widths)
                {
                    if (grid.Columns.Contains(kvp.Key))
                    {
                        DataGridViewColumn col = grid.Columns[kvp.Key];
                        if (col.Visible && kvp.Value >= col.MinimumWidth)
                        {
                            col.Width = kvp.Value;
                        }
                    }
                }
            }
            catch
            {
                // Database may not have the table yet; ignore.
            }
        }

        /// <summary>
        /// Persists current column widths and fill weights for <paramref name="grid"/>.
        /// </summary>
        public void SaveColumnWidths(string gridName, DataGridView grid)
        {
            Database.SaveColumnWidths(con, gridName, grid);
        }

        /// <summary>
        /// Loads saved multi-column sort state for the given grid.  Returns
        /// true if a saved sort was applied; false if no saved sort exists
        /// (the caller is expected to apply a default sort in that case).
        /// </summary>
        public bool TryLoadSortState(string gridName, DataGridView grid)
        {
            try
            {
                var sorts = Database.GetSortState(con, gridName);
                if (sorts.Count == 0) return false;

                var list = grid.DataSource as SortableBindingList<Timers.GridData>;
                if (list == null) return false;

                var tuples = new (string, ListSortDirection)[sorts.Count];
                for (int i = 0; i < sorts.Count; i++)
                {
                    tuples[i] = (sorts[i].Item1, sorts[i].Item2);
                }
                list.ApplyMultiSort(tuples);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Persists the multi-column sort state for <paramref name="grid"/>.
        /// </summary>
        public void SaveSortState(string gridName, ListSortDescriptionCollection sortDescriptions)
        {
            Database.SaveSortState(con, gridName, sortDescriptions);
        }
    }
}
