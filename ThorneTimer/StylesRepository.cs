using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;

namespace ThorneTimer
{
    public class StylesRepository
    {
        private readonly SQLiteConnection con;
        private Dictionary<string, StyleData> styleCache;

        public StylesRepository(SQLiteConnection con)
        {
            this.con = con;
        }

        public static void EnsureSchema(SQLiteConnection con)
        {
            if (!Database.isTableExist(con, "styles"))
            {
                using (var cmd = new SQLiteCommand(con))
                {
                    cmd.CommandText = "CREATE TABLE styles(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE, ForeColor INTEGER NOT NULL, BackColor INTEGER NOT NULL, SortOrder INTEGER DEFAULT 0)";
                    cmd.ExecuteNonQuery();
                }

                // Brand-new styles table (clean v0.5.0 → v0.6.0 upgrade or fresh DB).
                // Seed defaults and migrate any user-customized Normal/Buff/Ping colors
                // forward from the legacy miniviews table. After this initial pass, we
                // never touch existing rows again — deletions and edits stick.
                SeedDefaultStyles(con);
                MigrateUserColorsFromLegacyViews(con);
            }
        }

        public static void SeedDefaultStyles(SQLiteConnection con)
        {
            EnsureStyle(con, "Normal", 1, Color.Yellow.ToArgb(), Color.Black.ToArgb());
            EnsureStyle(con, "Buff", 2, Color.Orange.ToArgb(), Color.Black.ToArgb());
            EnsureStyle(con, "Pet", 3, Color.FromArgb(220, 160, 255).ToArgb(), Color.Black.ToArgb());
            EnsureStyle(con, "Ping", 4, Color.LightGreen.ToArgb(), Color.Black.ToArgb());
            EnsureStyle(con, "Spawn", 5, Color.Cyan.ToArgb(), Color.Black.ToArgb());
            EnsureStyle(con, "Lockout", 6, Color.DodgerBlue.ToArgb(), Color.Black.ToArgb());
            EnsureStyle(con, "Character", 7, Color.White.ToArgb(), Color.Black.ToArgb());
        }

        public List<StyleData> GetStyles()
        {
            var styles = new List<StyleData>();

            using (var cmd = new SQLiteCommand(con))
            {
                cmd.CommandText = "SELECT ID, Name, ForeColor, BackColor, SortOrder FROM styles ORDER BY SortOrder, Name";
                using (SQLiteDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        styles.Add(new StyleData
                        {
                            ID = rdr.GetInt64(rdr.GetOrdinal("ID")),
                            Name = rdr.IsDBNull(rdr.GetOrdinal("Name")) ? "" : rdr.GetString(rdr.GetOrdinal("Name")),
                            ForeColor = rdr.IsDBNull(rdr.GetOrdinal("ForeColor")) ? Color.Yellow.ToArgb() : rdr.GetInt32(rdr.GetOrdinal("ForeColor")),
                            BackColor = rdr.IsDBNull(rdr.GetOrdinal("BackColor")) ? Color.Black.ToArgb() : rdr.GetInt32(rdr.GetOrdinal("BackColor")),
                            SortOrder = rdr.IsDBNull(rdr.GetOrdinal("SortOrder")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("SortOrder"))
                        });
                    }
                }
            }

            return styles;
        }

        public StyleData GetStyle(string name)
        {
            EnsureCache();

            if (string.IsNullOrWhiteSpace(name))
                name = "Normal";

            StyleData style;
            if (styleCache.TryGetValue(name, out style))
                return style;

            if (styleCache.TryGetValue("Normal", out style))
                return style;

            return new StyleData
            {
                Name = "Normal",
                ForeColor = Color.Yellow.ToArgb(),
                BackColor = Color.Black.ToArgb(),
                SortOrder = 1
            };
        }

        public Color GetRowBaseColor(string name)
        {
            return GetStyle(name).ForeColorValue;
        }

        public List<string> GetStyleNames()
        {
            var names = new List<string>();
            foreach (StyleData style in GetStyles())
            {
                if (!string.IsNullOrWhiteSpace(style.Name))
                    names.Add(style.Name);
            }
            return names;
        }

        public StyleData CreateDefaultStyle()
        {
            string name = GetUniqueStyleName("New Style");
            var style = new StyleData
            {
                ID = -1,
                Name = name,
                ForeColor = Color.Yellow.ToArgb(),
                BackColor = Color.Black.ToArgb(),
                SortOrder = GetNextSortOrder()
            };

            SaveStyle(style);
            return GetStyle(name);
        }

        public void DeleteStyle(long id)
        {
            StyleData style = GetStyles().Find(s => s.ID == id);
            if (style == null) return;

            using (var tx = con.BeginTransaction())
            using (var cmd = new SQLiteCommand(con))
            {
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE timers SET Style = 'Normal' WHERE Style = @name";
                cmd.Parameters.AddWithValue("@name", style.Name);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE miniviews SET StyleFilter = 'Normal' WHERE StyleFilter = @name";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM styles WHERE ID = @id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                tx.Commit();
            }

            styleCache = null;
        }

