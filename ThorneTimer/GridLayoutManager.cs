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

                // Layout-schema guard: a saved layout only describes the columns that
                // existed when it was written.  When a new column is later added to a
                // grid (e.g. the Styles "Time Format" column), the persisted layout has
                // no entry for it, so the new column's coded default width is added on
                // top of every restored width — pushing the total past the grid's client
                // area until the user manually resizes.
                //
                // Rather than discard the user's saved widths (unfriendly), we keep their
                // overall footprint and make room for the new column(s) by proportionally
                // shrinking the existing saved columns down to their MinimumWidth floors.
                // This is width-independent on purpose: the grid's client width is
                // unreliable for tabs that have never been shown (see the FillWeight note
                // below), but the user's previously-saved footprint is a layout they had
                // already accepted as fitting.  SaveColumnWidths rewrites the full current
                // column set on close, so the adjusted layout becomes the new baseline.
                if (IsSavedLayoutStale(grid, widths))
                {
                    ApplyWidthsWithFitForNewColumns(grid, widths);
                    return;
                }

                foreach (var kvp in widths)
                {
                    if (grid.Columns.Contains(kvp.Key))
                    {
                        DataGridViewColumn col = grid.Columns[kvp.Key];

                        // For Fill-mode columns, layout is governed by FillWeight
                        // (restored above), NOT pixel Width.  Assigning col.Width
                        // here makes WinForms back-compute FillWeight against the
                        // grid's CURRENT client width — which, for tabs that have
                        // never been shown, is wrong and corrupts the restored
                        // proportions (the Characters/Categories misalignment).
                        // Only apply pixel Width to non-Fill columns.
                        if (col.InheritedAutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
                            continue;

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
        /// Applies a saved layout that is missing one or more newly-added columns,
        /// fitting the new column(s) into the user's existing footprint instead of
        /// discarding their saved widths.
        ///
        /// Strategy (width-independent): treat the sum of the saved widths of the
        /// currently-visible, non-Fill columns as the budget the user already accepted.
        /// New columns (no saved width) want their coded default width; we reclaim that
        /// many pixels from the existing saved columns, distributed in proportion to how
        /// much slack each one has above its <see cref="DataGridViewColumn.MinimumWidth"/>.
        /// Columns already at their floor give nothing.  If the existing columns cannot
        /// yield enough (everything is at its floor), the footprint grows by the shortfall
        /// — a best-effort minimum rather than a hard overflow.  Hidden and Fill columns
        /// are left to the normal saved-width / FillWeight handling.
        /// </summary>
        private static void ApplyWidthsWithFitForNewColumns(DataGridView grid, Dictionary<string, int> savedWidths)
        {
            var existing = new List<DataGridViewColumn>();
            var newCols = new List<DataGridViewColumn>();

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (!col.Visible)
                    continue;
                if (col.Resizable == DataGridViewTriState.False)
                    continue;
                if (col.InheritedAutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
                    continue;

                if (savedWidths.ContainsKey(col.Name))
                    existing.Add(col);
                else
                    newCols.Add(col);
            }

            // Pixels the new columns want, at their coded default (clamped to floor).
            int need = 0;
            foreach (DataGridViewColumn col in newCols)
                need += System.Math.Max(col.Width, col.MinimumWidth);

            // Total slack available above each existing column's minimum width.
            int totalSlack = 0;
            foreach (DataGridViewColumn col in existing)
            {
                int saved = System.Math.Max(savedWidths[col.Name], col.MinimumWidth);
                totalSlack += saved - col.MinimumWidth;
            }

            int reclaim = System.Math.Min(need, totalSlack);

            // Apply existing columns at their saved width minus a proportional share of
            // the reclaim, floored at MinimumWidth.
            int distributed = 0;
            for (int i = 0; i < existing.Count; i++)
            {
                DataGridViewColumn col = existing[i];
                int saved = System.Math.Max(savedWidths[col.Name], col.MinimumWidth);
                int slack = saved - col.MinimumWidth;

                int give;
                if (i == existing.Count - 1)
                {
                    // Last column absorbs any rounding remainder so the total reclaim is exact.
                    give = reclaim - distributed;
                }
                else if (totalSlack > 0)
                {
                    give = (int)System.Math.Round((double)reclaim * slack / totalSlack);
                }
                else
                {
                    give = 0;
                }

                if (give < 0) give = 0;
                if (give > slack) give = slack;
                distributed += give;

                col.Width = saved - give;
            }

            // New columns keep their coded default (clamped to their floor).
            foreach (DataGridViewColumn col in newCols)
            {
                if (col.Width < col.MinimumWidth)
                    col.Width = col.MinimumWidth;
            }

            // Hidden non-Fill columns don't contribute to the visible footprint, but
            // their saved widths are still meaningful (e.g. the Timers grid persists
            // hidden-column widths for compact/advanced view toggling).  Restore them
            // verbatim so the stale path doesn't silently drop that state.
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Visible)
                    continue;
                if (col.Resizable == DataGridViewTriState.False)
                    continue;
                if (col.InheritedAutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
                    continue;
                if (!savedWidths.ContainsKey(col.Name))
                    continue;

                int saved = savedWidths[col.Name];
                if (saved >= col.MinimumWidth)
                    col.Width = saved;
            }
        }

        /// <summary>
        /// Returns true when a non-empty saved layout is missing one or more of the
        /// grid's current resizable columns — i.e. the layout was written before that
        /// column existed and is therefore stale.  An empty layout (first run, or a
        /// grid that has never been persisted) is NOT considered stale; callers fall
        /// through to the normal saved-width restore (which is a no-op for unknown
        /// columns) in that case.  When stale, the caller fits the new column(s) into
        /// the user's existing footprint via <see cref="ApplyWidthsWithFitForNewColumns"/>
        /// rather than discarding their saved widths.
        /// Mirrors the column-eligibility filter in <see cref="Database.SaveColumnWidths"/>
        /// (resizable columns only) so the staleness check matches what gets saved.
        /// </summary>
        private static bool IsSavedLayoutStale(DataGridView grid, Dictionary<string, int> savedWidths)
        {
            if (savedWidths == null || savedWidths.Count == 0)
                return false;

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Resizable == DataGridViewTriState.False)
                    continue;

                if (!savedWidths.ContainsKey(col.Name))
                    return true;
            }

            return false;
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
