using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Speech.Synthesis;
using System.Windows.Forms;

namespace ThorneTimer
{
    class Database
    {
        /// <summary>
        /// Returns the default database path in the Data subdirectory
        /// relative to the running executable.
        /// </summary>
        static public string GetDefaultDatabasePath()
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string basePath = Path.GetDirectoryName(exePath);
            return Path.Combine(basePath, "Data", "ThorneTimer.tdb");
        }

        /// <summary>
        /// Opens a connection to the database at the specified path.
        /// If the file doesn't exist, a new database is created with default schema.
        /// </summary>
        static public SQLiteConnection Connection(string dbPath)
        {
            string newDbName = dbPath;
            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string oldDbName = Path.Combine(exePath, "EQTimer.db");

       bool newDatabase = false;
        // Migration: If ThorneTimer.db does not exist but EQTimer.db does (next to exe), copy it
        if (!File.Exists(newDbName) && File.Exists(oldDbName))
        {
        string directory = Path.GetDirectoryName(newDbName);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.Copy(oldDbName, newDbName);
        MessageBox.Show(
            "Your EQTimer tome has been migrated to:\n" + newDbName +
            "\n\nAll your timers, characters, and settings have been preserved." +
            "\nThe original EQTimer.db was not modified.",
            "Tome Migrated",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Migration: If target doesn't exist but ThorneTimer.db is next to exe (pre-Data layout), copy it
        if (!File.Exists(newDbName))
        {
        string legacyDb = Path.Combine(exePath, "ThorneTimer.db");
        if (!File.Exists(legacyDb))
        {
            // Also check for old .db in the Data folder (pre-.tdb rename)
            legacyDb = Path.Combine(exePath, "Data", "ThorneTimer.db");
        }
        if (File.Exists(legacyDb) && !string.Equals(Path.GetFullPath(legacyDb), Path.GetFullPath(newDbName), StringComparison.OrdinalIgnoreCase))
        {
            string directory = Path.GetDirectoryName(newDbName);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.Copy(legacyDb, newDbName);
            MessageBox.Show(
                "Your tome has been moved to:\n" + newDbName +
                "\n\nAll your data has been preserved." +
                "\nThe original file was not modified.",
                "Tome Migrated",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        }

        if (!File.Exists(newDbName))
        {
        string directory = Path.GetDirectoryName(newDbName);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        SQLiteConnection.CreateFile(newDbName);
        newDatabase = true;
        }

            SQLiteConnection con = new SQLiteConnection("URI=file:" + newDbName);
            con.Open();

            string voice = "";
            using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
            {
                voice = synthesizer.Voice.Name;
            }

            if (newDatabase)
            {
                SQLiteCommand cmd = new SQLiteCommand(con)
                {
                    CommandText = "CREATE TABLE timers(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, CategoryID INTEGER, StartKeyword TEXT, EndKeyword TEXT, WAVFile TEXT, Speech TEXT, Duration TEXT, ActiveYn INTEGER, CaseYn INTEGER, EndlessYn INTEGER, Style TEXT DEFAULT 'Normal', Scope TEXT DEFAULT 'World', DependsOnTimer TEXT DEFAULT '', DependsOnDelay INTEGER DEFAULT 0, ClassID INTEGER)"
                };
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE characters(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, LogFile TEXT, MiniViewX INTEGER, MiniViewY INTEGER, ClassID INTEGER)";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE categories(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, StartKeyword TEXT, EndKeyword TEXT, AutoStop INTEGER)";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE settings(ID INTEGER PRIMARY KEY, ActiveCharacterID TEXT, ActiveVoice TEXT, MiniViewFontSize INTEGER, MiniViewWarnFore INTEGER, MiniViewWarnBack INTEGER, MiniViewWarnTime TEXT, MiniViewOpacity INTEGER, VoiceVolume INTEGER, VoiceRate INTEGER, VoiceEnabled INTEGER, MiniViewNormFore INTEGER, MiniViewNormBack INTEGER, MiniViewShowPing INTEGER, MiniViewPingFore INTEGER, MiniViewPingBack INTEGER, MiniViewPingTime TEXT, MiniViewBuffFore INTEGER, MiniViewBuffBack INTEGER, ShowAllClasses INTEGER DEFAULT 1, CompactView INTEGER DEFAULT 0, AutoSwitchEnabled INTEGER DEFAULT 1)";
                cmd.ExecuteNonQuery();

                // Create miniviews table used by the UI
                cmd.CommandText = "CREATE TABLE miniviews(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, PositionX INTEGER DEFAULT 100, PositionY INTEGER DEFAULT 100, ViewType TEXT DEFAULT 'Normal', SortOrder INTEGER DEFAULT 0, ActiveYn INTEGER DEFAULT 1, StyleFilter TEXT DEFAULT 'Normal')";
                cmd.ExecuteNonQuery();

                // Create grid_columns table for persisting column widths across sessions
                cmd.CommandText = "CREATE TABLE grid_columns(ID INTEGER PRIMARY KEY AUTOINCREMENT, GridName TEXT, ColumnName TEXT, Width INTEGER)";
                cmd.ExecuteNonQuery();

                // Create grid_sort_state table for persisting multi-column sort across sessions
                cmd.CommandText = "CREATE TABLE grid_sort_state(ID INTEGER PRIMARY KEY AUTOINCREMENT, GridName TEXT, ColumnName TEXT, SortDirection INTEGER, SortOrder INTEGER)";
                cmd.ExecuteNonQuery();

                // Create timer_runtime_state table for persisting timer counts and state
                cmd.CommandText = "CREATE TABLE timer_runtime_state(ID INTEGER PRIMARY KEY AUTOINCREMENT, TimerID INTEGER NOT NULL, CharacterID INTEGER, Remaining TEXT, ButtonState TEXT, Count INTEGER DEFAULT 0, StartedAt TEXT, ActiveYn INTEGER DEFAULT 1, UNIQUE(TimerID, CharacterID))";
                cmd.ExecuteNonQuery();

                // Create classes table for EQ character classes
                cmd.CommandText = "CREATE TABLE classes(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)";
                cmd.ExecuteNonQuery();

                // Seed default EQ classes
                string[] eqClasses = { "Bard", "Beastlord", "Berserker", "Cleric", "Druid", "Enchanter", "Magician", "Monk", "Necromancer", "Paladin", "Ranger", "Rogue", "Shadow Knight", "Shaman", "Warrior", "Wizard" };
                foreach (string className in eqClasses)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = "INSERT INTO classes (Name) VALUES (@className)";
                    cmd.Parameters.AddWithValue("@className", className);
                    cmd.ExecuteNonQuery();
                }
                cmd.Parameters.Clear();

                // Insert default settings
                cmd.CommandText = "INSERT INTO settings(ID, ActiveCharacterID, ActiveVoice, MiniViewFontSize, MiniViewWarnFore, MiniViewWarnBack, MiniViewWarnTime, MiniViewOpacity, VoiceVolume, VoiceRate, VoiceEnabled, MiniViewNormFore, MiniViewNormBack, MiniViewShowPing, MiniViewPingFore, MiniViewPingBack, MiniViewPingTime, MiniViewBuffFore, MiniViewBuffBack) VALUES(@id,@activeChar,@activeVoice,@fontSize,@warnFore,@warnBack,@warnTime,@opacity,@voiceVolume,@voiceRate,@voiceEnabled,@normFore,@normBack,@showPing,@pingFore,@pingBack,@pingTime,@buffFore,@buffBack)";
                cmd.Parameters.AddWithValue("@id", 1);
                cmd.Parameters.AddWithValue("@activeChar", "");
                cmd.Parameters.AddWithValue("@activeVoice", voice);
                cmd.Parameters.AddWithValue("@fontSize", 8);
                cmd.Parameters.AddWithValue("@warnFore", Color.White.ToArgb());
                cmd.Parameters.AddWithValue("@warnBack", Color.Red.ToArgb());
                cmd.Parameters.AddWithValue("@warnTime", "00:30");
                cmd.Parameters.AddWithValue("@opacity", 100);
                cmd.Parameters.AddWithValue("@voiceVolume", 100);
                cmd.Parameters.AddWithValue("@voiceRate", -2);
                cmd.Parameters.AddWithValue("@voiceEnabled", 1);
                cmd.Parameters.AddWithValue("@normFore", Color.Black.ToArgb());
                cmd.Parameters.AddWithValue("@normBack", Color.White.ToArgb());
                cmd.Parameters.AddWithValue("@showPing", 1);
                cmd.Parameters.AddWithValue("@pingFore", Color.LightGreen.ToArgb());
                cmd.Parameters.AddWithValue("@pingBack", Color.Black.ToArgb());
                cmd.Parameters.AddWithValue("@pingTime", "00:30");
                cmd.Parameters.AddWithValue("@buffFore", Color.Orange.ToArgb());
                cmd.Parameters.AddWithValue("@buffBack", Color.Black.ToArgb());
                cmd.ExecuteNonQuery();
                // Clear parameters so the same command object can be reused safely later
                cmd.Parameters.Clear();
            }
            else
            {
                if (!isFieldExist(con, "settings", "ActiveVoice"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD ActiveVoice TEXT"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE settings SET ActiveVoice = @voice WHERE ID = 1";
                    cmd.Parameters.AddWithValue("@voice", voice);
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "settings", "MiniViewFontSize"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD MiniViewFontSize INTEGER"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE settings SET MiniViewFontSize = 8 WHERE ID = 1";
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "categories", "StartKeyword"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE categories ADD StartKeyword TEXT"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE categories ADD EndKeyword TEXT";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE categories SET StartKeyword = '', EndKeyword = ''";
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "timers", "ActiveYn"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE timers ADD ActiveYn INTEGER"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE timers SET ActiveYn = 1";
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "categories", "AutoStop"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE categories ADD AutoStop INTEGER"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE categories SET AutoStop = 0";
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "characters", "MiniViewX"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE characters ADD MiniViewX INTEGER"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE characters ADD MiniViewY INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE characters SET MiniViewX = 100, MiniViewY = 100";
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "settings", "MiniViewWarnFore"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD MiniViewWarnFore INTEGER"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD MiniViewWarnBack INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD MiniViewWarnTime TEXT";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD MiniViewOpacity INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD VoiceVolume INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD VoiceRate INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD MiniViewNormFore INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD MiniViewNormBack INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE settings SET MiniViewWarnFore = @warnFore, MiniViewWarnBack = @warnBack, MiniViewWarnTime = @warnTime, MiniViewOpacity = @opacity, VoiceVolume = @voiceVol, VoiceRate = @voiceRate, MiniViewNormFore = @normFore, MiniViewNormBack = @normBack WHERE ID = 1";
                    cmd.Parameters.AddWithValue("@warnFore", Color.White.ToArgb());
                    cmd.Parameters.AddWithValue("@warnBack", Color.Red.ToArgb());
                    cmd.Parameters.AddWithValue("@warnTime", "00:30");
                    cmd.Parameters.AddWithValue("@opacity", 100);
                    cmd.Parameters.AddWithValue("@voiceVol", 100);
                    cmd.Parameters.AddWithValue("@voiceRate", -2);
                    cmd.Parameters.AddWithValue("@normFore", Color.Black.ToArgb());
                    cmd.Parameters.AddWithValue("@normBack", Color.White.ToArgb());
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }

                if (!isFieldExist(con, "timers", "CaseYn"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE timers ADD CaseYn INTEGER"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE timers ADD EndlessYn INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE timers SET CaseYn = 0, EndlessYn = 0";
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "settings", "MiniViewPingFore"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD MiniViewShowPing INTEGER"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD MiniViewPingFore INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD MiniViewPingBack INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD MiniViewPingTime TEXT";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE settings SET MiniViewShowPing = @showPing, MiniViewPingFore = @pingFore, MiniViewPingBack = @pingBack, MiniViewPingTime = @pingTime WHERE ID = 1";
                    cmd.Parameters.AddWithValue("@showPing", 1);
                    cmd.Parameters.AddWithValue("@pingFore", Color.LightGreen.ToArgb());
                    cmd.Parameters.AddWithValue("@pingBack", Color.Black.ToArgb());
                    cmd.Parameters.AddWithValue("@pingTime", "00:30");
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }

                if (!isFieldExist(con, "settings", "MiniViewBuffFore"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD MiniViewBuffFore INTEGER"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE settings ADD MiniViewBuffBack INTEGER";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE settings SET MiniViewBuffFore = @buffFore, MiniViewBuffBack = @buffBack WHERE ID = 1";
                    cmd.Parameters.AddWithValue("@buffFore", Color.Orange.ToArgb());
                    cmd.Parameters.AddWithValue("@buffBack", Color.Black.ToArgb());
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }

                if (!isFieldExist(con, "settings", "VoiceEnabled"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD VoiceEnabled INTEGER"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE settings SET VoiceEnabled = 1 WHERE ID = 1";
                    cmd.ExecuteNonQuery();
                }

                if (!isTableExist(con, "miniviews"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "CREATE TABLE miniviews(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)"
                    };
                    cmd.ExecuteNonQuery();
                }

                // Add position columns to miniviews table if they don't exist
                if (!isFieldExist(con, "miniviews", "PositionX"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE miniviews ADD PositionX INTEGER DEFAULT 100"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE miniviews ADD PositionY INTEGER DEFAULT 100";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE miniviews ADD ViewType TEXT DEFAULT 'Normal'";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE miniviews ADD SortOrder INTEGER DEFAULT 0";
                    cmd.ExecuteNonQuery();
                }

                // Add ActiveYn and StyleFilter columns to miniviews table
                if (!isFieldExist(con, "miniviews", "ActiveYn"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE miniviews ADD ActiveYn INTEGER DEFAULT 1"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE miniviews ADD StyleFilter TEXT DEFAULT 'Normal'";
                    cmd.ExecuteNonQuery();

                    // Set StyleFilter to match ViewType for existing rows
                    cmd.CommandText = "UPDATE miniviews SET ActiveYn = 1, StyleFilter = ViewType";
                    cmd.ExecuteNonQuery();
                }

                // Add Style column to timers table and migrate legacy prefix conventions
                if (!isFieldExist(con, "timers", "Style"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE timers ADD Style TEXT DEFAULT 'Normal'"
                    };
                    cmd.ExecuteNonQuery();

                    // Infer Style from legacy EndKeyword prefixes and Duration
                    cmd.CommandText = "UPDATE timers SET Style = 'Buff' WHERE EndKeyword LIKE '@%'";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE timers SET Style = 'Pet' WHERE EndKeyword LIKE '#%'";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE timers SET Style = 'Ping' WHERE Duration = '00:00:00' AND Style = 'Normal'";
                    cmd.ExecuteNonQuery();

                    // Set Ping timers' Duration to the current ping time setting so each timer owns its countdown
                    string pingTime = GetSetting(con, "MiniViewPingTime");
                    if (string.IsNullOrEmpty(pingTime)) pingTime = "00:30";
                    cmd.CommandText = "UPDATE timers SET Duration = @pingDuration WHERE Style = 'Ping'";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@pingDuration", "00:" + pingTime);
                    cmd.ExecuteNonQuery();

                    // Strip the legacy @/# prefixes from EndKeyword (handle @@ and ## before single)
                    cmd.Parameters.Clear();
                    cmd.CommandText = "UPDATE timers SET EndKeyword = SUBSTR(EndKeyword, 3) WHERE Style = 'Buff' AND EndKeyword LIKE '@@%'";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE timers SET EndKeyword = SUBSTR(EndKeyword, 2) WHERE Style = 'Buff' AND EndKeyword LIKE '@%'";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE timers SET EndKeyword = SUBSTR(EndKeyword, 3) WHERE Style = 'Pet' AND EndKeyword LIKE '##%'";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE timers SET EndKeyword = SUBSTR(EndKeyword, 2) WHERE Style = 'Pet' AND EndKeyword LIKE '#%'";
                    cmd.ExecuteNonQuery();
                }

                // Create grid_columns table if it doesn't exist (for column width persistence)
                if (!isTableExist(con, "grid_columns"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "CREATE TABLE grid_columns(ID INTEGER PRIMARY KEY AUTOINCREMENT, GridName TEXT, ColumnName TEXT, Width INTEGER, FillWeight REAL DEFAULT 100)"
                    };
                    cmd.ExecuteNonQuery();
                }

                // Add FillWeight column if upgrading from an older schema
                if (isTableExist(con, "grid_columns") && !isFieldExist(con, "grid_columns", "FillWeight"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE grid_columns ADD FillWeight REAL DEFAULT 100"
                    };
                    cmd.ExecuteNonQuery();
                }

                // Create grid_sort_state table if it doesn't exist (for multi-column sort persistence)
                if (!isTableExist(con, "grid_sort_state"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "CREATE TABLE grid_sort_state(ID INTEGER PRIMARY KEY AUTOINCREMENT, GridName TEXT, ColumnName TEXT, SortDirection INTEGER, SortOrder INTEGER)"
                    };
                    cmd.ExecuteNonQuery();
                }

                // Populate Duration for Ping timers that still have 00:00:00
                // (handles databases where Style was added before per-timer duration code existed)
                if (isFieldExist(con, "timers", "Style"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "SELECT COUNT(*) FROM timers WHERE Style = 'Ping' AND Duration = '00:00:00'"
                    };
                    long pingCount = (long)cmd.ExecuteScalar();
                    if (pingCount > 0)
                    {
                        string pingTime = GetSetting(con, "MiniViewPingTime");
                        if (string.IsNullOrEmpty(pingTime)) pingTime = "00:30";
                        cmd.CommandText = "UPDATE timers SET Duration = @pingDuration WHERE Style = 'Ping' AND Duration = '00:00:00'";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@pingDuration", "00:" + pingTime);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Add Scope column to timers table
                // Default to 'World' — existing timers predate scope awareness and
                // behaved as world timers (kept running regardless of character).
                if (!isFieldExist(con, "timers", "Scope"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE timers ADD Scope TEXT DEFAULT 'World'"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE timers SET Scope = 'World'";
                    cmd.ExecuteNonQuery();
                }

                // Add DependsOnTimer and DependsOnDelay columns, migrating legacy *Name|Delay from EndKeyword
                if (!isFieldExist(con, "timers", "DependsOnTimer"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE timers ADD DependsOnTimer TEXT DEFAULT ''"
                    };
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE timers ADD DependsOnDelay INTEGER DEFAULT 0";
                    cmd.ExecuteNonQuery();

                    // Migrate legacy *Name|Delay values from EndKeyword into the new columns.
                    // Format: *TimerName|DelayInSeconds  or  *TimerName  (default delay = 15s)
                    cmd.CommandText = "SELECT ID, EndKeyword FROM timers WHERE EndKeyword LIKE '*%'";
                    SQLiteDataReader rdr = cmd.ExecuteReader();
                    var migrations = new List<Tuple<long, string, long>>();
                    while (rdr.Read())
                    {
                        long id = rdr.GetInt64(0);
                        string ek = rdr.GetString(1);
                        string timerName;
                        long delaySec = 15;
                        int pipeIndex = ek.IndexOf('|');
                        if (pipeIndex > 0)
                        {
                            timerName = ek.Substring(1, pipeIndex - 1);
                            long.TryParse(ek.Substring(pipeIndex + 1), out delaySec);
                        }
                        else
                        {
                            timerName = ek.Substring(1);
                        }
                        migrations.Add(Tuple.Create(id, timerName, delaySec));
                    }
                    try { rdr.Close(); } catch { }

                    foreach (var m in migrations)
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = "UPDATE timers SET DependsOnTimer = @name, DependsOnDelay = @delay, EndKeyword = '' WHERE ID = @id";
                        cmd.Parameters.AddWithValue("@name", m.Item2);
                        cmd.Parameters.AddWithValue("@delay", m.Item3);
                        cmd.Parameters.AddWithValue("@id", m.Item1);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Create timer_runtime_state table for persisting timer counts and state
                if (!isTableExist(con, "timer_runtime_state"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "CREATE TABLE timer_runtime_state(ID INTEGER PRIMARY KEY AUTOINCREMENT, TimerID INTEGER NOT NULL, CharacterID INTEGER, Remaining TEXT, ButtonState TEXT, Count INTEGER DEFAULT 0, StartedAt TEXT, ActiveYn INTEGER DEFAULT 1, UNIQUE(TimerID, CharacterID))"
                    };
                    cmd.ExecuteNonQuery();
                }

                // Add ActiveYn to timer_runtime_state for per-character activation preferences
                if (isTableExist(con, "timer_runtime_state") && !isFieldExist(con, "timer_runtime_state", "ActiveYn"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE timer_runtime_state ADD ActiveYn INTEGER DEFAULT 1"
                    };
                    cmd.ExecuteNonQuery();
                }

                // Create classes table and seed default EQ classes
                if (!isTableExist(con, "classes"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "CREATE TABLE classes(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)"
                    };
                    cmd.ExecuteNonQuery();

                    string[] eqClasses = { "Bard", "Beastlord", "Berserker", "Cleric", "Druid", "Enchanter", "Magician", "Monk", "Necromancer", "Paladin", "Ranger", "Rogue", "Shadow Knight", "Shaman", "Warrior", "Wizard" };
                    foreach (string className in eqClasses)
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = "INSERT INTO classes (Name) VALUES (@className)";
                        cmd.Parameters.AddWithValue("@className", className);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Add ClassID column to timers table (nullable = Global timer)
                if (!isFieldExist(con, "timers", "ClassID"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE timers ADD ClassID INTEGER"
                    };
                    cmd.ExecuteNonQuery();
                }

                // Add ClassID column to characters table
                if (!isFieldExist(con, "characters", "ClassID"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE characters ADD ClassID INTEGER"
                    };
                    cmd.ExecuteNonQuery();
                }

                // Add toggle-state columns to settings for persistence across sessions
                if (!isFieldExist(con, "settings", "ShowAllClasses"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD ShowAllClasses INTEGER DEFAULT 1"
                    };
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "settings", "CompactView"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD CompactView INTEGER DEFAULT 0"
                    };
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "settings", "AutoSwitchEnabled"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD AutoSwitchEnabled INTEGER DEFAULT 1"
                    };
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "settings", "CompactWidth"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD CompactWidth INTEGER"
                    };
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "settings", "FullWidth"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD FullWidth INTEGER"
                    };
                    cmd.ExecuteNonQuery();
                }

                // One-time migration: set Scope='Character' and ClassID on timers whose
                // category name matches an EQ class name.  Older tomes used categories as
                // class proxies (e.g. "Necro", "Enchanter") before Scope/ClassID existed.
                // Categories that don't match a class name are left as World scope.
                if (!isFieldExist(con, "settings", "CategoryScopeMigrated"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD CategoryScopeMigrated INTEGER DEFAULT 0"
                    };
                    cmd.ExecuteNonQuery();
                }

                {
                    string migrated = GetSetting(con, "CategoryScopeMigrated");
                    if (migrated == "0" || migrated == "")
                    {
                        MigrateCategoryScopesToCharacter(con);

                        SQLiteCommand cmd = new SQLiteCommand(con)
                        {
                            CommandText = "UPDATE settings SET CategoryScopeMigrated = 1"
                        };
                        cmd.ExecuteNonQuery();
                    }
                }

                // Seed default views if miniviews table is empty
                SeedDefaultViews(con);
            }

            return con;
        }

        /// <summary>
        /// Opens a connection using the default database path (Data subdirectory next to the executable).
        /// </summary>
        static public SQLiteConnection Connection()
        {
            return Connection(GetDefaultDatabasePath());
        }

        /// <summary>
        /// One-time migration: examines each category name and, if it matches (or is
        /// a recognizable abbreviation of) an EQ class name, sets all timers in that
        /// category to Scope='Character' and assigns the matching ClassID.
        /// Non-matching categories (e.g. "Spawn", "Misc") are left as World scope.
        /// </summary>
        static private void MigrateCategoryScopesToCharacter(SQLiteConnection con)
        {
            // Build a lookup of class names from the classes table
            // Key = lowercase class name, Value = class ID
            var classLookup = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT ID, Name FROM classes"
            };
            using (SQLiteDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    classLookup[rdr.GetString(1)] = rdr.GetInt64(0);
                }
            }

            // Read all categories
            var categoriesToMigrate = new List<Tuple<long, long, bool>>(); // CategoryID, ClassID, isPet
            cmd.CommandText = "SELECT ID, Name FROM categories";
            using (SQLiteDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    long catId = rdr.GetInt64(0);
                    string catName = rdr.GetString(1).Trim();

                    long matchedClassId = MatchCategoryToClass(catName, classLookup);

                    if (matchedClassId >= 0)
                    {
                        bool isPet = catName.IndexOf("(Pet)", StringComparison.OrdinalIgnoreCase) >= 0;
                        categoriesToMigrate.Add(Tuple.Create(catId, matchedClassId, isPet));
                    }
                }
            }

            // Update timers in matched categories.
            // Deactivate them (ActiveYn=0) so upgrading users start clean —
            // they opt-in to class-specific timers per character rather than
            // inheriting whatever was active in the old database for every character.
            // For (Pet) categories, also set Style='Pet' since the EndKeyword-based
            // inference may have classified some pet buffs as 'Buff' instead.
            foreach (var cat in categoriesToMigrate)
            {
                cmd.Parameters.Clear();
                if (cat.Item3)
                    cmd.CommandText = "UPDATE timers SET Scope = 'Character', ClassID = @classId, ActiveYn = 0, Style = 'Pet' WHERE CategoryID = @catId";
                else
                    cmd.CommandText = "UPDATE timers SET Scope = 'Character', ClassID = @classId, ActiveYn = 0 WHERE CategoryID = @catId";
                cmd.Parameters.AddWithValue("@classId", cat.Item2);
                cmd.Parameters.AddWithValue("@catId", cat.Item1);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Tries to match a category name to an EQ class. Handles exact matches,
        /// abbreviation prefixes ("Necro" → "Necromancer"), and parenthetical
        /// suffixes ("Enchanter (Pet)" → "Enchanter").
        /// Returns the matched ClassID, or -1 if no match.
        /// </summary>
        static private long MatchCategoryToClass(string catName, Dictionary<string, long> classLookup)
        {
            // Exact match
            if (classLookup.TryGetValue(catName, out long exactId))
                return exactId;

            // Partial match: category name is a prefix of a class name
            // e.g. "Necro" → "Necromancer", "Shadow" → "Shadow Knight"
            foreach (var kvp in classLookup)
            {
                if (kvp.Key.StartsWith(catName, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            // Strip parenthetical suffix and retry
            // e.g. "Enchanter (Pet)" → "Enchanter", "Necro (Pet)" → "Necro"
            int parenIndex = catName.IndexOf('(');
            if (parenIndex > 0)
            {
                string baseName = catName.Substring(0, parenIndex).Trim();
                if (baseName.Length > 0)
                    return MatchCategoryToClass(baseName, classLookup);
            }

            return -1;
        }

        /// <summary>
        /// Creates the 4 default views (Normal, Pet, Buff, Ping) if they don't exist.
        /// Uses the active character's MiniViewX/Y as the base position.
        /// </summary>
        static private void SeedDefaultViews(SQLiteConnection con)
        {
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT COUNT(*) FROM miniviews"
            };
            long count = (long)cmd.ExecuteScalar();

            if (count == 0)
            {
                // Get base position from active character if available
                int baseX = 100;
                int baseY = 100;

                cmd.CommandText = "SELECT ActiveCharacterID FROM settings WHERE ID = 1";
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    string activeCharId = result.ToString();
                    cmd.CommandText = "SELECT MiniViewX, MiniViewY FROM characters WHERE ID = @id";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@id", activeCharId);
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0)) baseX = rdr.GetInt32(0);
                            if (!rdr.IsDBNull(1)) baseY = rdr.GetInt32(1);
                        }
                    }
                }

                // Insert the 4 default views with positions matching current hardcoded offsets
                cmd.Parameters.Clear();
                cmd.CommandText = "INSERT INTO miniviews (Name, ViewType, PositionX, PositionY, SortOrder, ActiveYn, StyleFilter) VALUES (@name, @type, @x, @y, @order, @active, @style)";

                // Normal view
                cmd.Parameters.AddWithValue("@name", "Normal Timers");
                cmd.Parameters.AddWithValue("@type", "Normal");
                cmd.Parameters.AddWithValue("@x", baseX);
                cmd.Parameters.AddWithValue("@y", baseY);
                cmd.Parameters.AddWithValue("@order", 1);
                cmd.Parameters.AddWithValue("@active", 1);
                cmd.Parameters.AddWithValue("@style", "Normal");
                cmd.ExecuteNonQuery();

                // Pet view (offset +200)
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@name", "Pet Timers");
                cmd.Parameters.AddWithValue("@type", "Pet");
                cmd.Parameters.AddWithValue("@x", baseX + 200);
                cmd.Parameters.AddWithValue("@y", baseY);
                cmd.Parameters.AddWithValue("@order", 2);
                cmd.Parameters.AddWithValue("@active", 1);
                cmd.Parameters.AddWithValue("@style", "Pet");
                cmd.ExecuteNonQuery();

                // Buff view (offset +400)
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@name", "Buff Timers");
                cmd.Parameters.AddWithValue("@type", "Buff");
                cmd.Parameters.AddWithValue("@x", baseX + 400);
                cmd.Parameters.AddWithValue("@y", baseY);
                cmd.Parameters.AddWithValue("@order", 3);
                cmd.Parameters.AddWithValue("@active", 1);
                cmd.Parameters.AddWithValue("@style", "Buff");
                cmd.ExecuteNonQuery();

                // Ping view (offset +1000)
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@name", "Ping Timers");
                cmd.Parameters.AddWithValue("@type", "Ping");
                cmd.Parameters.AddWithValue("@x", baseX + 1000);
                cmd.Parameters.AddWithValue("@y", baseY);
                cmd.Parameters.AddWithValue("@order", 4);
                cmd.Parameters.AddWithValue("@active", 1);
                cmd.Parameters.AddWithValue("@style", "Ping");
                cmd.ExecuteNonQuery();
            }
        }

        static public bool isFieldExist(SQLiteConnection con, string tableName, string fieldName)
        {
            bool isExist = false;

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "PRAGMA table_info(" + tableName + ")"
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                string currentColumn = rdr.GetString(1);
                if (currentColumn.Equals(fieldName))
                {
                    isExist = true;
                    break;
                }
            }

            // Ensure reader is closed
            try { rdr.Close(); } catch { }

            return isExist;
        }

        static public string GetSetting(SQLiteConnection con, string column)
        {
            string retValue = "";

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from settings LIMIT 1"
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                string dt = rdr.GetDataTypeName(rdr.GetOrdinal(column));
                try
                {
                    if (dt == "INTEGER")
                        retValue = Convert.ToString(rdr.GetInt32(rdr.GetOrdinal(column)));
                    else
                        retValue = rdr.GetString(rdr.GetOrdinal(column));
                }
                catch (Exception)
                {
                    retValue = "0";
                }
            }

            try { rdr.Close(); } catch { }

            return retValue;
        }

        static public bool isTableExist(SQLiteConnection con, string tableName)
        {
            bool exists = false;
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@table LIMIT 1"
            };
            cmd.Parameters.AddWithValue("@table", tableName);
            SQLiteDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read()) exists = true;
            try { rdr.Close(); } catch { }
            return exists;
        }

        // Valid column names for the settings table (whitelist for SetSetting)
        private static readonly HashSet<string> ValidSettingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ActiveCharacterID", "ActiveVoice",
            "MiniViewFontSize", "MiniViewOpacity",
            "MiniViewWarnFore", "MiniViewWarnBack", "MiniViewWarnTime",
            "MiniViewNormFore", "MiniViewNormBack",
            "MiniViewShowPing", "MiniViewPingFore", "MiniViewPingBack", "MiniViewPingTime",
            "MiniViewBuffFore", "MiniViewBuffBack",
            "VoiceVolume", "VoiceRate", "VoiceEnabled",
            "ShowAllClasses", "CompactView", "AutoSwitchEnabled",
            "CompactWidth", "FullWidth"
        };

        static public void SetSetting(SQLiteConnection con, string column, string value)
        {
            if (!ValidSettingColumns.Contains(column)) return;
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "UPDATE settings SET " + column + " = @value"
            };
            cmd.Parameters.AddWithValue("@value", value);
            cmd.ExecuteNonQuery();
        }

