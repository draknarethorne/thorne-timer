using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace ThorneTimer
{
    /// <summary>
    /// Read-only aggregation repository that gathers summary statistics about
    /// the current Tome database for the "Tome Information" dialog (and any
    /// future stats/reporting surfaces).
    ///
    /// Unlike <see cref="StylesRepository"/> / <see cref="ViewsRepository"/> /
    /// <see cref="CategoriesRepository"/> this class does not own a single
    /// table — it reads across <c>timers</c>, <c>characters</c>, <c>categories</c>,
    /// <c>styles</c>, <c>miniviews</c>, <c>classes</c>, <c>timer_runtime_state</c>,
    /// and <c>db_meta</c>.  It still follows the repository naming convention so
    /// it's easy to find and so adding a new statistic only touches one file.
    /// </summary>
    static class TomeStatisticsRepository
    {
        /// <summary>
        /// Builds a fresh <see cref="TomeStatistics"/> snapshot.  Every individual
        /// query is wrapped in a try/catch via <see cref="SafeCount"/> /
        /// <see cref="PopulateBreakdown"/> so a missing table or column never
        /// crashes the caller — older tomes simply report 0 / empty for those
        /// fields.
        /// </summary>
        static public TomeStatistics Get(SQLiteConnection con)
        {
            var stats = new TomeStatistics();

            // ---- Catalog counts ----
            stats.TimerCount        = SafeCount(con, "SELECT COUNT(*) FROM timers");
            stats.ActiveTimerCount  = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE ActiveYn = 1");
            stats.CharacterCount    = SafeCount(con, "SELECT COUNT(*) FROM characters");
            stats.CategoryCount     = SafeCount(con, "SELECT COUNT(*) FROM categories");
            stats.ViewCount         = SafeCount(con, "SELECT COUNT(*) FROM miniviews");
            stats.ClassCount        = SafeCount(con, "SELECT COUNT(*) FROM classes");
            stats.StyleCount        = SafeCount(con, "SELECT COUNT(*) FROM styles");

            // ---- Running timers (distinct TimerIDs with a "running" button state) ----
            // Stop / Buff / Pet are the running button states (see Timers.TimerRunning).
            stats.RunningTimerCount = SafeCount(con,
                "SELECT COUNT(DISTINCT TimerID) FROM timer_runtime_state " +
                "WHERE ButtonState IN ('Stop','Buff','Pet')");

            // ---- Feature usage ----
            stats.WithStartKeyword     = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE StartKeyword IS NOT NULL AND StartKeyword <> ''");
            stats.WithEndKeyword       = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE EndKeyword   IS NOT NULL AND EndKeyword   <> ''");
            stats.WithMultiStartKey    = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE StartKeyword LIKE '%|%'");
            stats.WithWildcardKey      = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE StartKeyword LIKE '%*%'");
            stats.WithCaseSensitive    = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE CaseYn = 1");
            stats.WithSpeech           = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE Speech  IS NOT NULL AND Speech  <> ''");
            stats.WithSoundFile        = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE WAVFile IS NOT NULL AND WAVFile <> ''");
            stats.WithDuration         = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE Duration IS NOT NULL AND Duration <> '' AND Duration <> '00:00:00'");
            stats.WithEndless          = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE EndlessYn = 1");
            stats.WithDependsOn        = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE DependsOnTimer IS NOT NULL AND DependsOnTimer <> ''");
            stats.WithClassAssigned    = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE ClassID    IS NOT NULL AND ClassID    > 0");
            stats.WithCategoryAssigned = SafeCount(con, "SELECT COUNT(*) FROM timers WHERE CategoryID IS NOT NULL AND CategoryID > 0");

            // ---- Breakdown: timers by style ----
            PopulateBreakdown(con, stats.TimersByStyle,
                "SELECT COALESCE(NULLIF(Style,''),'Normal'), COUNT(*) FROM timers GROUP BY COALESCE(NULLIF(Style,''),'Normal')");

            // ---- Breakdown: timers by scope ----
            PopulateBreakdown(con, stats.TimersByScope,
                "SELECT COALESCE(NULLIF(Scope,''),'World'), COUNT(*) FROM timers GROUP BY COALESCE(NULLIF(Scope,''),'World')");

            // ---- Breakdown: timers by category (resolve name via LEFT JOIN) ----
            PopulateBreakdown(con, stats.TimersByCategory,
                "SELECT COALESCE(c.Name,'(none)'), COUNT(*) FROM timers t " +
                "LEFT JOIN categories c ON t.CategoryID = c.ID " +
                "GROUP BY t.CategoryID, c.Name");

            // ---- Breakdown: timers by class (resolve name via LEFT JOIN) ----
            // Unassigned class (ClassID = 0 / NULL) is shown as "All" to match
            // the class combo in the main timer grid, which uses "All" for the
            // no-restriction row.
            PopulateBreakdown(con, stats.TimersByClass,
                "SELECT COALESCE(cl.Name,'All'), COUNT(*) FROM timers t " +
                "LEFT JOIN classes cl ON t.ClassID = cl.ID " +
                "GROUP BY t.ClassID, cl.Name");

            // ---- Tome version metadata (db_meta — schema plumbing lives in Database.cs) ----
            stats.SchemaVersion        = Database.GetMetaValue(con, "SchemaVersion");
            stats.CreatedByVersion     = Database.GetMetaValue(con, "CreatedByVersion");
            stats.LastWrittenByVersion = Database.GetMetaValue(con, "LastWrittenByVersion");

            return stats;
        }

        /// <summary>
        /// Executes a "SELECT key, count FROM ..." query and fills the given dictionary.
        /// Silently swallows errors so a missing column/table never crashes the dialog.
        /// </summary>
        static private void PopulateBreakdown(SQLiteConnection con, Dictionary<string, int> target, string sql)
        {
            try
            {
                using (var cmd = new SQLiteCommand(sql, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string key = reader.IsDBNull(0) ? "(none)" : reader.GetString(0);
                        if (string.IsNullOrEmpty(key)) key = "(none)";
                        int count = reader.GetInt32(1);
                        if (target.ContainsKey(key)) target[key] += count;
                        else target[key] = count;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Runs a scalar COUNT query, returning 0 if the table doesn't exist
        /// or any other error occurs.
        /// </summary>
        static private int SafeCount(SQLiteConnection con, string sql)
        {
            try
            {
                using (var cmd = new SQLiteCommand(sql, con))
                    return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Summary statistics about a Tome database.
    /// Populated by <see cref="TomeStatisticsRepository.Get"/>.
    /// </summary>
    class TomeStatistics
    {
        // Timer state counts
        public int TimerCount { get; set; }
        public int ActiveTimerCount { get; set; }
        public int RunningTimerCount { get; set; }

        // Catalog counts
        public int CharacterCount { get; set; }
        public int CategoryCount { get; set; }
        public int StyleCount { get; set; }
        public int ViewCount { get; set; }
        public int ClassCount { get; set; }

        // Feature usage — how many timers use each authoring feature
        public int WithStartKeyword { get; set; }
        public int WithEndKeyword { get; set; }
        public int WithMultiStartKey { get; set; }
        public int WithWildcardKey { get; set; }
        public int WithSpeech { get; set; }
        public int WithSoundFile { get; set; }
        public int WithDependsOn { get; set; }
        public int WithDuration { get; set; }
        public int WithCaseSensitive { get; set; }
        public int WithEndless { get; set; }
        public int WithClassAssigned { get; set; }
        public int WithCategoryAssigned { get; set; }

        // Tome version metadata (db_meta table)
        public string SchemaVersion { get; set; }
        public string CreatedByVersion { get; set; }
        public string LastWrittenByVersion { get; set; }

        // Breakdowns
        public Dictionary<string, int> TimersByStyle    { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TimersByScope    { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TimersByCategory { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TimersByClass    { get; set; } = new Dictionary<string, int>();
    }
}
