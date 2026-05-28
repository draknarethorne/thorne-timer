using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Repository owning the <c>characters</c> table CRUD, plus the
    /// read-only <c>classes</c> lookup helper used to populate class
    /// combo boxes for both characters and timers.
    ///
    /// Extracted from <see cref="Database"/> in v0.6.0.  The static API
    /// matches the original <c>Database.*</c> signatures so call sites in
    /// FormMain / MiniViews can be updated with a simple find/replace.
    /// </summary>
    class CharactersRepository
    {
        private readonly SQLiteConnection con;

        public CharactersRepository(SQLiteConnection con)
        {
            this.con = con;
        }

        /// <summary>Underlying connection (exposed for the matching controller).</summary>
        internal SQLiteConnection Con => con;

        // ---------------------------------------------------------------
        // Static API â€” moved from Database.cs as part of the v0.6.0
        // repository extraction.
        // ---------------------------------------------------------------

        /// <summary>Loads a single character by ID (string for grid compatibility).</summary>
        static public Characters.GridData GetCharacter(SQLiteConnection con, string ID)
        {
            var data = new Characters.GridData();

            if (!int.TryParse(ID, out int idValue))
            {
                // Invalid ID supplied; return empty data
                return data;
            }

            using (var cmd = new SQLiteCommand("SELECT * from characters WHERE ID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", idValue);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        data.ID = rdr.GetInt32(rdr.GetOrdinal("ID"));
                        data.Name = rdr.GetString(rdr.GetOrdinal("Name"));
                        data.LogFile = rdr.GetString(rdr.GetOrdinal("LogFile"));
                        data.MiniViewX = rdr.GetInt32(rdr.GetOrdinal("MiniViewX"));
                        data.MiniViewY = rdr.GetInt32(rdr.GetOrdinal("MiniViewY"));
                        int classOrdinal = -1;
                        try { classOrdinal = rdr.GetOrdinal("ClassID"); } catch { }
                        data.ClassID = classOrdinal >= 0 && !rdr.IsDBNull(classOrdinal) ? rdr.GetInt64(classOrdinal) : 0;
                    }
                }
            }

            return data;
        }

        /// <summary>Returns combo-box items for the active-character picker.</summary>
        static public List<ComboBoxItem> GetActiveCharacters(SQLiteConnection con)
        {
            var cboData = new List<ComboBoxItem>();

            using (var cmd = new SQLiteCommand("SELECT * from characters ORDER BY Name", con))
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

        /// <summary>Returns all characters ordered by Name (Characters tab grid + dump helpers).</summary>
        static public List<Characters.GridData> GetCharacters(SQLiteConnection con)
        {
            var gridData = new List<Characters.GridData>();

            using (var cmd = new SQLiteCommand("SELECT * from characters ORDER BY Name", con))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    var data = new Characters.GridData
                    {
                        ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                        Name = rdr.GetString(rdr.GetOrdinal("Name")),
                        LogFile = rdr.GetString(rdr.GetOrdinal("LogFile")),
                        MiniViewX = rdr.GetInt32(rdr.GetOrdinal("MiniViewX")),
                        MiniViewY = rdr.GetInt32(rdr.GetOrdinal("MiniViewY"))
                    };
                    int classOrdinal = -1;
                    try { classOrdinal = rdr.GetOrdinal("ClassID"); } catch { }
                    data.ClassID = classOrdinal >= 0 && !rdr.IsDBNull(classOrdinal) ? rdr.GetInt64(classOrdinal) : 0;

                    gridData.Add(data);
                }
            }

            return gridData;
        }

        /// <summary>
        /// Deletes a character and any persisted timer runtime state belonging to them.
        /// Logs both operations via <see cref="ThorneLog"/>.
        /// </summary>
        static public void DeleteCharacter(SQLiteConnection con, string ID)
        {
            if (!int.TryParse(ID, out int idValue)) return;

            // Delete persisted timer runtime state for this character
            using (var cmdState = new SQLiteCommand(
                "DELETE FROM timer_runtime_state WHERE CharacterID = @id", con))
            {
                cmdState.Parameters.AddWithValue("@id", idValue);
                int stateRows = cmdState.ExecuteNonQuery();
                ThorneLog.Info($"DeleteCharacter ID={idValue}: removed {stateRows} timer_runtime_state row(s)");
            }

            // Delete the character record
            using (var cmd = new SQLiteCommand("DELETE FROM characters WHERE ID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", idValue);
                cmd.ExecuteNonQuery();
            }
            ThorneLog.Info($"DeleteCharacter ID={idValue}: character deleted");
        }

        /// <summary>Saves a character from a DataGridView row.</summary>
        static public void SaveCharacter(SQLiteConnection con, DataGridView dataGridView, DataGridViewRow row)
        {
            DataGridViewCell ID = row.Cells[dataGridView.Columns["ID"].Index];
            DataGridViewCell Name = row.Cells[dataGridView.Columns["Name"].Index];
            DataGridViewCell LogFile = row.Cells[dataGridView.Columns["LogFile"].Index];
            DataGridViewCell MiniViewX = row.Cells[dataGridView.Columns["MiniViewX"].Index];
            DataGridViewCell MiniViewY = row.Cells[dataGridView.Columns["MiniViewY"].Index];
            DataGridViewCell ClassIDCell = row.Cells[dataGridView.Columns["ClassID"].Index];

            using (var cmd = new SQLiteCommand(con))
            {
                if (Convert.ToString(ID.Value) == "-1")
                {
                    cmd.CommandText = "INSERT INTO characters (Name, LogFile, MiniViewX, MiniViewY, ClassID) VALUES (@name, @logFile, @miniViewX, @miniViewY, @classID)";
                }
                else
                {
                    cmd.CommandText = "UPDATE characters SET Name = @name, LogFile = @logFile, MiniViewX = @miniViewX, MiniViewY = @miniViewY, ClassID = @classID WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(ID.Value));
                }

                cmd.Parameters.AddWithValue("@name", Convert.ToString(Name.Value));
                cmd.Parameters.AddWithValue("@logFile", Convert.ToString(LogFile.Value));
                cmd.Parameters.AddWithValue("@miniViewX", Convert.ToInt32(MiniViewX.Value));
                cmd.Parameters.AddWithValue("@miniViewY", Convert.ToInt32(MiniViewY.Value));
                cmd.Parameters.AddWithValue("@classID",
                    ClassIDCell.Value == null || ClassIDCell.Value == DBNull.Value || Convert.ToInt64(ClassIDCell.Value) == 0
                        ? (object)DBNull.Value
                        : Convert.ToInt64(ClassIDCell.Value));
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

        /// <summary>
        /// Returns combo-box items for EQ class selection.  Now lives in
        /// <see cref="ClassesRepository.GetGridClasses(SQLiteConnection)"/>;
        /// kept here as a thin pass-through for backward compatibility so
        /// older call sites in FormMain continue to compile.
        /// </summary>
        [Obsolete("Use ClassesRepository.GetGridClasses instead.")]
        static internal List<ComboBoxItem> GetGridClasses(SQLiteConnection con)
        {
            return ClassesRepository.GetGridClasses(con);
        }
    }
}
