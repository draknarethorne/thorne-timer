using System;
using System.Drawing;
using System.Windows.Forms;

namespace ThorneTimer
{
    public class StylesController : IDisposable
    {
        private readonly StylesRepository repository;
        private readonly Action stylesChanged;
        private DataGridView grid;

        public StylesController(StylesRepository repository, Action stylesChanged)
        {
            this.repository = repository;
            this.stylesChanged = stylesChanged;
        }

        public DataGridView Grid
        {
            get { return grid; }
        }

        public void Initialize(DataGridView stylesGrid)
        {
            if (stylesGrid == null) return;

            grid = stylesGrid;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoGenerateColumns = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            ConfigureGrid();
            Reload();
        }

        public void Reload()
        {
            if (grid == null) return;
            grid.DataSource = repository.GetStyles();
        }

        public void AddStyle()
        {
            if (grid == null) return;

            StyleData created = repository.CreateDefaultStyle();
            Reload();
            stylesChanged?.Invoke();

            if (created == null) return;

            for (int r = 0; r < grid.Rows.Count; r++)
            {
                if (Convert.ToInt64(grid.Rows[r].Cells["ID"].Value) == created.ID)
                {
                    grid.CurrentCell = grid.Rows[r].Cells["ForeColor"];
                    grid.FirstDisplayedScrollingRowIndex = r;
                    break;
                }
            }
        }

        public bool DeleteCurrentStyle()
        {
            if (grid?.CurrentCell == null) return false;

            int rowIndex = grid.CurrentCell.RowIndex;
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return false;

            string name = Convert.ToString(grid.Rows[rowIndex].Cells["Name"].Value);
            if (string.Equals(name, "Normal", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The 'Normal' style cannot be deleted.", "Delete Style", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            DialogResult result = MessageBox.Show("Delete style '" + name + "'? Timers and views using this style will be reset to 'Normal'.", "Delete Style", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return false;

            long id = Convert.ToInt64(grid.Rows[rowIndex].Cells["ID"].Value);
            repository.DeleteStyle(id);
            Reload();
            stylesChanged?.Invoke();
            return true;
        }

        private void ConfigureGrid()
        {
            UnwireEvents();
            grid.Columns.Clear();

            grid.Columns.Add("ID", "ID");
            grid.Columns["ID"].DataPropertyName = "ID";
            grid.Columns["ID"].Visible = false;

            grid.Columns.Add("Name", "Name");
            grid.Columns["Name"].DataPropertyName = "Name";
            grid.Columns["Name"].Width = 160;
            grid.Columns["Name"].MinimumWidth = 100;
            grid.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            grid.Columns.Add("ForeColor", "Text Color");
            grid.Columns["ForeColor"].DataPropertyName = "ForeColor";
            grid.Columns["ForeColor"].Width = 90;
            grid.Columns["ForeColor"].MinimumWidth = 70;
            grid.Columns["ForeColor"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            grid.Columns.Add("BackColor", "Base Color");
            grid.Columns["BackColor"].DataPropertyName = "BackColor";
            grid.Columns["BackColor"].Width = 90;
            grid.Columns["BackColor"].MinimumWidth = 70;
            grid.Columns["BackColor"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            grid.Columns.Add("Example", "Example");
            grid.Columns["Example"].ReadOnly = true;
            grid.Columns["Example"].MinimumWidth = 160;

            var timeFormatCol = new DataGridViewComboBoxColumn
            {
                Name = "TimeFormat",
                HeaderText = "Time Format",
                DataPropertyName = "TimeFormat",
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing,
                ValueType = typeof(TimeFormat),
                Width = 120,
                MinimumWidth = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            timeFormatCol.Items.AddRange(TimeFormat.Classic, TimeFormat.Long, TimeFormat.AdaptiveCompact, TimeFormat.FullCompact);
            grid.Columns.Add(timeFormatCol);

            grid.Columns.Add("SortOrder", "SortOrder");
            grid.Columns["SortOrder"].DataPropertyName = "SortOrder";
            grid.Columns["SortOrder"].Visible = false;

            grid.CellPainting += Grid_CellPainting;
            grid.CellFormatting += Grid_CellFormatting;
            grid.CellClick += Grid_CellClick;
            grid.RowValidating += Grid_RowValidating;
            grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            grid.CellValueChanged += Grid_CellValueChanged;
        }

        public void Dispose()
        {
            UnwireEvents();
        }

        private void UnwireEvents()
        {
            if (grid == null) return;

            grid.CellPainting -= Grid_CellPainting;
            grid.CellFormatting -= Grid_CellFormatting;
            grid.CellClick -= Grid_CellClick;
            grid.RowValidating -= Grid_RowValidating;
            grid.CurrentCellDirtyStateChanged -= Grid_CurrentCellDirtyStateChanged;
            grid.CellValueChanged -= Grid_CellValueChanged;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name != "Example") return;

            int foreColor = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["ForeColor"].Value ?? Color.Black.ToArgb());
            int backColor = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["BackColor"].Value ?? Color.Yellow.ToArgb());

            TimeFormat fmt = ParseTimeFormat(grid.Rows[e.RowIndex].Cells["TimeFormat"].Value);
            string sample = TimerTimeFormatter.Format(new TimeSpan(1, 2, 3, 45), fmt);

            e.Value = "Sample Timer " + sample;
            e.CellStyle.ForeColor = Color.FromArgb(foreColor);
            e.CellStyle.BackColor = Color.FromArgb(backColor);
            e.FormattingApplied = true;
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = grid.Columns[e.ColumnIndex].Name;
            if (colName != "ForeColor" && colName != "BackColor") return;

            int argb = Convert.ToInt32(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value ?? Color.Yellow.ToArgb());
            Color cellColor = Color.FromArgb(argb);

            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

            Rectangle colorRect = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 4, e.CellBounds.Width - 8, e.CellBounds.Height - 8);
            using (var brush = new SolidBrush(cellColor))
            {
                e.Graphics.FillRectangle(brush, colorRect);
            }

            using (var pen = new Pen(Color.DarkGray, 1))
            {
                e.Graphics.DrawRectangle(pen, colorRect);
            }

            e.Handled = true;
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = grid.Columns[e.ColumnIndex].Name;
            if (colName != "ForeColor" && colName != "BackColor") return;

            DataGridViewCell cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            int currentArgb = Convert.ToInt32(cell.Value ?? Color.Yellow.ToArgb());

            using (var dlg = new ColorDialog())
            {
                dlg.Color = Color.FromArgb(currentArgb);
                dlg.FullOpen = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    cell.Value = dlg.Color.ToArgb();
                    SaveRow(grid.Rows[e.RowIndex]);
                    grid.InvalidateRow(e.RowIndex);
                    stylesChanged?.Invoke();
                }
            }
        }

