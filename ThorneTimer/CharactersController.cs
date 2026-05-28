using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Controller for the Characters tab.  Owns the grid layout, the
    /// Add/Delete actions, the LOG-button file picker, and per-row save
    /// dispatch.  Mirrors <see cref="CategoriesController"/>.
    ///
    /// FormMain still owns cross-cutting concerns that touch other
    /// subsystems (mini-view position sync, active-character combo
    /// refresh, runtime reload on delete) and injects those via the
    /// <see cref="BeforeRowSave"/> hook and the <see cref="CharactersChanged"/>
    /// callback so this controller stays focused on the Characters tab.
    /// </summary>
    internal class CharactersController : IDisposable
    {
        private readonly CharactersRepository repository;
        private readonly Action charactersChanged;
        private DataGridView grid;

        /// <summary>
        /// Optional pre-save hook invoked for each row immediately before it
        /// is persisted.  FormMain uses this to copy the current mini-view
        /// position into the MiniViewX / MiniViewY cells of the active row.
        /// </summary>
        public Action<DataGridViewRow> BeforeRowSave { get; set; }

        public CharactersController(CharactersRepository repository, Action charactersChanged)
        {
            this.repository = repository;
            this.charactersChanged = charactersChanged;
        }

        public void Initialize(DataGridView charactersGrid)
        {
            if (charactersGrid == null) return;

            grid = charactersGrid;
            ConfigureGrid();
            Reload();
        }

        public void Reload()
        {
            if (grid == null) return;
            grid.DataSource = CharactersRepository.GetCharacters(repository.Con);
        }

        public void SaveAll()
        {
            if (grid == null) return;

            for (int r = 0; r < grid.Rows.Count; r++)
            {
                var row = grid.Rows[r];
                if (row.IsNewRow) continue;

                BeforeRowSave?.Invoke(row);
                CharactersRepository.SaveCharacter(repository.Con, grid, row);
            }
        }

        public void AddCharacter()
        {
            if (grid == null) return;

            var data = CharactersRepository.GetCharacters(repository.Con);
            data.Add(new Characters.GridData
            {
                ID = -1,
                MiniViewX = 100,
                MiniViewY = 100
            });
            grid.DataSource = data;

            grid.CurrentCell = grid.Rows[grid.Rows.Count - 1].Cells[grid.Columns["Name"].Index];
            grid.BeginEdit(true);
        }

        public bool DeleteCurrentCharacter()
        {
            if (grid?.CurrentCell == null) return false;

            DialogResult result = MessageBox.Show(
                "Are you sure you want to delete this character?",
                "Delete Character",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result != DialogResult.Yes) return false;

            DataGridViewCell idCell = grid.Rows[grid.CurrentCell.RowIndex].Cells[grid.Columns["ID"].Index];
            CharactersRepository.DeleteCharacter(repository.Con, Convert.ToString(idCell.Value));
            Reload();
            charactersChanged?.Invoke();
            return true;
        }

        public void Dispose()
        {
            UnwireEvents();
        }

        // -----------------------------------------------------------------
        // Internal grid wiring
        // -----------------------------------------------------------------

        private void ConfigureGrid()
        {
            UnwireEvents();
            grid.Columns.Clear();

            grid.Columns.Add("ID", "ID");
            grid.Columns["ID"].DataPropertyName = "ID";
            grid.Columns["ID"].Visible = false;

            grid.Columns.Add("Name", "Name");
            grid.Columns["Name"].DataPropertyName = "Name";
            grid.Columns["Name"].FillWeight = 100;

            grid.Columns.Add("LogFile", "Log File");
            grid.Columns["LogFile"].DataPropertyName = "LogFile";
            grid.Columns["LogFile"].Width = 600;
            grid.Columns["LogFile"].FillWeight = 300;

            grid.Columns.Add("MiniViewX", "MiniViewX");
            grid.Columns["MiniViewX"].DataPropertyName = "MiniViewX";
            grid.Columns["MiniViewX"].Visible = false;

            grid.Columns.Add("MiniViewY", "MiniViewY");
            grid.Columns["MiniViewY"].DataPropertyName = "MiniViewY";
            grid.Columns["MiniViewY"].Visible = false;

            var cboCharClass = new DataGridViewComboBoxColumn
            {
                HeaderText = "Class",
                Name = "ClassID",
                DataPropertyName = "ClassID",
                ValueType = typeof(ComboBoxItem),
                DisplayMember = "Text",
                ValueMember = "Value",
                DataSource = ClassesRepository.GetGridClasses(repository.Con),
                FlatStyle = FlatStyle.Flat
            };
            grid.Columns.Add(cboCharClass);
            grid.Columns["ClassID"].Width = 120;
            grid.Columns["ClassID"].MinimumWidth = 80;

            var buttonLogFile = new DataGridViewButtonColumn
            {
                HeaderText = "",
                Name = "LOG",
                Text = "...",
                UseColumnTextForButtonValue = true
            };
            grid.Columns.Add(buttonLogFile);
            grid.Columns["LOG"].Width = 30;
            grid.Columns["LOG"].MinimumWidth = 30;
            grid.Columns["LOG"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            grid.Columns["LOG"].Resizable = DataGridViewTriState.False;

            // Explicit visible column ordering
            int ci = 0;
            grid.Columns["ID"].DisplayIndex = ci++;
            grid.Columns["Name"].DisplayIndex = ci++;
            grid.Columns["ClassID"].DisplayIndex = ci++;
            grid.Columns["LogFile"].DisplayIndex = ci++;
            grid.Columns["MiniViewX"].DisplayIndex = ci++;
            grid.Columns["MiniViewY"].DisplayIndex = ci++;
            grid.Columns["LOG"].DisplayIndex = ci++;

            grid.RowValidating += Grid_RowValidating;
            grid.CellClick += Grid_CellClick;
        }

        private void UnwireEvents()
        {
            if (grid == null) return;
            grid.RowValidating -= Grid_RowValidating;
            grid.CellClick -= Grid_CellClick;
        }

        private void Grid_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            SaveAll();
            charactersChanged?.Invoke();
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex != grid.Columns["LOG"].Index) return;

            BrowseForLogFile(e.RowIndex);
        }

        private void BrowseForLogFile(int rowIndex)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Log files (*.txt)|*.txt|All files (*.*)|*.*",
                DereferenceLinks = false,
                AutoUpgradeEnabled = true
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                var logFileCell = grid.Rows[rowIndex].Cells[grid.Columns["LogFile"].Index];
                foreach (string filename in openFileDialog.FileNames)
                {
                    logFileCell.Value = filename;
                    break;
                }
            }
        }
    }
}
