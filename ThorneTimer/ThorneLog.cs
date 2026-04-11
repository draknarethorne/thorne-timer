using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Log severity levels, ordered from most verbose to most critical.
    /// </summary>
    enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3
    }

    /// <summary>
    /// Determines how log files are created.
    /// Session = one file per application launch.
    /// Daily   = one file per calendar day (appended across launches).
    /// </summary>
    enum LogMode
    {
        Session,
        Daily
    }

    /// <summary>
    /// Diagnostic file logger for tracing timer lifecycle and state.
    /// Log files are written to a Logs subfolder next to the executable.
    /// Thread-safe via lock. Auto-flushes each write.
    /// 
    /// Configuration is read from the [Logging] section of
    /// ThorneTimer.ini (next to the exe) at startup.  If the INI file
    /// is missing, built-in defaults are used.  Database settings
    /// (LogMinLevel, LogRetentionDays) serve as a fallback when the
    /// INI file is absent.
    /// 
    /// File cleanup uses <see cref="ThorneArchive.PruneFiles"/> with
    /// tiered retention (recent → daily → monthly → expired).
    /// 
    /// Toggle Enabled to activate/deactivate without removing call sites.
    /// </summary>
    static class ThorneLog
    {
        private static readonly object _lock = new object();
        private static readonly string _logPath;
        private static readonly string _logDir;

        // Tracks whether settings were loaded from INI so that
        // LoadSettings (DB) doesn't override them.
        private static bool _iniLoaded;

        /// <summary>
        /// Master switch — set to false to silence all logging without
        /// removing call sites. Flip back to true when debugging.
        /// </summary>
        public static bool Enabled = true;

        /// <summary>
        /// Minimum log level. Messages below this level are silently dropped.
        /// Default is Info. Set to Debug for verbose tracing.
        /// </summary>
        public static LogLevel MinLevel = LogLevel.Info;

        /// <summary>
        /// How log files are created: Session (one per launch) or Daily
        /// (one per calendar day, appended across launches).
        /// </summary>
        public static LogMode Mode = LogMode.Session;

        /// <summary>
        /// Tiered retention policy for log files.
        /// Loaded from [Logging] in ThorneTimer.ini, with
        /// <see cref="RetentionPolicy.LogDefaults"/> as fallback.
        /// </summary>
        public static RetentionPolicy Retention = RetentionPolicy.LogDefaults;

        static ThorneLog()
        {
            string exeDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            _logDir = Path.Combine(exeDir, "Logs");
            try
            {
                if (!Directory.Exists(_logDir))
                    Directory.CreateDirectory(_logDir);
            }
            catch { }

            // Load INI before building the log path so Mode is known.
            LoadIniSettings();

            if (Mode == LogMode.Daily)
            {
                string day = DateTime.Now.ToString("yyyyMMdd");
                _logPath = Path.Combine(_logDir, $"ThorneLog_{day}.txt");
            }
            else
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _logPath = Path.Combine(_logDir, $"ThorneLog_{timestamp}.txt");
            }

            ThorneArchive.PruneFiles(_logDir, "ThorneLog_*.txt", Retention);
        }

        // ── INI file support ────────────────────────────────────────

        /// <summary>
        /// Reads the [Logging] section of ThorneTimer.ini and applies
        /// settings found there.  Missing keys keep their defaults.
        /// </summary>
        private static void LoadIniSettings()
        {
            try
            {
                string iniPath = ThorneArchive.GetIniPath();
                var settings = ThorneArchive.ParseIniSection(iniPath, "Logging");
                if (settings.Count == 0) return;

                _iniLoaded = true;

                if (settings.TryGetValue("enabled", out string enabledVal))
                    Enabled = enabledVal.Equals("true", StringComparison.OrdinalIgnoreCase);

                if (settings.TryGetValue("minlevel", out string levelVal)
                    && Enum.TryParse(levelVal, true, out LogLevel parsed))
                    MinLevel = parsed;

                if (settings.TryGetValue("mode", out string modeVal)
                    && Enum.TryParse(modeVal, true, out LogMode parsedMode))
                    Mode = parsedMode;

                Retention = ThorneArchive.ReadRetentionPolicy(
                    settings, RetentionPolicy.LogDefaults);
            }
            catch { }
        }

        // ── Database fallback ───────────────────────────────────────

        /// <summary>
        /// Loads log settings from the database settings table.
        /// Only applies values that were NOT already set by ThorneTimer.ini.
        /// Call after the database connection is established.
        /// </summary>
        public static void LoadSettings(SQLiteConnection con)
        {
            if (_iniLoaded) return;
            try
            {
                string level = Database.GetSetting(con, "LogMinLevel");
                if (!string.IsNullOrEmpty(level) && Enum.TryParse(level, true, out LogLevel parsed))
                    MinLevel = parsed;

                string days = Database.GetSetting(con, "LogRetentionDays");
                if (!string.IsNullOrEmpty(days) && int.TryParse(days, out int d) && d >= 0)
                    Retention.RetentionDays = d;
            }
            catch { }
        }

        // ── Core write methods ──────────────────────────────────────

        private static void Write(LogLevel level, string message)
        {
            if (!Enabled || level < MinLevel) return;
            string tag = level.ToString().ToUpper();
            try
            {
                lock (_lock)
                {
                    using (StreamWriter sw = new StreamWriter(_logPath, true))
                    {
                        sw.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{tag}] {message}");
                        sw.Flush();
                    }
                }
            }
            catch
            {
                // Logging must never crash the app
            }
        }

        /// <summary>Logs at DEBUG level — verbose tracing, timer ticks, state details.</summary>
        public static void Debug(string message) => Write(LogLevel.Debug, message);

        /// <summary>Logs at INFO level — lifecycle events, character switches, data loads.</summary>
        public static void Info(string message) => Write(LogLevel.Info, message);

        /// <summary>Logs at WARN level — unusual but recoverable conditions.</summary>
        public static void Warn(string message) => Write(LogLevel.Warn, message);

        /// <summary>Logs at ERROR level — failures that may affect functionality.</summary>
        public static void Error(string message) => Write(LogLevel.Error, message);

        /// <summary>Logs at ERROR level with exception details.</summary>
        public static void Error(string message, Exception ex) =>
            Write(LogLevel.Error, $"{message}: {ex.GetType().Name}: {ex.Message}");

        /// <summary>
        /// Backward-compatible Log() — maps to Info level.
        /// Existing call sites continue to work without changes.
        /// </summary>
        public static void Log(string message) => Info(message);

        // ── Separators ──────────────────────────────────────────────

        public static void Separator(string label = null)
        {
            Info(string.IsNullOrEmpty(label)
                ? "────────────────────────────────────"
                : $"──── {label} ────");
        }

        // ── Data dump helpers ───────────────────────────────────────

        /// <summary>
        /// Dumps all in-memory timer states from TimerRuntime.
        /// </summary>
        public static void DumpTimerStates(string context, List<TimerState> states)
        {
            if (!Enabled || LogLevel.Debug < MinLevel) return;
            Debug($"DUMP [{context}]: {states.Count} timer(s)");
            foreach (var ts in states)
            {
                bool running = ts.IsRunning;
                if (running || !string.IsNullOrEmpty(ts.Remaining)
                    || ts.ButtonState != Timers.btnStart || ts.Count > 0)
                {
                    Debug($"  TID={ts.TimerID} \"{ts.Name}\" Scope={ts.Scope} Btn={ts.ButtonState} Rem={ts.Remaining} Act={ts.ActiveYn} Count={ts.Count} Running={running}");
                }
            }
        }

        /// <summary>
        /// Dumps all timer states — verbose version that includes every timer regardless of state.
        /// </summary>
        public static void DumpAllTimerStates(string context, List<TimerState> states)
        {
            if (!Enabled || LogLevel.Debug < MinLevel) return;
            Debug($"FULL DUMP [{context}]: {states.Count} timer(s)");
            foreach (var ts in states)
            {
                Debug($"  TID={ts.TimerID} \"{ts.Name}\" Scope={ts.Scope} Style={ts.Style} Btn={ts.ButtonState} Rem={ts.Remaining} Act={ts.ActiveYn} Count={ts.Count} Running={ts.IsRunning} ClassID={ts.ClassID}");
            }
        }

        /// <summary>
        /// Dumps saved-state dictionary (from LoadTimerStates).
        /// </summary>
        public static void DumpSavedStates(string context, Dictionary<long, TimerState> states)
        {
            if (!Enabled || LogLevel.Debug < MinLevel) return;
            Debug($"SAVED STATES [{context}]: {states.Count} row(s)");
            foreach (var kvp in states)
            {
                var s = kvp.Value;
                Debug($"  TID={kvp.Key} Btn={s.ButtonState} Rem={s.Remaining} Act={s.ActiveYn} Count={s.Count} SavedUtc={s.SavedAtUtc}");
            }
        }

        /// <summary>
        /// Dumps the timer grid contents (what the user sees).
        /// </summary>
        public static void DumpTimerGrid(string context, DataGridView grid)
        {
            if (!Enabled || LogLevel.Debug < MinLevel) return;
            try
            {
                Debug($"GRID DUMP [{context}]: {grid.Rows.Count} row(s)");
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    long id = Convert.ToInt64(row.Cells[grid.Columns["ID"].Index].Value);
                    string name = Convert.ToString(row.Cells[grid.Columns["Name"].Index].Value);
                    string scope = Convert.ToString(row.Cells[grid.Columns["Scope"].Index].Value);
                    string style = Convert.ToString(row.Cells[grid.Columns["Style"].Index].Value);
                    long activeYn = Convert.ToInt64(row.Cells[grid.Columns["ActiveYn"].Index].Value ?? 0);
                    string remaining = Convert.ToString(row.Cells[grid.Columns["Remaining"].Index].Value);
                    string btn = Convert.ToString(row.Cells[grid.Columns["StartStop"].Index].Value);

                    if (btn != Timers.btnStart || !string.IsNullOrEmpty(remaining))
                    {
                        Debug($"  TID={id} \"{name}\" Scope={scope} Style={style} Btn={btn} Rem={remaining} Act={activeYn}");
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"GRID DUMP [{context}]", ex);
            }
        }

        /// <summary>
        /// Dumps loaded categories at DEBUG level.
        /// </summary>
        public static void DumpCategories(string context, List<Categories.GridData> categories)
        {
            if (!Enabled || LogLevel.Debug < MinLevel) return;
            Debug($"CATEGORIES [{context}]: {categories.Count} category(s)");
            foreach (var c in categories)
            {
                Debug($"  ID={c.ID} \"{c.Name}\" StartKW=\"{c.StartKeyword}\" EndKW=\"{c.EndKeyword}\" AutoStop={c.AutoStop}");
            }
        }

        /// <summary>
        /// Dumps loaded characters at DEBUG level.
        /// </summary>
        public static void DumpCharacters(string context, List<ComboBoxItem> characters)
        {
            if (!Enabled || LogLevel.Debug < MinLevel) return;
            Debug($"CHARACTERS [{context}]: {characters.Count} character(s)");
            foreach (var c in characters)
            {
                Debug($"  ID={c.Value} \"{c.Text}\"");
            }
        }

        /// <summary>
        /// Dumps loaded classes at DEBUG level.
        /// </summary>
        public static void DumpClasses(string context, SQLiteConnection con)
        {
            if (!Enabled || LogLevel.Debug < MinLevel) return;
            try
            {
                var classes = new List<string>();
                using (var cmd = new SQLiteCommand("SELECT ID, Name FROM classes ORDER BY Name", con))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        classes.Add($"  ID={rdr.GetInt64(0)} \"{rdr.GetString(1)}\"");
                }
                Debug($"CLASSES [{context}]: {classes.Count} class(es)");
                foreach (var line in classes)
                    Debug(line);
            }
            catch (Exception ex)
            {
                Error($"CLASSES [{context}]", ex);
            }
        }
    }
}
