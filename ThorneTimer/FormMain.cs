using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Media;
using System.Threading;
using System.Speech.Synthesis;
using System.Data.SQLite;
using System.Globalization;
using System.Diagnostics;

namespace ThorneTimer
{
    public partial class FormMain : Form
    {
        // Helper to safely parse int with fallback
        private int SafeParseInt(string value, int defaultValue)
        {
            if (int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }
        public FormMain()
        {
            InitializeComponent();

            // Resolve initial database: saved path > default (next to exe)
            string dbPath = Properties.Settings.Default.DatabasePath;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            {
                dbPath = Database.GetDefaultDatabasePath();
                Properties.Settings.Default.DatabasePath = dbPath;
                Properties.Settings.Default.Save();
            }
            con = Database.Connection(dbPath);
            AddToRecentDatabases(dbPath);
            UpdateTitleBar(dbPath);
        }

        int activeTimers = 0;
        int runningTimers = 0;

        string activeCharacterID;
        string activeVoice = "";
        int voiceEnabled = 1;

        int voiceVolume = 100;
        int voiceRate = -2;

        const string blankTime = "";
        const string noTime = "00:00:00";
        const string pingHour = "00:";

        const string btnStartParsingLog = "Start Parsing Log";
        const string btnStopParsingLog = "Stop Parsing Log";

        readonly MiniViews miniViews = new MiniViews();
        readonly List<TimerPlus> timers = new List<TimerPlus>();
        SQLiteConnection con;

        private void FormMain_Load(object sender, EventArgs e)
        {
            this.FormClosing += FormMain_FormClosing;
            txtWarningTime.LostFocus += WarningTime_LostFocus;
            txtPingTime.LostFocus += PingTime_LostFocus;

            this.RestoreWindowPosition();


            tbOpacity.Value = Math.Max(tbOpacity.Minimum, Math.Min(tbOpacity.Maximum, SafeParseInt(Database.GetSetting(con, "MiniViewOpacity"), 100)));
            miniViews.mvOpacity = tbOpacity.Value;
            tbFontSize.Value = Math.Max(tbFontSize.Minimum, Math.Min(tbFontSize.Maximum, SafeParseInt(Database.GetSetting(con, "MiniViewFontSize"), 8)));
            miniViews.mvFontSize = tbFontSize.Value;

            miniViews.mvNormForeColor = SafeParseInt(Database.GetSetting(con, "MiniViewNormFore"), Color.Black.ToArgb());
            lblNormPickFore.BackColor = Color.FromArgb(miniViews.mvNormForeColor);
            miniViews.mvNormBackColor = SafeParseInt(Database.GetSetting(con, "MiniViewNormBack"), Color.White.ToArgb());
            lblNormPickBack.BackColor = Color.FromArgb(miniViews.mvNormBackColor);

            miniViews.mvWarnForeColor = SafeParseInt(Database.GetSetting(con, "MiniViewWarnFore"), Color.White.ToArgb());
            lblWarnPickFore.BackColor = Color.FromArgb(miniViews.mvWarnForeColor);
            miniViews.mvWarnBackColor = SafeParseInt(Database.GetSetting(con, "MiniViewWarnBack"), Color.Red.ToArgb());
            lblWarnPickBack.BackColor = Color.FromArgb(miniViews.mvWarnBackColor);
            miniViews.mvWarnTime = Database.GetSetting(con, "MiniViewWarnTime");
            txtWarningTime.Text = miniViews.mvWarnTime;

            miniViews.mvShowPing = SafeParseInt(Database.GetSetting(con, "MiniViewShowPing"), 1);
            chkShowPing.Checked = miniViews.ShowPing();
            miniViews.mvPingForeColor = SafeParseInt(Database.GetSetting(con, "MiniViewPingFore"), Color.LightGreen.ToArgb());
            lblPingPickFore.BackColor = Color.FromArgb(miniViews.mvPingForeColor);
            miniViews.mvPingBackColor = SafeParseInt(Database.GetSetting(con, "MiniViewPingBack"), Color.Black.ToArgb());
            lblPingPickBack.BackColor = Color.FromArgb(miniViews.mvPingBackColor);
            miniViews.mvPingTime = Database.GetSetting(con, "MiniViewPingTime");
            txtPingTime.Text = miniViews.mvPingTime;

            miniViews.mvBuffForeColor = SafeParseInt(Database.GetSetting(con, "MiniViewBuffFore"), Color.Orange.ToArgb());
            lblBuffPickFore.BackColor = Color.FromArgb(miniViews.mvBuffForeColor);
            miniViews.mvBuffBackColor = SafeParseInt(Database.GetSetting(con, "MiniViewBuffBack"), Color.Black.ToArgb());
            lblBuffPickBack.BackColor = Color.FromArgb(miniViews.mvBuffBackColor);

            UpdateMiniAppearance();


            activeVoice = Database.GetSetting(con, "ActiveVoice");
            voiceVolume = SafeParseInt(Database.GetSetting(con, "VoiceVolume"), 100);
            voiceRate = SafeParseInt(Database.GetSetting(con, "VoiceRate"), -2);
            voiceEnabled = SafeParseInt(Database.GetSetting(con, "VoiceEnabled"), 1);
            chkVoiceEnabled.Checked = (voiceEnabled == 1);

            SetupActiveVoice();

            // grdTimers.CellValueChanged += new DataGridViewCellEventHandler(grdTimers_CellValueChanged);
            // grdTimers.CurrentCellDirtyStateChanged += new EventHandler(grdTimers_CurrentCellDirtyStateChanged);
            grdTimers.DataError += new DataGridViewDataErrorEventHandler(GrdTimers_DataError);

            activeCharacterID = Database.GetSetting(con, "ActiveCharacterID");

            //labelLogFile.Text = "Idle";
            if (Properties.Settings.Default.ParseLog)
            {
                StartLog();
            }

            if (Properties.Settings.Default.MiniView)
            {
                ShowMiniView();
            }

            SetupActiveCharacters();
            SetupTimerGrid();
            SetupCharacterGrid();
            SetupCategoriesGrid();

            UpdateMiniView();

            PopulateRecentDatabases();
        }

        private void UpdateTitleBar(string dbPath)
        {
            string dbName = Path.GetFileName(dbPath);
            string dbDir = Path.GetDirectoryName(dbPath);
            this.Text = "Thorne Timer - " + dbName + "  [" + dbDir + "]";
        }

        private void AddToRecentDatabases(string dbPath)
        {
            var recent = Properties.Settings.Default.RecentDatabases;
            if (recent == null)
            {
                recent = new System.Collections.Specialized.StringCollection();
                Properties.Settings.Default.RecentDatabases = recent;
            }

            // Remove if already present (so it moves to top)
            for (int i = recent.Count - 1; i >= 0; i--)
            {
                if (string.Equals(recent[i], dbPath, StringComparison.OrdinalIgnoreCase))
                {
                    recent.RemoveAt(i);
                }
            }

            // Insert at the front
            recent.Insert(0, dbPath);

            // Keep at most 10 entries
            while (recent.Count > 10)
            {
                recent.RemoveAt(recent.Count - 1);
            }

            Properties.Settings.Default.Save();
        }

        private void PopulateRecentDatabases()
        {
            openRecentToolStripMenuItem.DropDownItems.Clear();

            var recent = Properties.Settings.Default.RecentDatabases;
            if (recent == null || recent.Count == 0)
            {
                openRecentToolStripMenuItem.Enabled = false;
                return;
            }

            openRecentToolStripMenuItem.Enabled = true;
            foreach (string path in recent)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                ToolStripMenuItem item = new ToolStripMenuItem(path);
                item.Click += (s, ev) =>
                {
                    if (File.Exists(path))
                    {
                        OpenDatabase(path);
                    }
                    else
                    {
                        MessageBox.Show("Tome not found:\n" + path, "Tome Not Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };
                openRecentToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        private void OpenDatabase(string dbPath)
        {
            // Save current state before switching
            SaveDataTimers();
            SaveDataCharacters();
            SaveDataCategories();

            // Snapshot what was running so we can restore after reload
            bool wasParsingLog = (btnStartStopLog.Text == btnStopParsingLog);
            bool wasMiniViewActive = miniViews.MiniViewsActive();

            // Stop all running timers
            StopAllTimers();

            // Stop log parsing
            if (wasParsingLog)
            {
                StopLog();
            }

            // Hide mini views
            if (wasMiniViewActive)
            {
                HideMiniView();
            }

            // Remember previous database in case the new one fails
            string previousDbPath = Properties.Settings.Default.DatabasePath;

            // Close old connection
            try { con.Close(); } catch { }

            // Try to open the new database
            try
            {
                con = Database.Connection(dbPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to open the tome:\n" + dbPath +
                    "\n\n" + ex.Message,
                    "Invalid Tome",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Re-open the previous database so the app stays usable
                if (!string.IsNullOrEmpty(previousDbPath) && File.Exists(previousDbPath))
                {
                    con = Database.Connection(previousDbPath);
                }
                else
                {
                    con = Database.Connection(Database.GetDefaultDatabasePath());
                }

                ReloadFromDatabase();
                UpdateTitleBar(Properties.Settings.Default.DatabasePath);
                PopulateRecentDatabases();

                if (wasParsingLog) StartLog();
                if (wasMiniViewActive) ShowMiniView();
                return;
            }

            // Save the new path and add to recent list
            Properties.Settings.Default.DatabasePath = dbPath;
            AddToRecentDatabases(dbPath);

            // Reload all UI from new database
            ReloadFromDatabase();

            UpdateTitleBar(dbPath);
            PopulateRecentDatabases();

            // Restore log parsing and mini views to their previous state
            if (wasParsingLog)
            {
                StartLog();
            }

            if (wasMiniViewActive)
            {
                ShowMiniView();
            }
        }

        private void ReloadFromDatabase()
        {
            // Reload settings from new database
            tbOpacity.Value = Math.Max(tbOpacity.Minimum, Math.Min(tbOpacity.Maximum, SafeParseInt(Database.GetSetting(con, "MiniViewOpacity"), 100)));
            miniViews.mvOpacity = tbOpacity.Value;
            tbFontSize.Value = Math.Max(tbFontSize.Minimum, Math.Min(tbFontSize.Maximum, SafeParseInt(Database.GetSetting(con, "MiniViewFontSize"), 8)));
            miniViews.mvFontSize = tbFontSize.Value;

            miniViews.mvNormForeColor = SafeParseInt(Database.GetSetting(con, "MiniViewNormFore"), Color.Black.ToArgb());
            lblNormPickFore.BackColor = Color.FromArgb(miniViews.mvNormForeColor);
            miniViews.mvNormBackColor = SafeParseInt(Database.GetSetting(con, "MiniViewNormBack"), Color.White.ToArgb());
            lblNormPickBack.BackColor = Color.FromArgb(miniViews.mvNormBackColor);

            miniViews.mvWarnForeColor = SafeParseInt(Database.GetSetting(con, "MiniViewWarnFore"), Color.White.ToArgb());
            lblWarnPickFore.BackColor = Color.FromArgb(miniViews.mvWarnForeColor);
            miniViews.mvWarnBackColor = SafeParseInt(Database.GetSetting(con, "MiniViewWarnBack"), Color.Red.ToArgb());
            lblWarnPickBack.BackColor = Color.FromArgb(miniViews.mvWarnBackColor);
            miniViews.mvWarnTime = Database.GetSetting(con, "MiniViewWarnTime");
            txtWarningTime.Text = miniViews.mvWarnTime;

            miniViews.mvShowPing = SafeParseInt(Database.GetSetting(con, "MiniViewShowPing"), 1);
            chkShowPing.Checked = miniViews.ShowPing();
            miniViews.mvPingForeColor = SafeParseInt(Database.GetSetting(con, "MiniViewPingFore"), Color.LightGreen.ToArgb());
            lblPingPickFore.BackColor = Color.FromArgb(miniViews.mvPingForeColor);
            miniViews.mvPingBackColor = SafeParseInt(Database.GetSetting(con, "MiniViewPingBack"), Color.Black.ToArgb());
            lblPingPickBack.BackColor = Color.FromArgb(miniViews.mvPingBackColor);
            miniViews.mvPingTime = Database.GetSetting(con, "MiniViewPingTime");
            txtPingTime.Text = miniViews.mvPingTime;

            miniViews.mvBuffForeColor = SafeParseInt(Database.GetSetting(con, "MiniViewBuffFore"), Color.Orange.ToArgb());
            lblBuffPickFore.BackColor = Color.FromArgb(miniViews.mvBuffForeColor);
            miniViews.mvBuffBackColor = SafeParseInt(Database.GetSetting(con, "MiniViewBuffBack"), Color.Black.ToArgb());
            lblBuffPickBack.BackColor = Color.FromArgb(miniViews.mvBuffBackColor);

            UpdateMiniAppearance();

            activeVoice = Database.GetSetting(con, "ActiveVoice");
            voiceVolume = SafeParseInt(Database.GetSetting(con, "VoiceVolume"), 100);
            voiceRate = SafeParseInt(Database.GetSetting(con, "VoiceRate"), -2);
            voiceEnabled = SafeParseInt(Database.GetSetting(con, "VoiceEnabled"), 1);
            chkVoiceEnabled.Checked = (voiceEnabled == 1);

            activeCharacterID = Database.GetSetting(con, "ActiveCharacterID");

            // Unhook event handlers before tearing down grids to prevent
            // validation firing against columns that no longer exist.
            grdTimers.RowValidating -= ValidateRowTimers;
            grdCharacters.RowValidating -= ValidateRowCharacters;
            grdCharacters.CellClick -= grdCharacters_CellClick;
            grdCategories.RowValidating -= ValidateRowCategories;

            // Reload grids
            SetupActiveCharacters();
            grdTimers.DataSource = null;
            grdTimers.Columns.Clear();
            SetupTimerGrid();
            grdCharacters.DataSource = null;
            grdCharacters.Columns.Clear();
            SetupCharacterGrid();
            grdCategories.DataSource = null;
            grdCategories.Columns.Clear();
            SetupCategoriesGrid();

            UpdateMiniView();
        }

        private string GetDataDirectory()
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            return Path.Combine(Path.GetDirectoryName(exePath), "Data");
        }

        private void newDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string dataDir = GetDataDirectory();
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            SaveFileDialog dlg = new SaveFileDialog
            {
                Title = "New Tome",
                Filter = "Database files (*.db)|*.db",
                InitialDirectory = dataDir,
                FileName = "ThorneTimer.db",
                OverwritePrompt = true,
                AutoUpgradeEnabled = true
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                // If the file already exists, delete it so Connection() creates a fresh one
                if (File.Exists(dlg.FileName))
                {
                    File.Delete(dlg.FileName);
                }

                OpenDatabase(dlg.FileName);
            }
        }

        private void openDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string currentDir = Path.GetDirectoryName(
                Properties.Settings.Default.DatabasePath ?? Database.GetDefaultDatabasePath());

            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Open Tome",
                Filter = "Database files (*.db)|*.db|All files (*.*)|*.*",
                InitialDirectory = currentDir,
                DereferenceLinks = false,
                AutoUpgradeEnabled = true
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string selectedPath = dlg.FileName;
            string selectedName = Path.GetFileName(selectedPath);

            // EQTimer.db: migrate it into Data\ as ThorneTimer.db instead of opening in place
            if (string.Equals(selectedName, "EQTimer.db", StringComparison.OrdinalIgnoreCase))
            {
                string dataDir = GetDataDirectory();
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                string targetPath = Path.Combine(dataDir, "ThorneTimer.db");

                if (File.Exists(targetPath))
                {
                    DialogResult result = MessageBox.Show(
                        "A tome named \"ThorneTimer.db\" already exists in the Data folder.\n\n" +
                        "Yes \u2014 Replace the existing tome\n" +
                        "No \u2014 Save with a different name",
                        "Tome Already Exists",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                    if (result == DialogResult.Cancel)
                        return;

                    if (result == DialogResult.No)
                    {
                        // Let them pick a different name
                        SaveFileDialog saveDlg = new SaveFileDialog
                        {
                            Title = "Save Migrated Tome As",
                            Filter = "Database files (*.db)|*.db",
                            InitialDirectory = dataDir,
                            FileName = "ThorneTimer.db",
                            OverwritePrompt = true,
                            AutoUpgradeEnabled = true
                        };

                        if (saveDlg.ShowDialog() != DialogResult.OK)
                            return;

                        targetPath = saveDlg.FileName;
                    }
                    else
                    {
                        // Yes — replace the existing one
                        string currentDbPath = Properties.Settings.Default.DatabasePath ?? "";
                        if (string.Equals(Path.GetFullPath(currentDbPath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                        {
                            try { con.Close(); } catch { }
                        }

                        File.Delete(targetPath);
                    }
                }

                File.Copy(selectedPath, targetPath);

                MessageBox.Show(
                    "Your EQTimer tome has been migrated to:\n" + targetPath +
                    "\n\nAll your timers, characters, and settings have been preserved." +
                    "\nThe original EQTimer.db was not modified.",
                    "Tome Migrated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                OpenDatabase(targetPath);
                return;
            }

            // Any other database: open it directly wherever it lives
            OpenDatabase(selectedPath);
        }

        private void saveDatabaseAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string currentDbPath = Properties.Settings.Default.DatabasePath ?? Database.GetDefaultDatabasePath();
            string dataDir = GetDataDirectory();
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            SaveFileDialog dlg = new SaveFileDialog
            {
                Title = "Save Tome As",
                Filter = "Database files (*.db)|*.db",
                InitialDirectory = dataDir,
                FileName = Path.GetFileName(currentDbPath),
                OverwritePrompt = true,
                AutoUpgradeEnabled = true
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            // Don't allow saving over the currently open database
            if (string.Equals(Path.GetFullPath(dlg.FileName), Path.GetFullPath(currentDbPath), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "The tome is already open under that name.",
                    "Save Tome As",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Save any pending changes before copying
            SaveDataTimers();
            SaveDataCharacters();
            SaveDataCategories();

            // Copy the current database to the new location
            File.Copy(currentDbPath, dlg.FileName, true);

            // Switch to the new copy
            OpenDatabase(dlg.FileName);
        }

        private void FormMain_FormClosing(Object sender, FormClosingEventArgs e)
        {
            SaveProperties();

            SaveDataTimers();
            SaveDataCharacters();
            SaveDataCategories();

            if (tParseLog != null)
                tParseLog.Abort();
        }

        private void RestoreWindowPosition()
        {
            if (Properties.Settings.Default.HasSetDefaults)
            {
                this.WindowState = Properties.Settings.Default.WindowState;
                this.Location = Properties.Settings.Default.Location;
                this.Size = Properties.Settings.Default.Size;
            }
        }

        private void SaveProperties()
        {
            Properties.Settings.Default.WindowState = this.WindowState;

            if (this.WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.Location = this.Location;
                Properties.Settings.Default.Size = this.Size;
            }
            else
            {
                Properties.Settings.Default.Location = this.RestoreBounds.Location;
                Properties.Settings.Default.Size = this.RestoreBounds.Size;
            }

            Properties.Settings.Default.ParseLog = (bool)(btnStartStopLog.Text == btnStopParsingLog);
            Properties.Settings.Default.MiniView = miniViews.MiniViewsActive();
            if (grdTimers.SortedColumn != null)
            {
                Properties.Settings.Default.SortColumn = (grdTimers.SortedColumn.Name.Length > 0) ? grdTimers.SortedColumn.Name : "Name";
            }
            else
            {
                Properties.Settings.Default.SortColumn = "Name";
            }

            Properties.Settings.Default.HasSetDefaults = true;
            Properties.Settings.Default.Save();
        }

        void GrdTimers_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // (No need to write anything in here)
        }

        private void ResetTimersGridColumns()
        {
            int i = 1;
            grdTimers.Columns["ActiveYn"].DisplayIndex = i++;
            grdTimers.Columns["Name"].DisplayIndex = i++;
            grdTimers.Columns["Count"].DisplayIndex = i++;
            grdTimers.Columns["CategoryID"].DisplayIndex = i++;
            grdTimers.Columns["StartKeyword"].DisplayIndex = i++;
            grdTimers.Columns["EndKeyword"].DisplayIndex = i++;
            grdTimers.Columns["WAV"].DisplayIndex = i++;
            grdTimers.Columns["WAVFile"].DisplayIndex = i++;
            grdTimers.Columns["Speech"].DisplayIndex = i++;
            grdTimers.Columns["Duration"].DisplayIndex = i++;
            grdTimers.Columns["Remaining"].DisplayIndex = i++;
            grdTimers.Columns["CaseYn"].DisplayIndex = i++;
            grdTimers.Columns["EndlessYn"].DisplayIndex = i++;
            grdTimers.Columns["StartStop"].DisplayIndex = i++;

            grdTimers.Columns["ActiveYn"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["Name"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["CategoryID"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["StartKeyword"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["EndKeyword"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["WAV"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["WAVFile"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["Speech"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["Duration"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["Remaining"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["CaseYn"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["EndlessYn"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["StartStop"].SortMode = DataGridViewColumnSortMode.NotSortable;

            RepaintTimerGrid(true);
        }

        private void RepaintTimerGrid(bool changeColor)
        {
            try
            {
                // Reset the timer counters
                activeTimers = 0;
                runningTimers = 0;

                foreach (DataGridViewRow row in grdTimers.Rows)
                {
                    DataGridViewCell ActiveYn = (DataGridViewCell)row.Cells[grdTimers.Columns["ActiveYn"].Index];

                    if (Convert.ToInt32(ActiveYn.Value) == 1)
                    {
                        if (changeColor)
                        {
                            DataGridViewButtonCell btnCell = (DataGridViewButtonCell)row.Cells[grdTimers.Columns["StartStop"].Index];
                            if (Timers.PingTimer((string)btnCell.Value))
                            {
                                row.DefaultCellStyle.BackColor = Color.LightGreen;
                            }
                            else
                            {
                                row.DefaultCellStyle.BackColor = Color.White;
                            }
                        }

                        activeTimers++;

                        DataGridViewCell cellRemaining = (DataGridViewCell)row.Cells[grdTimers.Columns["Remaining"].Index];
                        String remainingText = (string)cellRemaining.Value + "";
                        if (remainingText.Length > 0)
                        {
                            runningTimers++;
                        }
                    }
                    else
                    {
                        if (changeColor)
                        {
                            row.DefaultCellStyle.BackColor = Color.LightPink;
                        }
                    }
                }

                String timerText = "Timers: " + grdTimers.RowCount + "   Active: " + activeTimers + "   Running: " + runningTimers;
                labelTimerCount.Invoke(new Action(() => labelTimerCount.Text = timerText));
                //labelTimerCount.Text = timerText;
            }
            catch
            {
            }
        }

        private void SetupActiveVoice()
        {
            // Initialize a new instance of the speech synthesizer.  
            using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
            {
                foreach (InstalledVoice voice in synthesizer.GetInstalledVoices(new CultureInfo("en-US")))
                {
                    VoiceInfo info = voice.VoiceInfo;

                    cboActiveVoice.Items.Add(info.Name);
                }
            }

            cboActiveVoice.SelectedItem = activeVoice;
        }

        private void SetupActiveCharacters()
        {
            string oldActiveCharacterID = activeCharacterID;

            cboActiveCharacter.DataSource = Database.GetActiveCharacters(con);

            foreach (ComboBoxItem item in (List<ComboBoxItem>)cboActiveCharacter.DataSource)
            {
                if (Convert.ToString(item.Value) == oldActiveCharacterID)
                {
                    cboActiveCharacter.SelectedItem = item;
                    break;
                }
            }
        }

        private void RefreshGridCategorySource()
        {
            DataGridViewComboBoxColumn cboCategory = (DataGridViewComboBoxColumn)grdTimers.Columns[grdTimers.Columns["CategoryID"].Index];
            cboCategory.DataSource = Database.GetGridCategories(con);
        }

        private void SetupTimerGrid()
        {
            grdTimers.Columns.Add("ID", "ID");
            grdTimers.Columns[0].DataPropertyName = grdTimers.Columns[0].Name;
            grdTimers.Columns[0].Visible = false;
            grdTimers.Columns[0].Width = 20;
            grdTimers.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;

            DataGridViewCheckBoxColumn chkActiveYn = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Active",
                Name = "ActiveYn",
                TrueValue = 1,
                FalseValue = 0
            };
            grdTimers.Columns.Add(chkActiveYn);
            grdTimers.Columns[1].DataPropertyName = grdTimers.Columns[1].Name;
            grdTimers.Columns[1].Width = 40;
            grdTimers.Columns[1].MinimumWidth = 40;
            grdTimers.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            grdTimers.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grdTimers.Columns.Add("Name", "Name");
            grdTimers.Columns[2].DataPropertyName = grdTimers.Columns[2].Name;

            DataGridViewComboBoxColumn cboCategory = new DataGridViewComboBoxColumn
            {
                HeaderText = "Category",
                Name = "CategoryID",
                DataPropertyName = "CategoryID",
                ValueType = typeof(ComboBoxItem),
                DisplayMember = "Text",
                ValueMember = "Value",
                DataSource = Database.GetGridCategories(con)
            };
            grdTimers.Columns.Add(cboCategory);

            grdTimers.Columns.Add("StartKeyword", "Start Keyword");
            grdTimers.Columns[4].DataPropertyName = grdTimers.Columns[4].Name;

            grdTimers.Columns.Add("EndKeyword", "End Keyword");
            grdTimers.Columns[5].DataPropertyName = grdTimers.Columns[5].Name;

            grdTimers.Columns.Add("WAVFile", "Sound");
            grdTimers.Columns[6].DataPropertyName = grdTimers.Columns[6].Name;
            grdTimers.Columns[6].Width = 70;

            grdTimers.Columns.Add("Speech", "Speech");
            grdTimers.Columns[7].DataPropertyName = grdTimers.Columns[7].Name;

            grdTimers.Columns.Add("Duration", "Duration");
            grdTimers.Columns[8].DataPropertyName = grdTimers.Columns[8].Name;
            grdTimers.Columns[8].Width = 60;
            grdTimers.Columns[8].MinimumWidth = 60;
            grdTimers.Columns[8].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grdTimers.Columns[8].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;

            grdTimers.Columns.Add("Remaining", "Remaining");
            grdTimers.Columns[9].DataPropertyName = grdTimers.Columns[9].Name;
            grdTimers.Columns[9].Width = 60;
            grdTimers.Columns[9].MinimumWidth = 60;
            grdTimers.Columns[9].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grdTimers.Columns[9].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;

            DataGridViewCheckBoxColumn chkCaseYn = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Case",
                Name = "CaseYn",
                TrueValue = 1,
                FalseValue = 0
            };
            grdTimers.Columns.Add(chkCaseYn);
            grdTimers.Columns[10].DataPropertyName = grdTimers.Columns[10].Name;
            grdTimers.Columns[10].Width = 40;
            grdTimers.Columns[10].MinimumWidth = 40;
            grdTimers.Columns[10].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            grdTimers.Columns[10].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            DataGridViewCheckBoxColumn chkEndlessYn = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Loop",
                Name = "EndlessYn",
                TrueValue = 1,
                FalseValue = 0
            };
            grdTimers.Columns.Add(chkEndlessYn);
            grdTimers.Columns[11].DataPropertyName = grdTimers.Columns[11].Name;
            grdTimers.Columns[11].Width = 40;
            grdTimers.Columns[11].MinimumWidth = 40;
            grdTimers.Columns[11].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            grdTimers.Columns[11].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            DataGridViewButtonColumn buttonWAV = new DataGridViewButtonColumn
            {
                HeaderText = "Play",
                Name = "WAV",
                Text = "...",
                UseColumnTextForButtonValue = true
            };
            grdTimers.Columns.Add(buttonWAV);
            grdTimers.Columns["WAV"].Width = 30;
            grdTimers.Columns["WAV"].MinimumWidth = 30;
            grdTimers.Columns["WAV"].DisplayIndex = grdTimers.Columns["WAVFile"].Index + 1;
            grdTimers.Columns["WAV"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;

            DataGridViewButtonColumn buttonColumn = new DataGridViewButtonColumn
            {
                HeaderText = "",
                Name = "StartStop",
                Text = "Start",
                UseColumnTextForButtonValue = true
            };
            grdTimers.Columns.Add(buttonColumn);
            grdTimers.Columns["StartStop"].Width = 50;
            grdTimers.Columns["StartStop"].MinimumWidth = 50;

            grdTimers.Columns.Add("Count", "Count");
            //grdTimers.Columns["Count"].DataPropertyName = grdTimers.Columns["Count"].Name;
            grdTimers.Columns["Count"].Width = 50;
            grdTimers.Columns["Count"].MinimumWidth = 50;
            grdTimers.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grdTimers.Columns["Count"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;

            grdTimers.DataSource = Database.GetTimers(con);

            grdTimers.RowValidating += ValidateRowTimers;

            string sortName = (Properties.Settings.Default.SortColumn.Length > 0) ? Properties.Settings.Default.SortColumn : "Name";
            DataGridViewColumn sortColumn = grdTimers.Columns[sortName];
            if (sortColumn != null)
            {
                grdTimers.Sort(sortColumn, ListSortDirection.Ascending);
            }

            ResetTimersGridColumns();
        }

        private void SetupCharacterGrid()
        {
            grdCharacters.Columns.Add("ID", "ID");
            grdCharacters.Columns[0].DataPropertyName = grdCharacters.Columns[0].Name;
            grdCharacters.Columns[0].Visible = false;
            grdCharacters.Columns.Add("Name", "Name");
            grdCharacters.Columns[1].DataPropertyName = grdCharacters.Columns[1].Name;
            grdCharacters.Columns[1].FillWeight = 100;
            grdCharacters.Columns.Add("LogFile", "Log File");
            grdCharacters.Columns[2].DataPropertyName = grdCharacters.Columns[2].Name;
            grdCharacters.Columns[2].Width = 600;
            grdCharacters.Columns[2].FillWeight = 300;
            grdCharacters.Columns.Add("MiniViewX", "MiniViewX");
            grdCharacters.Columns[3].DataPropertyName = grdCharacters.Columns[3].Name;
            grdCharacters.Columns[3].Visible = false;
            grdCharacters.Columns.Add("MiniViewY", "MiniViewY");
            grdCharacters.Columns[4].DataPropertyName = grdCharacters.Columns[4].Name;
            grdCharacters.Columns[4].Visible = false;

            grdCharacters.RowValidating += ValidateRowCharacters;

            DataGridViewButtonColumn buttonLogFile = new DataGridViewButtonColumn
            {
                HeaderText = "",
                Name = "LOG",
                Text = "...",
                UseColumnTextForButtonValue = true
            };
            grdCharacters.Columns.Add(buttonLogFile);

            grdCharacters.Columns["LOG"].Width = 30;
            grdCharacters.Columns["LOG"].MinimumWidth = 30;
            grdCharacters.Columns["LOG"].DisplayIndex = 3;

            grdCharacters.DataSource = Database.GetCharacters(con);

            grdCharacters.CellClick += new DataGridViewCellEventHandler(grdCharacters_CellClick);
        }

        private void SetupCategoriesGrid()
        {
            grdCategories.Columns.Add("ID", "ID");
            grdCategories.Columns[0].DataPropertyName = grdCategories.Columns[0].Name;
            grdCategories.Columns[0].Visible = false;
            grdCategories.Columns.Add("Name", "Name");
            grdCategories.Columns[1].Width = 100;
            grdCategories.Columns[1].FillWeight = 100;
            grdCategories.Columns[1].DataPropertyName = grdCategories.Columns[1].Name;
            grdCategories.Columns.Add("StartKeyword", "Start Keyword");
            grdCategories.Columns[2].DataPropertyName = grdCategories.Columns[2].Name;
            grdCategories.Columns[2].Width = 300;
            grdCategories.Columns[2].FillWeight = 300;
            grdCategories.Columns.Add("EndKeyword", "End Keyword");
            grdCategories.Columns[3].DataPropertyName = grdCategories.Columns[3].Name;
            grdCategories.Columns[3].Width = 300;
            grdCategories.Columns[3].FillWeight = 300;

            DataGridViewCheckBoxColumn chkActiveYn = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Auto Stop",
                Name = "AutoStop",
                TrueValue = 1,
                FalseValue = 0
            };
            grdCategories.Columns.Add(chkActiveYn);
            grdCategories.Columns[4].DataPropertyName = grdCategories.Columns[4].Name;
            grdCategories.Columns[4].Width = 50;
            grdCategories.Columns[4].MinimumWidth = 50;

            grdCategories.DataSource = Database.GetCategories(con);

            grdCategories.RowValidating += ValidateRowCategories;
        }

        void grdTimers_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            //if (grdTimers.IsCurrentCellDirty)
            //{
            //    // This fires the cell value changed handler below
            //    grdTimers.CommitEdit(DataGridViewDataErrorContexts.Commit);
            //}
        }

        private void grdTimers_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //if (e.RowIndex == -1) return;

            //DataGridViewComboBoxCell cb = (DataGridViewComboBoxCell)grdTimers.Rows[e.RowIndex].Cells[grdTimers.Columns["cboCategory"].Index];

            //if (cb.Value != null)
            //{
            //    DataGridViewRow row = grdTimers.Rows[e.RowIndex];

            //    grdTimers.CellValueChanged -= grdTimers_CellValueChanged;
            //    row.Cells[grdTimers.Columns["CategoryID"].Index].Value = cb.Value;
            //    grdTimers.CellValueChanged += grdTimers_CellValueChanged;
            //    grdTimers.Invalidate();
            //}
        }

        void ValidateRowCategories(object sender, DataGridViewCellCancelEventArgs data)
        {
            SaveDataCategories();
        }

        void ValidateRowCharacters(object sender, DataGridViewCellCancelEventArgs data)
        {
            SaveDataCharacters();
        }

        void SaveDataCategories()
        {
            for (int r = 0; r < grdCategories.Rows.Count; r++)
            {
                DataGridViewRow row = grdCategories.Rows[r];

                Database.SaveCategory(con, grdCategories, row);
            }

            RefreshGridCategorySource();
        }

        void SaveDataCharacters()
        {
            Characters.GridData character = Database.GetCharacter(con, activeCharacterID);

            for (int r = 0; r < grdCharacters.Rows.Count; r++)
            {
                DataGridViewRow row = grdCharacters.Rows[r];

                if (miniViews.MiniViewsActive())
                {
                    DataGridViewCell Name = row.Cells[grdCharacters.Columns["Name"].Index];
                    DataGridViewCell MiniViewX = row.Cells[grdCharacters.Columns["MiniViewX"].Index];
                    DataGridViewCell MiniViewY = row.Cells[grdCharacters.Columns["MiniViewY"].Index];

                    if (character.Name == Convert.ToString(Name.Value))
                    {
                        MiniViewX.Value = miniViews.MV().Location.X;
                        MiniViewY.Value = miniViews.MV().Location.Y;
                    }
                }

                Database.SaveCharacter(con, grdCharacters, row);
            }

            // Save all view positions to the miniviews table
            if (miniViews.MiniViewsActive())
            {
                Dictionary<string, Point> positions = miniViews.GetCurrentViewPositions();
                Database.SaveViewPositions(con, positions);
            }

            SetupActiveCharacters();
        }

        void SaveDataTimers()
        {
            for (int r = 0; r < grdTimers.Rows.Count; r++)
            {
                DataGridViewRow row = grdTimers.Rows[r];
                grdTimers.EndEdit();
                Database.SaveTimer(con, grdTimers, row);
            }

            RepaintTimerGrid(true);
        }

        void ValidateRowTimers(object sender, DataGridViewCellCancelEventArgs data)
        {
            DataGridViewRow row = grdTimers.Rows[data.RowIndex];

            if ((ValidDataTimers(row)) && (row.Index != 0))
            {
                SaveDataTimers();
            }

        }

        bool ValidDataTimers(DataGridViewRow row)
        {
            DataGridViewCell durationCell = row.Cells[grdTimers.Columns["Duration"].Index];

            if (!ValidDuration(durationCell))
            {
                return false;
            }

            return true;
        }

        bool ValidDuration(DataGridViewCell durationCell)
        {
            string durationText = (string)durationCell.Value + "";

            durationCell.ErrorText = "Invalid Duration. Use 'HH:MM:SS'";

            if (durationText.Length != 8)
            {
                return false;
            }

            if (durationText.Substring(2, 1) != ":" || durationText.Substring(5, 1) != ":")
            {
                return false;
            }

            string s1 = durationText.Substring(0, 2);
            string s2 = durationText.Substring(3, 2);
            string s3 = durationText.Substring(6, 2);
            bool r1 = int.TryParse(s1, out _);
            bool r2 = int.TryParse(s2, out _);
            bool r3 = int.TryParse(s3, out _);

            if (r1 == false || r2 == false || r3 == false)
            {
                return false;
            }

            durationCell.ErrorText = "";

            return true;
        }

        void FindWAVFile(int rowIndex)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*",
                InitialDirectory = Application.StartupPath + "\\Sounds",
                DereferenceLinks = false,
                AutoUpgradeEnabled = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                DataGridViewCell wavCell = (DataGridViewCell)grdTimers.Rows[rowIndex].Cells[grdTimers.Columns["WAVFile"].Index];

                foreach (string filename in openFileDialog.FileNames)
                {
                    wavCell.Value = Path.GetFileName(filename);
                    break;
                }
            }
        }

        void FindLogFile(int rowIndex)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Log files (*.txt)|*.txt|All files (*.*)|*.*",
                DereferenceLinks = false,
                AutoUpgradeEnabled = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                DataGridViewCell logFileCell = (DataGridViewCell)grdCharacters.Rows[rowIndex].Cells[grdCharacters.Columns["LogFile"].Index];

                foreach (string filename in openFileDialog.FileNames)
                {
                    logFileCell.Value = filename;// Path.GetFileName(filename);
                    break;
                }
            }
        }

        void grdTimers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex == grdTimers.Columns["WAV"].Index)
            {
                FindWAVFile(e.RowIndex);
            }
            else if (e.ColumnIndex == grdTimers.Columns["ActiveYn"].Index)
            {
                // TODO: Need to figure out what the hook is for after the cell changes
                //RepaintTimers(true);
            }
            else if (e.ColumnIndex == grdTimers.Columns["StartStop"].Index)
            {
                DataGridViewButtonCell btnCell = (DataGridViewButtonCell)grdTimers.Rows[e.RowIndex].Cells[e.ColumnIndex];

                if (Timers.TimerStopped((string)btnCell.Value))
                {
                    DataGridViewCell ActiveYn = (DataGridViewCell)grdTimers.Rows[e.RowIndex].Cells[grdTimers.Columns["ActiveYn"].Index];

                    if (Convert.ToInt32(ActiveYn.Value) == 1)
                    {
                        TriggerRowTimer(btnCell, e.RowIndex);
                    }
                    else
                    {
                        MessageBox.Show("Timer is not active.  Check the Active box to continue.", "Inactive Timer", MessageBoxButtons.OK);
                    }
                }
                else
                {
                    StopRowTimer(btnCell, e.RowIndex);
                }
            }
        }

        void grdCharacters_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Ignore clicks that are not on button cells. 
            if (e.RowIndex < 0 || (e.ColumnIndex != grdCharacters.Columns["LOG"].Index)) return;


            if (e.ColumnIndex == grdCharacters.Columns["LOG"].Index)
            {
                FindLogFile(e.RowIndex);
            }
        }

        private string GetDependentName(string endKeyword)
        {
            // Check timer that has a dependency identified
            string dependentName = endKeyword.Substring(1, endKeyword.Length - 1);

            int delayIndex = endKeyword.IndexOf("|");
            if (delayIndex > 0)
            {
                dependentName = endKeyword.Substring(1, delayIndex - 1);
            }

            return dependentName;
        }

        private double GetDependentDelay(string endKeyword)
        {
            // Check timer that has a dependency identified
            double delayMS = 15000;

            int delayIndex = endKeyword.IndexOf("|");
            if (delayIndex > 0)
            {
                string delayStr = endKeyword.Substring(delayIndex + 1, endKeyword.Length - delayIndex - 1);
                delayMS = Convert.ToDouble(delayStr) * 1000;
            }

            return delayMS;
        }

        private bool DependentTimer(string endKeyword)
        {
            // Check for dependency tag
            bool bOk = true;

            if (endKeyword.Length > 0)
            {
                string endChar = endKeyword.Substring(0, 1);
                if (endChar == "*")
                {
                    string dependentName = GetDependentName(endKeyword);
                    double delayMS = GetDependentDelay(endKeyword);

                    // Make sure enough time has elapsed
                    bOk = CheckDependentTimer(dependentName, delayMS);
                }
            }

            return bOk;
        }

        private bool CheckDependentTimer(string dependentName, double delayMS)
        {
            for (int r = 0; r < grdTimers.Rows.Count; r++)
            {
                DataGridViewRow row = grdTimers.Rows[r];
                DataGridViewCell cellStartStop = row.Cells[grdTimers.Columns["StartStop"].Index];

                if (Timers.TimerRunning((string)cellStartStop.Value))
                {
                    DataGridViewCell cellName = row.Cells[grdTimers.Columns["Name"].Index];
                    if ((string)cellName.Value == dependentName)
                    {
                        DataGridViewCell cellRemaining = row.Cells[grdTimers.Columns["Remaining"].Index];
                        DataGridViewCell cellDuration = row.Cells[grdTimers.Columns["Duration"].Index];
                        if (ValidDuration(cellDuration))
                        {
                            double remainingMS = TimerPlus.GetMilliseconds((string)cellRemaining.Value);
                            double durationMS = TimerPlus.GetMilliseconds((string)cellDuration.Value);
                            double elapsedMS = (durationMS - remainingMS);

                            if (elapsedMS > delayMS)
                            {
                                DataGridViewCell cellEndkeyword = row.Cells[grdTimers.Columns["EndKeyword"].Index];
                                string endKeyword = (string)cellEndkeyword.Value + "";
                                if (endKeyword.Length <= 0)
                                {
                                    return true;
                                }
                                else
                                {
                                    return DependentTimer(endKeyword);
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void ResetTimerCounts()
        {
            for (int r = 0; r < grdTimers.Rows.Count; r++)
            {
                DataGridViewCell countCell = grdTimers.Rows[r].Cells[grdTimers.Columns["Count"].Index];
                countCell.Value = null;
            }
            UpdateMiniView();
        }

        private bool BasicTimer(string endKeyword)
        {
            return (endKeyword.Length <= 0);
        }

        void TriggerRowTimer(DataGridViewButtonCell btnCell, int rowIndex)
        {
            DataGridViewCell cellEndkeyword = grdTimers.Rows[rowIndex].Cells[grdTimers.Columns["EndKeyword"].Index];

            // Check for any possible tags
            string endKeyword = (string)cellEndkeyword.Value + "";
            if (BasicTimer(endKeyword))
            {
                // No keyword, so just start the timer
                StartRowTimer(btnCell, rowIndex);
            }
            else
            {
                // Complex timer, check for dependency 
                if (DependentTimer(endKeyword))
                {
                    StartRowTimer(btnCell, rowIndex);
                }
            }
        }

        private delegate void StartRowTimerDelegate(DataGridViewButtonCell btnCell, int rowIndex);

        void StartRowTimer(DataGridViewButtonCell btnCell, int rowIndex)
        {
            if (InvokeRequired)
            {
                object[] parameters = new object[] { btnCell, rowIndex };
                var d = new StartRowTimerDelegate(StartRowTimer);
                this.Invoke(d, parameters);
                return;
            }

            DataGridViewCell durationCell = grdTimers.Rows[rowIndex].Cells[grdTimers.Columns["Duration"].Index];
            string durationText = (string)durationCell.Value + "";

            if (ValidDuration(durationCell))
            {
                DataGridViewCell countCell = grdTimers.Rows[rowIndex].Cells[grdTimers.Columns["Count"].Index];
                int counter = Convert.ToInt32(countCell.Value) + 1;
                countCell.Value = counter.ToString();

                btnCell.UseColumnTextForButtonValue = false;
                if (TimerPlus.GetMilliseconds(durationText) != 0)
                {
                    DataGridViewCell endKeywordCell = grdTimers.Rows[rowIndex].Cells[grdTimers.Columns["EndKeyword"].Index];
                    string endKeyword = (string)endKeywordCell.Value + "";
                    if (BuffTimer(endKeyword))
                    {
                        btnCell.Value = Timers.btnBuff;
                        StartTimer(rowIndex, grdTimers.Columns["Remaining"].Index, durationText, TimerPlus.TimerType.Buff);
                    }
                    else if (PetTimer(endKeyword))
                    {
                        btnCell.Value = Timers.btnPet;
                        StartTimer(rowIndex, grdTimers.Columns["Remaining"].Index, durationText, TimerPlus.TimerType.Pet);
                    }
                    else
                    {
                        btnCell.Value = Timers.btnStop;
                        StartTimer(rowIndex, grdTimers.Columns["Remaining"].Index, durationText, TimerPlus.TimerType.Normal);
                    }
                }
                else
                {
                    string pingText = pingHour + miniViews.mvPingTime;
                    if (TimerPlus.GetMilliseconds(pingText) != 0)
                    {
                        btnCell.Value = Timers.btnPing;
                        StartTimer(rowIndex, grdTimers.Columns["Remaining"].Index, pingText, TimerPlus.TimerType.Ping);
                    }

                    PlayTimerSounds(rowIndex);
                }
            }
        }

        private void KillAllTimers()
        {
            for (int r = 0; r < grdTimers.Rows.Count; r++)
            {
                DataGridViewButtonCell btnCell = (DataGridViewButtonCell)grdTimers.Rows[r].Cells[grdTimers.Columns["StartStop"].Index];
                StopRowTimer(btnCell, r);
            }
        }

        private void StopAllTimers()
        {
            for (int r = 0; r < grdTimers.Rows.Count; r++)
            {
                DataGridViewButtonCell btnCell = (DataGridViewButtonCell)grdTimers.Rows[r].Cells[grdTimers.Columns["StartStop"].Index];
                if ((string)btnCell.Value != Timers.btnStart)
                {
                    StopRowTimer(btnCell, r);
                }
            }
            UpdateMiniView();
        }

        void StopRowTimer(DataGridViewButtonCell btnCell, int rowIndex)
        {
            // Only Called if EndKeyword or Manually Stopped with Button
            btnCell.Value = Timers.btnStart;

            StopTimer(rowIndex, false);
        }

        private delegate void StartTimerDelegate(int row, int col, string duration, TimerPlus.TimerType theType);

        void StartTimer(int row, int col, string duration, TimerPlus.TimerType theType)
        {
            if (InvokeRequired)
            {
                object[] parameters = new object[] { row, col, duration, theType };
                var d = new StartTimerDelegate(StartTimer);
                this.Invoke(d, parameters);
                return;
            }

            DataGridViewCell cell = grdTimers.Rows[row].Cells[col];

            TimerPlus t1 = new TimerPlus
            {
                RowIndex = row,
                Interval = 1000, // 1 sec = 1000 ms
                ElapsedTime = 0,
                DurationTime = TimerPlus.GetMilliseconds(duration)
            };
            t1.TimerElapsed += TimerElapsed;
            t1.TimerExpired += TimerExpired;
            t1.TheType = theType;
            timers.Add(t1);

            cell.Value = t1.GetTimeRemaining();
            if (theType == TimerPlus.TimerType.Ping)
            {
                grdTimers.Rows[row].DefaultCellStyle.BackColor = Color.LightGreen;
                cell.Style.BackColor = Color.LightGreen;
            }
            else if (theType == TimerPlus.TimerType.Buff)
            {
                cell.Style.BackColor = Color.Orange;
            }
            else if (theType == TimerPlus.TimerType.Pet)
            {
                cell.Style.BackColor = Color.Orange;
            }
            else
            {
                cell.Style.BackColor = Color.Yellow;
            }

            t1.Start();

            RepaintTimerGrid(false);
            UpdateMiniView();
        }

        void StopTimer(int row, bool resetYn)
        {
            foreach (TimerPlus timer in timers)
            {
                if (timer.RowIndex == row)
                {
                    if (resetYn)
                    {
                        timer.Stop();
                        timer.ElapsedTime = 0;
                        timer.Start();
                    }
                    else
                    {
                        timer.Stop();
                        timers.Remove(timer);
                        timer.Dispose();
                    }
                    break;
                }
            }

            DataGridViewCell remainingCell = grdTimers.Rows[row].Cells[grdTimers.Columns["Remaining"].Index];
            remainingCell.Value = blankTime;

            grdTimers.Rows[row].DefaultCellStyle.BackColor = Color.White;
            remainingCell.Style.BackColor = Color.White;

            RepaintTimerGrid(false);
            UpdateMiniView();
        }

        void TimerExpired(object sender, TimerPlus e)
        {
            DataGridViewButtonCell btnCell = (DataGridViewButtonCell)grdTimers.Rows[e.RowIndex].Cells[grdTimers.Columns["StartStop"].Index];
            DataGridViewCell EndlessYn = grdTimers.Rows[e.RowIndex].Cells[grdTimers.Columns["EndlessYn"].Index];

            if (Convert.ToInt32(EndlessYn.Value) == 0)
            {
                btnCell.Value = Timers.btnStart;
                StopTimer(e.RowIndex, false);
            }
            else
            {
                StopTimer(e.RowIndex, true);
            }

            if (e.TheType != TimerPlus.TimerType.Ping)
            {
                PlayTimerSounds(e.RowIndex);
            }

            RepaintTimerGrid(false);
            UpdateMiniView();
        }

        void PlayTimerSounds(int row)
        {
            DataGridViewCell wavCell = (DataGridViewCell)grdTimers.Rows[row].Cells[grdTimers.Columns["WAVFile"].Index];
            string wavText = wavCell.Value + "";
            if (wavText.Length > 0)
            {
                SoundPlayer sp = new SoundPlayer(Application.StartupPath + "\\Sounds\\" + wavText);
                sp.Play();
            }

            DataGridViewCell speechCell = (DataGridViewCell)grdTimers.Rows[row].Cells[grdTimers.Columns["Speech"].Index];
            string speechText = speechCell.Value + "";

            if ((speechText.Length > 0) && (voiceEnabled == 1))
            {
                SpeechSynthesizer synth = new SpeechSynthesizer();

                if (activeVoice.Length > 0)
                {
                    synth.SelectVoice(activeVoice);
                }

                // Configure the audio output.   
                synth.SetOutputToDefaultAudioDevice();

                synth.Rate = voiceRate;
                synth.Volume = voiceVolume;

                // Speak a string.  
                synth.SpeakAsync(speechText);
            }
        }

        void TimerElapsed(object sender, TimerPlus e)
        {
            try
            {
                DataGridViewCell cell = grdTimers.Rows[e.RowIndex].Cells[grdTimers.Columns["Remaining"].Index];

                Color color = Color.White;
                if (e.ElapsedTime < e.DurationTime)
                {
                    if (e.TheType == TimerPlus.TimerType.Ping)
                    {
                        color = Color.LightGreen;
                    }
                    else if (e.TheType == TimerPlus.TimerType.Buff)
                    {
                        color = Color.Orange;
                    }
                    else if (e.TheType == TimerPlus.TimerType.Pet)
                    {
                        color = Color.Orange;
                    }
                    else
                    {
                        color = Color.Yellow;
                    }
                }

                cell.Style.BackColor = color;
                cell.Value = e.GetTimeRemaining();

                UpdateMiniView(false);
            }
            catch
            {
            }
        }

        System.Threading.Thread tParseLog;

        private void ToggleLog()
        {
            if (btnStartStopLog.Text == btnStartParsingLog)
            {
                StartLog();
            }
            else
            {
                StopLog();
            }
        }

        private void RestartLog()
        {
            if (btnStartStopLog.Text == btnStopParsingLog)
            {
                StopLog();
                StartLog();
            }
        }

        private void StartLog()
        {
            Characters.GridData character = Database.GetCharacter(con, activeCharacterID);

            string filePath = character.LogFile;

            if (filePath.Length > 0 && File.Exists(filePath))
            {
                btnStartStopLog.Text = "Stop Parsing Log";
                btnStartStopLog.BackColor = Color.LightGreen;
                labelLogFile.Text = filePath;

                // Process Events on Another Thread
                tParseLog = new Thread(new ThreadStart(ParseLog));
                tParseLog.Start();
            }
        }

        private void StopLog()
        {
            btnStartStopLog.Text = "Start Parsing Log";
            btnStartStopLog.BackColor = btnAddTimer.BackColor;
            btnStartStopLog.UseVisualStyleBackColor = true;
            labelLogFile.Text = "Idle";

            tParseLog.Abort();
        }

        private void btnStartStopLog_Click(object sender, EventArgs e)
        {
            ToggleLog();
        }

        public class ThreadSharedData
        {
            public List<TimerPlus> timers = new List<TimerPlus>();
        }

        private void ActivateCategoryTimers(Int32 ID, string ActiveYn)
        {
            for (int r = 0; r < grdTimers.Rows.Count; r++)
            {
                DataGridViewCell CategoryIDCell = (DataGridViewCell)grdTimers.Rows[r].Cells[grdTimers.Columns["CategoryID"].Index];

                if (Convert.ToInt32(CategoryIDCell.Value) == ID)
                {
                    DataGridViewCheckBoxCell ActiveYnCell = (DataGridViewCheckBoxCell)grdTimers.Rows[r].Cells[grdTimers.Columns["ActiveYn"].Index];
                    ActiveYnCell.Value = ActiveYn;

                    // Shut Off Any Running Timers about to go Inactive
                    if (ActiveYn == "0")
                    {
                        DataGridViewButtonCell btnCell = (DataGridViewButtonCell)grdTimers.Rows[r].Cells[grdTimers.Columns["StartStop"].Index];

                        StopRowTimer(btnCell, r);
                    }
                }
            }

            SaveDataTimers();
        }

        public void ProcessLogText(string chunk)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(ProcessLogText), new object[] { chunk });
                return;
            }

            // Process Categories
            for (int r = 0; r < grdCategories.Rows.Count; r++)
            {
                Int32 ID = Convert.ToInt32(grdCategories.Rows[r].Cells[grdCategories.Columns["ID"].Index].Value);

                string startKeyword = (string)grdCategories.Rows[r].Cells[grdCategories.Columns["StartKeyword"].Index].Value + "";
                if (chunk.Contains(startKeyword) && startKeyword.Length > 0)
                {
                    // Activate Timers of this Category
                    ActivateCategoryTimers(ID, "1");
                }
                else
                {
                    string endKeyword = (string)grdCategories.Rows[r].Cells[grdCategories.Columns["EndKeyword"].Index].Value + "";
                    DataGridViewCell AutoStopYn = (DataGridViewCell)grdCategories.Rows[r].Cells[grdCategories.Columns["AutoStop"].Index];

                    if (chunk.Contains(endKeyword) && endKeyword.Length > 0)
                    {
                        if (Convert.ToInt32(AutoStopYn.Value) == 1)
                        {
                            // De-Activate Timers of this Category
                            ActivateCategoryTimers(ID, "0");
                        }
                    }
                }
            }

            // Process Active Timers
            for (int r = 0; r < grdTimers.Rows.Count; r++)
            {
                DataGridViewCell ActiveYn = (DataGridViewCell)grdTimers.Rows[r].Cells[grdTimers.Columns["ActiveYn"].Index];
                DataGridViewCell CaseYn = (DataGridViewCell)grdTimers.Rows[r].Cells[grdTimers.Columns["CaseYn"].Index];
                DataGridViewButtonCell btnCell = (DataGridViewButtonCell)grdTimers.Rows[r].Cells[grdTimers.Columns["StartStop"].Index];

                if (Convert.ToInt32(ActiveYn.Value) == 1)
                {
                    // Force a refresh
                    //Application.DoEvents();

                    string startKeyword = (string)grdTimers.Rows[r].Cells[grdTimers.Columns["StartKeyword"].Index].Value + "";
                    string endKeyword = (string)grdTimers.Rows[r].Cells[grdTimers.Columns["EndKeyword"].Index].Value + "";

                    // TODO:  Redo this to check that endKeyword doesn't include "@#$*"
                    // TODO:  Redo this so that we don't check case sensitive and then sensitive

                    bool containsStart = chunk.IndexOf(startKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool containsEnd = chunk.IndexOf(endKeyword, StringComparison.OrdinalIgnoreCase) >= 0;

                    // Check if Case-Sensitive
                    if (Convert.ToInt32(CaseYn.Value) != 0)
                    {
                        containsStart = chunk.IndexOf(startKeyword, StringComparison.Ordinal) >= 0;
                        containsEnd = chunk.IndexOf(endKeyword, StringComparison.Ordinal) >= 0;
                    }

                    if (containsStart && startKeyword.Length > 0)
                    {
                        // Check to start a timer
                        if (Timers.TimerStopped((string)btnCell.Value))
                        {
                            TriggerRowTimer(btnCell, r);
                        }
                        else if (Timers.TimerRunning((string)btnCell.Value) && ResetTimer(endKeyword))
                        {
                            // Reset the timer since it has the reset tags present
                            StopRowTimer(btnCell, r);
                            StartRowTimer(btnCell, r);
                        }
                    }

                    if (containsEnd && endKeyword.Length > 0)
                    {
                        // Check to stop a running timer
                        if (Timers.TimerRunning((string)btnCell.Value))
                        {
                            StopRowTimer(btnCell, r);
                        }
                    }
                }
            }
        }

        private bool PetTimer(string endKeyword)
        {
            return CheckTimer(endKeyword, "#");
        }

        private bool BuffTimer(string endKeyword)
        {
            return CheckTimer(endKeyword, "@");
        }

        private bool ValidEndKeyword(string endKeyword)
        {
            return CheckTimer(endKeyword, "@#$*");
        }

        private bool CheckTimer(string endKeyword, string checkChar)
        {
            bool bReset = false;
            if (endKeyword.Length > 0)
            {
                string endChar = endKeyword.Substring(0, 1);
                if ((endChar == checkChar))
                {
                    bReset = true;
                }
            }
            return bReset;
        }

        private bool ResetTimer(string endKeyword)
        {
            return BuffTimer(endKeyword) || PetTimer(endKeyword);
        }

        private void ParseLog()
        {
            Characters.GridData character = Database.GetCharacter(con, activeCharacterID);

            string filePath = character.LogFile;

            if (filePath.Length > 0 && File.Exists(filePath))
            {
                var initialFileSize = new FileInfo(filePath).Length;
                var lastReadLength = initialFileSize;
                if (lastReadLength < 0) lastReadLength = 0;

                while (true)
                {
                    try
                    {
                        var fileSize = new FileInfo(filePath).Length;
                        if (fileSize > lastReadLength)
                        {
                            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                fs.Seek(lastReadLength, SeekOrigin.Begin);
                                var buffer = new byte[1024];

                                while (true)
                                {
                                    var bytesRead = fs.Read(buffer, 0, buffer.Length);
                                    lastReadLength += bytesRead;

                                    if (bytesRead == 0)
                                        break;

                                    var text = ASCIIEncoding.ASCII.GetString(buffer, 0, bytesRead);

                                    ProcessLogText(text);
                                }
                            }
                        }
                    }
                    catch
                    {
                    }

                    Thread.Sleep(100);
                }
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnDeleteTimer_Click(object sender, EventArgs e)
        {
            if (grdTimers.CurrentCell != null)
            {
                string deleteWarning = "Are you sure you want to delete this timer?";
                if (runningTimers > 0)
                {
                    deleteWarning = "Deleting this timer will also stop all running timers, are you sure?";
                }

                if (MessageBox.Show(deleteWarning, "Delete Timer", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.Yes)
                {
                    StopAllTimers();

                    DataGridViewCell idCell = grdTimers.Rows[grdTimers.CurrentCell.RowIndex].Cells[grdTimers.Columns["ID"].Index];
                    Database.DeleteTimer(con, Convert.ToString(idCell.Value));

                    grdTimers.DataSource = Database.GetTimers(con);

                    ResetTimersGridColumns();
                }
            }
        }

        private void btnAddTimer_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = grdTimers.CurrentRow;

            if (row == null || ValidDataTimers(row))
            {
                if (MessageBox.Show("Adding a timer will stop all running timers, are you sure?", "Add Timer", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.Yes)
                {
                    StopAllTimers();

                    SortableBindingList<Timers.GridData> data = Database.GetTimers(con);

                    Timers.GridData gd = new Timers.GridData
                    {
                        ID = -1
                    };
                    data.Add(gd);

                    grdTimers.DataSource = data;

                    ResetTimersGridColumns();

                    grdTimers.Rows[grdTimers.Rows.Count - 1].Cells[grdTimers.Columns["Duration"].Index].Value = noTime;
                    grdTimers.CurrentCell = grdTimers.Rows[grdTimers.Rows.Count - 1].Cells[grdTimers.Columns["Name"].Index];

                    grdTimers.BeginEdit(true);
                }
            }
        }

        private void btnDeleteCharacter_Click(object sender, EventArgs e)
        {
            if (grdCharacters.CurrentCell != null)
            {
                if (MessageBox.Show("Are you sure you want to delete this character?", "Delete Character", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.Yes)
                {
                    DataGridViewCell idCell = grdCharacters.Rows[grdCharacters.CurrentCell.RowIndex].Cells[grdCharacters.Columns["ID"].Index];
                    Database.DeleteCharacter(con, Convert.ToString(idCell.Value));

                    grdCharacters.DataSource = Database.GetCharacters(con);
                    SetupActiveCharacters();
                }
            }
        }

        private void btnAddCharacter_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = grdCharacters.CurrentRow;

            //if (row == null)
            {
                List<Characters.GridData> data = Database.GetCharacters(con);

                Characters.GridData gd = new Characters.GridData
                {
                    ID = -1,
                    MiniViewX = 100,
                    MiniViewY = 100
                };
                data.Add(gd);

                grdCharacters.DataSource = data;

                grdCharacters.CurrentCell = grdCharacters.Rows[grdCharacters.Rows.Count - 1].Cells[grdCharacters.Columns["Name"].Index];
                grdCharacters.BeginEdit(true);
            }
        }

        private void cboActiveCharacter_SelectedIndexChanged(object sender, EventArgs e)
        {
            activeCharacterID = (cboActiveCharacter.SelectedItem as ComboBoxItem).Value.ToString();

            Database.SetSetting(con, "ActiveCharacterID", activeCharacterID);

            RestartLog();
        }

        private void btnAddCategory_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = grdCategories.CurrentRow;

            //if (row == null)
            {
                List<Categories.GridData> data = Database.GetCategories(con);

                Categories.GridData gd = new Categories.GridData
                {
                    ID = -1
                };
                data.Add(gd);

                grdCategories.DataSource = data;

                grdCategories.CurrentCell = grdCategories.Rows[grdCategories.Rows.Count - 1].Cells[grdCategories.Columns["Name"].Index];
                grdCategories.BeginEdit(true);
            }
        }

        private void btnDeleteCategory_Click(object sender, EventArgs e)
        {
            if (grdCategories.CurrentCell != null)
            {
                if (MessageBox.Show("Are you sure you want to delete this category?", "Delete Category", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.Yes)
                {
                    DataGridViewCell idCell = grdCategories.Rows[grdCategories.CurrentCell.RowIndex].Cells[grdCategories.Columns["ID"].Index];
                    Database.DeleteCategory(con, Convert.ToString(idCell.Value));

                    grdCategories.DataSource = Database.GetCategories(con);
                    RefreshGridCategorySource();
                }
            }
        }

        private void btnAddView_Click(object sender, EventArgs e)
        {
            //miniViews.AddView(con, grdViews);
        }

        private void btnDeleteView_Click(object sender, EventArgs e)
        {
            //miniViews.DeleteView(con, grdViews);
        }

        private void cboActiveVoice_SelectedIndexChanged(object sender, EventArgs e)
        {
            string voice = (string)cboActiveVoice.SelectedItem;

            if (voice.Length > 0)
            {
                activeVoice = voice;

                Database.SetSetting(con, "ActiveVoice", activeVoice);
            }
        }

        private void ShowMiniView()
        {
            if (miniViews.CreateMiniViews(con, activeCharacterID))
            {
                btnMiniView.BackColor = Color.LightGreen;

                UpdateMiniView();
            }
        }

        private void UpdateMiniView(bool bForce=true)
        {
            miniViews.UpdateMiniTimers(grdTimers, bForce);
        }

        private void UpdateMiniAppearance()
        {
            miniViews.UpdateMiniAppearance();
        }

        private void HideMiniView()
        {
            if (miniViews.MiniViewsActive())
            {
                // Save positions while the views still exist
                SaveDataCharacters();

                miniViews.DestroyMiniViews();

                btnMiniView.BackColor = btnAddTimer.BackColor;
                btnMiniView.UseVisualStyleBackColor = true;
            }
        }

        private void btnMiniView_Click(object sender, EventArgs e)
        {
            if (miniViews.MiniViewsHidden())
            {
                ShowMiniView();
            }
            else
            {
                HideMiniView();
            }
        }

        private void tbFontSize_Scroll(object sender, EventArgs e)
        {
            miniViews.mvFontSize = tbFontSize.Value;
            Database.SetSetting(con, "MiniViewFontSize", miniViews.mvFontSize);
            UpdateMiniAppearance();
        }

        private void lblWarnPickFore_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = lblWarnPickFore.BackColor;
            colorDialog1.ShowDialog();
            lblWarnPickFore.BackColor = colorDialog1.Color;

            miniViews.mvWarnForeColor = lblWarnPickFore.BackColor.ToArgb();
            Database.SetSetting(con, "MiniViewWarnFore", miniViews.mvWarnForeColor);
            UpdateMiniAppearance();
        }

        private void lblWarnPickBack_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = lblWarnPickBack.BackColor;
            colorDialog1.ShowDialog();
            lblWarnPickBack.BackColor = colorDialog1.Color;

            miniViews.mvWarnBackColor = lblWarnPickBack.BackColor.ToArgb();
            Database.SetSetting(con, "MiniViewWarnBack", miniViews.mvWarnBackColor);
            UpdateMiniAppearance();
        }

        private void WarningTime_LostFocus(object sender, EventArgs e)
        {
            if (!ValidTime(txtWarningTime.Text))
            {
                MessageBox.Show("Invalid Warning Time Format. Use 'MM:SS'", "Error");

                tabCtrlMain.SelectedIndex = 3;
                txtWarningTime.Focus();
            }
            else
            {
                miniViews.mvWarnTime = txtWarningTime.Text;
                Database.SetSetting(con, "MiniViewWarnTime", miniViews.mvWarnTime);
                UpdateMiniAppearance();
            }
        }

        bool ValidTime(string theTime)
        {
            if (theTime.Length != 5)
            {
                return false;
            }

            if (theTime.Substring(2, 1) != ":")
            {
                return false;
            }

            string s1 = theTime.Substring(0, 2);
            string s2 = theTime.Substring(3, 2);
            bool r1 = int.TryParse(s1, out _);
            bool r2 = int.TryParse(s2, out _);

            if (r1 == false || r2 == false)
            {
                return false;
            }

            return true;
        }

        private void tbOpacity_Scroll(object sender, EventArgs e)
        {
            miniViews.mvOpacity = tbOpacity.Value;
            Database.SetSetting(con, "MiniViewOpacity", miniViews.mvOpacity);
            UpdateMiniAppearance();
        }

        private void tbVolume_Scroll(object sender, EventArgs e)
        {
            voiceVolume = tbVolume.Value;
            Database.SetSetting(con, "VoiceVolume", voiceVolume);
        }

        private void btnTestVolume_Click(object sender, EventArgs e)
        {
            SpeechSynthesizer synth = new SpeechSynthesizer();

            if (activeVoice.Length > 0)
            {
                synth.SelectVoice(activeVoice);
            }

            // Configure the audio output.   
            synth.SetOutputToDefaultAudioDevice();

            synth.Rate = voiceRate;
            synth.Volume = voiceVolume;

            // Speak a string.  
            synth.Speak("Test");
        }

        private void tbVoiceRate_Scroll(object sender, EventArgs e)
        {
            voiceRate = tbVoiceRate.Value;
            Database.SetSetting(con, "VoiceRate", voiceRate);
        }

        private void lblNormPickFore_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = lblNormPickFore.BackColor;
            colorDialog1.ShowDialog();
            lblNormPickFore.BackColor = colorDialog1.Color;

            miniViews.mvNormForeColor = lblNormPickFore.BackColor.ToArgb();
            Database.SetSetting(con, "MiniViewNormFore", miniViews.mvNormForeColor);
            UpdateMiniAppearance();
        }

        private void lblNormPickBack_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = lblNormPickBack.BackColor;
            colorDialog1.ShowDialog();
            lblNormPickBack.BackColor = colorDialog1.Color;

            miniViews.mvNormBackColor = lblNormPickBack.BackColor.ToArgb();
            Database.SetSetting(con, "MiniViewNormBack", miniViews.mvNormBackColor);
            UpdateMiniAppearance();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var aboutForm = new FormAbout
            {
                StartPosition = FormStartPosition.CenterParent
            };
            aboutForm.ShowDialog(this);
        }

        private void lblPingPickFore_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = lblPingPickFore.BackColor;
            colorDialog1.ShowDialog();
            lblPingPickFore.BackColor = colorDialog1.Color;

            miniViews.mvPingForeColor = lblPingPickFore.BackColor.ToArgb();
            Database.SetSetting(con, "MiniViewPingFore", miniViews.mvPingForeColor);
            UpdateMiniAppearance();
        }

        private void lblPingPickBack_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = lblPingPickBack.BackColor;
            colorDialog1.ShowDialog();
            lblPingPickBack.BackColor = colorDialog1.Color;

            miniViews.mvPingBackColor = lblPingPickBack.BackColor.ToArgb();
            Database.SetSetting(con, "MiniViewPingBack", miniViews.mvPingBackColor);
            UpdateMiniAppearance();
        }

        private void PingTime_LostFocus(object sender, EventArgs e)
        {
            if (!ValidTime(txtPingTime.Text))
            {
                MessageBox.Show("Invalid Ping Time Format. Use 'MM:SS'", "Error");

                tabCtrlMain.SelectedIndex = 3;
                txtPingTime.Focus();
            }
            else
            {
                miniViews.mvPingTime = txtPingTime.Text;
                Database.SetSetting(con, "MiniViewPingTime", miniViews.mvPingTime);
                UpdateMiniAppearance();
            }
        }

        private void chkShowPing_Click(object sender, EventArgs e)
        {
            miniViews.mvShowPing = Convert.ToInt32(chkShowPing.Checked);
            Database.SetSetting(con, "MiniViewShowPing", miniViews.mvShowPing);
            UpdateMiniAppearance();
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            StopAllTimers();
        }

        private void grdTimers_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (runningTimers > 0)
            {
                KillAllTimers();
            }
            RepaintTimerGrid(true);
        }

        private void lblBuffPickFore_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = lblBuffPickFore.BackColor;
            colorDialog1.ShowDialog();
            lblBuffPickFore.BackColor = colorDialog1.Color;

            miniViews.mvBuffForeColor = lblBuffPickFore.BackColor.ToArgb();
            Database.SetSetting(con, "MiniViewBuffFore", miniViews.mvBuffForeColor);
            UpdateMiniAppearance();
        }

        private void lblBuffPickBack_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = lblBuffPickBack.BackColor;
            colorDialog1.ShowDialog();
            lblBuffPickBack.BackColor = colorDialog1.Color;

            miniViews.mvBuffBackColor = lblBuffPickBack.BackColor.ToArgb();
            Database.SetSetting(con, "MiniViewBuffBack", miniViews.mvBuffBackColor);
            UpdateMiniAppearance();
        }

        private void btnResetCounts_Click(object sender, EventArgs e)
        {
            ResetTimerCounts();
        }

        private void chkVoiceEnabled_Click(object sender, EventArgs e)
        {
            voiceEnabled = Convert.ToInt32(chkVoiceEnabled.Checked);
            Database.SetSetting(con, "VoiceEnabled", voiceEnabled);
        }
    }
}
