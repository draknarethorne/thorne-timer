using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Forms;

namespace ThorneTimer
{
    public class CategoriesRepository
    {
        private readonly SQLiteConnection con;

        public CategoriesRepository(SQLiteConnection con)
        {
            this.con = con;
        }

        public List<Categories.GridData> GetCategories()
        {
            return GetCategories(con);
        }

        public void SaveCategory(Categories.GridData category)
        {
            if (category == null) return;

            using (var cmd = new SQLiteCommand(con))
            {
                if (category.ID == -1)
                {
                    cmd.CommandText = "INSERT INTO categories (Name, StartKeyword, EndKeyword, AutoStop) VALUES (@name, @startKeyword, @endKeyword, @autoStop)";
                }
                else
                {
                    cmd.CommandText = "UPDATE categories SET Name = @name, StartKeyword = @startKeyword, EndKeyword = @endKeyword, AutoStop = @autoStop WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", category.ID);
                }

                cmd.Parameters.AddWithValue("@name", category.Name ?? "");
                cmd.Parameters.AddWithValue("@startKeyword", category.StartKeyword ?? "");
                cmd.Parameters.AddWithValue("@endKeyword", category.EndKeyword ?? "");
                cmd.Parameters.AddWithValue("@autoStop", category.AutoStop);
                cmd.ExecuteNonQuery();

                if (category.ID == -1)
                {
                    cmd.CommandText = "SELECT last_insert_rowid()";
                    cmd.Parameters.Clear();
                    category.ID = (long)cmd.ExecuteScalar();
                }
            }
        }

        public void DeleteCategory(long id)
        {
            DeleteCategory(con, id.ToString());
        }

        // ---------------------------------------------------------------
        // Static API â€” moved from Database.cs as part of the v0.6.0
        // repository extraction.  These are the original signatures used
        // by FormMain so existing call sites keep working unchanged.
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns combo-box items for category selection (with a leading "--" blank row).
        /// </summary>
        static internal List<ComboBoxItem> GetGridCategories(SQLiteConnection con)
        {
            List<ComboBoxItem> cboData = new List<ComboBoxItem>();

            cboData.Add(new ComboBoxItem { Value = 0, Text = "--" });

            using (var cmd = new SQLiteCommand("SELECT * from categories ORDER BY Name", con))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    cboData.Add(new ComboBoxItem
                    {
                        Value = rdr.GetInt32(rdr.GetOrdinal("ID")),
                        Text = rdr.GetString(rdr.GetOrdinal("Name"))
                    });
                }
            }

            return cboData;
        }

        /// <summary>
        /// Returns all categories ordered by Name (used by the Categories grid and logging dumps).
        /// </summary>
        static internal List<Categories.GridData> GetCategories(SQLiteConnection con)
        {
            var gridData = new List<Categories.GridData>();

            using (var cmd = new SQLiteCommand("SELECT * from categories ORDER BY Name", con))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    gridData.Add(new Categories.GridData
                    {
                        ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                        Name = rdr.GetString(rdr.GetOrdinal("Name")),
                        StartKeyword = rdr.GetString(rdr.GetOrdinal("StartKeyword")),
                        EndKeyword = rdr.GetString(rdr.GetOrdinal("EndKeyword")),
                        AutoStop = rdr.GetInt32(rdr.GetOrdinal("AutoStop"))
                    });
                }
            }

            return gridData;
        }

        /// <summary>Deletes a category by ID (string for grid compatibility).</summary>
        static internal void DeleteCategory(SQLiteConnection con, string ID)
        {
            if (!int.TryParse(ID, out int idValue)) return;
            using (var cmd = new SQLiteCommand("DELETE FROM categories WHERE ID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", idValue);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Saves a category from a DataGridView row.  Inserts when ID = -1,
        /// updates otherwise, and writes the new ID back into the grid on insert.
        /// </summary>
        static internal void SaveCategory(SQLiteConnection con, DataGridView dataGridView, DataGridViewRow row)
        {
            DataGridViewCell ID = row.Cells[dataGridView.Columns["ID"].Index];
            DataGridViewCell Name = row.Cells[dataGridView.Columns["Name"].Index];
            DataGridViewCell StartKeyword = row.Cells[dataGridView.Columns["StartKeyword"].Index];
            DataGridViewCell EndKeyword = row.Cells[dataGridView.Columns["EndKeyword"].Index];
            DataGridViewCell AutoStop = row.Cells[dataGridView.Columns["AutoStop"].Index];

            using (var cmd = new SQLiteCommand(con))
            {
                if (Convert.ToString(ID.Value) == "-1")
                {
                    cmd.CommandText = "INSERT INTO categories (Name, StartKeyword, EndKeyword, AutoStop) VALUES (@name, @startKeyword, @endKeyword, @autoStop)";
                }
                else
                {
                    cmd.CommandText = "UPDATE categories SET Name = @name, StartKeyword = @startKeyword, EndKeyword = @endKeyword, AutoStop = @autoStop WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(ID.Value));
                }

                cmd.Parameters.AddWithValue("@name", Convert.ToString(Name.Value));
                cmd.Parameters.AddWithValue("@startKeyword", Convert.ToString(StartKeyword.Value));
                cmd.Parameters.AddWithValue("@endKeyword", Convert.ToString(EndKeyword.Value));
                cmd.Parameters.AddWithValue("@autoStop", Convert.ToInt32(AutoStop.Value));
                cmd.ExecuteNonQuery();

                // Update ID in Grid When INSERTing
                if (Convert.ToString(ID.Value) == "-1")
                {
                    cmd.CommandText = "SELECT last_insert_rowid()";
                    cmd.Parameters.Clear();
                    row.Cells[dataGridView.Columns["ID"].Index].Value = (long)cmd.ExecuteScalar();
                }
            }
        }
    }
}

