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
     static public SQLiteConnection Connection()
        {
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string basePath = Path.GetDirectoryName(exePath);
        string newDbName = Path.Combine(basePath, "ThorneTimer.db");
        string oldDbName = Path.Combine(basePath, "EQTimer.db");

       bool newDatabase = false;
        // Migration: If ThorneTimer.db does not exist but EQTimer.db does, copy it
        if (!File.Exists(newDbName) && File.Exists(oldDbName))
        {
        File.Copy(oldDbName, newDbName);
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

                cmd.CommandText = "INSERT INTO settings(ID, ActiveCharacterID) VALUES(1, '', '" + voice + "', 8)";
                cmd.ExecuteNonQuery();
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
            }

            return con;
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

            return retValue;
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

            rdr.Close();

            return gridData;
        }

        static public Characters.GridData GetCharacter(SQLiteConnection con, string ID)
        {
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "SELECT * from characters WHERE ID = " + ID + " ORDER BY Name"
            };
            SQLiteDataReader rdr = cmd.ExecuteReader();

            Characters.GridData data = new Characters.GridData();

            while (rdr.Read())
            {
                data.ID = rdr.GetInt32(rdr.GetOrdinal("ID"));
                data.Name = rdr.GetString(rdr.GetOrdinal("Name"));
                data.LogFile = rdr.GetString(rdr.GetOrdinal("LogFile"));
                data.MiniViewX = rdr.GetInt32(rdr.GetOrdinal("MiniViewX"));
                data.MiniViewY = rdr.GetInt32(rdr.GetOrdinal("MiniViewY"));
            }

            rdr.Close();

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
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "DELETE FROM characters WHERE ID = " + ID
            };
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
            SQLiteCommand cmd = new SQLiteCommand(con)
            {
                CommandText = "DELETE FROM miniviews WHERE ID = " + ID
            };
            cmd.ExecuteNonQuery();
        }

        static public void SaveView(SQLiteConnection con, DataGridView dataGridView, DataGridViewRow row)
        {
            DataGridViewCell ID = row.Cells[dataGridView.Columns["ID"].Index];
            DataGridViewCell Name = row.Cells[dataGridView.Columns["Name"].Index];

            SQLiteCommand cmd = new SQLiteCommand(con);

            string sql = "";

            if (Convert.ToString(ID.Value) == "-1")
            {
                sql += "INSERT INTO miniviews ";
                sql += "(";
                sql += "Name,";
                sql += ") VALUES (";
                sql += "'" + Convert.ToString(Name.Value) + "', ";
                sql += ")";
            }
            else
            {
                sql += "UPDATE miniviews SET ";
                sql += "Name = '" + Convert.ToString(Name.Value) + "', ";
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
    }
}
