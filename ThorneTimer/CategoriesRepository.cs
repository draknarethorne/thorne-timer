using System;
using System.Collections.Generic;
using System.Data.SQLite;

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
            var categories = new List<Categories.GridData>();

            using (var cmd = new SQLiteCommand(con))
            {
                cmd.CommandText = "SELECT ID, Name, StartKeyword, EndKeyword, AutoStop FROM categories ORDER BY Name";
                using (SQLiteDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        categories.Add(new Categories.GridData
                        {
                            ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                            Name = rdr.IsDBNull(rdr.GetOrdinal("Name")) ? "" : rdr.GetString(rdr.GetOrdinal("Name")),
                            StartKeyword = rdr.IsDBNull(rdr.GetOrdinal("StartKeyword")) ? "" : rdr.GetString(rdr.GetOrdinal("StartKeyword")),
                            EndKeyword = rdr.IsDBNull(rdr.GetOrdinal("EndKeyword")) ? "" : rdr.GetString(rdr.GetOrdinal("EndKeyword")),
                            AutoStop = rdr.IsDBNull(rdr.GetOrdinal("AutoStop")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("AutoStop"))
                        });
                    }
                }
            }

            return categories;
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
            using (var cmd = new SQLiteCommand(con))
            {
                cmd.CommandText = "DELETE FROM categories WHERE ID = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
