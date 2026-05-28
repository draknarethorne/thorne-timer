using System;
using System.Drawing;
using System.Windows.Forms;

namespace ThorneTimer
{
    public class ViewsController : IDisposable
    {
        private readonly ViewsRepository repository;
        private readonly StylesRepository stylesRepository;
        private readonly Action viewsChanged;
        private DataGridView grid;

        public ViewsController(ViewsRepository repository, StylesRepository stylesRepository, Action viewsChanged)
        {
            this.repository = repository;
            this.stylesRepository = stylesRepository;
            this.viewsChanged = viewsChanged;
        }

        public void Initialize(DataGridView viewsGrid)
        {
            if (viewsGrid == null) return;

            grid = viewsGrid;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AutoGenerateColumns = false;
            ConfigureGrid();
            Reload();
        }

        public void Reload()
        {
            if (grid == null) return;
            grid.DataSource = repository.GetViews();
        }

        public void RefreshStyleOptions()
        {
            if (grid == null) return;
            var col = grid.Columns["StyleFilter"] as DataGridViewComboBoxColumn;
            if (col == null) return;

            col.Items.Clear();
            if (stylesRepository != null)
            {
                foreach (string name in stylesRepository.GetStyleNames())
                    col.Items.Add(name);
            }
            if (col.Items.Count == 0)
                col.Items.Add("Normal");
        }

        public void AddView()
        {
            if (grid == null) return;

            var view = new ViewData
            {
                ID = -1,
                Name = "New View",
                ActiveYn = 1,
                StyleFilter = "Normal",
                ShowWarning = 1,
                EmptyBehavior = "ViewName",
                PositionX = 100,
                PositionY = 100,
                SortOrder = 0
            };
            repository.SaveView(view);
            Reload();
            viewsChanged?.Invoke();

            for (int r = 0; r < grid.Rows.Count; r++)
            {
                if (Convert.ToInt64(grid.Rows[r].Cells["ID"].Value) == view.ID)
                {
                    grid.CurrentCell = grid.Rows[r].Cells["Name"];
                    grid.BeginEdit(true);
                    break;
                }
            }
        }

