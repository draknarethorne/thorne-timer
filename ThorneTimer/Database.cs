using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
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
                cmd.CommandText = "CREATE TABLE miniviews(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, PositionX INTEGER DEFAULT 100, PositionY INTEGER DEFAULT 100, ViewType TEXT DEFAULT 'Normal', SortOrder INTEGER DEFAULT 0, ActiveYn INTEGER DEFAULT 1, StyleFilter TEXT DEFAULT 'Normal', ForeColor INTEGER, BackColor INTEGER, ShowWarning INTEGER DEFAULT 1)";
                cmd.ExecuteNonQuery();

                // Seed default 7 views for new databases (v0.6.0: per-view colors)
                // Normal, Buffs, Pings, Character are Active by default (most commonly used)
                cmd.CommandText = @"INSERT INTO miniviews (Name, StyleFilter, ForeColor, BackColor, ShowWarning, PositionX, PositionY, SortOrder, ActiveYn) VALUES
                    ('Normal', 'Normal', -256, -16777216, 1, 100, 100, 1, 1),
                    ('Buffs', 'Buff', -23296, -16777216, 1, 400, 100, 2, 1),
                    ('Pets', 'Pet', -6684825, -16777216, 1, 700, 100, 3, 0),
                    ('Pings', 'Ping', -16711936, -16777216, 0, 100, 300, 4, 1),
                    ('Spawns', 'Spawn', -256, -16777216, 1, 400, 300, 5, 0),
                    ('Lockouts', 'Lockout', -23296, -16777216, 1, 700, 300, 6, 0),
                    ('Character', 'Character', -1, -16777216, 0, 100, 500, 7, 1)";
                cmd.ExecuteNonQuery();

                StylesRepository.EnsureSchema(con);

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

                // Add per-view color configuration columns (ForeColor, BackColor, ShowWarning)
                // v0.6.0: Move from global settings to per-view database-driven colors
                if (!isFieldExist(con, "miniviews", "ForeColor"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con);

                    // EmptyBehavior must exist before EnsureViewExists is invoked below,
                    // since the INSERT in EnsureViewExists references the EmptyBehavior column.
                    if (!isFieldExist(con, "miniviews", "EmptyBehavior"))
                    {
                        cmd.CommandText = "ALTER TABLE miniviews ADD COLUMN EmptyBehavior TEXT DEFAULT 'ViewName'";
                        cmd.ExecuteNonQuery();
                    }

                    // Add columns
                    cmd.CommandText = "ALTER TABLE miniviews ADD COLUMN ForeColor INTEGER DEFAULT NULL";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE miniviews ADD COLUMN BackColor INTEGER DEFAULT NULL";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE miniviews ADD COLUMN ShowWarning INTEGER DEFAULT 1";
                    cmd.ExecuteNonQuery();

                    // Migrate existing views with colors from old global settings
                    // Preserve user's custom colors by copying from settings table
                    cmd.CommandText = @"
                        UPDATE miniviews SET 
                            ForeColor = CASE StyleFilter
                                WHEN 'Normal' THEN (SELECT MiniViewNormFore FROM settings WHERE ID = 1)
                                WHEN 'Buff' THEN (SELECT MiniViewBuffFore FROM settings WHERE ID = 1)
                                WHEN 'Pet' THEN (SELECT MiniViewBuffFore FROM settings WHERE ID = 1)
                                WHEN 'Ping' THEN (SELECT MiniViewPingFore FROM settings WHERE ID = 1)
                                ELSE -256
                            END,
                            BackColor = CASE StyleFilter
                                WHEN 'Normal' THEN (SELECT MiniViewNormBack FROM settings WHERE ID = 1)
                                WHEN 'Buff' THEN (SELECT MiniViewBuffBack FROM settings WHERE ID = 1)
                                WHEN 'Pet' THEN (SELECT MiniViewBuffBack FROM settings WHERE ID = 1)
                                WHEN 'Ping' THEN (SELECT MiniViewPingBack FROM settings WHERE ID = 1)
                                ELSE -16777216
                            END,
                            ShowWarning = CASE StyleFilter
                                WHEN 'Ping' THEN 0
                                ELSE 1
                            END
                        WHERE ForeColor IS NULL";
                    cmd.ExecuteNonQuery();

                    // Seed new style views if they don't exist (no "Timers" suffix)
                    EnsureViewExists(con, "Spawns", "Spawn", -256, -16777216, 1, "HideEmpty", 400, 300, 10, 0);
                    EnsureViewExists(con, "Lockouts", "Lockout", -23296, -16777216, 1, "HideEmpty", 700, 300, 11, 0);
                    EnsureViewExists(con, "Character", "Character", -1, -16777216, 0, "CharacterName", 100, 500, 12, 1);

                    // Create separate Pet view if none exists
                    cmd.CommandText = "SELECT COUNT(*) FROM miniviews WHERE StyleFilter = 'Pet'";
                    long petCount = (long)cmd.ExecuteScalar();
                    if (petCount == 0)
                    {
                        EnsureViewExists(con, "Pets", "Pet", -6684825, -16777216, 1, "ViewName", 700, 100, 13, 0);
                    }
                }

                // Add EmptyBehavior column to miniviews table (v0.6.0)
                // Controls what displays when a view has no active timers:
                //   'CharacterName' - Show active character (e.g., "Gandalf")
                //   'ViewName' - Show view's Name field (e.g., "Normal", "Buffs")
                //   'Spaces' - Show empty spaces (invisible but positionable)
                //   'HideEmpty' - Hide window completely
                if (!isFieldExist(con, "miniviews", "EmptyBehavior"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con);

                    cmd.CommandText = "ALTER TABLE miniviews ADD COLUMN EmptyBehavior TEXT DEFAULT 'ViewName'";
                    cmd.ExecuteNonQuery();

                    // Set sensible defaults for existing views based on their StyleFilter:
                    // - Character view: Always shows character name
                    // - Lockout/Spawn: Hide when empty (episodic timers)
                    // - Normal/Buff/Ping/Pet: Show view name (always-visible gameplay info)
                    cmd.CommandText = @"
                        UPDATE miniviews SET EmptyBehavior = CASE StyleFilter
                            WHEN 'Character' THEN 'CharacterName'
                            WHEN 'Lockout' THEN 'HideEmpty'
                            WHEN 'Spawn' THEN 'HideEmpty'
                            ELSE 'ViewName'
                        END
                        WHERE EmptyBehavior IS NULL";
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
                        CommandText = "CREATE TABLE timer_runtime_state(ID INTEGER PRIMARY KEY AUTOINCREMENT, TimerID INTEGER NOT NULL, CharacterID INTEGER, Remaining TEXT, ButtonState TEXT, Count INTEGER DEFAULT 0, SavedAtUtc TEXT, ActiveYn INTEGER DEFAULT 1, UNIQUE(TimerID, CharacterID))"
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

                // Rename StartedAt → SavedAtUtc to reflect actual semantics
                // (it stores the UTC timestamp when the remaining-time snapshot was saved,
                //  not when the timer originally started)
                if (isTableExist(con, "timer_runtime_state") && isFieldExist(con, "timer_runtime_state", "StartedAt"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE timer_runtime_state RENAME COLUMN StartedAt TO SavedAtUtc"
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

                if (!isFieldExist(con, "settings", "ShowActiveOnly"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD ShowActiveOnly INTEGER DEFAULT 0"
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

                // One-time cleanup: deduplicate World timer rows (NULL CharacterID)
                // caused by SQLite treating NULL as distinct in UNIQUE constraints.
                // For each TimerID with multiple NULL-CharacterID rows, keep only the
                // one with the highest ID (most recent) and delete the rest.
                if (!isFieldExist(con, "settings", "NullRowsDeduped"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD NullRowsDeduped INTEGER DEFAULT 0"
                    };
                    cmd.ExecuteNonQuery();
                }

                {
                    string deduped = GetSetting(con, "NullRowsDeduped");
                    if (deduped == "0" || deduped == "")
                    {
                        SQLiteCommand cmd = new SQLiteCommand(con)
                        {
                            CommandText = "DELETE FROM timer_runtime_state WHERE CharacterID IS NULL AND ID NOT IN (SELECT MAX(ID) FROM timer_runtime_state WHERE CharacterID IS NULL GROUP BY TimerID)"
                        };
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "UPDATE settings SET NullRowsDeduped = 1";
                        cmd.ExecuteNonQuery();
                    }
                }

                // Add log settings columns
                if (!isFieldExist(con, "settings", "LogMinLevel"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD LogMinLevel TEXT DEFAULT 'Debug'"
                    };
                    cmd.ExecuteNonQuery();
                }

                if (!isFieldExist(con, "settings", "LogRetentionDays"))
                {
                    SQLiteCommand cmd = new SQLiteCommand(con)
                    {
                        CommandText = "ALTER TABLE settings ADD LogRetentionDays INTEGER DEFAULT 30"
                    };
                    cmd.ExecuteNonQuery();
                }

                StylesRepository.EnsureSchema(con);

                // Seed default views if miniviews table is empty
                SeedDefaultViews(con);
            }

            // Always ensure tome metadata (version stamps) — runs on every open,
            // both for new and existing tomes.
            EnsureMetaSchema(con);

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
        /// Creates a timestamped backup of the database file in a Backups subfolder
        /// next to the original.  Old backups are pruned using the tiered
        /// <see cref="ThorneArchive.PruneFiles"/> algorithm with the supplied
        /// <paramref name="retention"/> policy (or <see cref="RetentionPolicy.BackupDefaults"/>).
        /// Returns the backup path on success, or null on failure.
        /// </summary>
        static public string BackupDatabase(string dbPath, RetentionPolicy retention = null)
        {
            try
            {
                if (!File.Exists(dbPath)) return null;

                string dbDir = Path.GetDirectoryName(dbPath);
                string backupDir = Path.Combine(dbDir, "Backups");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                string baseName = Path.GetFileNameWithoutExtension(dbPath);
                string ext = Path.GetExtension(dbPath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(backupDir, $"{baseName}_{timestamp}{ext}");

                File.Copy(dbPath, backupPath, overwrite: true);

                // Tiered pruning of old backups
                var policy = retention ?? RetentionPolicy.BackupDefaults;
                ThorneArchive.PruneFiles(backupDir, $"{baseName}_*{ext}", policy);

                return backupPath;
            }
            catch
            {
                return null;
            }
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
        /// Ensures each style in the styles table has at least one paired mini-view,
        /// but ONLY when the miniviews table is completely empty (fresh DB or user
        /// explicitly cleared it). Once any views exist, this is a no-op so that
        /// user-deleted views are never silently re-added on startup. A future
        /// "Reset Defaults" action can call this explicitly.
        /// </summary>
        static private void SeedDefaultViews(SQLiteConnection con)
        {
            using (var checkCmd = new SQLiteCommand(con))
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM miniviews";
                long count = (long)checkCmd.ExecuteScalar();
                if (count > 0) return;
            }

            // Resolve base position from active character if available
            int baseX = 100;
            int baseY = 100;

            using (var cmd = new SQLiteCommand(con))
            {
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
            }

            // Read existing styles in sort order, then create one paired view per style.
            var styles = new StylesRepository(con).GetStyles();
            int slot = 0;
            foreach (StyleData style in styles)
            {
                if (string.IsNullOrWhiteSpace(style.Name)) continue;

                string viewName = DefaultViewNameForStyle(style.Name);
                string emptyBehavior = DefaultEmptyBehaviorForStyle(style.Name);

                // Column / row layout: 3 columns of 250px, rows 200px tall
                int x = baseX + (slot % 3) * 250;
                int y = baseY + (slot / 3) * 200;

                EnsureViewExists(con, viewName, style.Name,
                    style.ForeColor, style.BackColor, 1, emptyBehavior,
                    x, y, style.SortOrder, 1);

                slot++;
            }
        }

        static private string DefaultViewNameForStyle(string styleName)
        {
            switch (styleName)
            {
                case "Normal": return "Normal Timers";
                case "Buff": return "Buff Timers";
                case "Pet": return "Pet Timers";
                case "Ping": return "Ping Timers";
                case "Spawn": return "Spawns";
                case "Lockout": return "Lockouts";
                case "Character": return "Character";
                default: return styleName + " Timers";
            }
        }

        static private string DefaultEmptyBehaviorForStyle(string styleName)
        {
            switch (styleName)
            {
                case "Character": return "CharacterName";
                case "Spawn":
                case "Lockout":
                    return "HideEmpty";
                default: return "ViewName";
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

        /// <summary>
        /// Ensures a mini-view with the specified StyleFilter exists in the database.
        /// If no view exists for the given style, inserts a new one with default configuration.
        /// Used during database migration to seed new timer styles (Spawn, Lockout, Character, Pet).
        /// </summary>
        static private void EnsureViewExists(SQLiteConnection con, string name, string styleFilter,
                                            int foreColor, int backColor, int showWarning, string emptyBehavior,
                                            int x, int y, int sortOrder, int active)
        {
            using (var cmd = new SQLiteCommand(con))
            {
                // Check if view already exists for this style
                cmd.CommandText = "SELECT COUNT(*) FROM miniviews WHERE StyleFilter = @style";
                cmd.Parameters.AddWithValue("@style", styleFilter);
                long count = (long)cmd.ExecuteScalar();

                if (count == 0)
                {
                    // Insert new view with specified configuration
                    cmd.CommandText = @"INSERT INTO miniviews (Name, StyleFilter, ForeColor, BackColor, ShowWarning, EmptyBehavior,
                                       PositionX, PositionY, SortOrder, ActiveYn) 
                                       VALUES (@name, @style, @fore, @back, @warn, @empty, @x, @y, @order, @active)";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@style", styleFilter);
                    cmd.Parameters.AddWithValue("@fore", foreColor);
                    cmd.Parameters.AddWithValue("@back", backColor);
                    cmd.Parameters.AddWithValue("@warn", showWarning);
                    cmd.Parameters.AddWithValue("@empty", emptyBehavior);
                    cmd.Parameters.AddWithValue("@x", x);
                    cmd.Parameters.AddWithValue("@y", y);
                    cmd.Parameters.AddWithValue("@order", sortOrder);
                    cmd.Parameters.AddWithValue("@active", active);
                    cmd.ExecuteNonQuery();
                }
            }
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
            "ShowAllClasses", "ShowActiveOnly", "CompactView", "AutoSwitchEnabled",
            "CompactWidth", "FullWidth",
            "LogMinLevel", "LogRetentionDays"
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

        // ---------------------------------------------------------------
        // The CRUD/data-loading methods for timers, characters, categories,
        // views, and timer runtime state previously lived here.  They were
        // extracted in v0.6.0 into dedicated repository classes:
        //   • TimersRepository           (timers table)
        //   • CharactersRepository       (characters + classes lookup)
        //   • CategoriesRepository       (categories + category combo)
        //   • ViewsRepository            (miniviews + ViewPositionData)
        //   • TimerStateRepository       (timer_runtime_state table)
        //   • TomeStatisticsRepository   (Tome Information stats / db_meta)
        // Database.cs now owns only: connection, schema/migrations, settings,
        // grid layout persistence (column widths, sort state, fill weights),
        // and the db_meta key/value helpers used by Tome metadata stamping.
        // ---------------------------------------------------------------

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
        /// Current tome schema version.  Bump this when the on-disk schema
        /// changes in a way migrations care about.  v1 corresponds to v0.6.0.
        /// </summary>
        public const string CurrentSchemaVersion = "1";

        /// <summary>
        /// Ensures the db_meta key/value table exists and stamps the running
        /// application version (and schema version) into it.  CreatedByVersion
        /// is written once on first creation; LastWrittenByVersion is updated
        /// on every connection so users can see when the tome was last touched.
        /// </summary>
        static public void EnsureMetaSchema(SQLiteConnection con)
        {
            if (!isTableExist(con, "db_meta"))
            {
                using (var cmd = new SQLiteCommand(
                    "CREATE TABLE db_meta(Key TEXT PRIMARY KEY, Value TEXT)", con))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            string appVersion;
            try
            {
                var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                appVersion = v != null ? v.ToString() : "";
            }
            catch { appVersion = ""; }

            // Always stamp schema version + last-written version
            SetMetaValue(con, "SchemaVersion", CurrentSchemaVersion);
            SetMetaValue(con, "LastWrittenByVersion", appVersion);
            SetMetaValue(con, "LastWrittenAtUtc", DateTime.UtcNow.ToString("o"));

            // CreatedByVersion is a one-shot stamp
            if (string.IsNullOrEmpty(GetMetaValue(con, "CreatedByVersion")))
                SetMetaValue(con, "CreatedByVersion", appVersion);
        }

        /// <summary>
        /// Reads a db_meta value by key.  Returns null if the table or row is missing.
        /// </summary>
        static public string GetMetaValue(SQLiteConnection con, string key)
        {
            if (!isTableExist(con, "db_meta")) return null;
            try
            {
                using (var cmd = new SQLiteCommand("SELECT Value FROM db_meta WHERE Key = @k", con))
                {
                    cmd.Parameters.AddWithValue("@k", key);
                    object o = cmd.ExecuteScalar();
                    return o == null || o == DBNull.Value ? null : Convert.ToString(o);
                }
            }
            catch { return null; }
        }

        /// <summary>
        /// Upserts a db_meta key/value pair.
        /// </summary>
        static public void SetMetaValue(SQLiteConnection con, string key, string value)
        {
            try
            {
                using (var cmd = new SQLiteCommand(
                    "INSERT OR REPLACE INTO db_meta (Key, Value) VALUES (@k, @v)", con))
                {
                    cmd.Parameters.AddWithValue("@k", key);
                    cmd.Parameters.AddWithValue("@v", value ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }
    }
}
