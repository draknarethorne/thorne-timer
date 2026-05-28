using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ThorneTimer
{
    public partial class FormTomeInfo : Form
    {
        private readonly SQLiteConnection _con;
        private readonly string _dbPath;

        public FormTomeInfo(SQLiteConnection con, string dbPath)
        {
            InitializeComponent();
            _con = con;
            _dbPath = dbPath;
        }

        private void FormTomeInfo_Load(object sender, EventArgs e)
        {
            // ----- Tome file info -----
            labelTomeName.Text = Path.GetFileName(_dbPath);
            labelTomePath.Text = _dbPath;
            toolTipPath.SetToolTip(labelTomePath, _dbPath);

            try
            {
                var fileInfo = new FileInfo(_dbPath);
                labelFileSize.Text = FormatFileSize(fileInfo.Length);
                labelCreated.Text = fileInfo.CreationTime.ToString("yyyy-MM-dd  HH:mm");
                labelModified.Text = fileInfo.LastWriteTime.ToString("yyyy-MM-dd  HH:mm");
            }
            catch
            {
                labelFileSize.Text = "\u2014";
                labelCreated.Text = "\u2014";
                labelModified.Text = "\u2014";
            }

            // Current running application version.  Once the planned db_meta
            // stamp lands (Phase C add-on) this will become "stamped vs running"
            // so users can see when a tome was last written by an older build.
            try
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                labelAppVersion.Text = v != null ? v.ToString() : "\u2014";
            }
            catch
            {
                labelAppVersion.Text = "\u2014";
            }

            // ----- Database statistics -----
            var stats = TomeStatisticsRepository.Get(_con);

            // Tome Version stamp (db_meta) shown next to the file info.
            // Falls back to "—" for tomes written by builds prior to v0.6.0
            // that hadn't introduced the db_meta table yet.
            labelTomeVersion.Text = !string.IsNullOrEmpty(stats.LastWrittenByVersion)
                ? stats.LastWrittenByVersion
                + (string.IsNullOrEmpty(stats.SchemaVersion) ? "" : "  (schema v" + stats.SchemaVersion + ")")
                : "\u2014";

            labelCreatedBy.Text = !string.IsNullOrEmpty(stats.CreatedByVersion)
                ? stats.CreatedByVersion
                : "\u2014";

            // Timer counts (Total / Active / Running) — mirrors the format used
            // in the main form's status bar so the two stay visually consistent.
            labelTimersValue.Text        = stats.TimerCount.ToString();
            labelActiveTimersValue.Text  = stats.ActiveTimerCount.ToString();
            labelRunningTimersValue.Text = stats.RunningTimerCount.ToString();

            // Catalog counts (one bold value per cell — no truncation, easy to scan)
            labelCountCharacters.Text = stats.CharacterCount.ToString();
            labelCountCategories.Text = stats.CategoryCount.ToString();
            labelCountStyles.Text     = stats.StyleCount.ToString();
            labelCountViews.Text      = stats.ViewCount.ToString();
            labelCountClasses.Text    = stats.ClassCount.ToString();

            // ----- Feature usage -----
            PopulateUsage(stats);

            // ----- Breakdown lists -----
            PopulateBreakdown(listByCategory, stats.TimersByCategory);
            PopulateBreakdown(listByStyle,    stats.TimersByStyle);
            PopulateBreakdown(listByClass,    stats.TimersByClass);
            PopulateBreakdown(listByScope,    stats.TimersByScope);

            // Click-to-sort on every list. Column 0 sorts as text; the
            // remaining columns sort numerically (count / percent) so "10"
            // doesn't fall between "1" and "2".
            EnableColumnSort(listUsage,      numericColumns: new[] { 1, 2 });
            EnableColumnSort(listByCategory, numericColumns: new[] { 1 });
            EnableColumnSort(listByStyle,    numericColumns: new[] { 1 });
            EnableColumnSort(listByClass,    numericColumns: new[] { 1 });
            EnableColumnSort(listByScope,    numericColumns: new[] { 1 });

            // Fit columns once the form is fully laid out and visible —
            // ClientSize is not reliable during Load because the
            // TableLayoutPanel hasn't finished arranging its cells yet.
            this.Shown += (s, ev) =>
            {
                ThorneLog.Info($"FormTomeInfo.Shown: groupBreakdown={groupBreakdown.ClientSize}, tableBreakdown={tableBreakdown.ClientSize}");
                ThorneLog.Info($"FormTomeInfo.Shown: listByCategory.ClientSize={listByCategory.ClientSize}, listByStyle.ClientSize={listByStyle.ClientSize}");
                ThorneLog.Info($"FormTomeInfo.Shown: listByClass.ClientSize={listByClass.ClientSize}, listByScope.ClientSize={listByScope.ClientSize}");

                FitBreakdownColumns(listByCategory, EventArgs.Empty);
                FitBreakdownColumns(listByStyle,    EventArgs.Empty);
                FitBreakdownColumns(listByClass,    EventArgs.Empty);
                FitBreakdownColumns(listByScope,    EventArgs.Empty);

                ThorneLog.Info($"FormTomeInfo.Shown(after fit): listByCategory cols=[{listByCategory.Columns[0].Width},{listByCategory.Columns[1].Width}]");
            };
        }

        private void PopulateUsage(TomeStatistics stats)
        {
            int total = stats.TimerCount;

            // Feature, Count.  Order is roughly "most informative first".
            var rows = new (string label, int count)[]
            {
                ("Start keyword",        stats.WithStartKeyword),
                ("End keyword",          stats.WithEndKeyword),
                ("Multi-keyword (|)",    stats.WithMultiStartKey),
                ("Wildcard (*)",         stats.WithWildcardKey),
                ("Case-sensitive",       stats.WithCaseSensitive),
                ("Speech",               stats.WithSpeech),
                ("Sound file",           stats.WithSoundFile),
                ("Duration",             stats.WithDuration),
                ("Endless",              stats.WithEndless),
                ("DependsOn",            stats.WithDependsOn),
                ("Category assigned",    stats.WithCategoryAssigned),
                ("Class assigned",       stats.WithClassAssigned),
            };

            listUsage.BeginUpdate();
            listUsage.Items.Clear();
            foreach (var row in rows)
            {
                string pct = total > 0
                    ? ((row.count * 100.0) / total).ToString("F0") + "%"
                    : "\u2014";
                var item = new ListViewItem(new[] { row.label, row.count.ToString(), pct });
                if (row.count == 0)
                    item.ForeColor = System.Drawing.Color.Gray;
                listUsage.Items.Add(item);
            }
            listUsage.EndUpdate();
        }

        private static void PopulateBreakdown(ListView list, Dictionary<string, int> data)
        {
            list.BeginUpdate();
            list.Items.Clear();
            if (data == null || data.Count == 0)
            {
                var empty = new ListViewItem(new[] { "(none)", "0" });
                empty.ForeColor = System.Drawing.Color.Gray;
                list.Items.Add(empty);
            }
            else
            {
                // Descending by count, then alpha — most-used styles/categories
                // float to the top, which is what the user usually wants to see.
                foreach (var kvp in data.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
                {
                    list.Items.Add(new ListViewItem(new[] { kvp.Key, kvp.Value.ToString() }));
                }
            }
            list.EndUpdate();

            // Hook the size-changed event so the columns refit *after* the
            // TableLayoutPanel finishes laying out (Load runs before layout,
            // so ClientSize.Width is still the design default during Populate).
            // Idempotent: remove first so repeated reloads don't stack handlers.
            list.ClientSizeChanged -= FitBreakdownColumns;
            list.ClientSizeChanged += FitBreakdownColumns;

            // Best-effort initial fit (covers the case where the list is
            // already sized, e.g. on the second show).
            FitBreakdownColumns(list, EventArgs.Empty);
        }

        /// <summary>
        /// Sizes the count column to its widest value, then gives every
        /// remaining pixel to the name column so the list fills its frame
        /// and long names don't truncate.
        /// </summary>
        private static void FitBreakdownColumns(object sender, EventArgs e)
        {
            var list = sender as ListView;
            if (list == null || list.Columns.Count < 2) return;
            if (list.ClientSize.Width <= 0) return;

            list.BeginUpdate();
            try
            {
                list.Columns[1].Width = -1;                 // size to widest count value
                if (list.Columns[1].Width < 40)
                    list.Columns[1].Width = 40;

                int remaining = list.ClientSize.Width - list.Columns[1].Width;
                if (remaining < 60) remaining = 60;
                list.Columns[0].Width = remaining;
            }
            finally
            {
                list.EndUpdate();
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " bytes";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            return (bytes / (1024.0 * 1024.0)).ToString("F1") + " MB";
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void listByCategory_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Wires a column-click sorter onto the given list.  The columns whose
        /// indexes appear in <paramref name="numericColumns"/> are sorted as
        /// integers (stripping a trailing '%' for the usage % column); all
        /// other columns sort as case-insensitive text.  Clicking the same
        /// column header again toggles ascending/descending.
        /// </summary>
        private static void EnableColumnSort(ListView list, int[] numericColumns)
        {
            if (list == null) return;
            var sorter = new BreakdownListSorter(numericColumns ?? new int[0]);
            list.ListViewItemSorter = sorter;
            list.ColumnClick -= BreakdownListSorter.OnColumnClick;
            list.ColumnClick += BreakdownListSorter.OnColumnClick;
        }

        /// <summary>
        /// Lightweight <see cref="System.Collections.IComparer"/> for the
        /// Tome Info lists.  Tracks the active sort column / direction and
        /// knows which columns are numeric so it can avoid lexical sorts
        /// on integer / percent values.
        /// </summary>
        private sealed class BreakdownListSorter : System.Collections.IComparer
        {
            private readonly System.Collections.Generic.HashSet<int> _numeric;
            private int _column;
            private System.Windows.Forms.SortOrder _order = System.Windows.Forms.SortOrder.Descending;

            public BreakdownListSorter(int[] numericColumns)
            {
                _numeric = new System.Collections.Generic.HashSet<int>(numericColumns);
                // Default to the first numeric column descending so the lists
                // open in their natural "most-used first" order.
                _column = numericColumns != null && numericColumns.Length > 0
                    ? numericColumns[0]
                    : 0;
            }

            public int Compare(object x, object y)
            {
                var a = x as System.Windows.Forms.ListViewItem;
                var b = y as System.Windows.Forms.ListViewItem;
                if (a == null || b == null) return 0;
                if (_column >= a.SubItems.Count || _column >= b.SubItems.Count) return 0;

                string sa = a.SubItems[_column].Text;
                string sb = b.SubItems[_column].Text;

                int result;
                if (_numeric.Contains(_column))
                {
                    int ia, ib;
                    if (!int.TryParse(sa.TrimEnd('%').Trim(), out ia)) ia = int.MinValue;
                    if (!int.TryParse(sb.TrimEnd('%').Trim(), out ib)) ib = int.MinValue;
                    result = ia.CompareTo(ib);
                }
                else
                {
                    result = string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
                }

                return _order == System.Windows.Forms.SortOrder.Descending ? -result : result;
            }

            public static void OnColumnClick(object sender, System.Windows.Forms.ColumnClickEventArgs e)
            {
                var list = sender as System.Windows.Forms.ListView;
                if (list == null) return;
                var sorter = list.ListViewItemSorter as BreakdownListSorter;
                if (sorter == null) return;

                if (sorter._column == e.Column)
                {
                    sorter._order = sorter._order == System.Windows.Forms.SortOrder.Ascending
                        ? System.Windows.Forms.SortOrder.Descending
                        : System.Windows.Forms.SortOrder.Ascending;
                }
                else
                {
                    sorter._column = e.Column;
                    // First click on a new column: numeric columns start
                    // descending (largest first), text columns ascending.
                    sorter._order = sorter._numeric.Contains(e.Column)
                        ? System.Windows.Forms.SortOrder.Descending
                        : System.Windows.Forms.SortOrder.Ascending;
                }

                list.Sort();
            }
        }
    }
}