        static public void SetSetting(SQLiteConnection con, string column, int value)
        {
            if (!ValidSettingColumns.Contains(column)) return;
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "UPDATE settings SET " + column + " = @value"
            };
            cmd.Parameters.AddWithValue("@value", value);
            cmd.ExecuteNonQuery();
        }

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

            SQLiteCommand cmd = new SQLiteCommand(con);

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
            cmd.Parameters.AddWithValue("@activeYn", Convert.ToInt32(ActiveYn.Value));
            cmd.Parameters.AddWithValue("@caseYn", Convert.ToInt32(CaseYn.Value));
            cmd.Parameters.AddWithValue("@endlessYn", Convert.ToInt32(EndlessYn.Value));
            cmd.Parameters.AddWithValue("@style", Convert.ToString(Style.Value));
            cmd.Parameters.AddWithValue("@scope", Convert.ToString(Scope.Value));
            cmd.Parameters.AddWithValue("@dependsOnTimer", Convert.ToString(DependsOnTimer.Value));
            cmd.Parameters.AddWithValue("@dependsOnDelay", Convert.ToInt32(DependsOnDelay.Value));
            cmd.Parameters.AddWithValue("@classID", ClassIDCell.Value == null || ClassIDCell.Value == DBNull.Value || Convert.ToInt64(ClassIDCell.Value) == 0 ? (object)DBNull.Value : Convert.ToInt64(ClassIDCell.Value));
            cmd.ExecuteNonQuery();

            // Update ID in Grid When INSERTing
            if (Convert.ToString(ID.Value) == "-1")
            {
                cmd.CommandText = "SELECT last_insert_rowid()";
                cmd.Parameters.Clear();
                row.Cells[dataGridView.Columns["ID"].Index].Value = (long)cmd.ExecuteScalar();
            }
        }

        static public void DeleteTimer(SQLiteConnection con, string ID)
        {
            if (!int.TryParse(ID, out int idValue)) return;
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "DELETE FROM timers WHERE ID = @id"
            };
            cmd.Parameters.AddWithValue("@id", idValue);
            cmd.ExecuteNonQuery();
        }


        static public SortableBindingList<Timers.GridData> GetTimers(SQLiteConnection con)
        {
            SortableBindingList<Timers.GridData> gridData = new SortableBindingList<Timers.GridData>();

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from timers" // ORDER BY ActiveYn DESC";
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            bool hasScope = isFieldExist(con, "timers", "Scope");
            int scopeOrdinal = hasScope ? -1 : -1;

            bool hasDependsOn = isFieldExist(con, "timers", "DependsOnTimer");

            bool hasClassID = isFieldExist(con, "timers", "ClassID");

            while (rdr.Read())
            {
                try
                {
                    if (hasScope && scopeOrdinal < 0)
                        scopeOrdinal = rdr.GetOrdinal("Scope");

                    Timers.GridData data = new Timers.GridData
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

            rdr.Close();

            return gridData;
        }

        static public Characters.GridData GetCharacter(SQLiteConnection con, string ID)
        {
            Characters.GridData data = new Characters.GridData();

            if (!int.TryParse(ID, out int idValue))
            {
                // Invalid ID supplied; return empty data
                return data;
            }

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from characters WHERE ID = @id"
            };
            cmd.Parameters.AddWithValue("@id", idValue);
            SQLiteDataReader rdr = cmd.ExecuteReader();

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

            try { rdr.Close(); } catch { }

            return data;
        }

        static public List<ComboBoxItem> GetActiveCharacters(SQLiteConnection con)
        {
            List<ComboBoxItem> cboData = new List<ComboBoxItem>();

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from characters ORDER BY Name"
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                ComboBoxItem data = new ComboBoxItem
                {
                    Value = rdr.GetInt32(rdr.GetOrdinal("ID")),
                    Text = rdr.GetString(rdr.GetOrdinal("Name"))
                };

                cboData.Add(data);
            }

            rdr.Close();

            return cboData;
        }

        static public List<Characters.GridData> GetCharacters(SQLiteConnection con)
        {
            List<Characters.GridData> gridData = new List<Characters.GridData>();

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from characters ORDER BY Name"
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                Characters.GridData data = new Characters.GridData
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

            rdr.Close();

            return gridData;
        }

        static public void DeleteCharacter(SQLiteConnection con, string ID)
        {
            if (!int.TryParse(ID, out int idValue)) return;
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "DELETE FROM characters WHERE ID = @id"
            };
            cmd.Parameters.AddWithValue("@id", idValue);
            cmd.ExecuteNonQuery();
        }

        static public void SaveCharacter(SQLiteConnection con, DataGridView dataGridView, DataGridViewRow row)
        {
            DataGridViewCell ID = row.Cells[dataGridView.Columns["ID"].Index];
            DataGridViewCell Name = row.Cells[dataGridView.Columns["Name"].Index];
            DataGridViewCell LogFile = row.Cells[dataGridView.Columns["LogFile"].Index];
            DataGridViewCell MiniViewX = row.Cells[dataGridView.Columns["MiniViewX"].Index];
            DataGridViewCell MiniViewY = row.Cells[dataGridView.Columns["MiniViewY"].Index];
            DataGridViewCell ClassIDCell = row.Cells[dataGridView.Columns["ClassID"].Index];

            SQLiteCommand cmd = new SQLiteCommand(con);

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
            cmd.Parameters.AddWithValue("@classID", ClassIDCell.Value == null || ClassIDCell.Value == DBNull.Value || Convert.ToInt64(ClassIDCell.Value) == 0 ? (object)DBNull.Value : Convert.ToInt64(ClassIDCell.Value));
            cmd.ExecuteNonQuery();

            // Update ID in Grid When INSERTing
            if (Convert.ToString(ID.Value) == "-1")
            {
                cmd.CommandText = "SELECT last_insert_rowid()";
                cmd.Parameters.Clear();
                row.Cells[dataGridView.Columns["ID"].Index].Value = (long)cmd.ExecuteScalar();
            }
        }

        static public List<ComboBoxItem> GetGridCategories(SQLiteConnection con)
        {
            List<ComboBoxItem> cboData = new List<ComboBoxItem>();

            ComboBoxItem blankData = new ComboBoxItem
            {
                Value = 0,
                Text = "--"
            };
            cboData.Add(blankData);

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from categories ORDER BY Name"
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                ComboBoxItem data = new ComboBoxItem
                {
                    Value = rdr.GetInt32(rdr.GetOrdinal("ID")),
                    Text = rdr.GetString(rdr.GetOrdinal("Name"))
                };

                cboData.Add(data);
            }

            rdr.Close();

            return cboData;
        }

        static public List<ComboBoxItem> GetGridClasses(SQLiteConnection con)
        {
            List<ComboBoxItem> cboData = new List<ComboBoxItem>();

            ComboBoxItem globalData = new ComboBoxItem
            {
                Value = 0,
                Text = "All"
            };
            cboData.Add(globalData);

            if (!isTableExist(con, "classes")) return cboData;

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from classes ORDER BY Name"
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                ComboBoxItem data = new ComboBoxItem
                {
                    Value = rdr.GetInt64(rdr.GetOrdinal("ID")),
                    Text = rdr.GetString(rdr.GetOrdinal("Name"))
                };

                cboData.Add(data);
            }

            rdr.Close();

            return cboData;
        }

        static public List<Categories.GridData> GetCategories(SQLiteConnection con)
        {
            List<Categories.GridData> gridData = new List<Categories.GridData>();

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from categories ORDER BY Name"
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                Categories.GridData data = new Categories.GridData
                {
                    ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                    Name = rdr.GetString(rdr.GetOrdinal("Name")),
                    StartKeyword = rdr.GetString(rdr.GetOrdinal("StartKeyword")),
                    EndKeyword = rdr.GetString(rdr.GetOrdinal("EndKeyword")),
                    AutoStop = rdr.GetInt32(rdr.GetOrdinal("AutoStop"))
                };

                gridData.Add(data);
            }

            rdr.Close();

            return gridData;
        }

        static public void DeleteCategory(SQLiteConnection con, string ID)
        {
            if (!int.TryParse(ID, out int idValue)) return;
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "DELETE FROM categories WHERE ID = @id"
            };
            cmd.Parameters.AddWithValue("@id", idValue);
            cmd.ExecuteNonQuery();
        }

        static public void SaveCategory(SQLiteConnection con, DataGridView dataGridView, DataGridViewRow row)
        {
            DataGridViewCell ID = row.Cells[dataGridView.Columns["ID"].Index];
            DataGridViewCell Name = row.Cells[dataGridView.Columns["Name"].Index];
            DataGridViewCell StartKeyword = row.Cells[dataGridView.Columns["StartKeyword"].Index];
            DataGridViewCell EndKeyword = row.Cells[dataGridView.Columns["EndKeyword"].Index];
            DataGridViewCell AutoStop = row.Cells[dataGridView.Columns["AutoStop"].Index];

            SQLiteCommand cmd = new SQLiteCommand(con);

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

        static public List<MiniViews.GridData> GetViews(SQLiteConnection con)
        {
            List<MiniViews.GridData> gridData = new List<MiniViews.GridData>();

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from miniviews ORDER BY SortOrder, Name"
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                MiniViews.GridData data = new MiniViews.GridData
                {
                    ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                    Name = rdr.IsDBNull(rdr.GetOrdinal("Name")) ? "" : rdr.GetString(rdr.GetOrdinal("Name")),
                    ActiveYn = rdr.IsDBNull(rdr.GetOrdinal("ActiveYn")) ? 1 : rdr.GetInt32(rdr.GetOrdinal("ActiveYn")),
                    StyleFilter = rdr.IsDBNull(rdr.GetOrdinal("StyleFilter")) ? "Normal" : rdr.GetString(rdr.GetOrdinal("StyleFilter")),
                    PositionX = rdr.IsDBNull(rdr.GetOrdinal("PositionX")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionX")),
                    PositionY = rdr.IsDBNull(rdr.GetOrdinal("PositionY")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionY")),
                    SortOrder = rdr.IsDBNull(rdr.GetOrdinal("SortOrder")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("SortOrder"))
                };

                gridData.Add(data);
            }

            rdr.Close();

            return gridData;
        }

        static public void DeleteView(SQLiteConnection con, string ID)
        {
            if (!int.TryParse(ID, out int idValue)) return;
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "DELETE FROM miniviews WHERE ID = @id"
            };
            cmd.Parameters.AddWithValue("@id", idValue);
            cmd.ExecuteNonQuery();
        }

        static public void SaveView(SQLiteConnection con, DataGridView dataGridView, DataGridViewRow row)
        {
            DataGridViewCell ID = row.Cells[dataGridView.Columns["ID"].Index];
            DataGridViewCell Name = row.Cells[dataGridView.Columns["Name"].Index];
            DataGridViewCell ActiveYn = row.Cells[dataGridView.Columns["ActiveYn"].Index];
            DataGridViewCell StyleFilter = row.Cells[dataGridView.Columns["StyleFilter"].Index];

            SQLiteCommand cmd = new SQLiteCommand(con);

            if (Convert.ToString(ID.Value) == "-1")
            {
                cmd.CommandText = "INSERT INTO miniviews (Name, ActiveYn, StyleFilter) VALUES (@name, @active, @style)";
                cmd.Parameters.AddWithValue("@name", Convert.ToString(Name.Value));
                cmd.Parameters.AddWithValue("@active", Convert.ToInt32(ActiveYn.Value));
                cmd.Parameters.AddWithValue("@style", Convert.ToString(StyleFilter.Value));
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            else
            {
                cmd.CommandText = "UPDATE miniviews SET Name = @name, ActiveYn = @active, StyleFilter = @style WHERE ID = @id";
                cmd.Parameters.AddWithValue("@name", Convert.ToString(Name.Value));
                cmd.Parameters.AddWithValue("@active", Convert.ToInt32(ActiveYn.Value));
                cmd.Parameters.AddWithValue("@style", Convert.ToString(StyleFilter.Value));
                cmd.Parameters.AddWithValue("@id", Convert.ToInt32(ID.Value));
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }

            // Update ID in Grid When INSERTing
            if (Convert.ToString(ID.Value) == "-1")
            {
                cmd.CommandText = "SELECT last_insert_rowid()";
                row.Cells[dataGridView.Columns["ID"].Index].Value = (long)cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Data class for view position information
        /// </summary>
        public class ViewPositionData
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public int PositionX { get; set; }
            public int PositionY { get; set; }
            public int SortOrder { get; set; }
            public int ActiveYn { get; set; }
            public string StyleFilter { get; set; }
        }

        /// <summary>
        /// Gets all view definitions from the database, ordered by SortOrder.
        /// </summary>
        static public List<ViewPositionData> GetViewPositions(SQLiteConnection con)
        {
            List<ViewPositionData> views = new List<ViewPositionData>();

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT ID, Name, PositionX, PositionY, SortOrder, ActiveYn, StyleFilter FROM miniviews ORDER BY SortOrder"
            };

            using (SQLiteDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    ViewPositionData data = new ViewPositionData
                    {
                        ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                        Name = rdr.IsDBNull(rdr.GetOrdinal("Name")) ? "" : rdr.GetString(rdr.GetOrdinal("Name")),
                        PositionX = rdr.IsDBNull(rdr.GetOrdinal("PositionX")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionX")),
                        PositionY = rdr.IsDBNull(rdr.GetOrdinal("PositionY")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionY")),
                        SortOrder = rdr.IsDBNull(rdr.GetOrdinal("SortOrder")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("SortOrder")),
                        ActiveYn = rdr.IsDBNull(rdr.GetOrdinal("ActiveYn")) ? 1 : rdr.GetInt32(rdr.GetOrdinal("ActiveYn")),
                        StyleFilter = rdr.IsDBNull(rdr.GetOrdinal("StyleFilter")) ? "Normal" : rdr.GetString(rdr.GetOrdinal("StyleFilter"))
                    };

                    views.Add(data);
                }
            }

            return views;
        }

        /// <summary>
        /// Saves view positions to the database by ID.
        /// </summary>
        static public void SaveViewPositions(SQLiteConnection con, Dictionary<int, Point> positions)
        {
            SQLiteCommand cmd = new SQLiteCommand(con);

            foreach (var kvp in positions)
            {
                cmd.CommandText = "UPDATE miniviews SET PositionX = @x, PositionY = @y WHERE ID = @id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@x", kvp.Value.X);
                cmd.Parameters.AddWithValue("@y", kvp.Value.Y);
                cmd.Parameters.AddWithValue("@id", kvp.Key);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Gets saved column widths for a grid from the database.
        /// Returns a dictionary keyed by ColumnName with Width values.
        /// </summary>
        static public Dictionary<string, int> GetColumnWidths(SQLiteConnection con, string gridName)
        {
            Dictionary<string, int> widths = new Dictionary<string, int>();

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT ColumnName, Width FROM grid_columns WHERE GridName = @grid"
            };
            cmd.Parameters.AddWithValue("@grid", gridName);

            using (SQLiteDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    string colName = rdr.GetString(0);
                    int width = rdr.GetInt32(1);
                    widths[colName] = width;
                }
            }

            return widths;
        }

        /// <summary>
        /// Gets saved column FillWeights for a grid from the database.
        /// Returns a dictionary keyed by ColumnName with FillWeight values.
        /// </summary>
        static public Dictionary<string, float> GetColumnFillWeights(SQLiteConnection con, string gridName)
        {
            var weights = new Dictionary<string, float>();

            if (!isFieldExist(con, "grid_columns", "FillWeight"))
                return weights;

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT ColumnName, FillWeight FROM grid_columns WHERE GridName = @grid"
            };
            cmd.Parameters.AddWithValue("@grid", gridName);

            using (SQLiteDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    string colName = rdr.GetString(0);
                    float fw = rdr.IsDBNull(1) ? 100f : (float)rdr.GetDouble(1);
                    weights[colName] = fw;
                }
            }

            return weights;
        }

        /// <summary>
        /// Saves column widths and FillWeights for a grid to the database.
        /// Saves all resizable columns (including hidden ones) so that
        /// compact/advanced view widths survive across sessions.
        /// </summary>
        static public void SaveColumnWidths(SQLiteConnection con, string gridName, DataGridView grid)
        {
            SQLiteCommand cmd = new SQLiteCommand(con);

            // Delete existing entries for this grid
            cmd.CommandText = "DELETE FROM grid_columns WHERE GridName = @grid";
            cmd.Parameters.AddWithValue("@grid", gridName);
            cmd.ExecuteNonQuery();

            // Insert current widths for all resizable columns (visible and hidden)
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Resizable == DataGridViewTriState.False)
                    continue;

                cmd.CommandText = "INSERT INTO grid_columns (GridName, ColumnName, Width, FillWeight) VALUES (@grid, @col, @width, @fw)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@grid", gridName);
                cmd.Parameters.AddWithValue("@col", col.Name);
                cmd.Parameters.AddWithValue("@width", col.Width);
                cmd.Parameters.AddWithValue("@fw", col.FillWeight);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Gets saved sort state for a grid from the database.
        /// Returns a list of (ColumnName, SortDirection) in sort order.
        /// </summary>
        static public List<Tuple<string, ListSortDirection>> GetSortState(SQLiteConnection con, string gridName)
        {
            var sorts = new List<Tuple<string, ListSortDirection>>();
            if (!isTableExist(con, "grid_sort_state")) return sorts;

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT ColumnName, SortDirection FROM grid_sort_state WHERE GridName = @grid ORDER BY SortOrder"
            };
            cmd.Parameters.AddWithValue("@grid", gridName);

            using (SQLiteDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    string colName = rdr.GetString(0);
                    int dir = rdr.GetInt32(1);
                    sorts.Add(Tuple.Create(colName, dir == 0 ? ListSortDirection.Ascending : ListSortDirection.Descending));
                }
            }

            return sorts;
        }

        /// <summary>
        /// Saves the current multi-column sort state for a grid to the database.
        /// </summary>
        static public void SaveSortState(SQLiteConnection con, string gridName, ListSortDescriptionCollection sortDescriptions)
        {
            if (!isTableExist(con, "grid_sort_state")) return;

            SQLiteCommand cmd = new SQLiteCommand(con);

            // Delete existing entries for this grid
            cmd.CommandText = "DELETE FROM grid_sort_state WHERE GridName = @grid";
            cmd.Parameters.AddWithValue("@grid", gridName);
            cmd.ExecuteNonQuery();

            if (sortDescriptions == null) return;

            // Insert each sort column in order
            for (int i = 0; i < sortDescriptions.Count; i++)
            {
                var desc = sortDescriptions[i];
                cmd.CommandText = "INSERT INTO grid_sort_state (GridName, ColumnName, SortDirection, SortOrder) VALUES (@grid, @col, @dir, @order)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@grid", gridName);
                cmd.Parameters.AddWithValue("@col", desc.PropertyDescriptor.Name);
                cmd.Parameters.AddWithValue("@dir", desc.SortDirection == ListSortDirection.Ascending ? 0 : 1);
                cmd.Parameters.AddWithValue("@order", i);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Saves runtime timer state (counts, remaining, button state) for a character.
        /// Uses INSERT OR REPLACE to upsert on the (TimerID, CharacterID) unique key.
        /// </summary>
        static public void SaveTimerStates(SQLiteConnection con, List<TimerState> states, string characterID)
        {
            if (!isTableExist(con, "timer_runtime_state")) return;

            SQLiteCommand cmd = new SQLiteCommand(con);

            foreach (var ts in states)
            {
                cmd.CommandText = "INSERT OR REPLACE INTO timer_runtime_state (TimerID, CharacterID, Remaining, ButtonState, Count, StartedAt, ActiveYn) VALUES (@timerID, @charID, @remaining, @btnState, @count, @startedAt, @activeYn)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@timerID", ts.TimerID);
                cmd.Parameters.AddWithValue("@charID", string.IsNullOrEmpty(characterID) ? (object)DBNull.Value : (object)characterID);
                cmd.Parameters.AddWithValue("@remaining", ts.Remaining ?? "");
                cmd.Parameters.AddWithValue("@btnState", ts.ButtonState ?? Timers.btnStart);
                cmd.Parameters.AddWithValue("@count", ts.Count);
                cmd.Parameters.AddWithValue("@startedAt", ts.IsRunning ? DateTime.UtcNow.ToString("o") : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@activeYn", ts.ActiveYn);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Loads saved timer runtime state for a character.
        /// Returns a dictionary keyed by TimerID.
        /// </summary>
        static public Dictionary<long, TimerState> LoadTimerStates(SQLiteConnection con, string characterID)
        {
            var result = new Dictionary<long, TimerState>();
            if (!isTableExist(con, "timer_runtime_state")) return result;

            SQLiteCommand cmd = new SQLiteCommand(con);

            if (string.IsNullOrEmpty(characterID))
            {
                cmd.CommandText = "SELECT * FROM timer_runtime_state WHERE CharacterID IS NULL";
            }
            else
            {
                cmd.CommandText = "SELECT * FROM timer_runtime_state WHERE CharacterID = @charID";
                cmd.Parameters.AddWithValue("@charID", characterID);
            }

            using (SQLiteDataReader rdr = cmd.ExecuteReader())
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
                    result[timerID] = ts;
                }
            }

            return result;
        }

        /// <summary>
        /// Clears saved timer state for a character (or all if characterID is null).
        /// </summary>
        static public void ClearTimerStates(SQLiteConnection con, string characterID)
        {
            if (!isTableExist(con, "timer_runtime_state")) return;

            SQLiteCommand cmd = new SQLiteCommand(con);

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
