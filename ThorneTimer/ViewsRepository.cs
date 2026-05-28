using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace ThorneTimer
{
    public class ViewsRepository
    {
        private readonly SQLiteConnection con;

        public ViewsRepository(SQLiteConnection con)
        {
            this.con = con;
        }

        public List<ViewData> GetViews()
        {
            var views = new List<ViewData>();

            using (var cmd = new SQLiteCommand(con))
            {
                cmd.CommandText = "SELECT ID, Name, ActiveYn, StyleFilter, PositionX, PositionY, SortOrder, ShowWarning, EmptyBehavior FROM miniviews ORDER BY SortOrder, Name";
                using (SQLiteDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        views.Add(new ViewData
                        {
                            ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                            Name = rdr.IsDBNull(rdr.GetOrdinal("Name")) ? "" : rdr.GetString(rdr.GetOrdinal("Name")),
                            ActiveYn = rdr.IsDBNull(rdr.GetOrdinal("ActiveYn")) ? 1 : rdr.GetInt32(rdr.GetOrdinal("ActiveYn")),
                            StyleFilter = rdr.IsDBNull(rdr.GetOrdinal("StyleFilter")) ? "Normal" : rdr.GetString(rdr.GetOrdinal("StyleFilter")),
                            PositionX = rdr.IsDBNull(rdr.GetOrdinal("PositionX")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionX")),
                            PositionY = rdr.IsDBNull(rdr.GetOrdinal("PositionY")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionY")),
                            SortOrder = rdr.IsDBNull(rdr.GetOrdinal("SortOrder")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("SortOrder")),
                            ShowWarning = rdr.IsDBNull(rdr.GetOrdinal("ShowWarning")) ? 1 : rdr.GetInt32(rdr.GetOrdinal("ShowWarning")),
                            EmptyBehavior = rdr.IsDBNull(rdr.GetOrdinal("EmptyBehavior")) ? "ViewName" : rdr.GetString(rdr.GetOrdinal("EmptyBehavior"))
                        });
                    }
                }
            }

            return views;
        }

        public void SaveView(ViewData view)
        {
            if (view == null) return;

            using (var cmd = new SQLiteCommand(con))
            {
                if (view.ID == -1)
                {
                    cmd.CommandText = "INSERT INTO miniviews (Name, ActiveYn, StyleFilter, ShowWarning, EmptyBehavior, PositionX, PositionY, SortOrder) VALUES (@name, @active, @style, @warn, @empty, 100, 100, 0)";
                }
                else
                {
                    cmd.CommandText = "UPDATE miniviews SET Name = @name, ActiveYn = @active, StyleFilter = @style, ShowWarning = @warn, EmptyBehavior = @empty WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", view.ID);
                }

                cmd.Parameters.AddWithValue("@name", view.Name ?? "");
                cmd.Parameters.AddWithValue("@active", view.ActiveYn);
                cmd.Parameters.AddWithValue("@style", string.IsNullOrEmpty(view.StyleFilter) ? "Normal" : view.StyleFilter);
                cmd.Parameters.AddWithValue("@warn", view.ShowWarning);
                cmd.Parameters.AddWithValue("@empty", string.IsNullOrEmpty(view.EmptyBehavior) ? "ViewName" : view.EmptyBehavior);
                cmd.ExecuteNonQuery();

                if (view.ID == -1)
                {
                    cmd.CommandText = "SELECT last_insert_rowid()";
                    cmd.Parameters.Clear();
                    view.ID = (long)cmd.ExecuteScalar();
                }
            }
        }

        public void DeleteView(long id)
        {
            using (var cmd = new SQLiteCommand(con))
            {
                cmd.CommandText = "DELETE FROM miniviews WHERE ID = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // ---------------------------------------------------------------
        // Static API â€” moved from Database.cs as part of the v0.6.0
        // repository extraction.  These keep the original Database.*
        // signatures so FormMain / MiniViews callers compile unchanged.
        // ---------------------------------------------------------------

        /// <summary>
        /// Loads MiniViews grid data with all colour / behaviour columns.
        /// Used to populate the Views tab grid.
        /// </summary>
        static internal List<MiniViews.GridData> GetViews(SQLiteConnection con)
        {
            var gridData = new List<MiniViews.GridData>();

            using (var cmd = new SQLiteCommand(
                "SELECT ID, Name, ActiveYn, StyleFilter, PositionX, PositionY, SortOrder, ForeColor, BackColor, ShowWarning, EmptyBehavior FROM miniviews ORDER BY SortOrder, Name", con))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    gridData.Add(new MiniViews.GridData
                    {
                        ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                        Name = rdr.IsDBNull(rdr.GetOrdinal("Name")) ? "" : rdr.GetString(rdr.GetOrdinal("Name")),
                        ActiveYn = rdr.IsDBNull(rdr.GetOrdinal("ActiveYn")) ? 1 : rdr.GetInt32(rdr.GetOrdinal("ActiveYn")),
                        StyleFilter = rdr.IsDBNull(rdr.GetOrdinal("StyleFilter")) ? "Normal" : rdr.GetString(rdr.GetOrdinal("StyleFilter")),
                        PositionX = rdr.IsDBNull(rdr.GetOrdinal("PositionX")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionX")),
                        PositionY = rdr.IsDBNull(rdr.GetOrdinal("PositionY")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionY")),
                        SortOrder = rdr.IsDBNull(rdr.GetOrdinal("SortOrder")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("SortOrder")),
                        ForeColor = rdr.IsDBNull(rdr.GetOrdinal("ForeColor")) ? Color.Yellow.ToArgb() : rdr.GetInt32(rdr.GetOrdinal("ForeColor")),
                        BackColor = rdr.IsDBNull(rdr.GetOrdinal("BackColor")) ? Color.Black.ToArgb() : rdr.GetInt32(rdr.GetOrdinal("BackColor")),
                        ShowWarning = rdr.IsDBNull(rdr.GetOrdinal("ShowWarning")) ? 1 : rdr.GetInt32(rdr.GetOrdinal("ShowWarning")),
                        EmptyBehavior = rdr.IsDBNull(rdr.GetOrdinal("EmptyBehavior")) ? "ViewName" : rdr.GetString(rdr.GetOrdinal("EmptyBehavior"))
                    });
                }
            }

            return gridData;
        }

        /// <summary>Deletes a view by ID (string for grid compatibility).</summary>
        static internal void DeleteView(SQLiteConnection con, string ID)
        {
            if (!int.TryParse(ID, out int idValue)) return;
            using (var cmd = new SQLiteCommand("DELETE FROM miniviews WHERE ID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", idValue);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Saves a view from a DataGridView row.  Inserts new rows with
        /// default position (100,100); updates name/active/style/warn/empty.
        /// </summary>
        static internal void SaveView(SQLiteConnection con, DataGridView dataGridView, DataGridViewRow row)
        {
            DataGridViewCell ID = row.Cells[dataGridView.Columns["ID"].Index];
            DataGridViewCell Name = row.Cells[dataGridView.Columns["Name"].Index];
            DataGridViewCell ActiveYn = row.Cells[dataGridView.Columns["ActiveYn"].Index];
            DataGridViewCell StyleFilter = row.Cells[dataGridView.Columns["StyleFilter"].Index];
            DataGridViewCell ShowWarning = row.Cells[dataGridView.Columns["ShowWarning"].Index];
            DataGridViewCell EmptyBehavior = row.Cells[dataGridView.Columns["EmptyBehavior"].Index];

            using (var cmd = new SQLiteCommand(con))
            {
                if (Convert.ToString(ID.Value) == "-1")
                {
                    cmd.CommandText = "INSERT INTO miniviews (Name, ActiveYn, StyleFilter, ShowWarning, EmptyBehavior, PositionX, PositionY, SortOrder) VALUES (@name, @active, @style, @warn, @empty, 100, 100, 0)";
                }
                else
                {
                    cmd.CommandText = "UPDATE miniviews SET Name = @name, ActiveYn = @active, StyleFilter = @style, ShowWarning = @warn, EmptyBehavior = @empty WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(ID.Value));
                }

                cmd.Parameters.AddWithValue("@name", Convert.ToString(Name.Value));
                cmd.Parameters.AddWithValue("@active", Convert.ToInt32(ActiveYn.Value));
                cmd.Parameters.AddWithValue("@style", Convert.ToString(StyleFilter.Value));
                cmd.Parameters.AddWithValue("@warn", Convert.ToInt32(ShowWarning.Value ?? 1));
                cmd.Parameters.AddWithValue("@empty", Convert.ToString(EmptyBehavior.Value ?? "ViewName"));
                cmd.ExecuteNonQuery();

                if (Convert.ToString(ID.Value) == "-1")
                {
                    cmd.CommandText = "SELECT last_insert_rowid()";
                    cmd.Parameters.Clear();
                    row.Cells[dataGridView.Columns["ID"].Index].Value = (long)cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Gets all view definitions from the database, ordered by SortOrder.
        /// Returns a richer DTO including colors and behaviour columns used by
        /// the runtime MiniView windows.
        /// </summary>
        static internal List<ViewPositionData> GetViewPositions(SQLiteConnection con)
        {
            var views = new List<ViewPositionData>();

            using (var cmd = new SQLiteCommand(
                "SELECT ID, Name, PositionX, PositionY, SortOrder, ActiveYn, StyleFilter, ForeColor, BackColor, ShowWarning, EmptyBehavior FROM miniviews ORDER BY SortOrder", con))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    views.Add(new ViewPositionData
                    {
                        ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                        Name = rdr.IsDBNull(rdr.GetOrdinal("Name")) ? "" : rdr.GetString(rdr.GetOrdinal("Name")),
                        PositionX = rdr.IsDBNull(rdr.GetOrdinal("PositionX")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionX")),
                        PositionY = rdr.IsDBNull(rdr.GetOrdinal("PositionY")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionY")),
                        SortOrder = rdr.IsDBNull(rdr.GetOrdinal("SortOrder")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("SortOrder")),
                        ActiveYn = rdr.IsDBNull(rdr.GetOrdinal("ActiveYn")) ? 1 : rdr.GetInt32(rdr.GetOrdinal("ActiveYn")),
                        StyleFilter = rdr.IsDBNull(rdr.GetOrdinal("StyleFilter")) ? "Normal" : rdr.GetString(rdr.GetOrdinal("StyleFilter")),
                        ForeColor = rdr.IsDBNull(rdr.GetOrdinal("ForeColor")) ? Color.Yellow.ToArgb() : rdr.GetInt32(rdr.GetOrdinal("ForeColor")),
                        BackColor = rdr.IsDBNull(rdr.GetOrdinal("BackColor")) ? Color.Black.ToArgb() : rdr.GetInt32(rdr.GetOrdinal("BackColor")),
                        ShowWarning = rdr.IsDBNull(rdr.GetOrdinal("ShowWarning")) ? 1 : rdr.GetInt32(rdr.GetOrdinal("ShowWarning")),
                        EmptyBehavior = rdr.IsDBNull(rdr.GetOrdinal("EmptyBehavior")) ? "ViewName" : rdr.GetString(rdr.GetOrdinal("EmptyBehavior"))
                    });
                }
            }

            return views;
        }

        /// <summary>Persists per-view X/Y positions, keyed by view ID.</summary>
        static internal void SaveViewPositions(SQLiteConnection con, Dictionary<int, Point> positions)
        {
            using (var cmd = new SQLiteCommand(con))
            {
                cmd.CommandText = "UPDATE miniviews SET PositionX = @x, PositionY = @y WHERE ID = @id";
                foreach (var kvp in positions)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@x", kvp.Value.X);
                    cmd.Parameters.AddWithValue("@y", kvp.Value.Y);
                    cmd.Parameters.AddWithValue("@id", kvp.Key);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    /// <summary>
    /// Data class for view position + display configuration.
    /// Used by runtime MiniView windows to render per-view colours / behaviour.
    /// Lives alongside <see cref="ViewsRepository"/> since it's the natural
    /// home for the miniviews table; previously declared inside Database.cs.
    /// </summary>
    class ViewPositionData
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int SortOrder { get; set; }
        public int ActiveYn { get; set; }
        public string StyleFilter { get; set; }
        public int ForeColor { get; set; }      // v0.6.0: Per-view foreground color
        public int BackColor { get; set; }      // v0.6.0: Per-view background color
        public int ShowWarning { get; set; }    // v0.6.0: Per-view warning color control
        public string EmptyBehavior { get; set; }  // v0.6.0: Per-view empty display behavior
    }
}
