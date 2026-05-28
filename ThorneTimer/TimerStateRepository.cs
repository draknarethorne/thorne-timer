using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace ThorneTimer
{
    /// <summary>
    /// Repository owning the <c>timer_runtime_state</c> table â€” per-character
    /// timer counts, remaining time, button state, and "running at save time"
    /// markers used by the offline-adjustment logic on reload.
    ///
    /// Extracted from <see cref="Database"/> in v0.6.0.  The static API matches
    /// the original <c>Database.*</c> signatures so existing FormMain call
    /// sites compile unchanged.  The table is created/migrated in
    /// <see cref="Database.Connection(string)"/>.
    /// </summary>
    class TimerStateRepository
    {
        private readonly SQLiteConnection con;

        public TimerStateRepository(SQLiteConnection con)
        {
            this.con = con;
        }

        // ---------------------------------------------------------------
        // Static API â€” matches the original Database.* signatures.
        // ---------------------------------------------------------------

        /// <summary>
        /// Saves runtime timer state (counts, remaining, button state) for a character.
        /// Wipe-and-replace: deletes all rows for the scopes being saved, then inserts
        /// fresh data.  This eliminates stale rows left behind when a timer's scope
        /// changes (e.g. World â†’ Character â†’ World) or when timers are deleted.
        /// </summary>
        static public void SaveTimerStates(SQLiteConnection con, List<TimerState> states, string characterID)
        {
            if (!Database.isTableExist(con, "timer_runtime_state")) return;

            ThorneLog.Info($"SaveTimerStates called: characterID={characterID}, stateCount={states.Count}");

            using (var txn = con.BeginTransaction())
            {
                SQLiteCommand cmd = new SQLiteCommand(con);

                // Clear all World-scope rows (CharacterID IS NULL)
                cmd.CommandText = "DELETE FROM timer_runtime_state WHERE CharacterID IS NULL";
                int worldDeleted = cmd.ExecuteNonQuery();
                ThorneLog.Debug($"  Bulk-delete: {worldDeleted} World (NULL) rows removed");

                // Clear all rows for this character
                if (!string.IsNullOrEmpty(characterID))
                {
                    cmd.CommandText = "DELETE FROM timer_runtime_state WHERE CharacterID = @delCharID";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@delCharID", characterID);
                    int charDeleted = cmd.ExecuteNonQuery();
                    ThorneLog.Debug($"  Bulk-delete: {charDeleted} Character (charID={characterID}) rows removed");
                }

                // Insert fresh rows for every timer
                foreach (var ts in states)
                {
                    // World timers are global â€” always store with NULL CharacterID
                    // so they load regardless of which character is active.
                    // Character / Character+ timers store per-character.
                    string effectiveCharID = (ts.Scope == "World") ? null : characterID;

                    // Log every timer that has non-default state (running, remaining, non-zero count)
                    if (ts.IsRunning || !string.IsNullOrEmpty(ts.Remaining) || ts.ButtonState != Timers.btnStart || ts.Count > 0)
                        ThorneLog.Debug($"  SAVE TID={ts.TimerID} \"{ts.Name}\" Scope={ts.Scope} effCID={effectiveCharID ?? "NULL"} Btn={ts.ButtonState} Rem={ts.Remaining} Act={ts.ActiveYn} IsRunning={ts.IsRunning} Count={ts.Count}");

                    cmd.CommandText = "INSERT INTO timer_runtime_state (TimerID, CharacterID, Remaining, ButtonState, Count, SavedAtUtc, ActiveYn) VALUES (@timerID, @charID, @remaining, @btnState, @count, @savedAtUtc, @activeYn)";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@timerID", ts.TimerID);
                    cmd.Parameters.AddWithValue("@charID", string.IsNullOrEmpty(effectiveCharID) ? (object)DBNull.Value : (object)effectiveCharID);
                    cmd.Parameters.AddWithValue("@remaining", ts.Remaining ?? "");
                    cmd.Parameters.AddWithValue("@btnState", ts.ButtonState ?? Timers.btnStart);
                    cmd.Parameters.AddWithValue("@count", ts.Count);

                    // Only persist SavedAtUtc for scopes that use offline adjustment
                    // (Character+ and World).  Character-scope timers pause on switch
                    // and resume with their saved remaining â€” no offline adjustment.
                    bool needsSavedAtUtc = ts.IsRunning
                        && (ts.Scope == "Character+" || ts.Scope == "World");
                    cmd.Parameters.AddWithValue("@savedAtUtc",
                        needsSavedAtUtc ? (object)DateTime.UtcNow.ToString("o") : (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@activeYn", ts.ActiveYn);
                    cmd.ExecuteNonQuery();
                }

                txn.Commit();
            }
        }

        /// <summary>
        /// Persists a single timer's runtime state for the given character.
        /// Used to immediately record state changes (e.g. user stops a timer)
        /// without waiting for a full save cycle.
        /// </summary>
        static public void SaveSingleTimerState(SQLiteConnection con, TimerState ts, string characterID)
        {
            if (!Database.isTableExist(con, "timer_runtime_state")) return;

            // World timers are global â€” always store with NULL CharacterID
            string effectiveCharID = (ts.Scope == "World") ? null : characterID;

            ThorneLog.Debug($"SaveSingleTimerState TID={ts.TimerID} \"{ts.Name}\" Scope={ts.Scope} charID={characterID} effCID={effectiveCharID ?? "NULL"} Btn={ts.ButtonState} Rem={ts.Remaining} Act={ts.ActiveYn} IsRunning={ts.IsRunning} Count={ts.Count}");

            SQLiteCommand cmd = new SQLiteCommand(con);

            // Always delete both scope variants for this TimerID to prevent
            // stale rows when a timer's scope changes (e.g. World â†’ Character).
            cmd.CommandText = "DELETE FROM timer_runtime_state WHERE TimerID = @delTID AND CharacterID IS NULL";
            cmd.Parameters.AddWithValue("@delTID", ts.TimerID);
            cmd.ExecuteNonQuery();

            if (!string.IsNullOrEmpty(characterID))
            {
                cmd.CommandText = "DELETE FROM timer_runtime_state WHERE TimerID = @delTID2 AND CharacterID = @delCharID";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@delTID2", ts.TimerID);
                cmd.Parameters.AddWithValue("@delCharID", characterID);
                cmd.ExecuteNonQuery();
            }

            cmd.CommandText = "INSERT INTO timer_runtime_state (TimerID, CharacterID, Remaining, ButtonState, Count, SavedAtUtc, ActiveYn) VALUES (@timerID, @charID, @remaining, @btnState, @count, @savedAtUtc, @activeYn)";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@timerID", ts.TimerID);
            cmd.Parameters.AddWithValue("@charID", string.IsNullOrEmpty(effectiveCharID) ? (object)DBNull.Value : (object)effectiveCharID);
            cmd.Parameters.AddWithValue("@remaining", ts.Remaining ?? "");
            cmd.Parameters.AddWithValue("@btnState", ts.ButtonState ?? Timers.btnStart);
            cmd.Parameters.AddWithValue("@count", ts.Count);

            bool needsSavedAtUtc = ts.IsRunning
                && (ts.Scope == "Character+" || ts.Scope == "World");
            cmd.Parameters.AddWithValue("@savedAtUtc",
                needsSavedAtUtc ? (object)DateTime.UtcNow.ToString("o") : (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@activeYn", ts.ActiveYn);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Loads saved timer runtime state for a character.  World rows load
        /// first; per-character rows then overwrite on TimerID conflict so the
        /// character-specific state always wins.
        /// </summary>
        static public Dictionary<long, TimerState> LoadTimerStates(SQLiteConnection con, string characterID)
        {
            var result = new Dictionary<long, TimerState>();
            if (!Database.isTableExist(con, "timer_runtime_state")) return result;

            var cmd = new SQLiteCommand(con);

            string worldQuery = "SELECT * FROM timer_runtime_state WHERE CharacterID IS NULL";
            string charQuery = string.IsNullOrEmpty(characterID)
                ? null
                : "SELECT * FROM timer_runtime_state WHERE CharacterID = @charID";

            // Pass 1: World rows
            cmd.CommandText = worldQuery;
            ReadTimerStateRows(cmd, result);

            // Pass 2: Character-specific rows (overwrite on conflict)
            if (charQuery != null)
            {
                cmd.CommandText = charQuery;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@charID", characterID);
                ReadTimerStateRows(cmd, result);
            }

            ThorneLog.DumpSavedStates($"LoadTimerStates charID={characterID}", result);

            return result;
        }

        /// <summary>
        /// Reads timer_runtime_state rows from the given command into the dictionary.
        /// Overwrites existing entries on TimerID conflict.
        /// </summary>
        static private void ReadTimerStateRows(SQLiteCommand cmd, Dictionary<long, TimerState> result)
        {
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    long timerID = rdr.GetInt64(rdr.GetOrdinal("TimerID"));
                    int activeOrdinal = -1;
                    try { activeOrdinal = rdr.GetOrdinal("ActiveYn"); } catch { }
                    var ts = new TimerState
                    {
                        TimerID = timerID,
                        Remaining = rdr.IsDBNull(rdr.GetOrdinal("Remaining")) ? "" : rdr.GetString(rdr.GetOrdinal("Remaining")),
                        ButtonState = rdr.IsDBNull(rdr.GetOrdinal("ButtonState")) ? Timers.btnStart : rdr.GetString(rdr.GetOrdinal("ButtonState")),
                        Count = rdr.IsDBNull(rdr.GetOrdinal("Count")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("Count")),
                        ActiveYn = activeOrdinal >= 0 && !rdr.IsDBNull(activeOrdinal) ? rdr.GetInt64(activeOrdinal) : 1
                    };

                    int savedAtUtcOrdinal = -1;
                    try { savedAtUtcOrdinal = rdr.GetOrdinal("SavedAtUtc"); } catch { }
                    if (savedAtUtcOrdinal >= 0 && !rdr.IsDBNull(savedAtUtcOrdinal))
                    {
                        DateTime parsed;
                        if (DateTime.TryParse(rdr.GetString(savedAtUtcOrdinal), null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out parsed))
                        {
                            ts.SavedAtUtc = parsed.ToUniversalTime();
                        }
                    }

                    result[timerID] = ts;
                }
            }
        }

        /// <summary>
        /// Clears saved timer state for a character (or all if characterID is null).
        /// </summary>
        static public void ClearTimerStates(SQLiteConnection con, string characterID)
        {
            if (!Database.isTableExist(con, "timer_runtime_state")) return;

            var cmd = new SQLiteCommand(con);

            if (string.IsNullOrEmpty(characterID))
            {
                cmd.CommandText = "DELETE FROM timer_runtime_state";
            }
            else
            {
                cmd.CommandText = "DELETE FROM timer_runtime_state WHERE CharacterID = @charID";
                cmd.Parameters.AddWithValue("@charID", characterID);
            }

            cmd.ExecuteNonQuery();
        }
    }
}
