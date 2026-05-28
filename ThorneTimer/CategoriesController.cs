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
            grid.DataSource = repository.GetCategories();
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
            grid.DataSource = data;

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

            grid.Columns.Add("Name", "Name");
            grid.Columns["Name"].DataPropertyName = "Name";
            grid.Columns["Name"].Width = 100;
            grid.Columns["Name"].FillWeight = 100;

            grid.Columns.Add("StartKeyword", "Start Keyword");
            grid.Columns["StartKeyword"].DataPropertyName = "StartKeyword";
            grid.Columns["StartKeyword"].Width = 300;
            grid.Columns["StartKeyword"].FillWeight = 300;

            grid.Columns.Add("EndKeyword", "End Keyword");
            grid.Columns["EndKeyword"].DataPropertyName = "EndKeyword";
            grid.Columns["EndKeyword"].Width = 300;
            grid.Columns["EndKeyword"].FillWeight = 300;

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

            grid.RowValidating += Grid_RowValidating;
        }

        private void UnwireEvents()
        {
            if (grid == null) return;
            grid.RowValidating -= Grid_RowValidating;
        }

        private void Grid_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            SaveAll();
            categoriesChanged?.Invoke();
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
