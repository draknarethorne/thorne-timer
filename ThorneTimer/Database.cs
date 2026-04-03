using System;
using System.Collections.Generic;
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
            return Path.Combine(basePath, "Data", "ThorneTimer.db");
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
        }

        // Migration: If target doesn't exist but ThorneTimer.db is next to exe (pre-Data layout), copy it
        if (!File.Exists(newDbName))
        {
        string legacyDb = Path.Combine(exePath, "ThorneTimer.db");
        if (File.Exists(legacyDb) && !string.Equals(Path.GetFullPath(legacyDb), Path.GetFullPath(newDbName), StringComparison.OrdinalIgnoreCase))
        {
            string directory = Path.GetDirectoryName(newDbName);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.Copy(legacyDb, newDbName);
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
                    CommandText = "CREATE TABLE timers(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, CategoryID INTEGER, StartKeyword TEXT, EndKeyword TEXT, WAVFile TEXT, Speech TEXT, Duration TEXT, ActiveYn INTEGER, CaseYn INTEGER, EndlessYn INTEGER)"
                };
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE characters(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, LogFile TEXT, MiniViewX INTEGER, MiniViewY INTEGER)";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE categories(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, StartKeyword TEXT, EndKeyword TEXT, AutoStop INTEGER)";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE settings(ID INTEGER PRIMARY KEY, ActiveCharacterID TEXT, ActiveVoice TEXT, MiniViewFontSize INTEGER, MiniViewWarnFore INTEGER, MiniViewWarnBack INTEGER, MiniViewWarnTime TEXT, MiniViewOpacity INTEGER, VoiceVolume INTEGER, VoiceRate INTEGER, VoiceEnabled INTEGER, MiniViewNormFore INTEGER, MiniViewNormBack INTEGER, MiniViewShowPing INTEGER, MiniViewPingFore INTEGER, MiniViewPingBack INTEGER, MiniViewPingTime TEXT, MiniViewBuffFore INTEGER, MiniViewBuffBack INTEGER)";
                cmd.ExecuteNonQuery();

                // Create miniviews table used by the UI
                cmd.CommandText = "CREATE TABLE miniviews(ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)";
                cmd.ExecuteNonQuery();

                // Insert default settings with sensible defaults for all known columns
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

                    cmd.CommandText = "UPDATE settings SET ActiveVoice = '" + voice + "' WHERE ID = 1";
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

                    cmd.CommandText = "UPDATE settings SET MiniViewWarnFore = " + Convert.ToString(Color.White.ToArgb()) + ", MiniViewWarnBack = " + Convert.ToString(Color.Red.ToArgb()) + ", MiniViewWarnTime = '00:30', MiniViewOpacity = 100, VoiceVolume = 100, VoiceRate = -2, MiniViewNormFore = " + Convert.ToString(Color.Black.ToArgb()) + ", MiniViewNormBack = " + Convert.ToString(Color.White.ToArgb()) + " WHERE ID = 1";
                    cmd.ExecuteNonQuery();
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

                    cmd.CommandText = "UPDATE settings SET MiniViewShowPing = 1, MiniViewPingFore = " + Convert.ToString(Color.LightGreen.ToArgb()) + ", MiniViewPingBack = " + Convert.ToString(Color.Black.ToArgb()) + ", MiniViewPingTime = '00:30' WHERE ID = 1";
                    cmd.ExecuteNonQuery();
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

                    cmd.CommandText = "UPDATE settings SET MiniViewBuffFore = " + Convert.ToString(Color.Orange.ToArgb()) + ", MiniViewBuffBack = " + Convert.ToString(Color.Black.ToArgb()) + " WHERE ID = 1";
                    cmd.ExecuteNonQuery();
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
                cmd.CommandText = "INSERT INTO miniviews (Name, ViewType, PositionX, PositionY, SortOrder) VALUES (@name, @type, @x, @y, @order)";

                // Normal view
                cmd.Parameters.AddWithValue("@name", "Normal Timers");
                cmd.Parameters.AddWithValue("@type", "Normal");
                cmd.Parameters.AddWithValue("@x", baseX);
                cmd.Parameters.AddWithValue("@y", baseY);
                cmd.Parameters.AddWithValue("@order", 1);
                cmd.ExecuteNonQuery();

                // Pet view (offset +200)
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@name", "Pet Timers");
                cmd.Parameters.AddWithValue("@type", "Pet");
                cmd.Parameters.AddWithValue("@x", baseX + 200);
                cmd.Parameters.AddWithValue("@y", baseY);
                cmd.Parameters.AddWithValue("@order", 2);
                cmd.ExecuteNonQuery();

                // Buff view (offset +400)
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@name", "Buff Timers");
                cmd.Parameters.AddWithValue("@type", "Buff");
                cmd.Parameters.AddWithValue("@x", baseX + 400);
                cmd.Parameters.AddWithValue("@y", baseY);
                cmd.Parameters.AddWithValue("@order", 3);
                cmd.ExecuteNonQuery();

                // Ping view (offset +1000)
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@name", "Ping Timers");
                cmd.Parameters.AddWithValue("@type", "Ping");
                cmd.Parameters.AddWithValue("@x", baseX + 1000);
                cmd.Parameters.AddWithValue("@y", baseY);
                cmd.Parameters.AddWithValue("@order", 4);
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

        static public void SetSetting(SQLiteConnection con, string column, string value)
        {
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "UPDATE settings SET " + column + " = '" + value + "'"
            };
            cmd.ExecuteNonQuery();
        }

        static public void SetSetting(SQLiteConnection con, string column, int value)
        {
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "UPDATE settings SET " + column + " = " + value + ""
            };
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

            SQLiteCommand cmd = new SQLiteCommand(con);
            string sql = "";
            if (Convert.ToString(ID.Value) == "-1")
            {
                sql += "INSERT INTO timers ";
                sql += "(";
                sql += "Name,";
                sql += "CategoryID,";
                sql += "StartKeyword,";
                sql += "EndKeyword,";
                sql += "WAVFile,";
                sql += "Speech,";
                sql += "Duration,";
                sql += "ActiveYn,";
                sql += "CaseYn,";
                sql += "EndlessYn";
                sql += ") VALUES (";
                sql += "'" + Convert.ToString(Name.Value) + "',";
                sql += "" + Convert.ToInt32(CategoryID.Value) + ",";
                sql += "'" + Convert.ToString(StartKeyword.Value) + "',";
                sql += "'" + Convert.ToString(EndKeyword.Value) + "',";
                sql += "'" + Convert.ToString(WAVFile.Value) + "',";
                sql += "'" + Convert.ToString(Speech.Value) + "',";
                sql += "'" + Convert.ToString(Duration.Value) + "',";
                sql += "" + Convert.ToInt32(ActiveYn.Value) + ",";
                sql += "" + Convert.ToInt32(CaseYn.Value) + ",";
                sql += "" + Convert.ToInt32(EndlessYn.Value) + "";
                sql += ")";
            } else
            {
                sql += "UPDATE timers SET ";
                sql += "Name = '" + Convert.ToString(Name.Value) + "',";
                sql += "CategoryID = " + Convert.ToInt32(CategoryID.Value) + ",";
                sql += "StartKeyword = '" + Convert.ToString(StartKeyword.Value) + "',";
                sql += "EndKeyword = '" + Convert.ToString(EndKeyword.Value) + "',";
                sql += "WAVFile = '" + Convert.ToString(WAVFile.Value) + "',";
                sql += "Speech = '" + Convert.ToString(Speech.Value) + "',";
                sql += "Duration = '" + Convert.ToString(Duration.Value) + "', ";
                sql += "ActiveYn = " + Convert.ToString(ActiveYn.Value) + ", ";
                sql += "CaseYn = " + Convert.ToString(CaseYn.Value) + ", ";
                sql += "EndlessYn = " + Convert.ToString(EndlessYn.Value) + " ";
                sql += "WHERE ID = " + Convert.ToString(ID.Value);
            }

            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

            // Update ID in Grid When INSERTing
            if (Convert.ToString(ID.Value) == "-1")
            {
                cmd.CommandText = "SELECT last_insert_rowid()";
                row.Cells[dataGridView.Columns["ID"].Index].Value = (long)cmd.ExecuteScalar();
            }
        }

        static public void DeleteTimer(SQLiteConnection con, string ID)
        {
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "DELETE FROM timers WHERE ID = " + ID
            };
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

            while (rdr.Read())
            {
                try
                {
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

            SQLiteCommand cmd = new SQLiteCommand(con);

            string sql = "";

            if (Convert.ToString(ID.Value) == "-1")
            {
                sql += "INSERT INTO characters ";
                sql += "(";
                sql += "Name,";
                sql += "LogFile,";
                sql += "MiniViewX,";
                sql += "MiniViewY";
                sql += ") VALUES (";
                sql += "'" + Convert.ToString(Name.Value) + "',";
                sql += "'" + Convert.ToString(LogFile.Value) + "',";
                sql += "" + Convert.ToInt32(MiniViewX.Value) + ",";
                sql += "" + Convert.ToInt32(MiniViewY.Value) + "";
                sql += ")";
            }
            else
            {
                sql += "UPDATE characters SET ";
                sql += "Name = '" + Convert.ToString(Name.Value) + "',";
                sql += "LogFile = '" + Convert.ToString(LogFile.Value) + "', ";
                sql += "MiniViewX = " + Convert.ToInt32(MiniViewX.Value) + ", ";
                sql += "MiniViewY = " + Convert.ToInt32(MiniViewY.Value) + " ";
                sql += "WHERE ID = " + Convert.ToString(ID.Value);
            }

            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

            // Update ID in Grid When INSERTing
            if (Convert.ToString(ID.Value) == "-1")
            {
                cmd.CommandText = "SELECT last_insert_rowid()";
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
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "DELETE FROM categories WHERE ID = " + ID
            };
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

            string sql = "";

            if (Convert.ToString(ID.Value) == "-1")
            {
                sql += "INSERT INTO categories ";
                sql += "(";
                sql += "Name,";
                sql += "StartKeyword,";
                sql += "EndKeyword,";
                sql += "AutoStop";
                sql += ") VALUES (";
                sql += "'" + Convert.ToString(Name.Value) + "', ";
                sql += "'" + Convert.ToString(StartKeyword.Value) + "', ";
                sql += "'" + Convert.ToString(EndKeyword.Value) + "', ";
                sql += "" + Convert.ToInt32(AutoStop.Value) + "";
                sql += ")";
            }
            else
            {
                sql += "UPDATE categories SET ";
                sql += "Name = '" + Convert.ToString(Name.Value) + "', ";
                sql += "StartKeyword = '" + Convert.ToString(StartKeyword.Value) + "', ";
                sql += "EndKeyword = '" + Convert.ToString(EndKeyword.Value) + "', ";
                sql += "AutoStop = " + Convert.ToInt32(AutoStop.Value) + " ";
                sql += "WHERE ID = " + Convert.ToString(ID.Value);
            }

            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

            // Update ID in Grid When INSERTing
            if (Convert.ToString(ID.Value) == "-1")
            {
                cmd.CommandText = "SELECT last_insert_rowid()";
                row.Cells[dataGridView.Columns["ID"].Index].Value = (long)cmd.ExecuteScalar();
            }
        }

        static public List<MiniViews.GridData> GetViews(SQLiteConnection con)
        {
            List<MiniViews.GridData> gridData = new List<MiniViews.GridData>();

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from miniviews ORDER BY Name"
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                MiniViews.GridData data = new MiniViews.GridData
                {
                    ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                    Name = rdr.GetString(rdr.GetOrdinal("Name"))
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

            SQLiteCommand cmd = new SQLiteCommand(con);

            if (Convert.ToString(ID.Value) == "-1")
            {
                cmd.CommandText = "INSERT INTO miniviews (Name) VALUES (@name)";
                cmd.Parameters.AddWithValue("@name", Convert.ToString(Name.Value));
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
            else
            {
                cmd.CommandText = "UPDATE miniviews SET Name = @name WHERE ID = @id";
                cmd.Parameters.AddWithValue("@name", Convert.ToString(Name.Value));
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
            public string ViewType { get; set; }
            public int PositionX { get; set; }
            public int PositionY { get; set; }
            public int SortOrder { get; set; }
        }

        /// <summary>
        /// Gets all view positions from the database, ordered by SortOrder.
        /// Returns a dictionary keyed by ViewType (Normal, Pet, Buff, Ping).
        /// </summary>
        static public Dictionary<string, ViewPositionData> GetViewPositions(SQLiteConnection con)
        {
            Dictionary<string, ViewPositionData> positions = new Dictionary<string, ViewPositionData>();

            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT ID, Name, ViewType, PositionX, PositionY, SortOrder FROM miniviews ORDER BY SortOrder"
            };

            using (SQLiteDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    ViewPositionData data = new ViewPositionData
                    {
                        ID = rdr.GetInt32(rdr.GetOrdinal("ID")),
                        Name = rdr.IsDBNull(rdr.GetOrdinal("Name")) ? "" : rdr.GetString(rdr.GetOrdinal("Name")),
                        ViewType = rdr.IsDBNull(rdr.GetOrdinal("ViewType")) ? "Normal" : rdr.GetString(rdr.GetOrdinal("ViewType")),
                        PositionX = rdr.IsDBNull(rdr.GetOrdinal("PositionX")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionX")),
                        PositionY = rdr.IsDBNull(rdr.GetOrdinal("PositionY")) ? 100 : rdr.GetInt32(rdr.GetOrdinal("PositionY")),
                        SortOrder = rdr.IsDBNull(rdr.GetOrdinal("SortOrder")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("SortOrder"))
                    };

                    // Use ViewType as key for easy lookup
                    if (!positions.ContainsKey(data.ViewType))
                    {
                        positions[data.ViewType] = data;
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// Saves view positions to the database.
        /// </summary>
        static public void SaveViewPositions(SQLiteConnection con, Dictionary<string, Point> positions)
        {
            SQLiteCommand cmd = new SQLiteCommand(con);

            foreach (var kvp in positions)
            {
                cmd.CommandText = "UPDATE miniviews SET PositionX = @x, PositionY = @y WHERE ViewType = @type";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@x", kvp.Value.X);
                cmd.Parameters.AddWithValue("@y", kvp.Value.Y);
                cmd.Parameters.AddWithValue("@type", kvp.Key);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
