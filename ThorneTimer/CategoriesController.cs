using System;
using System.Windows.Forms;

namespace ThorneTimer
{
    public class CategoriesController : IDisposable
    {
        private readonly CategoriesRepository repository;
        private readonly Action categoriesChanged;
        private DataGridView grid;

        public CategoriesController(CategoriesRepository repository, Action categoriesChanged)
        {
            this.repository = repository;
            this.categoriesChanged = categoriesChanged;
        }

        public void Initialize(DataGridView categoriesGrid)
        {
            if (categoriesGrid == null) return;

            grid = categoriesGrid;
            ConfigureGrid();
            Reload();
        }

        public void Reload()
        {
            if (grid == null) return;
            grid.DataSource = new SortableBindingList<Categories.GridData>(repository.GetCategories());
        }

        public void SaveAll()
        {
            if (grid == null) return;

            for (int r = 0; r < grid.Rows.Count; r++)
            {
                SaveRow(grid.Rows[r]);
            }
        }

        public void AddCategory()
        {
            if (grid == null) return;

            var data = repository.GetCategories();
            var category = new Categories.GridData { ID = -1 };
            data.Add(category);
            grid.DataSource = new SortableBindingList<Categories.GridData>(data);

            grid.CurrentCell = grid.Rows[grid.Rows.Count - 1].Cells["Name"];
            grid.BeginEdit(true);
        }

        public bool DeleteCurrentCategory()
        {
            if (grid?.CurrentCell == null) return false;

            DialogResult result = MessageBox.Show("Are you sure you want to delete this category?", "Delete Category", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (result != DialogResult.Yes) return false;

            long id = Convert.ToInt64(grid.Rows[grid.CurrentCell.RowIndex].Cells["ID"].Value);
            repository.DeleteCategory(id);
            Reload();
            categoriesChanged?.Invoke();
            return true;
        }

        public void Dispose()
        {
            UnwireEvents();
        }

        private void ConfigureGrid()
        {
            UnwireEvents();
            grid.Columns.Clear();

            grid.Columns.Add("ID", "ID");
            grid.Columns["ID"].DataPropertyName = "ID";
            grid.Columns["ID"].Visible = false;
            grid.Columns["ID"].SortMode = DataGridViewColumnSortMode.NotSortable;

            grid.Columns.Add("Name", "Name");
            grid.Columns["Name"].DataPropertyName = "Name";
            grid.Columns["Name"].Width = 100;
            grid.Columns["Name"].FillWeight = 100;
            grid.Columns["Name"].SortMode = DataGridViewColumnSortMode.Automatic;

            grid.Columns.Add("StartKeyword", "Start Keyword");
            grid.Columns["StartKeyword"].DataPropertyName = "StartKeyword";
            grid.Columns["StartKeyword"].Width = 300;
            grid.Columns["StartKeyword"].FillWeight = 300;
            grid.Columns["StartKeyword"].SortMode = DataGridViewColumnSortMode.Automatic;

            grid.Columns.Add("EndKeyword", "End Keyword");
            grid.Columns["EndKeyword"].DataPropertyName = "EndKeyword";
            grid.Columns["EndKeyword"].Width = 300;
            grid.Columns["EndKeyword"].FillWeight = 300;
            grid.Columns["EndKeyword"].SortMode = DataGridViewColumnSortMode.Automatic;

            var chkAutoStop = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Auto Stop",
                Name = "AutoStop",
                DataPropertyName = "AutoStop",
                TrueValue = 1,
                FalseValue = 0
            };
            grid.Columns.Add(chkAutoStop);
            grid.Columns["AutoStop"].Width = 70;
            grid.Columns["AutoStop"].MinimumWidth = 70;
            grid.Columns["AutoStop"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            grid.Columns["AutoStop"].SortMode = DataGridViewColumnSortMode.Automatic;

            grid.RowValidating += Grid_RowValidating;
            grid.DataError += Grid_DataError;
            grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
        }

        private void UnwireEvents()
        {
            if (grid == null) return;
            grid.RowValidating -= Grid_RowValidating;
            grid.DataError -= Grid_DataError;
            grid.CellToolTipTextNeeded -= Grid_CellToolTipTextNeeded;
        }

        private void Grid_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            SaveAll();
            categoriesChanged?.Invoke();
        }

        private void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            ThorneLog.Warn($"Categories grid data error at ({e.RowIndex}, {e.ColumnIndex}): {e.Exception?.Message ?? "Unknown"}");
            e.ThrowException = false;
        }

        private void Grid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = grid.Columns[e.ColumnIndex].Name;
            switch (colName)
            {
                case "Name":
                    e.ToolTipText = "A logical group of timers (e.g. a zone, raid, or spell set).\nAll timers assigned to this category can be activated or\ndeactivated together by the Start/End Keywords below.";
                    break;

                case "StartKeyword":
                    e.ToolTipText = "Log text that ACTIVATES every timer in this category.\nSeparate multiple alternatives with a pipe ( | ) to match ANY of them.\nExample: You have entered Plane of Hate|You have entered The Plane of Fear";
                    break;

                case "EndKeyword":
                    e.ToolTipText = "Log text that DEACTIVATES every timer in this category.\nSeparate multiple alternatives with a pipe ( | ) to match ANY of them.\nExample: LOADING, PLEASE WAIT";
                    break;

                case "AutoStop":
                    e.ToolTipText = "When checked, the End Keyword also stops any running timers\nin this category (not just deactivates them for matching).";
                    break;
            }
        }

        private void SaveRow(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return;

            var category = new Categories.GridData
            {
                ID = Convert.ToInt64(row.Cells["ID"].Value),
                Name = Convert.ToString(row.Cells["Name"].Value),
                StartKeyword = Convert.ToString(row.Cells["StartKeyword"].Value),
                EndKeyword = Convert.ToString(row.Cells["EndKeyword"].Value),
                AutoStop = Convert.ToInt64(row.Cells["AutoStop"].Value ?? 0)
            };

            repository.SaveCategory(category);
            row.Cells["ID"].Value = category.ID;
        }
    }
}