        public bool DeleteCurrentView()
        {
            if (grid?.CurrentCell == null) return false;

            int rowIndex = grid.CurrentCell.RowIndex;
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return false;

            string name = Convert.ToString(grid.Rows[rowIndex].Cells["Name"].Value);
            DialogResult result = MessageBox.Show("Delete view '" + name + "'?", "Delete View", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return false;

            long id = Convert.ToInt64(grid.Rows[rowIndex].Cells["ID"].Value);
            repository.DeleteView(id);
            Reload();
            viewsChanged?.Invoke();
            return true;
        }

        public void SaveAll()
        {
            if (grid == null) return;

            for (int r = 0; r < grid.Rows.Count; r++)
            {
                SaveRow(grid.Rows[r]);
            }
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
            grid.Columns["Name"].Width = 200;
            grid.Columns["Name"].FillWeight = 200;

            var cboStyle = new DataGridViewComboBoxColumn
            {
                HeaderText = "Style",
                Name = "StyleFilter",
                DataPropertyName = "StyleFilter",
                FlatStyle = FlatStyle.Flat
            };
            if (stylesRepository != null)
            {
                foreach (string name in stylesRepository.GetStyleNames())
                    cboStyle.Items.Add(name);
            }
            if (cboStyle.Items.Count == 0)
                cboStyle.Items.Add("Normal");
            grid.Columns.Add(cboStyle);
            grid.Columns["StyleFilter"].Width = 85;
            grid.Columns["StyleFilter"].MinimumWidth = 60;
            grid.Columns["StyleFilter"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            var chkActive = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Active",
                Name = "ActiveYn",
                DataPropertyName = "ActiveYn",
                TrueValue = (long)1,
                FalseValue = (long)0
            };
            grid.Columns.Add(chkActive);
            grid.Columns["ActiveYn"].Width = 50;
            grid.Columns["ActiveYn"].MinimumWidth = 50;
            grid.Columns["ActiveYn"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            var chkShowWarning = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Show Warning",
                Name = "ShowWarning",
                DataPropertyName = "ShowWarning",
                TrueValue = 1,
                FalseValue = 0
            };
            grid.Columns.Add(chkShowWarning);
            grid.Columns["ShowWarning"].Width = 90;
            grid.Columns["ShowWarning"].MinimumWidth = 80;
            grid.Columns["ShowWarning"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            var cboEmptyBehavior = new DataGridViewComboBoxColumn
            {
                HeaderText = "When Empty",
                Name = "EmptyBehavior",
                DataPropertyName = "EmptyBehavior",
                FlatStyle = FlatStyle.Flat
            };
            cboEmptyBehavior.Items.AddRange("CharacterName", "ViewName", "Spaces", "HideEmpty");
            grid.Columns.Add(cboEmptyBehavior);
            grid.Columns["EmptyBehavior"].Width = 120;
            grid.Columns["EmptyBehavior"].MinimumWidth = 100;
            grid.Columns["EmptyBehavior"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            grid.Columns.Add("PositionX", "PositionX");
            grid.Columns["PositionX"].DataPropertyName = "PositionX";
            grid.Columns["PositionX"].Visible = false;
            grid.Columns.Add("PositionY", "PositionY");
            grid.Columns["PositionY"].DataPropertyName = "PositionY";
            grid.Columns["PositionY"].Visible = false;
            grid.Columns.Add("SortOrder", "SortOrder");
            grid.Columns["SortOrder"].DataPropertyName = "SortOrder";
            grid.Columns["SortOrder"].Visible = false;

            int vi = 0;
            grid.Columns["ID"].DisplayIndex = vi++;
            grid.Columns["ActiveYn"].DisplayIndex = vi++;
            grid.Columns["Name"].DisplayIndex = vi++;
            grid.Columns["StyleFilter"].DisplayIndex = vi++;
            grid.Columns["ShowWarning"].DisplayIndex = vi++;
            grid.Columns["EmptyBehavior"].DisplayIndex = vi++;
            grid.Columns["PositionX"].DisplayIndex = vi++;
            grid.Columns["PositionY"].DisplayIndex = vi++;
            grid.Columns["SortOrder"].DisplayIndex = vi++;

            grid.RowValidating += Grid_RowValidating;
            grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            grid.CellValueChanged += Grid_CellValueChanged;
            grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
        }

        public void Dispose()
        {
            UnwireEvents();
        }

        private void UnwireEvents()
        {
            if (grid == null) return;

            grid.RowValidating -= Grid_RowValidating;
            grid.CurrentCellDirtyStateChanged -= Grid_CurrentCellDirtyStateChanged;
            grid.CellValueChanged -= Grid_CellValueChanged;
            grid.CellToolTipTextNeeded -= Grid_CellToolTipTextNeeded;
        }

        private void Grid_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            SaveAll();
            viewsChanged?.Invoke();
        }

        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grid.IsCurrentCellDirty && grid.CurrentCell?.OwningColumn?.Name == "ActiveYn")
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name != "ActiveYn") return;

            SaveAll();
            viewsChanged?.Invoke();
        }

        private void Grid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name != "EmptyBehavior") return;

            string cellValue = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
            switch (cellValue)
            {
                case "CharacterName":
                    e.ToolTipText = "Always show active character name (typically used for Character view)";
                    break;
                case "ViewName":
                    e.ToolTipText = "Show view name when empty (e.g., 'Buffs', 'Pets', 'Spawns')";
                    break;
                case "Spaces":
                    e.ToolTipText = "Show minimal blank space to maintain view presence on screen";
                    break;
                case "HideEmpty":
                    e.ToolTipText = "Completely hide view when no timers are active (view disappears)";
                    break;
                default:
                    e.ToolTipText = "Controls what displays when view has no active timers";
                    break;
            }
        }

        private void SaveRow(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return;

            var view = new ViewData
            {
                ID = Convert.ToInt64(row.Cells["ID"].Value),
                Name = Convert.ToString(row.Cells["Name"].Value),
                ActiveYn = Convert.ToInt64(row.Cells["ActiveYn"].Value ?? 1),
                StyleFilter = Convert.ToString(row.Cells["StyleFilter"].Value ?? "Normal"),
                ShowWarning = Convert.ToInt32(row.Cells["ShowWarning"].Value ?? 1),
                EmptyBehavior = Convert.ToString(row.Cells["EmptyBehavior"].Value ?? "ViewName")
            };

            repository.SaveView(view);
            row.Cells["ID"].Value = view.ID;
        }
    }
}