        private void Grid_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
            SaveRow(grid.Rows[e.RowIndex]);
            stylesChanged?.Invoke();
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
            if (grid.Columns[e.ColumnIndex].Name == "ForeColor" || grid.Columns[e.ColumnIndex].Name == "BackColor") return;
            SaveRow(grid.Rows[e.RowIndex]);

            // Keep the live "Example" preview in sync when the format changes.
            if (grid.Columns[e.ColumnIndex].Name == "TimeFormat")
                grid.InvalidateRow(e.RowIndex);

            stylesChanged?.Invoke();
        }

        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grid.IsCurrentCellDirty && grid.CurrentCell?.OwningColumn?.Name == "TimeFormat")
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void SaveRow(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return;

            var style = new StyleData
            {
                ID = Convert.ToInt64(row.Cells["ID"].Value),
                Name = Convert.ToString(row.Cells["Name"].Value),
                ForeColor = Convert.ToInt32(row.Cells["ForeColor"].Value ?? Color.Black.ToArgb()),
                BackColor = Convert.ToInt32(row.Cells["BackColor"].Value ?? Color.Yellow.ToArgb()),
                SortOrder = Convert.ToInt32(row.Cells["SortOrder"].Value ?? 0),
                TimeFormat = ParseTimeFormat(row.Cells["TimeFormat"].Value)
            };

            repository.SaveStyle(style);
        }

        private static TimeFormat ParseTimeFormat(object value)
        {
            if (value is TimeFormat tf) return tf;
            if (value == null) return TimeFormat.Classic;

            try { return (TimeFormat)Convert.ToInt32(value); }
            catch { return TimeFormat.Classic; }
        }
    }
}
