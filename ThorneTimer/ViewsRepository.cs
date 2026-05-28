using System;
using System.Collections.Generic;
using System.Data.SQLite;

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
    }
}