        public void SaveStyle(StyleData style)
        {
            if (style == null || string.IsNullOrWhiteSpace(style.Name)) return;

            using (var cmd = new SQLiteCommand(con))
            {
                if (style.ID <= 0)
                {
                    cmd.CommandText = "INSERT INTO styles (Name, ForeColor, BackColor, SortOrder) VALUES (@name, @fore, @back, @sort)";
                }
                else
                {
                    cmd.CommandText = "UPDATE styles SET Name = @name, ForeColor = @fore, BackColor = @back, SortOrder = @sort WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", style.ID);
                }

                cmd.Parameters.AddWithValue("@name", style.Name.Trim());
                cmd.Parameters.AddWithValue("@fore", style.ForeColor);
                cmd.Parameters.AddWithValue("@back", style.BackColor);
                cmd.Parameters.AddWithValue("@sort", style.SortOrder);
                cmd.ExecuteNonQuery();
            }

            styleCache = null;
        }

        private int GetNextSortOrder()
        {
            using (var cmd = new SQLiteCommand(con))
            {
                cmd.CommandText = "SELECT COALESCE(MAX(SortOrder), 0) + 1 FROM styles";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private string GetUniqueStyleName(string baseName)
        {
            var existing = new HashSet<string>(GetStyleNames(), StringComparer.OrdinalIgnoreCase);
            if (!existing.Contains(baseName)) return baseName;

            int suffix = 2;
            string candidate;
            do
            {
                candidate = baseName + " " + suffix;
                suffix++;
            } while (existing.Contains(candidate));

            return candidate;
        }

        public void RefreshCache()
        {
            styleCache = null;
            EnsureCache();
        }

        private void EnsureCache()
        {
            if (styleCache != null) return;

            styleCache = new Dictionary<string, StyleData>(StringComparer.OrdinalIgnoreCase);
            foreach (StyleData style in GetStyles())
            {
                if (!string.IsNullOrWhiteSpace(style.Name))
                    styleCache[style.Name] = style;
            }
        }

        private static void EnsureStyle(SQLiteConnection con, string name, int sortOrder, int foreColor, int backColor)
        {
            using (var cmd = new SQLiteCommand(con))
            {
                cmd.CommandText = "SELECT COUNT(*) FROM styles WHERE Name = @name";
                cmd.Parameters.AddWithValue("@name", name);
                long count = (long)cmd.ExecuteScalar();
                if (count > 0) return;

                cmd.CommandText = "INSERT INTO styles (Name, ForeColor, BackColor, SortOrder) VALUES (@name, @fore, @back, @sort)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@fore", foreColor);
                cmd.Parameters.AddWithValue("@back", backColor);
                cmd.Parameters.AddWithValue("@sort", sortOrder);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateStyleColors(SQLiteConnection con, string name, int foreColor, int backColor)
        {
            using (var cmd = new SQLiteCommand(con))
            {
                cmd.CommandText = "UPDATE styles SET ForeColor = @fore, BackColor = @back WHERE Name = @name";
                cmd.Parameters.AddWithValue("@fore", foreColor);
                cmd.Parameters.AddWithValue("@back", backColor);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// One-shot v0.5.0 → v0.6.0 migration: carries the user's previously chosen
        /// Normal/Buff/Ping colors out of the legacy settings table into the new
        /// styles table. Pet/Spawn/Lockout/Character had no user-pickable color in
        /// v0.5.0, so we leave their seeded defaults alone.
        /// Called exactly once, immediately after the styles table is first created.
        /// </summary>
        private static void MigrateUserColorsFromLegacyViews(SQLiteConnection con)
        {
            CopyLegacyColor(con, "Normal", "MiniViewNormFore", "MiniViewNormBack");
            CopyLegacyColor(con, "Buff", "MiniViewBuffFore", "MiniViewBuffBack");
            CopyLegacyColor(con, "Ping", "MiniViewPingFore", "MiniViewPingBack");
        }

        private static void CopyLegacyColor(SQLiteConnection con, string styleName, string foreCol, string backCol)
        {
            if (!Database.isFieldExist(con, "settings", foreCol) || !Database.isFieldExist(con, "settings", backCol))
                return;

            using (var cmd = new SQLiteCommand(con))
            {
                cmd.CommandText = "SELECT " + foreCol + ", " + backCol + " FROM settings WHERE ID = 1";
                using (SQLiteDataReader rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read()) return;
                    if (rdr.IsDBNull(0) || rdr.IsDBNull(1)) return;

                    int fore = rdr.GetInt32(0);
                    int back = rdr.GetInt32(1);
                    rdr.Close();

                    UpdateStyleColors(con, styleName, fore, back);
                }
            }
        }
    }
}
