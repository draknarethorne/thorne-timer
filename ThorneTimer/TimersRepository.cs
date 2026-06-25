using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Repository owning the <c>timers</c> table CRUD.
    ///
    /// Extracted from <see cref="Database"/> in v0.6.0.  Runtime state for
    /// timers (counts, remaining time, button state per character) lives in
    /// <see cref="TimerStateRepository"/> against the <c>timer_runtime_state</c>
    /// table.  Schema migrations remain in <see cref="Database.Connection(string)"/>.
    /// </summary>
    class TimersRepository
    {
        private readonly SQLiteConnection con;

        public TimersRepository(SQLiteConnection con)
        {
            this.con = con;
        }

        /// <summary>
        /// The connection this repository was constructed with.  Exposed so
        /// <see cref="TimersController"/> can drive the static CRUD helpers
        /// (<see cref="GetTimers"/>, <see cref="SaveTimer"/>,
        /// <see cref="DeleteTimer"/>) against the same database.
        /// </summary>
        public SQLiteConnection Con => con;

        // ---------------------------------------------------------------
        // Static API â€” matches the original Database.* signatures used by
        // FormMain so existing call sites compile unchanged.
        // ---------------------------------------------------------------

        /// <summary>
        /// Loads all timers as a <see cref="SortableBindingList{T}"/> for the
        /// Timers grid.  Tolerant of older schemas: missing Scope / DependsOn /
        /// ClassID columns degrade gracefully to defaults.
        /// </summary>
        static public SortableBindingList<Timers.GridData> GetTimers(SQLiteConnection con)
        {
            var gridData = new SortableBindingList<Timers.GridData>();

            // Suppress per-item ListChanged events during bulk load
            gridData.RaiseListChangedEvents = false;

            bool hasScope = Database.isFieldExist(con, "timers", "Scope");
            bool hasDependsOn = Database.isFieldExist(con, "timers", "DependsOnTimer");
            bool hasClassID = Database.isFieldExist(con, "timers", "ClassID");
            int scopeOrdinal = -1;

            using (var cmd = new SQLiteCommand("SELECT * from timers", con))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    try
                    {
                        if (hasScope && scopeOrdinal < 0)
                            scopeOrdinal = rdr.GetOrdinal("Scope");

                        var data = new Timers.GridData
                        {
                            ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                            Name = rdr.GetString(rdr.GetOrdinal("Name")),
                            CategoryID = rdr.GetInt32(rdr.GetOrdinal("CategoryID")),
                            StartKeyword = rdr.GetString(rdr.GetOrdinal("StartKeyword")),
                            EndKeyword = rdr.GetString(rdr.GetOrdinal("EndKeyword")),
                            WAVFile = rdr.GetString(rdr.GetOrdinal("WAVFile")),
                            Speech = rdr.GetString(rdr.GetOrdinal("Speech")),
                            Duration = rdr.GetString(rdr.GetOrdinal("Duration")),
                            ActiveYn = rdr.GetInt32(rdr.GetOrdinal("ActiveYn")),
                            CaseYn = rdr.GetInt32(rdr.GetOrdinal("CaseYn")),
                            EndlessYn = rdr.GetInt32(rdr.GetOrdinal("EndlessYn")),
                            Style = rdr.IsDBNull(rdr.GetOrdinal("Style")) ? "Normal" : rdr.GetString(rdr.GetOrdinal("Style")),
                            Scope = (hasScope && scopeOrdinal >= 0 && !rdr.IsDBNull(scopeOrdinal)) ? rdr.GetString(scopeOrdinal) : "World",
                            DependsOnTimer = hasDependsOn && !rdr.IsDBNull(rdr.GetOrdinal("DependsOnTimer")) ? rdr.GetString(rdr.GetOrdinal("DependsOnTimer")) : "",
                            DependsOnDelay = hasDependsOn && !rdr.IsDBNull(rdr.GetOrdinal("DependsOnDelay")) ? rdr.GetInt32(rdr.GetOrdinal("DependsOnDelay")) : 0,
                            ClassID = hasClassID && !rdr.IsDBNull(rdr.GetOrdinal("ClassID")) ? rdr.GetInt64(rdr.GetOrdinal("ClassID")) : 0,
                            Remaining = ""
                        };

                        gridData.Add(data);
                    }
                    catch (Exception)
                    {
                        // Schema might be partial; skip malformed row
                        continue;
                    }
                }
            }

            // Re-enable events and notify that the list is ready
            gridData.RaiseListChangedEvents = true;
            gridData.ResetBindings();

            return gridData;
        }

        /// <summary>
        /// Saves a timer from a DataGridView row.  For Character / Character+
        /// scoped timers, ActiveYn is forced to 0 in the global table â€” per-
        /// character active state lives in <see cref="TimerStateRepository"/>.
        /// </summary>
        static public void SaveTimer(SQLiteConnection con, DataGridView dataGridView, DataGridViewRow row)
        {
            DataGridViewCell ID = row.Cells[dataGridView.Columns["ID"].Index];
            DataGridViewCell Name = row.Cells[dataGridView.Columns["Name"].Index];
            DataGridViewCell CategoryID = row.Cells[dataGridView.Columns["CategoryID"].Index];
            DataGridViewCell StartKeyword = row.Cells[dataGridView.Columns["StartKeyword"].Index];
            DataGridViewCell EndKeyword = row.Cells[dataGridView.Columns["EndKeyword"].Index];
            DataGridViewCell WAVFile = row.Cells[dataGridView.Columns["WAVFile"].Index];
            DataGridViewCell Speech = row.Cells[dataGridView.Columns["Speech"].Index];
            DataGridViewCell Duration = row.Cells[dataGridView.Columns["Duration"].Index];
            DataGridViewCell ActiveYn = row.Cells[dataGridView.Columns["ActiveYn"].Index];
            DataGridViewCell CaseYn = row.Cells[dataGridView.Columns["CaseYn"].Index];
            DataGridViewCell EndlessYn = row.Cells[dataGridView.Columns["EndlessYn"].Index];
            DataGridViewCell Style = row.Cells[dataGridView.Columns["Style"].Index];
            DataGridViewCell Scope = row.Cells[dataGridView.Columns["Scope"].Index];
            DataGridViewCell DependsOnTimer = row.Cells[dataGridView.Columns["DependsOnTimer"].Index];
            DataGridViewCell DependsOnDelay = row.Cells[dataGridView.Columns["DependsOnDelay"].Index];
            DataGridViewCell ClassIDCell = row.Cells[dataGridView.Columns["ClassID"].Index];

            using (var cmd = new SQLiteCommand(con))
            {
                if (Convert.ToString(ID.Value) == "-1")
                {
                    cmd.CommandText = "INSERT INTO timers (Name, CategoryID, StartKeyword, EndKeyword, WAVFile, Speech, Duration, ActiveYn, CaseYn, EndlessYn, Style, Scope, DependsOnTimer, DependsOnDelay, ClassID) VALUES (@name, @categoryID, @startKeyword, @endKeyword, @wavFile, @speech, @duration, @activeYn, @caseYn, @endlessYn, @style, @scope, @dependsOnTimer, @dependsOnDelay, @classID)";
                }
                else
                {
                    cmd.CommandText = "UPDATE timers SET Name = @name, CategoryID = @categoryID, StartKeyword = @startKeyword, EndKeyword = @endKeyword, WAVFile = @wavFile, Speech = @speech, Duration = @duration, ActiveYn = @activeYn, CaseYn = @caseYn, EndlessYn = @endlessYn, Style = @style, Scope = @scope, DependsOnTimer = @dependsOnTimer, DependsOnDelay = @dependsOnDelay, ClassID = @classID WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(ID.Value));
                }

                cmd.Parameters.AddWithValue("@name", Convert.ToString(Name.Value));
                cmd.Parameters.AddWithValue("@categoryID", Convert.ToInt32(CategoryID.Value));
                cmd.Parameters.AddWithValue("@startKeyword", Convert.ToString(StartKeyword.Value));
                cmd.Parameters.AddWithValue("@endKeyword", Convert.ToString(EndKeyword.Value));
                cmd.Parameters.AddWithValue("@wavFile", Convert.ToString(WAVFile.Value));
                cmd.Parameters.AddWithValue("@speech", Convert.ToString(Speech.Value));
                cmd.Parameters.AddWithValue("@duration", Convert.ToString(Duration.Value));

                // For Character / Character+ scopes, ActiveYn is per-character and
                // stored in timer_runtime_state.  Always write 0 to the global timers
                // table so it doesn't bleed to other characters on reload.
                // World-scope ActiveYn is global and written as-is.
                string scopeValue = Convert.ToString(Scope.Value);
                int globalActiveYn = (scopeValue == "Character" || scopeValue == "Character+")
                    ? 0
                    : Convert.ToInt32(ActiveYn.Value);
                cmd.Parameters.AddWithValue("@activeYn", globalActiveYn);
                cmd.Parameters.AddWithValue("@caseYn", Convert.ToInt32(CaseYn.Value));
                cmd.Parameters.AddWithValue("@endlessYn", Convert.ToInt32(EndlessYn.Value));
                cmd.Parameters.AddWithValue("@style", Convert.ToString(Style.Value));
                cmd.Parameters.AddWithValue("@scope", Convert.ToString(Scope.Value));
                cmd.Parameters.AddWithValue("@dependsOnTimer", Convert.ToString(DependsOnTimer.Value));
                cmd.Parameters.AddWithValue("@dependsOnDelay", Convert.ToInt32(DependsOnDelay.Value));
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

        /// <summary>Deletes a timer by ID (string for grid compatibility).</summary>
        static public void DeleteTimer(SQLiteConnection con, string ID)
        {
            if (!int.TryParse(ID, out int idValue)) return;
            using (var cmd = new SQLiteCommand("DELETE FROM timers WHERE ID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", idValue);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
