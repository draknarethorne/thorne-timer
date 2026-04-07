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

            // Migrate user settings from previous version on first run after upgrade
            if (Properties.Settings.Default.NeedsUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.NeedsUpgrade = false;
                Properties.Settings.Default.Save();
                needsSizeNudge = true;
            }

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

        bool needsSizeNudge = false;

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

        const string startWatchingText = "Start Watching";
        const string stopWatchingText = "Stop Watching";

        const int DefaultFullViewWidth = 1400;
        const int DefaultCompactViewWidth = 800;

        int fullViewWidth = DefaultFullViewWidth;
        int compactViewWidth = DefaultCompactViewWidth;

        readonly MiniViews miniViews = new MiniViews();
        readonly TimerRuntime timerRuntime = new TimerRuntime();
        readonly LogMonitor logMonitor = new LogMonitor();
        SQLiteConnection con;

        Bitmap iconPlay;
        Bitmap iconStop;
        Bitmap iconMiniViews;
        Bitmap iconAutoSwitch;
        Bitmap iconAllClasses;
        Bitmap iconCompactView;

        private void CreateToolbarIcons()
        {
            // Green play triangle — Start Watching
            iconPlay = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconPlay))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(Color.FromArgb(0, 160, 0)))
                    g.FillPolygon(brush, new[] { new Point(4, 2), new Point(14, 8), new Point(4, 14) });
            }

            // Red stop square — Stop Watching
            iconStop = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconStop))
            {
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(Color.FromArgb(200, 30, 30)))
                    g.FillRectangle(brush, 3, 3, 10, 10);
            }

            // Mini Views icon — four small windows in a grid
            iconMiniViews = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconMiniViews))
            {
                g.Clear(Color.Transparent);
                using (var pen = new Pen(Color.FromArgb(80, 80, 80)))
                {
                    g.DrawRectangle(pen, 1, 1, 6, 5);
                    g.DrawRectangle(pen, 9, 1, 6, 5);
                    g.DrawRectangle(pen, 1, 8, 6, 5);
                    g.DrawRectangle(pen, 9, 8, 6, 5);
                }
            }

            // Auto-Switch icon — two curved arrows forming a cycle
            iconAutoSwitch = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconAutoSwitch))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var pen = new Pen(Color.FromArgb(0, 120, 180), 1.6f))
                {
                    // Top arc: left-to-right
                    g.DrawArc(pen, 3, 2, 10, 8, 180, 180);
                    // Bottom arc: right-to-left
                    g.DrawArc(pen, 3, 6, 10, 8, 0, 180);
                }
                // Arrowhead on top arc (right side, pointing right)
                using (var brush = new SolidBrush(Color.FromArgb(0, 120, 180)))
                    g.FillPolygon(brush, new[] { new Point(13, 3), new Point(13, 9), new Point(15, 6) });
                // Arrowhead on bottom arc (left side, pointing left)
                using (var brush = new SolidBrush(Color.FromArgb(0, 120, 180)))
                    g.FillPolygon(brush, new[] { new Point(3, 7), new Point(3, 13), new Point(1, 10) });
            }

            // All Classes icon — three person silhouettes
            iconAllClasses = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconAllClasses))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var purple = Color.FromArgb(100, 60, 160);
                using (var brush = new SolidBrush(purple))
                {
                    // Center person (head + body)
                    g.FillEllipse(brush, 6, 1, 4, 4);   // head
                    g.FillPie(brush, 4, 6, 8, 10, 180, 180); // shoulders

                    // Left person (slightly behind)
                    g.FillEllipse(brush, 1, 3, 3, 3);   // head
                    g.FillPie(brush, 0, 7, 6, 8, 180, 180); // shoulders

                    // Right person (slightly behind)
                    g.FillEllipse(brush, 12, 3, 3, 3);  // head
                    g.FillPie(brush, 10, 7, 6, 8, 180, 180); // shoulders
                }
            }

            // Compact View icon — three descending horizontal lines (collapsed columns)
            iconCompactView = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconCompactView))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var orange = Color.FromArgb(200, 120, 0);
                using (var pen = new Pen(orange, 2f))
                {
                    g.DrawLine(pen, 2, 4, 14, 4);
                    g.DrawLine(pen, 2, 8, 10, 8);
                    g.DrawLine(pen, 2, 12, 6, 12);
                }
            }

            // Set initial images
            tsbStartStopWatching.Image = iconPlay;
            startStopWatchingToolStripMenuItem.Image = iconPlay;
            tsbMiniViews.Image = iconMiniViews;
            miniViewsToolStripMenuItem.Image = iconMiniViews;
            tsbAutoSwitch.Image = iconAutoSwitch;
            autoSwitchToolStripMenuItem.Image = iconAutoSwitch;
            tsbShowAllClasses.Image = iconAllClasses;
            showAllClassesToolStripMenuItem.Image = iconAllClasses;
            tsbCompactView.Image = iconCompactView;
            compactViewToolStripMenuItem.Image = iconCompactView;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            CreateToolbarIcons();
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

            if (Properties.Settings.Default.ParseLog)
            {
                StartLog();
            }

            if (Properties.Settings.Default.MiniView)
            {
                ShowMiniView();
            }

            // Wire TimerRuntime events
            timerRuntime.TimerStateChanged += OnTimerStateChanged;
            timerRuntime.TimerSoundRequested += OnTimerSoundRequested;
            timerRuntime.CategoryTimersActivated += OnCategoryTimersActivated;

            // Wire LogMonitor character switch detection
            logMonitor.CharacterSwitched += OnCharacterSwitched;

            // Restore auto-switch setting
            bool autoSwitch = Database.GetSetting(con, "AutoSwitchEnabled") != "0";
            autoSwitchToolStripMenuItem.Checked = autoSwitch;
            tsbAutoSwitch.Checked = autoSwitch;
            logMonitor.AutoSwitchEnabled = autoSwitch;

            // Restore show-all-classes setting
            bool showAllClasses = Database.GetSetting(con, "ShowAllClasses") != "0";
            tsbShowAllClasses.Checked = showAllClasses;
            showAllClassesToolStripMenuItem.Checked = showAllClasses;
            timerRuntime.ShowAllClasses = showAllClasses;

            SetupActiveCharacters();
            SetupTimerGrid();
            SetupCharacterGrid();
            SetupCategoriesGrid();
            SetupViewsGrid();

            // Load timer and category data into TimerRuntime
            LoadTimerRuntime();

            // Apply class filter based on active character
            RefreshTimerGridDataSource();

            // Restore compact view setting
            compactViewWidth = SafeParseInt(Database.GetSetting(con, "CompactWidth"), this.Width < DefaultFullViewWidth ? this.Width : DefaultCompactViewWidth);
            fullViewWidth = SafeParseInt(Database.GetSetting(con, "FullWidth"), Math.Max(this.Width, DefaultFullViewWidth));
            bool compactView = Database.GetSetting(con, "CompactView") == "1";
            tsbCompactView.Checked = compactView;
            compactViewToolStripMenuItem.Checked = compactView;
            ApplyCompactView(compactView, initializing: true);

            // Restore persisted column widths
            LoadColumnWidths("Timers", grdTimers);
            LoadColumnWidths("Characters", grdCharacters);
            LoadColumnWidths("Categories", grdCategories);

            UpdateMiniView();

            PopulateRecentDatabases();
        }

        private void UpdateTitleBar(string dbPath)
        {
            string dbName = Path.GetFileName(dbPath);
            this.Text = "Thorne Timer - " + dbName;
            statusTomePath.Text = dbPath;
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
            SaveDataViews();

            // Snapshot what was running so we can restore after reload
            bool wasParsingLog = (tsbStartStopWatching.Text == stopWatchingText);
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

            // Restore toolbar toggle states from the new database
            bool autoSwitch = Database.GetSetting(con, "AutoSwitchEnabled") != "0";
            autoSwitchToolStripMenuItem.Checked = autoSwitch;
            tsbAutoSwitch.Checked = autoSwitch;
            logMonitor.AutoSwitchEnabled = autoSwitch;

            bool showAllClasses = Database.GetSetting(con, "ShowAllClasses") != "0";
            tsbShowAllClasses.Checked = showAllClasses;
            showAllClassesToolStripMenuItem.Checked = showAllClasses;
            timerRuntime.ShowAllClasses = showAllClasses;

            bool compactView = Database.GetSetting(con, "CompactView") == "1";
            tsbCompactView.Checked = compactView;
            compactViewToolStripMenuItem.Checked = compactView;

            // Unhook event handlers before tearing down grids to prevent
            // validation firing against columns that no longer exist.
            grdTimers.RowValidating -= ValidateRowTimers;
            grdCharacters.RowValidating -= ValidateRowCharacters;
            grdCharacters.CellClick -= grdCharacters_CellClick;
            grdCategories.RowValidating -= ValidateRowCategories;
            grdViews.RowValidating -= ValidateRowViews;

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
            grdViews.DataSource = null;
            grdViews.Columns.Clear();
            SetupViewsGrid();

            // Reload TimerRuntime with new database data
            LoadTimerRuntime();

            // Apply class filter based on active character
            RefreshTimerGridDataSource();

            // Re-apply compact view after grid rebuild
            ApplyCompactView(tsbCompactView.Checked);

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
                Filter = "Tome files (*.tdb)|*.tdb|Database files (*.db)|*.db",
                InitialDirectory = dataDir,
                FileName = "ThorneTimer.tdb",
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
                Filter = "Tome files (*.tdb)|*.tdb|Database files (*.db)|*.db|All files (*.*)|*.*",
                InitialDirectory = currentDir,
                DereferenceLinks = false,
                AutoUpgradeEnabled = true
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string selectedPath = dlg.FileName;
            string selectedName = Path.GetFileName(selectedPath);

            // EQTimer.db: migrate it into Data\ as ThorneTimer.tdb instead of opening in place
            if (string.Equals(selectedName, "EQTimer.db", StringComparison.OrdinalIgnoreCase))
            {
                string dataDir = GetDataDirectory();
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                string targetPath = Path.Combine(dataDir, "ThorneTimer.tdb");

                if (File.Exists(targetPath))
                {
                    DialogResult result = MessageBox.Show(
                        "A tome named \"ThorneTimer.tdb\" already exists in the Data folder.\n\n" +
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
                            Filter = "Tome files (*.tdb)|*.tdb|Database files (*.db)|*.db",
                            InitialDirectory = dataDir,
                            FileName = "ThorneTimer.tdb",
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
                Filter = "Tome files (*.tdb)|*.tdb|Database files (*.db)|*.db",
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
            SaveDataViews();

            // Copy the current database to the new location
            File.Copy(currentDbPath, dlg.FileName, true);

            // Switch to the new copy
            OpenDatabase(dlg.FileName);
        }

        private void FormMain_FormClosing(Object sender, FormClosingEventArgs e)
        {
            SaveProperties();

            // Persist column widths
            Database.SaveColumnWidths(con, "Timers", grdTimers);
            Database.SaveColumnWidths(con, "Characters", grdCharacters);
            Database.SaveColumnWidths(con, "Categories", grdCategories);

            SaveDataTimers();
            SaveDataCharacters();
            SaveDataCategories();
            SaveDataViews();

            // Save timer runtime state (counts + running timer info) for persistence
            var closingStates = timerRuntime.SaveCharacterState();
            Database.SaveTimerStates(con, closingStates, activeCharacterID);

            // Stop remaining timers (World-scope) and log monitor gracefully
            timerRuntime.StopAllTimers();
            logMonitor.Stop();
        }

        /// <summary>
        /// Returns true if the given rectangle is at least partially visible
        /// on any connected monitor. Returns false only when 100% of the
        /// window would be offscreen.
        /// </summary>
        static public bool IsVisibleOnAnyScreen(Rectangle rect)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(rect))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Ensures a window position is visible on at least one monitor.
        /// If the window is entirely offscreen, clamps it to 10 pixels
        /// inside the nearest screen's working area.
        /// </summary>
        static public Point EnsureVisibleOnScreen(Point location, Size size)
        {
            Rectangle windowRect = new Rectangle(location, size);
            if (IsVisibleOnAnyScreen(windowRect))
                return location;

            // Entirely offscreen — clamp to nearest screen with a small inset
            const int inset = 10;
            Screen nearest = Screen.FromPoint(location);
            Rectangle area = nearest.WorkingArea;
            int x = Math.Max(area.Left + inset, Math.Min(location.X, area.Right - size.Width - inset));
            int y = Math.Max(area.Top + inset, Math.Min(location.Y, area.Bottom - size.Height - inset));
            return new Point(x, y);
        }

        private void RestoreWindowPosition()
        {
            if (Properties.Settings.Default.HasSetDefaults)
            {
                this.WindowState = Properties.Settings.Default.WindowState;
                Point loc = Properties.Settings.Default.Location;
                Size sz = Properties.Settings.Default.Size;

                // One-time nudge on version upgrade: bump saved size up to
                // the new default so existing users see the improved layout.
                // Only fires once — after that their chosen size is respected.
                if (needsSizeNudge)
                {
                    bool isCompact = Database.GetSetting(con, "CompactView") == "1";
                    // Always nudge height; only nudge width for full-view users
                    // so compact-view users keep their narrower window.
                    sz = new Size(
                        isCompact ? sz.Width : Math.Max(sz.Width, DefaultFullViewWidth),
                        Math.Max(sz.Height, 700));
                }

                this.Location = EnsureVisibleOnScreen(loc, sz);
                this.Size = sz;
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

            Properties.Settings.Default.ParseLog = (bool)(tsbStartStopWatching.Text == stopWatchingText);
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

            // Persist compact/full view widths
            bool isCompact = tsbCompactView.Checked;
            if (this.WindowState == FormWindowState.Normal)
            {
                if (isCompact)
                    compactViewWidth = this.Width;
                else
                    fullViewWidth = this.Width;
            }
            Database.SetSetting(con, "CompactWidth", compactViewWidth);
            Database.SetSetting(con, "FullWidth", fullViewWidth);
        }

        /// <summary>
        /// Applies saved column widths from the database to a grid.
        /// Silently skips columns that no longer exist.
        /// </summary>
        private void LoadColumnWidths(string gridName, DataGridView grid)
        {
            try
            {
                Dictionary<string, int> widths = Database.GetColumnWidths(con, gridName);
                foreach (var kvp in widths)
                {
                    if (grid.Columns.Contains(kvp.Key))
                    {
                        DataGridViewColumn col = grid.Columns[kvp.Key];
                        if (col.Visible && kvp.Value >= col.MinimumWidth)
                        {
                            col.Width = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Database may not have the table yet; ignore
            }
        }

        void GrdTimers_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // (No need to write anything in here)
        }

        void GrdTimers_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is ComboBox)
            {
                e.CellStyle.BackColor = Color.White;
                e.CellStyle.ForeColor = Color.Black;
            }
        }

        private void ResetTimersGridColumns()
        {
            int i = 1;
            grdTimers.Columns["ActiveYn"].DisplayIndex = i++;
            grdTimers.Columns["Name"].DisplayIndex = i++;
            grdTimers.Columns["Count"].DisplayIndex = i++;
            grdTimers.Columns["CategoryID"].DisplayIndex = i++;
            grdTimers.Columns["Style"].DisplayIndex = i++;
            grdTimers.Columns["ClassID"].DisplayIndex = i++;
            grdTimers.Columns["Scope"].DisplayIndex = i++;
            grdTimers.Columns["StartKeyword"].DisplayIndex = i++;
            grdTimers.Columns["EndKeyword"].DisplayIndex = i++;
            grdTimers.Columns["WAV"].DisplayIndex = i++;
            grdTimers.Columns["WAVFile"].DisplayIndex = i++;
            grdTimers.Columns["Speech"].DisplayIndex = i++;
            grdTimers.Columns["Duration"].DisplayIndex = i++;
            grdTimers.Columns["Remaining"].DisplayIndex = i++;
            grdTimers.Columns["CaseYn"].DisplayIndex = i++;
            grdTimers.Columns["EndlessYn"].DisplayIndex = i++;
            grdTimers.Columns["DependsOnTimer"].DisplayIndex = i++;
            grdTimers.Columns["DependsOnDelay"].DisplayIndex = i++;
            grdTimers.Columns["StartStop"].DisplayIndex = i++;

            grdTimers.Columns["ActiveYn"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["Name"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["CategoryID"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["Style"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["ClassID"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["Scope"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["StartKeyword"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["EndKeyword"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["WAV"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["WAVFile"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["Speech"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["Duration"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["Remaining"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["CaseYn"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["EndlessYn"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["DependsOnTimer"].SortMode = DataGridViewColumnSortMode.Automatic;
            grdTimers.Columns["DependsOnDelay"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["StartStop"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void RepaintTimerGrid()
        {
            try
            {
                activeTimers = 0;
                runningTimers = 0;
                int visibleTimers = 0;
                int totalTimers = grdTimers.RowCount;

                foreach (DataGridViewRow row in grdTimers.Rows)
                {
                    if (!row.Visible) continue;

                    visibleTimers++;

                    if (Convert.ToInt32(row.Cells[grdTimers.Columns["ActiveYn"].Index].Value) == 1)
                    {
                        activeTimers++;

                        string remainingText = (string)row.Cells[grdTimers.Columns["Remaining"].Index].Value + "";
                        if (remainingText.Length > 0)
                        {
                            runningTimers++;
                        }
                    }
                }

                string timerText = "Timers: " + visibleTimers + "/" + totalTimers + "   Active: " + activeTimers + "   Running: " + runningTimers;

                statusTimerStats.GetCurrentParent()?.Invoke(new Action(() => statusTimerStats.Text = timerText));
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

            tscActiveCharacter.ComboBox.DataSource = Database.GetActiveCharacters(con);

            foreach (ComboBoxItem item in (List<ComboBoxItem>)tscActiveCharacter.ComboBox.DataSource)
            {
                if (Convert.ToString(item.Value) == oldActiveCharacterID)
                {
                    tscActiveCharacter.SelectedItem = item;
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
            grdTimers.Columns[2].Width = 140;
            grdTimers.Columns[2].MinimumWidth = 80;
            grdTimers.Columns[2].FillWeight = 60;

            DataGridViewComboBoxColumn cboCategory = new DataGridViewComboBoxColumn
            {
                HeaderText = "Category",
                Name = "CategoryID",
                DataPropertyName = "CategoryID",
                ValueType = typeof(ComboBoxItem),
                DisplayMember = "Text",
                ValueMember = "Value",
                DataSource = Database.GetGridCategories(con),
                FlatStyle = FlatStyle.Flat
            };
            grdTimers.Columns.Add(cboCategory);
            grdTimers.Columns["CategoryID"].Width = 100;
            grdTimers.Columns["CategoryID"].MinimumWidth = 60;

            grdTimers.Columns.Add("StartKeyword", "Start Keyword");
            grdTimers.Columns[4].DataPropertyName = grdTimers.Columns[4].Name;
            grdTimers.Columns[4].Width = 180;
            grdTimers.Columns[4].MinimumWidth = 60;

            grdTimers.Columns.Add("EndKeyword", "End Keyword");
            grdTimers.Columns[5].DataPropertyName = grdTimers.Columns[5].Name;
            grdTimers.Columns[5].Width = 120;
            grdTimers.Columns[5].MinimumWidth = 60;

            grdTimers.Columns.Add("WAVFile", "Sound");
            grdTimers.Columns[6].DataPropertyName = grdTimers.Columns[6].Name;
            grdTimers.Columns[6].Width = 100;
            grdTimers.Columns[6].MinimumWidth = 60;

            grdTimers.Columns.Add("Speech", "Speech");
            grdTimers.Columns[7].DataPropertyName = grdTimers.Columns[7].Name;
            grdTimers.Columns[7].Width = 100;
            grdTimers.Columns[7].MinimumWidth = 60;

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

            DataGridViewComboBoxColumn cboRole = new DataGridViewComboBoxColumn
            {
                HeaderText = "Style",
                Name = "Style",
                DataPropertyName = "Style",
                FlatStyle = FlatStyle.Flat
            };
            cboRole.Items.AddRange("Normal", "Buff", "Pet", "Ping");
            grdTimers.Columns.Add(cboRole);
            grdTimers.Columns["Style"].Width = 80;
            grdTimers.Columns["Style"].MinimumWidth = 60;

            DataGridViewComboBoxColumn cboClass = new DataGridViewComboBoxColumn
            {
                HeaderText = "Class",
                Name = "ClassID",
                DataPropertyName = "ClassID",
                ValueType = typeof(ComboBoxItem),
                DisplayMember = "Text",
                ValueMember = "Value",
                DataSource = Database.GetGridClasses(con),
                FlatStyle = FlatStyle.Flat
            };
            grdTimers.Columns.Add(cboClass);
            grdTimers.Columns["ClassID"].Width = 90;
            grdTimers.Columns["ClassID"].MinimumWidth = 60;

            DataGridViewComboBoxColumn cboScope = new DataGridViewComboBoxColumn
            {
                HeaderText = "Scope",
                Name = "Scope",
                DataPropertyName = "Scope",
                FlatStyle = FlatStyle.Flat
            };
            cboScope.Items.AddRange("Character", "World");
            grdTimers.Columns.Add(cboScope);
            grdTimers.Columns["Scope"].Width = 80;
            grdTimers.Columns["Scope"].MinimumWidth = 60;

            grdTimers.Columns.Add("DependsOnTimer", "Depends On");
            grdTimers.Columns["DependsOnTimer"].DataPropertyName = "DependsOnTimer";
            grdTimers.Columns["DependsOnTimer"].Width = 100;
            grdTimers.Columns["DependsOnTimer"].MinimumWidth = 60;

            grdTimers.Columns.Add("DependsOnDelay", "Delay (s)");
            grdTimers.Columns["DependsOnDelay"].DataPropertyName = "DependsOnDelay";
            grdTimers.Columns["DependsOnDelay"].Width = 55;
            grdTimers.Columns["DependsOnDelay"].MinimumWidth = 40;
            grdTimers.Columns["DependsOnDelay"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

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
            grdTimers.EditingControlShowing += GrdTimers_EditingControlShowing;

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

            DataGridViewComboBoxColumn cboCharClass = new DataGridViewComboBoxColumn
            {
                HeaderText = "Class",
                Name = "ClassID",
                DataPropertyName = "ClassID",
                ValueType = typeof(ComboBoxItem),
                DisplayMember = "Text",
                ValueMember = "Value",
                DataSource = Database.GetGridClasses(con),
                FlatStyle = FlatStyle.Flat
            };
            grdCharacters.Columns.Add(cboCharClass);
            grdCharacters.Columns["ClassID"].Width = 120;
            grdCharacters.Columns["ClassID"].MinimumWidth = 80;

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
            grdCharacters.Columns["LOG"].DisplayIndex = grdCharacters.Columns["LogFile"].Index + 1;

            // Position Class column after Name
            grdCharacters.Columns["ClassID"].DisplayIndex = 2;

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

        private void SetupViewsGrid()
        {
            grdViews.AllowUserToAddRows = false;
            grdViews.AllowUserToDeleteRows = false;

            grdViews.Columns.Add("ID", "ID");
            grdViews.Columns["ID"].DataPropertyName = "ID";
            grdViews.Columns["ID"].Visible = false;

            grdViews.Columns.Add("Name", "Name");
            grdViews.Columns["Name"].DataPropertyName = "Name";
            grdViews.Columns["Name"].Width = 200;
            grdViews.Columns["Name"].FillWeight = 200;

            DataGridViewComboBoxColumn cboStyle = new DataGridViewComboBoxColumn
            {
                HeaderText = "Style",
                Name = "StyleFilter",
                DataPropertyName = "StyleFilter",
                FlatStyle = FlatStyle.Flat
            };
            cboStyle.Items.AddRange("Normal", "Buff", "Pet", "Ping");
            grdViews.Columns.Add(cboStyle);
            grdViews.Columns["StyleFilter"].Width = 85;
            grdViews.Columns["StyleFilter"].MinimumWidth = 60;

            DataGridViewCheckBoxColumn chkActive = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Active",
                Name = "ActiveYn",
                DataPropertyName = "ActiveYn",
                TrueValue = (long)1,
                FalseValue = (long)0
            };
            grdViews.Columns.Add(chkActive);
            grdViews.Columns["ActiveYn"].Width = 50;
            grdViews.Columns["ActiveYn"].MinimumWidth = 50;

            // Hide position/sort columns — managed internally
            grdViews.Columns.Add("PositionX", "PositionX");
            grdViews.Columns["PositionX"].DataPropertyName = "PositionX";
            grdViews.Columns["PositionX"].Visible = false;
            grdViews.Columns.Add("PositionY", "PositionY");
            grdViews.Columns["PositionY"].DataPropertyName = "PositionY";
            grdViews.Columns["PositionY"].Visible = false;
            grdViews.Columns.Add("SortOrder", "SortOrder");
            grdViews.Columns["SortOrder"].DataPropertyName = "SortOrder";
            grdViews.Columns["SortOrder"].Visible = false;

            grdViews.DataSource = Database.GetViews(con);

            grdViews.RowValidating += ValidateRowViews;
        }

        void ValidateRowViews(object sender, DataGridViewCellCancelEventArgs data)
        {
            SaveDataViews();
            miniViews.RefreshMiniViews(con, activeCharacterID);
            UpdateMiniView();
        }

        void SaveDataViews()
        {
            for (int r = 0; r < grdViews.Rows.Count; r++)
            {
                DataGridViewRow row = grdViews.Rows[r];
                Database.SaveView(con, grdViews, row);
            }
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
                Dictionary<int, Point> positions = miniViews.GetCurrentViewPositions();
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

            RepaintTimerGrid();
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
                long timerID = Convert.ToInt64(grdTimers.Rows[e.RowIndex].Cells[grdTimers.Columns["ID"].Index].Value);
                // CellClick fires before the checkbox value flips, so the current value is the OLD state
                object cellValue = grdTimers.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                bool wasActive = cellValue != null && Convert.ToInt32(cellValue) == 1;
                timerRuntime.SetTimerActive(timerID, !wasActive);
                var ts = timerRuntime.GetState(timerID);
                if (ts != null)
                {
                    ApplyTimerRowColor(grdTimers.Rows[e.RowIndex], ts);
                }
                RepaintTimerGrid();
            }
            else if (e.ColumnIndex == grdTimers.Columns["StartStop"].Index)
            {
                long timerID = Convert.ToInt64(grdTimers.Rows[e.RowIndex].Cells[grdTimers.Columns["ID"].Index].Value);
                var ts = timerRuntime.GetState(timerID);
                if (ts == null) return;

                if (ts.IsStopped)
                {
                    if (ts.IsActive)
                    {
                        timerRuntime.StartTimer(timerID);
                    }
                    else
                    {
                        MessageBox.Show("Timer is not active.  Check the Active box to continue.", "Inactive Timer", MessageBoxButtons.OK);
                    }
                }
                else
                {
                    timerRuntime.StopTimer(timerID);
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

        private void ResetTimerCounts()
        {
            timerRuntime.ResetCounts();
            for (int r = 0; r < grdTimers.Rows.Count; r++)
            {
                DataGridViewCell countCell = grdTimers.Rows[r].Cells[grdTimers.Columns["Count"].Index];
                countCell.Value = null;
            }
            UpdateMiniView();
        }

        private void StopAllTimers()
        {
            timerRuntime.StopAllTimers();
            SyncRuntimeToGrid();
        }

        private void ToggleLog()
        {
            if (tsbStartStopWatching.Text == startWatchingText)
            {
                StartLog();
            }
            else
            {
                StopLog();
            }
        }

        private void StartLog()
        {
            // Build file state list from all registered characters
            var allCharacters = Database.GetCharacters(con);
            var fileStates = new List<CharacterFileState>();
            long activeID = 0;
            long.TryParse(activeCharacterID, out activeID);
            string activeFilePath = null;

            foreach (var c in allCharacters)
            {
                if (!string.IsNullOrEmpty(c.LogFile))
                {
                    fileStates.Add(new CharacterFileState
                    {
                        CharacterID = c.ID,
                        CharacterName = c.Name,
                        FilePath = c.LogFile
                    });

                    if (c.ID == activeID)
                    {
                        activeFilePath = c.LogFile;
                    }
                }
            }

            if (fileStates.Count > 0)
            {
                tsbStartStopWatching.Text = stopWatchingText;
                tsbStartStopWatching.Image = iconStop;
                startStopWatchingToolStripMenuItem.Text = "&Stop Watching";
                startStopWatchingToolStripMenuItem.Image = iconStop;
                statusParsing.Text = "Watching: " + (activeFilePath != null ? Path.GetFileName(activeFilePath) : "all characters");

                logMonitor.LogChunkReceived -= OnLogChunkReceived;
                logMonitor.LogChunkReceived += OnLogChunkReceived;
                logMonitor.Start(fileStates, activeID);
            }
        }

        private void StopLog()
        {
            tsbStartStopWatching.Text = startWatchingText;
            tsbStartStopWatching.Image = iconPlay;
            startStopWatchingToolStripMenuItem.Text = "&Start Watching";
            startStopWatchingToolStripMenuItem.Image = iconPlay;
            statusParsing.Text = "Idle";

            logMonitor.Stop();
        }

        private void OnLogChunkReceived(object sender, LogChunkReceivedEventArgs e)
        {
            timerRuntime.ProcessLogText(e.Text);
        }

        private void autoSwitchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool enabled = autoSwitchToolStripMenuItem.Checked;
            tsbAutoSwitch.Checked = enabled;
            logMonitor.AutoSwitchEnabled = enabled;
            Database.SetSetting(con, "AutoSwitchEnabled", enabled ? "1" : "0");
        }

        private void tsbAutoSwitch_Click(object sender, EventArgs e)
        {
            bool enabled = tsbAutoSwitch.Checked;
            autoSwitchToolStripMenuItem.Checked = enabled;
            logMonitor.AutoSwitchEnabled = enabled;
            Database.SetSetting(con, "AutoSwitchEnabled", enabled ? "1" : "0");
        }

        private void tsbShowAllClasses_Click(object sender, EventArgs e)
        {
            bool showAll = tsbShowAllClasses.Checked;
            showAllClassesToolStripMenuItem.Checked = showAll;
            timerRuntime.ShowAllClasses = showAll;
            Database.SetSetting(con, "ShowAllClasses", showAll ? "1" : "0");
            RefreshTimerGridDataSource();
            RepaintTimerGrid();
        }

        private void showAllClassesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool showAll = showAllClassesToolStripMenuItem.Checked;
            tsbShowAllClasses.Checked = showAll;
            timerRuntime.ShowAllClasses = showAll;
            Database.SetSetting(con, "ShowAllClasses", showAll ? "1" : "0");
            RefreshTimerGridDataSource();
            RepaintTimerGrid();
        }

        private void tsbCompactView_Click(object sender, EventArgs e)
        {
            bool compact = tsbCompactView.Checked;
            compactViewToolStripMenuItem.Checked = compact;
            Database.SetSetting(con, "CompactView", compact ? "1" : "0");
            ApplyCompactView(compact);
        }

        private void compactViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool compact = compactViewToolStripMenuItem.Checked;
            tsbCompactView.Checked = compact;
            Database.SetSetting(con, "CompactView", compact ? "1" : "0");
            ApplyCompactView(compact);
        }

        /// <summary>
        /// Columns hidden in compact mode — configuration-only columns
        /// that aren't needed during active play.
        /// </summary>
        private static readonly string[] CompactHiddenColumns = new[]
        {
            "StartKeyword", "EndKeyword", "WAVFile", "Speech",
            "CaseYn", "EndlessYn",
            "DependsOnTimer", "DependsOnDelay", "WAV"
        };

        /// <summary>
        /// Toggles visibility of configuration columns on the timer grid.
        /// Compact mode shows only: Active, Name, Category, Duration,
        /// Remaining, Start/Stop, Count.
        /// When switching to full view, auto-widens the window if it is
        /// too narrow for all columns, clamped to the current screen.
        /// </summary>
        private void ApplyCompactView(bool compact, bool initializing = false)
        {
            foreach (string colName in CompactHiddenColumns)
            {
                if (grdTimers.Columns.Contains(colName))
                {
                    grdTimers.Columns[colName].Visible = !compact;
                }
            }

            if (this.WindowState != FormWindowState.Normal) return;

            if (compact)
            {
                if (!initializing) fullViewWidth = this.Width;
                this.Width = compactViewWidth;
            }
            else
            {
                if (!initializing) compactViewWidth = this.Width;
                int targetWidth = Math.Max(fullViewWidth, DefaultFullViewWidth);
                Screen screen = Screen.FromControl(this);
                this.Width = Math.Min(targetWidth, screen.WorkingArea.Width);

                if (this.Right > screen.WorkingArea.Right)
                {
                    this.Left = Math.Max(screen.WorkingArea.Left,
                        screen.WorkingArea.Right - this.Width);
                }
            }
        }

        /// <summary>
        /// Gets the ClassID of the currently active character from the database.
        /// Returns 0 if no character is active or character has no class set.
        /// </summary>
        private long GetActiveCharacterClassID()
        {
            if (string.IsNullOrEmpty(activeCharacterID)) return 0;
            var character = Database.GetCharacter(con, activeCharacterID);
            return character.ClassID;
        }

        /// <summary>
        /// Refreshes the timer grid row visibility based on class filter.
        /// Called when ShowAllClasses toggle changes or after character switch.
        /// </summary>
        private void RefreshTimerGridDataSource()
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(RefreshTimerGridDataSource));
                return;
            }

            long classID = GetActiveCharacterClassID();
            bool showAll = timerRuntime.ShowAllClasses;

            // Detach the current cell before changing row visibility —
            // WinForms throws InvalidOperationException if you try to
            // hide the row the CurrencyManager is currently pointing to.
            grdTimers.CurrentCell = null;

            foreach (DataGridViewRow row in grdTimers.Rows)
            {
                if (row.IsNewRow) continue;

                if (showAll)
                {
                    row.Visible = true;
                }
                else
                {
                    long timerClassID = Convert.ToInt64(row.Cells[grdTimers.Columns["ClassID"].Index].Value);
                    // Global timers (ClassID=0) always visible.
                    // Class-specific timers visible only if they match the active character's class.
                    // If the character has no class set (classID=0), only global timers are shown.
                    row.Visible = (timerClassID == 0 || (classID > 0 && timerClassID == classID));
                }
            }

            // Restore selection to the first visible row
            foreach (DataGridViewRow row in grdTimers.Rows)
            {
                if (row.Visible && !row.IsNewRow)
                {
                    grdTimers.CurrentCell = row.Cells[grdTimers.Columns["Name"].Index];
                    break;
                }
            }
        }

        /// <summary>
        /// Handles auto-detected character switch from LogMonitor.
        /// Saves outgoing character state, switches active character,
        /// reloads timers, restores incoming character state.
        /// </summary>
        private void OnCharacterSwitched(object sender, CharacterSwitchedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnCharacterSwitched(sender, e)));
                return;
            }

            // Save outgoing character's timer state
            var outgoingStates = timerRuntime.SaveCharacterState();
            Database.SaveTimerStates(con, outgoingStates, activeCharacterID);

            // Update active character
            activeCharacterID = e.NewCharacterID.ToString();
            Database.SetSetting(con, "ActiveCharacterID", activeCharacterID);

            // Update the character dropdown without triggering SelectedIndexChanged again
            tscActiveCharacter.SelectedIndexChanged -= tscActiveCharacter_SelectedIndexChanged;
            foreach (ComboBoxItem item in (List<ComboBoxItem>)tscActiveCharacter.ComboBox.DataSource)
            {
                if (Convert.ToInt64(item.Value) == e.NewCharacterID)
                {
                    tscActiveCharacter.SelectedItem = item;
                    break;
                }
            }
            tscActiveCharacter.SelectedIndexChanged += tscActiveCharacter_SelectedIndexChanged;

            // Tell LogMonitor which character is now active
            logMonitor.SetActiveCharacter(e.NewCharacterID);

            // Reload timers and restore incoming character's state
            LoadTimerRuntime();

            // Apply class filter for new character
            RefreshTimerGridDataSource();

            // Update status bar
            statusParsing.Text = "Watching: " + Path.GetFileName(logMonitor.FilePath) + " (auto)";

            // Refresh mini views
            UpdateMiniView();
        }

        private void tsbStartStopWatching_Click(object sender, EventArgs e)
        {
            ToggleLog();
        }

        /// <summary>
        /// Loads timer and category data from the database into TimerRuntime.
        /// Restores persisted state (counts + Character-scope running timers)
        /// from timer_runtime_state.
        /// </summary>
        private void LoadTimerRuntime()
        {
            var timerData = Database.GetTimers(con);
            timerRuntime.LoadTimers(timerData);

            var catData = Database.GetCategories(con);
            timerRuntime.LoadCategories(catData);

            // Restore persisted Character-scope timer state
            var savedStates = Database.LoadTimerStates(con, activeCharacterID);
            timerRuntime.RestoreCharacterState(savedStates);

            // Sync to grid
            SyncRuntimeToGrid();
        }

        /// <summary>
        /// Updates grid cells to reflect current TimerRuntime state (counts, remaining, buttons, ActiveYn).
        /// </summary>
        private void SyncRuntimeToGrid()
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(SyncRuntimeToGrid));
                return;
            }

            var states = timerRuntime.GetAllStates();
            foreach (DataGridViewRow row in grdTimers.Rows)
            {
                long rowID = Convert.ToInt64(row.Cells[grdTimers.Columns["ID"].Index].Value);
                var ts = states.FirstOrDefault(s => s.TimerID == rowID);
                if (ts == null) continue;

                // Sync per-character ActiveYn preference
                DataGridViewCheckBoxCell activeCell = row.Cells[grdTimers.Columns["ActiveYn"].Index] as DataGridViewCheckBoxCell;
                if (activeCell != null)
                {
                    activeCell.Value = ts.ActiveYn;
                }

                // Sync count
                DataGridViewCell countCell = row.Cells[grdTimers.Columns["Count"].Index];
                countCell.Value = ts.Count > 0 ? ts.Count.ToString() : null;

                // Sync button state
                DataGridViewButtonCell btnCell = (DataGridViewButtonCell)row.Cells[grdTimers.Columns["StartStop"].Index];
                btnCell.Value = ts.ButtonState;
                if (ts.ButtonState != Timers.btnStart)
                {
                    btnCell.UseColumnTextForButtonValue = false;
                }

                // Sync remaining
                DataGridViewCell remainingCell = row.Cells[grdTimers.Columns["Remaining"].Index];
                remainingCell.Value = ts.Remaining;

                // Sync color
                ApplyTimerRowColor(row, ts);
            }

            RepaintTimerGrid();
            UpdateMiniView();
        }

        /// <summary>
        /// Blends a color toward white to produce a soft pastel suitable
        /// for a grid row background. A blend of 0.0 returns the original
        /// color; 1.0 returns pure white.
        /// </summary>
        private static Color LightenColor(Color source, float blend)
        {
            int r = (int)(source.R + (255 - source.R) * blend);
            int g = (int)(source.G + (255 - source.G) * blend);
            int b = (int)(source.B + (255 - source.B) * blend);
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Returns the mini view fore color for the given timer style.
        /// This ties the main grid's running-row tint to the same colors
        /// configured in Settings ? Mini View, so changing a style color
        /// in one place updates both.
        /// </summary>
        private Color GetStyleColor(string style)
        {
            switch (style)
            {
                case "Ping":
                    return Color.FromArgb(miniViews.mvPingForeColor);
                case "Buff":
                case "Pet":
                    return Color.FromArgb(miniViews.mvBuffForeColor);
                default:
                    return Color.FromArgb(miniViews.mvNormForeColor);
            }
        }

        /// <summary>
        /// Applies row colors based on timer state and style.
        /// Running timers paint the entire row with a lightened version
        /// of their style color (derived from mini view fore colors),
        /// with a deeper accent on the Remaining cell.
        /// Inactive timers get a pink entire row.
        /// </summary>
        private void ApplyTimerRowColor(DataGridViewRow row, TimerState ts)
        {
            DataGridViewCell remainingCell = row.Cells[grdTimers.Columns["Remaining"].Index];

            if (Timers.PingTimer(ts.ButtonState) || ts.IsRunning)
            {
                Color styleColor = GetStyleColor(ts.Style);
                Color rowColor = LightenColor(styleColor, 0.75f);
                Color accentColor = LightenColor(styleColor, 0.50f);
                row.DefaultCellStyle.BackColor = rowColor;
                remainingCell.Style.BackColor = accentColor;
            }
            else
            {
                Color bgColor = ts.IsActive ? Color.White : Color.LightPink;
                row.DefaultCellStyle.BackColor = bgColor;
                remainingCell.Style.BackColor = bgColor;
            }
        }

        /// <summary>
        /// Handles TimerStateChanged events from TimerRuntime — updates the grid row for the affected timer.
        /// </summary>
        private void OnTimerStateChanged(object sender, TimerStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<object, TimerStateChangedEventArgs>(OnTimerStateChanged), sender, e);
                return;
            }

            try
            {
                // Find the grid row for this timer
                foreach (DataGridViewRow row in grdTimers.Rows)
                {
                    long rowID = Convert.ToInt64(row.Cells[grdTimers.Columns["ID"].Index].Value);
                    if (rowID == e.TimerID)
                    {
                        // Update button
                        DataGridViewButtonCell btnCell = (DataGridViewButtonCell)row.Cells[grdTimers.Columns["StartStop"].Index];
                        btnCell.Value = e.ButtonState;
                        if (e.ButtonState != Timers.btnStart)
                        {
                            btnCell.UseColumnTextForButtonValue = false;
                        }

                        // Update remaining
                        DataGridViewCell remainingCell = row.Cells[grdTimers.Columns["Remaining"].Index];
                        remainingCell.Value = e.Remaining;

                        // Update count
                        DataGridViewCell countCell = row.Cells[grdTimers.Columns["Count"].Index];
                        countCell.Value = e.Count > 0 ? e.Count.ToString() : null;

                        // Update colors
                        var ts = timerRuntime.GetState(e.TimerID);
                        if (ts != null)
                        {
                            ApplyTimerRowColor(row, ts);
                        }

                        break;
                    }
                }

                RepaintTimerGrid();
                UpdateMiniView(false);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Handles sound/speech requests from TimerRuntime.
        /// </summary>
        private void OnTimerSoundRequested(object sender, TimerSoundRequestedEventArgs e)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<object, TimerSoundRequestedEventArgs>(OnTimerSoundRequested), sender, e);
                return;
            }

            if (e.WAVFile.Length > 0)
            {
                SoundPlayer sp = new SoundPlayer(Application.StartupPath + "\\Sounds\\" + e.WAVFile);
                sp.Play();
            }

            if ((e.Speech.Length > 0) && (voiceEnabled == 1))
            {
                SpeechSynthesizer synth = new SpeechSynthesizer();

                if (activeVoice.Length > 0)
                {
                    synth.SelectVoice(activeVoice);
                }

                synth.SetOutputToDefaultAudioDevice();
                synth.Rate = voiceRate;
                synth.Volume = voiceVolume;
                synth.SpeakAsync(e.Speech);
            }
        }

        /// <summary>
        /// Handles category activation events — syncs grid checkboxes and saves.
        /// </summary>
        private void OnCategoryTimersActivated(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<object, EventArgs>(OnCategoryTimersActivated), sender, e);
                return;
            }

            SyncRuntimeToGrid();
            SaveDataTimers();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnDeleteTimer_Click(object sender, EventArgs e)
        {
            if (grdTimers.CurrentCell != null)
            {
                if (MessageBox.Show("Are you sure you want to delete this timer?", "Delete Timer", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.Yes)
                {
                    // Stop this specific timer if running, via TimerRuntime
                    long timerID = Convert.ToInt64(grdTimers.Rows[grdTimers.CurrentCell.RowIndex].Cells[grdTimers.Columns["ID"].Index].Value);
                    timerRuntime.StopTimer(timerID);

                    Database.DeleteTimer(con, Convert.ToString(timerID));

                    grdTimers.DataSource = Database.GetTimers(con);
                    timerRuntime.LoadTimers((SortableBindingList<Timers.GridData>)grdTimers.DataSource);

                    ResetTimersGridColumns();
                    SyncRuntimeToGrid();
                }
            }
        }

        private void btnAddTimer_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = grdTimers.CurrentRow;

            if (row == null || ValidDataTimers(row))
            {
                SortableBindingList<Timers.GridData> data = Database.GetTimers(con);

                Timers.GridData gd = new Timers.GridData
                {
                    ID = -1,
                    ActiveYn = 1,
                    Style = "Normal",
                    Scope = "World",
                    DependsOnTimer = "",
                    DependsOnDelay = 0,
                    ClassID = 0
                };
                data.Add(gd);

                grdTimers.DataSource = data;
                timerRuntime.LoadTimers(data);

                ResetTimersGridColumns();
                SyncRuntimeToGrid();

                grdTimers.Rows[grdTimers.Rows.Count - 1].Cells[grdTimers.Columns["Duration"].Index].Value = noTime;
                grdTimers.CurrentCell = grdTimers.Rows[grdTimers.Rows.Count - 1].Cells[grdTimers.Columns["Name"].Index];

                grdTimers.BeginEdit(true);
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

        private void tscActiveCharacter_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Save outgoing character's timer state before switching
            var outgoingStates = timerRuntime.SaveCharacterState();
            Database.SaveTimerStates(con, outgoingStates, activeCharacterID);

            activeCharacterID = (tscActiveCharacter.SelectedItem as ComboBoxItem).Value.ToString();
            Database.SetSetting(con, "ActiveCharacterID", activeCharacterID);

            // Tell LogMonitor which character is now active
            long newCharID = 0;
            long.TryParse(activeCharacterID, out newCharID);
            logMonitor.SetActiveCharacter(newCharID);

            // Reload timers and restore incoming character's state
            LoadTimerRuntime();

            // Apply class filter for new character
            RefreshTimerGridDataSource();

            // Update status bar if watching
            if (tsbStartStopWatching.Text == stopWatchingText && logMonitor.FilePath != null)
            {
                statusParsing.Text = "Watching: " + Path.GetFileName(logMonitor.FilePath);
            }

            UpdateMiniView();
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
                tsbMiniViews.Checked = true;
                tsbMiniViews.BackColor = Color.LightGreen;
                miniViewsToolStripMenuItem.Checked = true;

                UpdateMiniView();
            }
        }

        private void UpdateMiniView(bool bForce=true)
        {
            var data = timerRuntime.GetMiniViewData();
            miniViews.UpdateMiniTimers(data, bForce);
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

                tsbMiniViews.Checked = false;
                tsbMiniViews.BackColor = Color.Empty;
                miniViewsToolStripMenuItem.Checked = false;
            }
        }

        private void tsbMiniViews_Click(object sender, EventArgs e)
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
            // SyncRuntimeToGrid already repaints colors and updates the status bar
            SyncRuntimeToGrid();

            // Sorting resets row visibility — reapply class filter
            RefreshTimerGridDataSource();
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
