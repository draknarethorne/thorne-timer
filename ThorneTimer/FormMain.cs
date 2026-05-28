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
        // Attempts to copy OneCore voices to SAPI registry location to expose them to System.Speech.Synthesis
        private void TryExposeOneCoreVoicesToSAPI()
        {
            ThorneLog.Info("TryExposeOneCoreVoicesToSAPI: starting");
            try
            {
                // Only works on Windows 10/11+
                using (var oneCore = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens"))
                using (var sapi = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Speech\Voices\Tokens", true))
                {
                    if (oneCore == null)
                    {
                        ThorneLog.Debug("TryExposeOneCoreVoicesToSAPI: OneCore registry key not found");
                        return;
                    }
                    if (sapi == null)
                    {
                        ThorneLog.Debug("TryExposeOneCoreVoicesToSAPI: SAPI registry key not accessible (requires admin?)");
                        return;
                    }

                    int copied = 0;
                    foreach (var voiceKeyName in oneCore.GetSubKeyNames())
                    {
                        try
                        {
                            if (sapi.OpenSubKey(voiceKeyName) != null)
                            {
                                ThorneLog.Debug($"TryExposeOneCoreVoicesToSAPI: voice key '{voiceKeyName}' already present in SAPI, skipping");
                                continue; // Already present
                            }

                            using (var src = oneCore.OpenSubKey(voiceKeyName))
                            using (var dst = sapi.CreateSubKey(voiceKeyName))
                            {
                                if (src == null || dst == null)
                                {
                                    ThorneLog.Debug($"TryExposeOneCoreVoicesToSAPI: failed to open src/dst for '{voiceKeyName}'");
                                    continue;
                                }

                                foreach (var valueName in src.GetValueNames())
                                {
                                    dst.SetValue(valueName, src.GetValue(valueName));
                                }
                                foreach (var subKeyName in src.GetSubKeyNames())
                                {
                                    using (var srcSub = src.OpenSubKey(subKeyName))
                                    using (var dstSub = dst.CreateSubKey(subKeyName))
                                    {
                                        if (srcSub == null || dstSub == null) continue;
                                        foreach (var valueName in srcSub.GetValueNames())
                                        {
                                            dstSub.SetValue(valueName, srcSub.GetValue(valueName));
                                        }
                                    }
                                }
                            }

                            copied++;
                            ThorneLog.Info($"TryExposeOneCoreVoicesToSAPI: copied OneCore voice key '{voiceKeyName}' to SAPI");
                        }
                        catch (Exception exVoice)
                        {
                            ThorneLog.Warn($"TryExposeOneCoreVoicesToSAPI: failed copying voice key '{voiceKeyName}': {exVoice.Message}");
                        }
                    }

                    ThorneLog.Info($"TryExposeOneCoreVoicesToSAPI: completed. Copied {copied} voice(s)");
                }
            }
            catch (Exception ex)
            {
                // Log failure but don't surface to user
                ThorneLog.Warn($"TryExposeOneCoreVoicesToSAPI: failed: {ex.Message}");
            }
        }
        // Helper to safely parse int with fallback
        private int SafeParseInt(string value, int defaultValue)
        {
            if (int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }
        public FormMain()
        {
            ThorneLog.Separator("FORM INIT");
            ThorneLog.Info("FormMain constructor starting");

            InitializeComponent();

            // Cross-cutting form helpers
            windowPositionManager = new WindowPositionManager(this);
            recentDatabasesManager = new RecentDatabasesManager(openRecentToolStripMenuItem, OpenDatabase);

            // Migrate user settings from previous version on first run after upgrade
            if (Properties.Settings.Default.NeedsUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.NeedsUpgrade = false;
                Properties.Settings.Default.Save();
                needsSizeNudge = true;
                ThorneLog.Info("User settings migrated from previous version");
            }

            // Resolve initial database: saved path > default (next to exe)
            string dbPath = Properties.Settings.Default.DatabasePath;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            {
                dbPath = Database.GetDefaultDatabasePath();
                Properties.Settings.Default.DatabasePath = dbPath;
                Properties.Settings.Default.Save();
                ThorneLog.Info($"Database path resolved to default: {dbPath}");
            }
            else
            {
                ThorneLog.Info($"Database path from settings: {dbPath}");
            }

            ThorneLog.Info("Opening database connection...");
            con = Database.Connection(dbPath);
            stylesRepository = new StylesRepository(con);
            miniViews.Styles = stylesRepository;
            gridLayoutManager = new GridLayoutManager(con);
            voiceManager = new VoiceManager(con, cboActiveVoice);
            miniViewSettingsManager = new MiniViewSettingsManager(
                con, miniViews,
                tbOpacity, tbFontSize,
                lblWarnPickFore, lblWarnPickBack,
                txtWarningTime, colorDialogPicker);
            ThorneLog.Info("Database connection established");

            // Load log settings from DB now that connection is open
            ThorneLog.LoadSettings(con);

            AddToRecentDatabases(dbPath);
            UpdateTitleBar(dbPath);
            ThorneLog.Info("FormMain constructor complete");
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

        // Per-view FillWeight cache so compact/advanced column proportions survive toggling
        private Dictionary<string, float> _advancedFillWeights = new Dictionary<string, float>();
        private Dictionary<string, float> _compactFillWeights = new Dictionary<string, float>();

        // Temporary auto-switch suppression after a manual character switch.
        // Cleared automatically when the active character's log generates content
        // (meaning the user settled on that character), or on explicit toggle.
        private bool autoSwitchSuppressed = false;

        // Reference count for nested BeginGridUpdate/EndGridUpdate calls
        private int _gridUpdateDepth = 0;

        // Sort state captured before applying Group Sort, so we can restore it
        // when the user toggles Group Sort off.  Null means no prior state saved.
        private (string, ListSortDirection)[] _preGroupSortState;

        readonly MiniViews miniViews = new MiniViews();
        readonly TimerRuntime timerRuntime = new TimerRuntime();
        readonly LogMonitor logMonitor = new LogMonitor();

        // v0.6.0 grid filter refactor:
        // _allTimers holds every timer definition loaded from the database.
        // _visibleTimers is the filtered subset currently bound to grdTimers
        // (filtered by ClassID / ActiveYn per the active character + view toggles).
        // Filtering swaps DataSource = _visibleTimers in one assignment instead
        // of toggling row.Visible 100+ times (each setter costs ~14 ms in
        // DataGridView, dominating character switch cost).
        SortableBindingList<Timers.GridData> _allTimers;
        SortableBindingList<Timers.GridData> _visibleTimers;

        // Filter signature of the currently-bound _visibleTimers.  Used to
        // short-circuit RefreshTimerGridDataSource when nothing has changed
        // (e.g. the 3 back-to-back refresh calls during FormMain_Load).
        // Format: "classID|showAll|activeOnly|allTimersVersion"
        string _appliedFilterSignature;

        // Incremented whenever _allTimers membership changes (Add/Delete/Reload)
        // so a no-op filter signature still triggers a refresh after data churn.
        int _allTimersVersion;
        StylesRepository stylesRepository;
        StylesController stylesController;
        ViewsRepository viewsRepository;
        ViewsController viewsController;
        CategoriesRepository categoriesRepository;
        CategoriesController categoriesController;
        CharactersRepository charactersRepository;
        CharactersController charactersController;
        SQLiteConnection con;

        // Cross-cutting form helpers (v0.6.0 polish)
        RecentDatabasesManager recentDatabasesManager;
        WindowPositionManager windowPositionManager;
        GridLayoutManager gridLayoutManager;
        VoiceManager voiceManager;
        MiniViewSettingsManager miniViewSettingsManager;

        // UI indicator for browsing mode (viewing != actively logging character)
        private Label lblBrowsingIndicator;

        Bitmap iconPlay;
        Bitmap iconStop;
        Bitmap iconMiniViews;
        Bitmap iconAutoSwitch;
        Bitmap iconAllClasses;
        Bitmap iconActiveOnly;
        Bitmap iconCompactView;
        Bitmap iconGroupSort;
        Bitmap iconNewTome;
        Bitmap iconOpenTome;
        Bitmap iconSaveAs;
        Bitmap iconRefreshSort;
        Bitmap iconAbout;
        Bitmap iconTomeInfo;
        Bitmap iconOpenRecent;
        Bitmap iconExit;

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

            // Active Only icon — a checkmark inside a circle
            iconActiveOnly = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconActiveOnly))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var green = Color.FromArgb(40, 160, 40);
                using (var pen = new Pen(green, 1.4f))
                    g.DrawEllipse(pen, 1, 1, 13, 13);
                using (var pen = new Pen(green, 2f))
                {
                    g.DrawLine(pen, 4, 8, 7, 11);
                    g.DrawLine(pen, 7, 11, 12, 4);
                }
            }

            // Group Sort icon — three horizontal bars descending in length with down arrow
            iconGroupSort = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconGroupSort))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var blue = Color.FromArgb(0, 100, 180);
                using (var pen = new Pen(blue, 1.8f))
                {
                    g.DrawLine(pen, 1, 3, 9, 3);
                    g.DrawLine(pen, 1, 7, 7, 7);
                    g.DrawLine(pen, 1, 11, 5, 11);
                }
                using (var brush = new SolidBrush(blue))
                    g.FillPolygon(brush, new[] { new Point(11, 5), new Point(14, 12), new Point(8, 12) });
            }

            // New Tome icon — page with a plus sign
            iconNewTome = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconNewTome))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var pen = new Pen(Color.FromArgb(80, 80, 80), 1.2f))
                {
                    g.DrawLine(pen, 3, 1, 3, 14);
                    g.DrawLine(pen, 3, 14, 10, 14);
                    g.DrawLine(pen, 10, 14, 10, 4);
                    g.DrawLine(pen, 10, 4, 7, 1);
                    g.DrawLine(pen, 7, 1, 3, 1);
                    g.DrawLine(pen, 7, 1, 7, 4);
                    g.DrawLine(pen, 7, 4, 10, 4);
                }
                var green = Color.FromArgb(40, 160, 40);
                using (var pen = new Pen(green, 1.8f))
                {
                    g.DrawLine(pen, 5, 9, 9, 9);
                    g.DrawLine(pen, 7, 7, 7, 11);
                }
            }

            // Open Tome icon — open folder
            iconOpenTome = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconOpenTome))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var folderYellow = Color.FromArgb(220, 180, 50);
                using (var brush = new SolidBrush(folderYellow))
                {
                    g.FillRectangle(brush, 1, 4, 12, 10);
                    g.FillRectangle(brush, 1, 3, 5, 2);
                }
                var folderFront = Color.FromArgb(240, 200, 80);
                using (var brush = new SolidBrush(folderFront))
                {
                    g.FillPolygon(brush, new[] {
                        new Point(1, 7), new Point(4, 14),
                        new Point(14, 14), new Point(14, 7)
                    });
                }
                using (var pen = new Pen(Color.FromArgb(160, 120, 20), 1f))
                    g.DrawRectangle(pen, 1, 4, 12, 10);
            }

            // Save As icon — floppy disk
            iconSaveAs = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconSaveAs))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var diskBlue = Color.FromArgb(60, 100, 170);
                using (var brush = new SolidBrush(diskBlue))
                    g.FillRectangle(brush, 2, 1, 12, 14);
                using (var brush = new SolidBrush(Color.White))
                    g.FillRectangle(brush, 4, 1, 7, 5);
                using (var brush = new SolidBrush(Color.FromArgb(220, 220, 220)))
                    g.FillRectangle(brush, 4, 9, 8, 5);
                using (var pen = new Pen(Color.FromArgb(40, 70, 120), 1f))
                    g.DrawRectangle(pen, 2, 1, 12, 14);
            }

            // Refresh Sort icon — circular arrow
            iconRefreshSort = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconRefreshSort))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var green = Color.FromArgb(0, 140, 60);
                using (var pen = new Pen(green, 1.8f))
                    g.DrawArc(pen, 2, 2, 11, 11, -60, 300);
                using (var brush = new SolidBrush(green))
                    g.FillPolygon(brush, new[] { new Point(10, 1), new Point(14, 5), new Point(10, 5) });
            }

            // About icon — "i" in a circle
            iconAbout = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconAbout))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var blue = Color.FromArgb(0, 100, 180);
                using (var pen = new Pen(blue, 1.4f))
                    g.DrawEllipse(pen, 1, 1, 13, 13);
                using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var brush = new SolidBrush(blue))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("i", font, brush, new RectangleF(0, 0, 16, 16), sf);
                }
            }

            // Open Recent icon — clock with circular arrow (history)
            iconOpenRecent = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconOpenRecent))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var gray = Color.FromArgb(90, 90, 90);
                using (var pen = new Pen(gray, 1.4f))
                    g.DrawEllipse(pen, 1, 1, 13, 13);
                using (var pen = new Pen(gray, 1.6f))
                {
                    g.DrawLine(pen, 7, 3, 7, 8);
                    g.DrawLine(pen, 7, 8, 11, 8);
                }
                using (var brush = new SolidBrush(gray))
                    g.FillPolygon(brush, new[] { new Point(2, 2), new Point(5, 0), new Point(5, 4) });
            }

            // Exit icon — door with right arrow
            iconExit = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconExit))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var dark = Color.FromArgb(100, 100, 100);
                using (var pen = new Pen(dark, 1.2f))
                {
                    g.DrawRectangle(pen, 1, 1, 8, 13);
                    g.DrawLine(pen, 5, 4, 5, 11);
                }
                var red = Color.FromArgb(190, 40, 40);
                using (var pen = new Pen(red, 1.8f))
                    g.DrawLine(pen, 9, 7, 15, 7);
                using (var brush = new SolidBrush(red))
                    g.FillPolygon(brush, new[] { new Point(12, 4), new Point(15, 7), new Point(12, 10) });
            }

            // Tome Info icon — open book with "i"
            iconTomeInfo = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(iconTomeInfo))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var brown = Color.FromArgb(140, 100, 50);
                using (var pen = new Pen(brown, 1.2f))
                {
                    // Left page
                    g.DrawLine(pen, 1, 2, 1, 13);
                    g.DrawLine(pen, 1, 2, 7, 3);
                    g.DrawLine(pen, 1, 13, 7, 14);
                    // Right page
                    g.DrawLine(pen, 14, 2, 14, 13);
                    g.DrawLine(pen, 14, 2, 8, 3);
                    g.DrawLine(pen, 14, 13, 8, 14);
                    // Spine
                    g.DrawLine(pen, 7, 3, 7, 14);
                    g.DrawLine(pen, 8, 3, 8, 14);
                }
                var blue = Color.FromArgb(0, 100, 180);
                using (var font = new Font("Segoe UI", 7f, FontStyle.Bold))
                using (var brush = new SolidBrush(blue))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("i", font, brush, new RectangleF(7, 3, 8, 11), sf);
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
            tsbShowActiveOnly.Image = iconActiveOnly;
            showActiveOnlyToolStripMenuItem.Image = iconActiveOnly;
            tsbCompactView.Image = iconCompactView;
            compactViewToolStripMenuItem.Image = iconCompactView;
            tsbDefaultSort.Image = iconGroupSort;
            defaultSortToolStripMenuItem.Image = iconGroupSort;
            newDatabaseToolStripMenuItem.Image = iconNewTome;
            openDatabaseToolStripMenuItem.Image = iconOpenTome;
            saveDatabaseAsToolStripMenuItem.Image = iconSaveAs;
            refreshTimersToolStripMenuItem.Image = iconRefreshSort;
            aboutToolStripMenuItem.Image = iconAbout;
            tomeInfoToolStripMenuItem.Image = iconTomeInfo;
            openRecentToolStripMenuItem.Image = iconOpenRecent;
            exitToolStripMenuItem.Image = iconExit;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            using (ThorneLog.Time("FormMain_Load TOTAL"))
            {
            CreateToolbarIcons();
            this.FormClosing += FormMain_FormClosing;
            txtWarningTime.LostFocus += WarningTime_LostFocus;
            // v0.6.0: Removed txtPingTime (now style/view configuration)

            this.RestoreWindowPosition();

            // v0.6.0: Load global mini view settings only
            miniViewSettingsManager.LoadFromDatabase();


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

            // Populate character dropdown BEFORE any code tries to access it
            // (needed by the offline character detection block below)
            tscActiveCharacter.SelectedIndexChanged -= tscActiveCharacter_SelectedIndexChanged;
            SetupActiveCharacters();
            tscActiveCharacter.SelectedIndexChanged += tscActiveCharacter_SelectedIndexChanged;

            if (Properties.Settings.Default.ParseLog)
            {
                StartLog();

                // Auto-detect if the last active character is actually online.
                // If their log file hasn't been modified recently (within 5 minutes),
                // assume they're offline and set character to "(None)" for cleaner UX.
                long lastActiveCharID = 0;
                long.TryParse(activeCharacterID, out lastActiveCharID);
                if (lastActiveCharID > 0 && !IsCharacterLogActive(lastActiveCharID, thresholdMinutes: 5))
                {
                    ThorneLog.Info($"FormMain_Load: Last active character (ID={lastActiveCharID}) appears offline (log not recently modified). Setting to '(None)'.");

                    // Set to "(None)" character (ID=0)
                    activeCharacterID = "0";
                    Database.SetSetting(con, "ActiveCharacterID", activeCharacterID);

                    // Update dropdown without triggering SelectedIndexChanged
                    tscActiveCharacter.SelectedIndexChanged -= tscActiveCharacter_SelectedIndexChanged;
                    foreach (ComboBoxItem item in (List<ComboBoxItem>)tscActiveCharacter.ComboBox.DataSource)
                    {
                        if (Convert.ToInt64(item.Value) == 0)
                        {
                            tscActiveCharacter.SelectedItem = item;
                            break;
                        }
                    }
                    tscActiveCharacter.SelectedIndexChanged += tscActiveCharacter_SelectedIndexChanged;

                    // Update LogMonitor to no active character
                    logMonitor.SetActiveCharacter(0);

                    // Update status bar
                    statusParsing.Text = "Watching: (no active character)";
                }
            }

            if (Properties.Settings.Default.MiniView)
            {
                ShowMiniView();
            }

            // Wire TimerRuntime events
            timerRuntime.TimerStateChanged += OnTimerStateChanged;
            timerRuntime.TimerSoundRequested += OnTimerSoundRequested;
            timerRuntime.CategoryTimersActivated += OnCategoryTimersActivated;

            // Wire LogMonitor events
            logMonitor.CharacterSwitched += OnCharacterSwitched;
            logMonitor.CharacterCampedOut += OnCharacterCampedOut;

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

            // Restore show-active-only setting
            bool showActiveOnly = Database.GetSetting(con, "ShowActiveOnly") == "1";
            tsbShowActiveOnly.Checked = showActiveOnly;
            showActiveOnlyToolStripMenuItem.Checked = showActiveOnly;
            timerRuntime.ShowActiveOnly = showActiveOnly;

            // Add tooltip to character dropdown explaining viewer behavior
            tscActiveCharacter.ToolTipText = "Select character to view timers (active character tracks in background)";

            // Create browsing mode indicator label (initially hidden)
            lblBrowsingIndicator = new Label
            {
                Text = "",
                AutoSize = false,
                Height = 24,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(255, 250, 205), // Light yellow
                ForeColor = Color.FromArgb(139, 69, 19),   // Dark brown
                Padding = new Padding(8, 4, 8, 4),
                Font = new Font(this.Font.FontFamily, 9f, FontStyle.Bold),
                Visible = false
            };
            // Insert the label above the timer grid in the Timers tab
            tabTimers.Controls.Add(lblBrowsingIndicator);
            lblBrowsingIndicator.BringToFront();

            ThorneLog.Separator("FORM LOAD");
            ThorneLog.Info($"FormMain_Load: activeCharacterID={activeCharacterID}");
            ThorneLog.Info($"FormMain_Load: autoSwitch={autoSwitch}, showAllClasses={showAllClasses}, showActiveOnly={showActiveOnly}");

            // Dump loaded reference data for diagnostics
            ThorneLog.DumpClasses("Startup", con);
            ThorneLog.DumpCharacters("Startup", CharactersRepository.GetActiveCharacters(con));
            ThorneLog.DumpCategories("Startup", CategoriesRepository.GetCategories(con));

            // Hide the grid and suppress layout/auto-size for the entire
            // setup + data-load sequence so nothing paints until the end.
            grdTimers.Visible = false;
            BeginGridUpdate();
            try
            {
                using (ThorneLog.Time("FormMain_Load: SetupTimerGrid"))
                    SetupTimerGrid();
                using (ThorneLog.Time("FormMain_Load: SetupCharacterGrid"))
                    SetupCharacterGrid();
                using (ThorneLog.Time("FormMain_Load: SetupCategoriesGrid"))
                    SetupCategoriesGrid();
                using (ThorneLog.Time("FormMain_Load: SetupViewsGrid"))
                    SetupViewsGrid();
                using (ThorneLog.Time("FormMain_Load: SetupStylesGrid"))
                    SetupStylesGrid();
                // Load timer and category data into TimerRuntime
                var savedStates = LoadTimerRuntime();

                // On app startup, restore World-scope timers that were running
                // when the app last closed, adjusting for elapsed offline time.
                // Reuse the savedStates already loaded above (no second DB query).
                ThorneLog.Info($"FormMain_Load: calling RestoreWorldTimersOnStartup with {savedStates.Count} saved states");
                using (ThorneLog.Time("FormMain_Load: RestoreWorldTimersOnStartup"))
                    timerRuntime.RestoreWorldTimersOnStartup(savedStates);

                // Sync world timer restores to the grid
                using (ThorneLog.Time("FormMain_Load: SyncRuntimeToGrid (post-RestoreWorld)"))
                    SyncRuntimeToGrid();

                ThorneLog.Info("FormMain_Load: timer load + restore complete");
                ThorneLog.DumpTimerGrid("Startup-complete", grdTimers);

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
                if (stylesController?.Grid != null)
                    LoadColumnWidths("Styles", stylesController.Grid);

                // Seed per-view FillWeight caches from the current (post-load) state
                // so the very first compact/advanced toggle has weights to restore.
                // The DB stores all columns: visible ones have the current view's
                // FillWeights; hidden ones have the other view's FillWeights.
                // Seed both caches — the first toggle will refine the "other" view.
                var initialWeights = new Dictionary<string, float>();
                foreach (DataGridViewColumn col in grdTimers.Columns)
                    initialWeights[col.Name] = col.FillWeight;
                _compactFillWeights = new Dictionary<string, float>(initialWeights);
                _advancedFillWeights = new Dictionary<string, float>(initialWeights);

                // Restore persisted multi-column sort state
                // (also restores the pre-Group Sort fallback so toggling
                // Group Sort off after a restart works correctly).
                LoadSortStateWithPreGroupSort("Timers", grdTimers);

                // Sorting fires ListChanged(Reset) which rebuilds grid rows,
                // clearing custom cell styles and row visibility.
                using (ThorneLog.Time("FormMain_Load: RefreshGridAfterSort"))
                    RefreshGridAfterSort();
            }
            finally
            {
                EndGridUpdate();
                grdTimers.Visible = true;
            }

            UpdateMiniView();

            PopulateRecentDatabases();

            this.Shown += FormMain_Shown;
            }
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            // Force the grid to recalculate its Fill layout now that
            // the form is fully visible.  Without this toggle, columns
            // appear draggable but won't actually resize until the user
            // manually resizes the form (a known WinForms quirk when
            // AutoSizeColumnsMode=Fill is set before initial layout).
            grdTimers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grdTimers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Re-apply persisted column widths.  The Fill recalc above
            // uses FillWeights (already restored by LoadColumnWidths in
            // FormMain_Load), so Fill columns keep their proportions.
            // Non-Fill columns (AutoSizeMode=None, AllCellsExceptHeader,
            // etc.) need their pixel widths re-applied explicitly.
            LoadColumnWidths("Timers", grdTimers);
        }

        private void UpdateTitleBar(string dbPath)
        {
            string dbName = Path.GetFileName(dbPath);
            this.Text = "Thorne Timer - " + dbName;
            statusTomePath.Text = dbPath;
        }

        private void AddToRecentDatabases(string dbPath)
        {
            recentDatabasesManager.Add(dbPath);
        }

        private void PopulateRecentDatabases()
        {
            recentDatabasesManager.Refresh();
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
            ResetRepositories();
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
            ResetRepositories();

            // Reload global mini view settings from database
            miniViewSettingsManager.LoadFromDatabase();

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

            bool showActiveOnly = Database.GetSetting(con, "ShowActiveOnly") == "1";
            tsbShowActiveOnly.Checked = showActiveOnly;
            showActiveOnlyToolStripMenuItem.Checked = showActiveOnly;
            timerRuntime.ShowActiveOnly = showActiveOnly;

            bool compactView = Database.GetSetting(con, "CompactView") == "1";
            tsbCompactView.Checked = compactView;
            compactViewToolStripMenuItem.Checked = compactView;

            // Unhook event handlers before tearing down grids to prevent
            // validation firing against columns that no longer exist.
            grdTimers.RowValidating -= ValidateRowTimers;
            grdCategories.RowValidating -= ValidateRowCategories;
            grdViews.RowValidating -= ValidateRowViews;

            // Reload grids
            tscActiveCharacter.SelectedIndexChanged -= tscActiveCharacter_SelectedIndexChanged;
            SetupActiveCharacters();
            tscActiveCharacter.SelectedIndexChanged += tscActiveCharacter_SelectedIndexChanged;
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
            grdStyles.DataSource = null;
            grdStyles.Columns.Clear();
            SetupStylesGrid();

            // Reload TimerRuntime with new database data
            BeginGridUpdate();
            try
            {
                LoadTimerRuntime();

                // Restore compact/full view widths from the new database
                compactViewWidth = SafeParseInt(Database.GetSetting(con, "CompactWidth"), this.Width < DefaultFullViewWidth ? this.Width : DefaultCompactViewWidth);
                fullViewWidth = SafeParseInt(Database.GetSetting(con, "FullWidth"), Math.Max(this.Width, DefaultFullViewWidth));

                // Re-apply compact view after grid rebuild
                ApplyCompactView(tsbCompactView.Checked);

                // Restore persisted column widths from the new database
                LoadColumnWidths("Timers", grdTimers);
                LoadColumnWidths("Characters", grdCharacters);
                LoadColumnWidths("Categories", grdCategories);
                LoadColumnWidths("Styles", grdStyles);

                // Seed per-view FillWeight caches so the first compact/advanced
                // toggle after a database switch has weights to restore.
                var initialWeights = new Dictionary<string, float>();
                foreach (DataGridViewColumn col in grdTimers.Columns)
                    initialWeights[col.Name] = col.FillWeight;
                _compactFillWeights = new Dictionary<string, float>(initialWeights);
                _advancedFillWeights = new Dictionary<string, float>(initialWeights);

                // Restore persisted sort state (falls back to Group Sort
                // if the new database has no saved sort state).
                LoadSortStateWithPreGroupSort("Timers", grdTimers);

                RefreshGridAfterSort();
            }
            finally
            {
                EndGridUpdate();
            }

            UpdateMiniView();
        }

        private void ResetRepositories()
        {
            stylesRepository = new StylesRepository(con);
            viewsRepository = new ViewsRepository(con);
            categoriesRepository = new CategoriesRepository(con);
            charactersRepository = new CharactersRepository(con);
            gridLayoutManager = new GridLayoutManager(con);
            voiceManager = new VoiceManager(con, cboActiveVoice);
            miniViewSettingsManager = new MiniViewSettingsManager(
                con, miniViews,
                tbOpacity, tbFontSize,
                lblWarnPickFore, lblWarnPickBack,
                txtWarningTime, colorDialogPicker);
            miniViews.Styles = stylesRepository;
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
            ThorneLog.Separator("FORM CLOSING");
            ThorneLog.Info($"FormClosing: activeCharacterID={activeCharacterID}");
            ThorneLog.DumpTimerGrid("FormClosing-before", grdTimers);

            SaveProperties();

            // Persist column widths
            Database.SaveColumnWidths(con, "Timers", grdTimers);
            Database.SaveColumnWidths(con, "Characters", grdCharacters);
            Database.SaveColumnWidths(con, "Categories", grdCategories);

            // Persist multi-column sort state
            var timerList = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
            if (timerList != null)
                Database.SaveSortState(con, "Timers", timerList.SortDescriptions);

            // Persist the pre-Group Sort fallback so toggling Group Sort
            // off after a restart restores the user's previous sort order.
            SavePreGroupSortState();

            SaveDataTimers();
            SaveDataCharacters();
            SaveDataCategories();
            SaveDataViews();

            // Save timer runtime state (counts + running timer info) for persistence
            ThorneLog.Info("FormClosing: saving character state");
            var closingStates = timerRuntime.SaveCharacterState();
            ThorneLog.Info($"FormClosing: saving timer states (charID={activeCharacterID}, {closingStates.Count} timer(s))");
            TimerStateRepository.SaveTimerStates(con, closingStates, activeCharacterID);
            ThorneLog.Info("FormClosing: state persisted, stopping timers");

            // Stop remaining timers (World-scope) and log monitor gracefully
            timerRuntime.StopAllTimers();
            logMonitor.Stop();

            ThorneLog.Info("FormClosing: shutdown complete");
        }

        /// <summary>Thin wrapper kept for MiniViews compatibility. See <see cref="WindowPositionManager.IsVisibleOnAnyScreen"/>.</summary>
        static public bool IsVisibleOnAnyScreen(Rectangle rect)
        {
            return WindowPositionManager.IsVisibleOnAnyScreen(rect);
        }

        /// <summary>Thin wrapper kept for MiniViews compatibility. See <see cref="WindowPositionManager.EnsureVisibleOnScreen"/>.</summary>
        static public Point EnsureVisibleOnScreen(Point location, Size size)
        {
            return WindowPositionManager.EnsureVisibleOnScreen(location, size);
        }

        private void RestoreWindowPosition()
        {
            bool isCompact = Database.GetSetting(con, "CompactView") == "1";
            windowPositionManager.Restore(needsSizeNudge, DefaultFullViewWidth, isCompact);
        }

        private void SaveProperties()
        {
            windowPositionManager.Save();

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
        /// Applies saved column widths/fill weights to <paramref name="grid"/>
        /// via <see cref="GridLayoutManager"/>.  See that class for details.
        /// </summary>
        private void LoadColumnWidths(string gridName, DataGridView grid)
        {
            gridLayoutManager.LoadColumnWidths(gridName, grid);
        }

        /// <summary>
        /// Applies saved multi-column sort state from the database to the timer grid.
        /// Falls back to the default sort (Class → Style → Name) when no saved
        /// sort state exists in the database.
        /// </summary>
        private void LoadSortState(string gridName, DataGridView grid)
        {
            if (gridLayoutManager.TryLoadSortState(gridName, grid))
            {
                UpdateSortGlyphs();
                UpdateGroupSortCheckedState();
                return;
            }

            // No saved sort state — apply the default sort
            ApplyDefaultSort();
        }

        /// <summary>
        /// Loads both the main sort state and the pre-Group Sort fallback
        /// state from the database.  Called during startup and database switch.
        /// </summary>
        private void LoadSortStateWithPreGroupSort(string gridName, DataGridView grid)
        {
            LoadSortState(gridName, grid);
            LoadPreGroupSortState();
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

        void GrdTimers_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex < 0) return;

            var grid = (DataGridView)sender;
            string colName = grid.Columns[e.ColumnIndex].Name;

            switch (colName)
            {
                case "Scope":
                    var scopeValue = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
                    switch (scopeValue)
                    {
                        case "World":
                            e.ToolTipText = "Shared timer across all characters. Always counting.";
                            break;
                        case "Character":
                            e.ToolTipText = "Per-character timer. Pauses when the character is offline.";
                            break;
                        case "Character+":
                            e.ToolTipText = "Per-character timer. Continues counting while the character is offline\n(server-tracked cooldowns like item recast timers).";
                            break;
                    }
                    break;

                case "Style":
                    var styleValue = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
                    switch (styleValue)
                    {
                        case "Normal":
                            e.ToolTipText = "Standard timer. Shows warning colors when nearing expiry.";
                            break;
                        case "Buff":
                            e.ToolTipText = "Buff timer. Restarts if the keyword fires again while running.\nUses buff-style colors in mini views.";
                            break;
                        case "Pet":
                            e.ToolTipText = "Pet timer. Restarts if the keyword fires again while running.\nUses pet-style colors in mini views.";
                            break;
                        case "Ping":
                            e.ToolTipText = "Ping timer. Fires a repeating notification.\nNo warning colors shown in mini views.";
                            break;
                    }
                    break;

                case "ActiveYn":
                    e.ToolTipText = "Controls whether this timer participates in log-file keyword matching.";
                    break;

                case "CaseYn":
                    e.ToolTipText = "When checked, keyword matching is case-sensitive.";
                    break;

                case "EndlessYn":
                    e.ToolTipText = "When checked, the timer restarts automatically when it expires.";
                    break;

                case "ClassID":
                    e.ToolTipText = "Filters which characters see this timer.\nGlobal (blank) means all characters see it.";
                    break;

                case "CategoryID":
                    e.ToolTipText = "Logical grouping for this timer. Categories with Start/End Keywords\ncan automatically activate or deactivate all their timers\nbased on log events (e.g. entering or leaving a zone).";
                    break;
            }
        }

        private void ResetTimersGridColumns()
        {
            int i = 1;
            grdTimers.Columns["ActiveYn"].DisplayIndex = i++;
            grdTimers.Columns["Name"].DisplayIndex = i++;
            grdTimers.Columns["ClassID"].DisplayIndex = i++;
            grdTimers.Columns["Style"].DisplayIndex = i++;
            grdTimers.Columns["Scope"].DisplayIndex = i++;
            grdTimers.Columns["CategoryID"].DisplayIndex = i++;
            grdTimers.Columns["StartKeyword"].DisplayIndex = i++;
            grdTimers.Columns["EndKeyword"].DisplayIndex = i++;
            grdTimers.Columns["Speech"].DisplayIndex = i++;
            grdTimers.Columns["WAVFile"].DisplayIndex = i++;
            grdTimers.Columns["WAV"].DisplayIndex = i++;
            grdTimers.Columns["CaseYn"].DisplayIndex = i++;
            grdTimers.Columns["EndlessYn"].DisplayIndex = i++;
            grdTimers.Columns["DependsOnTimer"].DisplayIndex = i++;
            grdTimers.Columns["DependsOnDelay"].DisplayIndex = i++;
            grdTimers.Columns["Duration"].DisplayIndex = i++;
            grdTimers.Columns["Remaining"].DisplayIndex = i++;
            grdTimers.Columns["Count"].DisplayIndex = i++;
            grdTimers.Columns["StartStop"].DisplayIndex = i++;

            grdTimers.Columns["ActiveYn"].SortMode = DataGridViewColumnSortMode.Programmatic;
            grdTimers.Columns["Name"].SortMode = DataGridViewColumnSortMode.Programmatic;
            grdTimers.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["CategoryID"].SortMode = DataGridViewColumnSortMode.Programmatic;
            grdTimers.Columns["Style"].SortMode = DataGridViewColumnSortMode.Programmatic;
            grdTimers.Columns["ClassID"].SortMode = DataGridViewColumnSortMode.Programmatic;
            grdTimers.Columns["Scope"].SortMode = DataGridViewColumnSortMode.Programmatic;
            grdTimers.Columns["StartKeyword"].SortMode = DataGridViewColumnSortMode.Programmatic;
            grdTimers.Columns["EndKeyword"].SortMode = DataGridViewColumnSortMode.Programmatic;
            grdTimers.Columns["WAV"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["WAVFile"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["Speech"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["Duration"].SortMode = DataGridViewColumnSortMode.Programmatic;
            grdTimers.Columns["Remaining"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["CaseYn"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["EndlessYn"].SortMode = DataGridViewColumnSortMode.NotSortable;
            grdTimers.Columns["DependsOnTimer"].SortMode = DataGridViewColumnSortMode.Programmatic;
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
            // VoiceManager owns enumeration + selection logic.  Mirror the
            // field state so existing timer-alert call sites continue to work.
            voiceManager.LoadFromDatabase();
            voiceManager.PopulateVoiceCombo();
            activeVoice = voiceManager.ActiveVoice;
            voiceRate = voiceManager.Rate;
            voiceVolume = voiceManager.Volume;
        }

        private void SetupActiveCharacters()
        {
            string oldActiveCharacterID = activeCharacterID;

            var characters = CharactersRepository.GetActiveCharacters(con);

            // Add "All Characters" option at the beginning for watching all character log files
            characters.Insert(0, new ComboBoxItem { Value = 0, Text = "All Characters" });

            tscActiveCharacter.ComboBox.DataSource = characters;

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
            cboCategory.DataSource = CategoriesRepository.GetGridCategories(con);
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
                DataSource = CategoriesRepository.GetGridCategories(con),
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
            cboRole.Items.AddRange("Normal", "Buff", "Pet", "Ping", "Spawn", "Lockout", "Character");
            grdTimers.Columns.Add(cboRole);
            grdTimers.Columns["Style"].Width = 80;
            grdTimers.Columns["Style"].MinimumWidth = 60;
            grdTimers.Columns["Style"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            DataGridViewComboBoxColumn cboClass = new DataGridViewComboBoxColumn
            {
                HeaderText = "Class",
                Name = "ClassID",
                DataPropertyName = "ClassID",
                ValueType = typeof(ComboBoxItem),
                DisplayMember = "Text",
                ValueMember = "Value",
                DataSource = ClassesRepository.GetGridClasses(con),
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
            cboScope.Items.AddRange("Character", "Character+", "World");
            grdTimers.Columns.Add(cboScope);
            grdTimers.Columns["Scope"].Width = 90;
            grdTimers.Columns["Scope"].MinimumWidth = 70;
            grdTimers.Columns["Scope"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            grdTimers.Columns.Add("DependsOnTimer", "Depends On");
            grdTimers.Columns["DependsOnTimer"].DataPropertyName = "DependsOnTimer";
            grdTimers.Columns["DependsOnTimer"].Width = 100;
            grdTimers.Columns["DependsOnTimer"].MinimumWidth = 60;

            grdTimers.Columns.Add("DependsOnDelay", "Depends Delay");
            grdTimers.Columns["DependsOnDelay"].DataPropertyName = "DependsOnDelay";
            grdTimers.Columns["DependsOnDelay"].Width = 55;
            grdTimers.Columns["DependsOnDelay"].MinimumWidth = 40;
            grdTimers.Columns["DependsOnDelay"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grdTimers.Columns["DependsOnDelay"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

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
            grdTimers.Columns["StartStop"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            grdTimers.Columns.Add("Count", "Count");
            //grdTimers.Columns["Count"].DataPropertyName = grdTimers.Columns["Count"].Name;
            grdTimers.Columns["Count"].Width = 50;
            grdTimers.Columns["Count"].MinimumWidth = 50;
            grdTimers.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grdTimers.Columns["Count"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;

            // v0.6.0 grid filter refactor:
            // Load every timer into the master list (_allTimers) and start
            // with a visible-list clone containing every entry.  The first
            // RefreshTimerGridDataSource call will narrow _visibleTimers
            // based on the active character + filter toggles.  GridData
            // instances are shared between both lists so any cell edit (which
            // flows through DataPropertyName binding) updates the underlying
            // object — and therefore both lists — automatically.
            _allTimers = TimersRepository.GetTimers(con);
            _allTimersVersion++;
            _visibleTimers = new SortableBindingList<Timers.GridData>(_allTimers.ToList());
            grdTimers.DataSource = _visibleTimers;

            RegisterTimerDisplayResolvers(_visibleTimers);

            grdTimers.RowValidating += ValidateRowTimers;
            grdTimers.EditingControlShowing += GrdTimers_EditingControlShowing;
            grdTimers.CellToolTipTextNeeded += GrdTimers_CellToolTipTextNeeded;

            ResetTimersGridColumns();
        }

        /// <summary>
        /// Registers display-name resolvers on the given timer list so that
        /// multi-column sorting on CategoryID/ClassID uses the visible name
        /// (e.g. "Necromancer") instead of the raw numeric ID.  Must be
        /// re-applied any time a fresh SortableBindingList is bound to
        /// grdTimers (e.g. after the filter refactor rebuilds _visibleTimers).
        /// </summary>
        private void RegisterTimerDisplayResolvers(SortableBindingList<Timers.GridData> timerList)
        {
            if (timerList == null) return;

            var categories = CategoriesRepository.GetGridCategories(con);
            var catLookup = new Dictionary<long, string>();
            foreach (var c in categories) catLookup[c.Value] = c.Text;
            timerList.RegisterDisplayResolver("CategoryID", raw =>
            {
                if (raw is long id && catLookup.TryGetValue(id, out string name)) return name;
                if (raw is int intId && catLookup.TryGetValue(intId, out string name2)) return name2;
                return raw?.ToString() ?? "";
            });

            var classes = ClassesRepository.GetGridClasses(con);
            var clsLookup = new Dictionary<long, string>();
            foreach (var c in classes) clsLookup[c.Value] = c.Text;
            timerList.RegisterDisplayResolver("ClassID", raw =>
            {
                if (raw is long id && clsLookup.TryGetValue(id, out string name)) return name;
                if (raw is int intId && clsLookup.TryGetValue(intId, out string name2)) return name2;
                return raw?.ToString() ?? "";
            });
        }

        private void SetupCharacterGrid()
        {
            if (charactersRepository == null)
                charactersRepository = new CharactersRepository(con);

            charactersController?.Dispose();
            charactersController = new CharactersController(charactersRepository, OnCharactersChanged)
            {
                BeforeRowSave = SyncMiniViewPositionToCharacterRow
            };
            charactersController.Initialize(grdCharacters);
        }

        /// <summary>
        /// Pre-save hook: if mini views are active and this row represents the
        /// active character, copy the live mini-view location into the row's
        /// MiniViewX / MiniViewY cells so the controller persists it.
        /// </summary>
        private void SyncMiniViewPositionToCharacterRow(DataGridViewRow row)
        {
            if (!miniViews.MiniViewsActive()) return;

            var character = CharactersRepository.GetCharacter(con, activeCharacterID);
            DataGridViewCell Name = row.Cells[grdCharacters.Columns["Name"].Index];
            if (character.Name != Convert.ToString(Name.Value)) return;

            row.Cells[grdCharacters.Columns["MiniViewX"].Index].Value = miniViews.MV().Location.X;
            row.Cells[grdCharacters.Columns["MiniViewY"].Index].Value = miniViews.MV().Location.Y;
        }

        private void OnCharactersChanged()
        {
            tscActiveCharacter.SelectedIndexChanged -= tscActiveCharacter_SelectedIndexChanged;
            SetupActiveCharacters();
            tscActiveCharacter.SelectedIndexChanged += tscActiveCharacter_SelectedIndexChanged;
        }

        private void SetupCategoriesGrid()
        {
            if (categoriesRepository == null)
                categoriesRepository = new CategoriesRepository(con);

            categoriesController?.Dispose();
            categoriesController = new CategoriesController(categoriesRepository, OnCategoriesChanged);
            categoriesController.Initialize(grdCategories);
        }

        private void OnCategoriesChanged()
        {
            RefreshGridCategorySource();
        }

        private void SetupViewsGrid()
        {
            if (viewsRepository == null)
                viewsRepository = new ViewsRepository(con);
            if (stylesRepository == null)
                stylesRepository = new StylesRepository(con);

            viewsController?.Dispose();
            viewsController = new ViewsController(viewsRepository, stylesRepository, OnViewsChanged);
            viewsController.Initialize(grdViews);
        }

        private void OnViewsChanged()
        {
            miniViews.RefreshMiniViews(con, activeCharacterID);
            UpdateMiniView();
        }

        private void SetupStylesGrid()
        {
            if (stylesRepository == null)
                stylesRepository = new StylesRepository(con);

            stylesController?.Dispose();
            stylesController = new StylesController(stylesRepository, OnStylesChanged);
            stylesController.Initialize(grdStyles);
        }

        private void OnStylesChanged()
        {
            stylesRepository?.RefreshCache();
            viewsController?.RefreshStyleOptions();
            miniViews.RefreshMiniViews(con, activeCharacterID);
            RepaintTimerGrid();
            UpdateMiniView();
        }

        void grdViews_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            // Provide tooltips for EmptyBehavior column
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && grdViews.Columns[e.ColumnIndex].Name == "EmptyBehavior")
            {
                var cellValue = grdViews.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
                switch (cellValue)
                {
                    case "CharacterName":
                        e.ToolTipText = "Always show active character name (typically used for Character view)";
                        break;
                    case "ViewName":
                        e.ToolTipText = "Show view name when empty (e.g., 'Buffs', 'Pets', 'Spawns')";
                        break;
                    case "Spaces":
                        e.ToolTipText = "Show minimal blank space to maintain view presence on screen";
                        break;
                    case "HideEmpty":
                        e.ToolTipText = "Completely hide view when no timers are active (view disappears)";
                        break;
                    default:
                        e.ToolTipText = "Controls what displays when view has no active timers";
                        break;
                }
            }
        }

        void grdViews_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Format the Example column with styled preview text
            if (grdViews.Columns[e.ColumnIndex].Name == "Example")
            {
                DataGridViewRow row = grdViews.Rows[e.RowIndex];

                // Get ForeColor and BackColor from hidden columns
                int foreColor = Convert.ToInt32(row.Cells[grdViews.Columns["ForeColor"].Index].Value ?? Color.Yellow.ToArgb());
                int backColor = Convert.ToInt32(row.Cells[grdViews.Columns["BackColor"].Index].Value ?? Color.Black.ToArgb());

                // Set example text
                e.Value = "Sample Timer 01:23";

                // Apply colors to the cell
                e.CellStyle.ForeColor = Color.FromArgb(foreColor);
                e.CellStyle.BackColor = Color.FromArgb(backColor);

                e.FormattingApplied = true;
            }
        }

        /// <summary>
        /// Draws colored rectangles for ForeColor and BackColor cells (Settings tab UX).
        /// </summary>
        void grdViews_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = grdViews.Columns[e.ColumnIndex].Name;
            if (colName != "ForeColor" && colName != "BackColor") return;

            // Get the ARGB integer value from the cell
            int argb = Convert.ToInt32(grdViews.Rows[e.RowIndex].Cells[e.ColumnIndex].Value ?? Color.Yellow.ToArgb());
            Color cellColor = Color.FromArgb(argb);

            // Paint the cell background and border
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

            // Draw a colored rectangle inset 4px from cell edges
            Rectangle colorRect = new Rectangle(
                e.CellBounds.X + 4,
                e.CellBounds.Y + 4,
                e.CellBounds.Width - 8,
                e.CellBounds.Height - 8);

            using (var brush = new SolidBrush(cellColor))
            {
                e.Graphics.FillRectangle(brush, colorRect);
            }

            // Draw a border around the colored rectangle
            using (var pen = new Pen(Color.DarkGray, 1))
            {
                e.Graphics.DrawRectangle(pen, colorRect);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles clicks on ForeColor and BackColor cells to open ColorDialog.
        /// </summary>
        void grdViews_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = grdViews.Columns[e.ColumnIndex].Name;
            if (colName != "ForeColor" && colName != "BackColor") return;

            // Get current color
            DataGridViewCell cell = grdViews.Rows[e.RowIndex].Cells[e.ColumnIndex];
            int currentArgb = Convert.ToInt32(cell.Value ?? Color.Yellow.ToArgb());
            Color currentColor = Color.FromArgb(currentArgb);

            // Show color picker
            using (ColorDialog dlg = new ColorDialog())
            {
                dlg.Color = currentColor;
                dlg.FullOpen = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    // Update cell value
                    cell.Value = dlg.Color.ToArgb();

                    // Persist to database
                    SaveDataViews();

                    // Refresh Example column preview
                    grdViews.InvalidateRow(e.RowIndex);

                    // Refresh mini views if active
                    miniViews.RefreshMiniViews(con, activeCharacterID);
                    UpdateMiniView();
                }
            }
        }

        void ValidateRowViews(object sender, DataGridViewCellCancelEventArgs data)
        {
            SaveDataViews();
            miniViews.RefreshMiniViews(con, activeCharacterID);
            UpdateMiniView();
        }

        void grdViews_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grdViews.IsCurrentCellDirty && grdViews.CurrentCell?.OwningColumn?.Name == "ActiveYn")
                grdViews.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        void grdViews_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == grdViews.Columns["ActiveYn"].Index)
            {
                SaveDataViews();
                miniViews.RefreshMiniViews(con, activeCharacterID);
                UpdateMiniView();
            }
        }

        void SaveDataViews()
        {
            viewsController?.SaveAll();
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



        void SaveDataCategories()
        {
            categoriesController?.SaveAll();
            RefreshGridCategorySource();
        }

        void SaveDataCharacters()
        {
            charactersController?.SaveAll();

            // Save all view positions to the miniviews table
            if (miniViews.MiniViewsActive())
            {
                Dictionary<int, Point> positions = miniViews.GetCurrentViewPositions();
                ViewsRepository.SaveViewPositions(con, positions);
            }

            // Detach SelectedIndexChanged before refreshing the combo box to
            // prevent a full LoadTimerRuntime cycle during exit/save.
            tscActiveCharacter.SelectedIndexChanged -= tscActiveCharacter_SelectedIndexChanged;
            SetupActiveCharacters();
            tscActiveCharacter.SelectedIndexChanged += tscActiveCharacter_SelectedIndexChanged;
        }

        void SaveDataTimers()
        {
            for (int r = 0; r < grdTimers.Rows.Count; r++)
            {
                DataGridViewRow row = grdTimers.Rows[r];
                grdTimers.EndEdit();
                TimersRepository.SaveTimer(con, grdTimers, row);
            }

            timerRuntime.SyncTimerFieldsFromGrid(grdTimers);
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

            durationCell.ErrorText = "Invalid Duration. Use 'HH:MM:SS' or 'DD HH:MM:SS' (or 'DDd HH:MM:SS')";

            // Check for DD HH:MM:SS or DDd HH:MM:SS (space separates days from time)
            int spaceIdx = durationText.IndexOf(' ');
            if (spaceIdx > 0)
            {
                string dayPart = durationText.Substring(0, spaceIdx).TrimEnd('d');
                if (dayPart.Length == 0 || !int.TryParse(dayPart, out _))
                    return false;

                string timePart = durationText.Substring(spaceIdx + 1);
                string[] parts = timePart.Split(':');
                if (parts.Length != 3)
                    return false;

                foreach (string p in parts)
                {
                    if (p.Length != 2 || !int.TryParse(p, out _))
                        return false;
                }

                durationCell.ErrorText = "";
                return true;
            }

            // HH:MM:SS
            string[] timeParts = durationText.Split(':');
            if (timeParts.Length != 3)
                return false;

            foreach (string p in timeParts)
            {
                if (p.Length != 2 || !int.TryParse(p, out _))
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
                InitialDirectory = SoundResolver.SoundsRoot,
                DereferenceLinks = false,
                AutoUpgradeEnabled = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                DataGridViewCell wavCell = (DataGridViewCell)grdTimers.Rows[rowIndex].Cells[grdTimers.Columns["WAVFile"].Index];

                string selected = openFileDialog.FileName;
                string relative = SoundResolver.GetRelativePath(selected);
                wavCell.Value = relative;

                // Persist to DB and sync to TimerRuntime immediately so
                // the active runtime state reflects the newly chosen sound.
                SaveDataTimers();
                SoundResolver.ClearCache();
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
            // Capture running state before stopping
            var states = timerRuntime.SaveCharacterState();
            TimerStateRepository.SaveTimerStates(con, states, activeCharacterID);

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
            var allCharacters = CharactersRepository.GetCharacters(con);
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
                tsbStartStopWatching.Checked = true;
                startStopWatchingToolStripMenuItem.Text = "&Stop Watching";
                startStopWatchingToolStripMenuItem.Image = iconStop;
                startStopWatchingToolStripMenuItem.Checked = true;
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
            tsbStartStopWatching.Checked = false;
            startStopWatchingToolStripMenuItem.Text = "&Start Watching";
            startStopWatchingToolStripMenuItem.Image = iconPlay;
            startStopWatchingToolStripMenuItem.Checked = false;
            statusParsing.Text = "Idle";

            // Clear temporary suppression — no log monitoring means nothing to suppress
            autoSwitchSuppressed = false;
            logMonitor.SuppressedAutoSwitchCharacterID = 0;

            // Hide browsing indicator when stopping log monitoring
            if (lblBrowsingIndicator != null)
                lblBrowsingIndicator.Visible = false;

            logMonitor.Stop();
        }

        /// <summary>
        /// Checks if the specified character's log file has been modified recently.
        /// Used to detect if a character is actually online when the app starts.
        /// </summary>
        /// <param name="characterID">Character ID to check</param>
        /// <param name="thresholdMinutes">Consider file "active" if modified within this many minutes (default: 5)</param>
        /// <returns>True if log file exists and was modified within threshold, false otherwise</returns>
        private bool IsCharacterLogActive(long characterID, int thresholdMinutes = 5)
        {
            try
            {
                // Get character's log file path from database
                var characters = CharactersRepository.GetCharacters(con);
                var character = characters.FirstOrDefault(c => c.ID == characterID);
                if (character == null || string.IsNullOrEmpty(character.LogFile))
                {
                    ThorneLog.Debug($"IsCharacterLogActive: charID={characterID} - no log file configured");
                    return false;
                }

                // Check if file exists and get last write time
                if (!File.Exists(character.LogFile))
                {
                    ThorneLog.Debug($"IsCharacterLogActive: charID={characterID} - log file does not exist: {character.LogFile}");
                    return false;
                }

                DateTime lastWrite = File.GetLastWriteTimeUtc(character.LogFile);
                double minutesSinceWrite = (DateTime.UtcNow - lastWrite).TotalMinutes;
                bool isActive = minutesSinceWrite <= thresholdMinutes;

                ThorneLog.Info($"IsCharacterLogActive: charID={characterID} ({character.Name}) - file: {character.LogFile}, lastWrite: {lastWrite:yyyy-MM-dd HH:mm:ss} UTC, minutesSince: {minutesSinceWrite:F1}, threshold: {thresholdMinutes}, isActive: {isActive}");
                return isActive;
            }
            catch (Exception ex)
            {
                ThorneLog.Error($"IsCharacterLogActive: charID={characterID} - exception: {ex.Message}");
                return false;
            }
        }

        private void OnLogChunkReceived(object sender, LogChunkReceivedEventArgs e)
        {
            // The active character's log is generating content.  If auto-switch
            // was temporarily suppressed (manual character switch), re-enable it
            // ONLY if the activity is from the NEW (active) character, not the
            // suppressed OLD character.
            if (autoSwitchSuppressed)
            {
                long currentCharID = 0;
                long.TryParse(activeCharacterID, out currentCharID);

                // Only clear suppression if the active character is NOT the suppressed one
                if (currentCharID > 0 && currentCharID != logMonitor.SuppressedAutoSwitchCharacterID)
                {
                    autoSwitchSuppressed = false;
                    logMonitor.SuppressedAutoSwitchCharacterID = 0;
                    this.BeginInvoke(new Action(() =>
                    {
                        if (tsbStartStopWatching.Text == stopWatchingText && logMonitor.FilePath != null)
                            statusParsing.Text = "Watching: " + Path.GetFileName(logMonitor.FilePath);
                    }));
                }
            }

            timerRuntime.ProcessLogText(e.Text);
        }

        private void autoSwitchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool enabled = autoSwitchToolStripMenuItem.Checked;
            tsbAutoSwitch.Checked = enabled;
            autoSwitchSuppressed = false;  // explicit toggle overrides temporary suppression
            logMonitor.SuppressedAutoSwitchCharacterID = 0;
            logMonitor.AutoSwitchEnabled = enabled;
            Database.SetSetting(con, "AutoSwitchEnabled", enabled ? "1" : "0");
        }

        private void tsbAutoSwitch_Click(object sender, EventArgs e)
        {
            bool enabled = tsbAutoSwitch.Checked;
            autoSwitchToolStripMenuItem.Checked = enabled;
            autoSwitchSuppressed = false;  // explicit toggle overrides temporary suppression
            logMonitor.SuppressedAutoSwitchCharacterID = 0;
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

        private void tsbShowActiveOnly_Click(object sender, EventArgs e)
        {
            bool activeOnly = tsbShowActiveOnly.Checked;
            showActiveOnlyToolStripMenuItem.Checked = activeOnly;
            timerRuntime.ShowActiveOnly = activeOnly;
            Database.SetSetting(con, "ShowActiveOnly", activeOnly ? "1" : "0");
            RefreshTimerGridDataSource();
            RepaintTimerGrid();
        }

        private void showActiveOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool activeOnly = showActiveOnlyToolStripMenuItem.Checked;
            tsbShowActiveOnly.Checked = activeOnly;
            timerRuntime.ShowActiveOnly = activeOnly;
            Database.SetSetting(con, "ShowActiveOnly", activeOnly ? "1" : "0");
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
        /// Columns hidden in compact mode — trigger, notification, and
        /// seldom-used configuration columns that aren't needed during
        /// active play.
        /// </summary>
        private static readonly string[] CompactHiddenColumns = new[]
        {
            "StartKeyword", "EndKeyword", "Speech", "WAVFile", "WAV",
            "CaseYn", "EndlessYn",
            "DependsOnTimer", "DependsOnDelay"
        };

        /// <summary>
        /// Columns made read-only in compact mode to prevent accidental
        /// edits during gameplay. These remain visible for sorting and
        /// at-a-glance identification.
        /// </summary>
        private static readonly string[] CompactReadOnlyColumns = new[]
        {
            "Name", "CategoryID", "Style", "ClassID", "Scope", "Duration"
        };

        /// <summary>
        /// Toggles visibility of configuration columns on the timer grid.
        /// Compact mode shows: Active, Name, Class, Style, Scope, Category,
        /// Duration, Remaining, Count, Start/Stop — with classification
        /// columns made read-only to prevent accidental edits during play.
        /// When switching to full view, auto-widens the window if it is
        /// too narrow for all columns, clamped to the current screen.
        /// Saves and restores per-view FillWeights so column proportions
        /// survive the toggle without being redistributed by Fill mode.
        /// </summary>
        private void ApplyCompactView(bool compact, bool initializing = false)
        {
            // Capture current FillWeights before the toggle
            if (!initializing)
            {
                var weights = new Dictionary<string, float>();
                foreach (DataGridViewColumn col in grdTimers.Columns)
                    weights[col.Name] = col.FillWeight;

                if (compact) // leaving advanced → save advanced weights
                    _advancedFillWeights = weights;
                else         // leaving compact  → save compact weights
                    _compactFillWeights = weights;
            }

            BeginGridUpdate();
            try
            {
                foreach (string colName in CompactHiddenColumns)
                {
                    if (grdTimers.Columns.Contains(colName))
                    {
                        grdTimers.Columns[colName].Visible = !compact;
                    }
                }

                // Make classification columns read-only in compact mode
                // to prevent accidental edits while playing.
                foreach (string colName in CompactReadOnlyColumns)
                {
                    if (grdTimers.Columns.Contains(colName))
                    {
                        grdTimers.Columns[colName].ReadOnly = compact;
                    }
                }
            }
            finally
            {
                EndGridUpdate();
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

            // Restore FillWeights for the target view so columns keep
            // the proportions the user had before the last toggle.
            var target = compact ? _compactFillWeights : _advancedFillWeights;
            if (target != null && target.Count > 0)
            {
                foreach (var kvp in target)
                {
                    if (grdTimers.Columns.Contains(kvp.Key))
                    {
                        var col = grdTimers.Columns[kvp.Key];
                        if (col.Visible && kvp.Value > 0)
                            col.FillWeight = kvp.Value;
                    }
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
            var character = CharactersRepository.GetCharacter(con, activeCharacterID);
            return character.ClassID;
        }

        /// <summary>
        /// Refreshes the timer grid by rebuilding _visibleTimers from _allTimers
        /// based on the active character's class and the ShowAllClasses /
        /// ShowActiveOnly filters, then swapping it onto grdTimers.DataSource
        /// in a single assignment.
        ///
        /// This replaces an earlier per-row Visible toggle loop that cost
        /// ~14 ms per row on a 100+ row grid (~1.8 s on character switch).
        /// Swapping the bound list lets DataGridView do its bulk reset once
        /// and avoids the layout cascade triggered by individual row.Visible
        /// mutations.  See Docs/perf/grid-filter-refactor.md.
        /// </summary>
        private void RefreshTimerGridDataSource()
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(RefreshTimerGridDataSource));
                return;
            }

            if (_allTimers == null) return;

            long classID = GetActiveCharacterClassID();
            bool showAll = timerRuntime.ShowAllClasses;
            bool activeOnly = timerRuntime.ShowActiveOnly;

            // Short-circuit when the filter inputs haven't changed since the
            // last bind — avoids redundant rebuilds during the three back-to-back
            // RefreshGridAfterSort calls on startup.  _allTimersVersion forces a
            // refresh after add/delete even if the filter signature is otherwise
            // identical.
            string signature = string.Format("{0}|{1}|{2}|{3}",
                classID, showAll ? 1 : 0, activeOnly ? 1 : 0, _allTimersVersion);
            if (signature == _appliedFilterSignature)
                return;

            // Capture sort state and selection so we can restore them on the
            // new bound list — re-binding clears CurrentCell and resets sort.
            var oldList = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
            var oldSort = oldList != null ? oldList.SortDescriptions : null;

            long selectedTimerID = -1;
            if (grdTimers.CurrentCell != null)
            {
                var idCol = grdTimers.Columns["ID"];
                var selRow = grdTimers.CurrentCell.OwningRow;
                if (idCol != null && selRow != null && !selRow.IsNewRow && selRow.Cells[idCol.Index].Value != null)
                {
                    selectedTimerID = Convert.ToInt64(selRow.Cells[idCol.Index].Value);
                }
            }

            // Build the filtered visible list from _allTimers.
            var filtered = new List<Timers.GridData>(_allTimers.Count);
            using (ThorneLog.Time($"RefreshTimerGridDataSource: filter ({_allTimers.Count} timers)"))
            {
                foreach (var gd in _allTimers)
                {
                    if (!showAll)
                    {
                        if (!(gd.ClassID == 0 || (classID > 0 && gd.ClassID == classID)))
                            continue;
                    }
                    if (activeOnly && gd.ActiveYn != 1)
                        continue;

                    filtered.Add(gd);
                }
            }

            // Swap the bound list in a single assignment.
            _visibleTimers = new SortableBindingList<Timers.GridData>(filtered);
            RegisterTimerDisplayResolvers(_visibleTimers);

            grdTimers.CurrentCell = null;
            BeginGridUpdate();
            try
            {
                using (ThorneLog.Time($"RefreshTimerGridDataSource: bind ({filtered.Count} rows)"))
                {
                    grdTimers.DataSource = _visibleTimers;
                }

                // Re-apply column DisplayIndex order — DataGridView resets
                // DisplayIndex to the property-declaration order whenever
                // DataSource is reassigned.
                ResetTimersGridColumns();

                // Restore prior sort if any.
                if (oldSort != null && oldSort.Count > 0)
                {
                    var sorts = new (string, ListSortDirection)[oldSort.Count];
                    for (int i = 0; i < oldSort.Count; i++)
                        sorts[i] = (oldSort[i].PropertyDescriptor.Name, oldSort[i].SortDirection);
                    using (ThorneLog.Time("RefreshTimerGridDataSource: reapply sort"))
                        _visibleTimers.ApplyMultiSort(sorts);
                }
            }
            finally
            {
                EndGridUpdate();
            }

            _appliedFilterSignature = signature;

            // Re-apply row colors — reassigning DataSource produces a fresh
            // set of DataGridViewRow objects with default cell styles, so the
            // Gainsboro tint we set on inactive rows (and the lightened style
            // color on running rows) is lost on every swap.  SyncRuntimeToGrid
            // covers this when RefreshGridAfterSort is the caller, but the
            // filter-toggle handlers call RefreshTimerGridDataSource directly.
            using (ThorneLog.Time("RefreshTimerGridDataSource: reapply row colors"))
            {
                var idCol2 = grdTimers.Columns["ID"];
                if (idCol2 != null)
                {
                    var stateDict = timerRuntime.GetAllStates().ToDictionary(s => s.TimerID);
                    foreach (DataGridViewRow row in grdTimers.Rows)
                    {
                        if (row.IsNewRow) continue;
                        var idVal = row.Cells[idCol2.Index].Value;
                        if (idVal == null) continue;
                        if (stateDict.TryGetValue(Convert.ToInt64(idVal), out var ts))
                            ApplyTimerRowColor(row, ts);
                    }
                }
            }

            // Restore selection to the previously-selected timer if still
            // visible; otherwise pick the first visible row.
            using (ThorneLog.Time("RefreshTimerGridDataSource: restore selection"))
            {
                var idCol = grdTimers.Columns["ID"];
                var nameCol = grdTimers.Columns["Name"];
                if (idCol != null && nameCol != null)
                {
                    DataGridViewRow target = null;
                    foreach (DataGridViewRow row in grdTimers.Rows)
                    {
                        if (row.IsNewRow) continue;
                        if (selectedTimerID >= 0 && row.Cells[idCol.Index].Value != null
                            && Convert.ToInt64(row.Cells[idCol.Index].Value) == selectedTimerID)
                        {
                            target = row;
                            break;
                        }
                        if (target == null) target = row;
                    }
                    if (target != null)
                        grdTimers.CurrentCell = target.Cells[nameCol.Index];
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

            ThorneLog.Separator("CHARACTER SWITCH (auto)");
            ThorneLog.Info($"Auto-switch FROM charID={activeCharacterID} TO charID={e.NewCharacterID}");
            ThorneLog.DumpTimerGrid("AutoSwitch-before", grdTimers);

            // Save outgoing character's timer state
            var outgoingStates = timerRuntime.SaveCharacterState();
            TimerStateRepository.SaveTimerStates(con, outgoingStates, activeCharacterID);

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

            // Suppress per-cell repaints during the full reload cycle
            grdTimers.Visible = false;
            BeginGridUpdate();
            try
            {
                // Reload timers and restore incoming character's state
                LoadTimerRuntime();

                ThorneLog.DumpTimerGrid("AutoSwitch-after", grdTimers);

                // Re-apply sort order — LoadTimerRuntime changed cell
                // values so the rows may be in stale order.
                var autoList = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
                if (autoList != null)
                    autoList.ReapplySort();
                RefreshGridAfterSort();
            }
            finally
            {
                EndGridUpdate();
                grdTimers.Visible = true;
            }

            // Update status bar — auto-switch just fired, so suppression is cleared
            autoSwitchSuppressed = false;
            logMonitor.SuppressedAutoSwitchCharacterID = 0;
            statusParsing.Text = "Watching: " + Path.GetFileName(logMonitor.FilePath) + " (auto)";
            lblBrowsingIndicator.Visible = false; // Auto-switch means we're viewing the active character

            // Refresh mini views
            UpdateMiniView();
        }

        /// <summary>
        /// Handles camp-out detection from LogMonitor.
        /// Sets active character to "None" (0) which stops all Character-scope timers.
        /// World and Character+ timers continue running.
        /// </summary>
        private void OnCharacterCampedOut(object sender, CharacterSwitchedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnCharacterCampedOut(sender, e)));
                return;
            }

            ThorneLog.Separator("CHARACTER CAMP-OUT (auto)");
            ThorneLog.Info($"Camp-out detected for charID={e.OldCharacterID}");
            ThorneLog.DumpTimerGrid("CampOut-before", grdTimers);

            // Save outgoing character's timer state
            var outgoingStates = timerRuntime.SaveCharacterState();
            TimerStateRepository.SaveTimerStates(con, outgoingStates, activeCharacterID);

            // Set active character to "None" (0)
            activeCharacterID = "0";
            Database.SetSetting(con, "ActiveCharacterID", activeCharacterID);

            // Update the character dropdown to "(None)" without triggering SelectedIndexChanged
            tscActiveCharacter.SelectedIndexChanged -= tscActiveCharacter_SelectedIndexChanged;
            foreach (ComboBoxItem item in (List<ComboBoxItem>)tscActiveCharacter.ComboBox.DataSource)
            {
                if (Convert.ToInt64(item.Value) == 0)
                {
                    tscActiveCharacter.SelectedItem = item;
                    break;
                }
            }
            tscActiveCharacter.SelectedIndexChanged += tscActiveCharacter_SelectedIndexChanged;

            // Tell LogMonitor there's no active character
            logMonitor.SetActiveCharacter(0);

            // Suppress per-cell repaints during the reload cycle
            grdTimers.Visible = false;
            BeginGridUpdate();
            try
            {
                // Reload timers — this will stop all Character-scope timers
                // since activeCharacterID is now "0"
                LoadTimerRuntime();

                ThorneLog.DumpTimerGrid("CampOut-after", grdTimers);

                // Re-apply sort order
                var list = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
                if (list != null)
                    list.ReapplySort();
                RefreshGridAfterSort();
            }
            finally
            {
                EndGridUpdate();
                grdTimers.Visible = true;
            }

            // Update status bar
            autoSwitchSuppressed = false;
            logMonitor.SuppressedAutoSwitchCharacterID = 0;
            statusParsing.Text = "Watching: (no active character)";
            lblBrowsingIndicator.Visible = false; // No active character means no browsing mode

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
        /// Returns the loaded saved states so callers (e.g. startup) can reuse
        /// them for RestoreWorldTimersOnStartup without a redundant DB query.
        /// </summary>
        private Dictionary<long, TimerState> LoadTimerRuntime()
        {
            ThorneLog.Info($"LoadTimerRuntime: activeCharacterID={activeCharacterID}");
            using (ThorneLog.Time("LoadTimerRuntime total"))
            {
                SortableBindingList<Timers.GridData> timerData;
                using (ThorneLog.Time("LoadTimerRuntime: TimersRepository.GetTimers"))
                    timerData = TimersRepository.GetTimers(con);
                using (ThorneLog.Time("LoadTimerRuntime: timerRuntime.LoadTimers"))
                    timerRuntime.LoadTimers(timerData);

                List<Categories.GridData> catData;
                using (ThorneLog.Time("LoadTimerRuntime: CategoriesRepository.GetCategories"))
                    catData = CategoriesRepository.GetCategories(con);
                using (ThorneLog.Time("LoadTimerRuntime: timerRuntime.LoadCategories"))
                    timerRuntime.LoadCategories(catData);

                // Determine if this character is actually active in LogMonitor
                // (actively logging) vs just being viewed in the UI.
                // Character-scope timers should only run when actively logging.
                long currentCharID = 0;
                long.TryParse(activeCharacterID, out currentCharID);
                bool isActive = logMonitor.IsRunning && logMonitor.GetActiveCharacterID() == currentCharID;

                // Restore persisted Character-scope timer state
                ThorneLog.Debug($"LoadTimerRuntime: calling LoadTimerStates for charID={activeCharacterID}");
                Dictionary<long, TimerState> savedStates;
                using (ThorneLog.Time("LoadTimerRuntime: TimerStateRepository.LoadTimerStates"))
                    savedStates = TimerStateRepository.LoadTimerStates(con, activeCharacterID);
                ThorneLog.Debug($"LoadTimerRuntime: calling RestoreCharacterState with {savedStates.Count} saved states, isActive={isActive}");
                using (ThorneLog.Time("LoadTimerRuntime: timerRuntime.RestoreCharacterState"))
                    timerRuntime.RestoreCharacterState(savedStates, isActive);

                // Push the now-current per-character ActiveYn (and any other
                // runtime-tracked fields used for filtering) from the runtime
                // back into the master _allTimers list.  RefreshTimerGridDataSource
                // filters by GridData.ActiveYn / ClassID, so the master list must
                // reflect the active character's state before the next rebuild.
                if (_allTimers != null)
                {
                    using (ThorneLog.Time("LoadTimerRuntime: sync ActiveYn into _allTimers"))
                    {
                        var stateDict = timerRuntime.GetAllStates().ToDictionary(s => s.TimerID);
                        foreach (var gd in _allTimers)
                        {
                            if (stateDict.TryGetValue(gd.ID, out var ts))
                                gd.ActiveYn = ts.ActiveYn;
                        }
                        _allTimersVersion++;
                    }
                }

                ThorneLog.Debug("LoadTimerRuntime: calling SyncRuntimeToGrid");

                // Sync to grid
                using (ThorneLog.Time("LoadTimerRuntime: SyncRuntimeToGrid"))
                    SyncRuntimeToGrid();

                ThorneLog.Info("LoadTimerRuntime: complete");
                return savedStates;
            }
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

            ThorneLog.Debug($"SyncRuntimeToGrid: {grdTimers.Rows.Count} grid rows");

            using (ThorneLog.Time($"SyncRuntimeToGrid ({grdTimers.Rows.Count} rows)"))
            {
                BeginGridUpdate();
                try
                {
                // Build dictionary for O(1) lookups instead of O(n) FirstOrDefault per row
                var stateDict = timerRuntime.GetAllStates().ToDictionary(s => s.TimerID);

                foreach (DataGridViewRow row in grdTimers.Rows)
                {
                    long rowID = Convert.ToInt64(row.Cells[grdTimers.Columns["ID"].Index].Value);
                    if (!stateDict.TryGetValue(rowID, out var ts)) continue;

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
            }
            finally
            {
                EndGridUpdate();
            }

            RepaintTimerGrid();
            UpdateMiniView();
            }
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
        /// Returns the configured base color for the given timer style.
        /// Style owns visual identity; views own window/display behavior.
        /// </summary>
        private Color GetStyleColor(string style)
        {
            if (stylesRepository == null)
                stylesRepository = new StylesRepository(con);

            return stylesRepository.GetRowBaseColor(style);
        }

        /// <summary>
        /// Applies row colors based on timer state and style.
        /// Running timers paint the entire row with a lightened version
        /// of their style base color,
        /// with a deeper accent on the Remaining cell.
        /// Inactive timers get a soft gray row — neutral so it doesn't
        /// compete with any user-chosen style color (e.g. red Lockout).
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
                Color bgColor = ts.IsActive ? Color.White : Color.Gainsboro;
                row.DefaultCellStyle.BackColor = bgColor;
                remainingCell.Style.BackColor = bgColor;
            }

            grdTimers.InvalidateRow(row.Index);
        }

        /// <summary>
        /// Handles TimerStateChanged events from TimerRuntime — updates the grid row for the affected timer.
        /// </summary>
        private void OnTimerStateChanged(object sender, TimerStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                // Capture the active character NOW (on the firing thread) so
                // that late-arriving events queued via BeginInvoke don't
                // persist state under a different character after a switch.
                string capturedCharID = activeCharacterID;
                this.BeginInvoke(new Action(() => HandleTimerStateChanged(e, capturedCharID)));
                return;
            }

            HandleTimerStateChanged(e, activeCharacterID);
        }

        private void HandleTimerStateChanged(TimerStateChangedEventArgs e, string forCharacterID)
        {
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
                UpdateMiniView(e.IsTransition);

                // Persist on meaningful state transitions (start, stop, expire,
                // keyword-stop, deactivate-stop, offline-expire).  This is the
                // single persistence point for ALL timer scopes — no per-scope
                // or per-UI-path special cases.  Ticks are excluded so we only
                // write to the DB when something actually changes.
                //
                // Guard: if the character has changed since this event fired
                // (e.g. BeginInvoke callback arrived after a character switch),
                // skip persistence — the event belongs to the old character and
                // LoadTimerRuntime has already loaded the new character's state.
                if (e.IsTransition && forCharacterID == activeCharacterID)
                {
                    var tsPersist = timerRuntime.GetState(e.TimerID);
                    if (tsPersist != null)
                    {
                        if (tsPersist.Scope == "Character" || tsPersist.Scope == "Character+")
                            ThorneLog.Debug($"HandleStateChanged PERSIST TID={e.TimerID} Scope={tsPersist.Scope} charID={activeCharacterID} Btn={tsPersist.ButtonState} Rem={tsPersist.Remaining} Act={tsPersist.ActiveYn}");
                        TimerStateRepository.SaveSingleTimerState(con, tsPersist, activeCharacterID);
                    }
                }
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
                try
                {
                    string resolvedPath = SoundResolver.Resolve(e.WAVFile);
                    if (resolvedPath != null)
                    {
                        ThorneLog.Info($"Playing sound: \"{e.WAVFile}\" → {resolvedPath}");
                        SoundPlayer sp = new SoundPlayer(resolvedPath);
                        sp.Play();
                    }
                    else
                    {
                        ThorneLog.Warn($"Sound not found: \"{e.WAVFile}\"");
                    }
                }
                catch (Exception ex)
                {
                    ThorneLog.Error($"Sound playback error for \"{e.WAVFile}\": {ex.Message}");
                }
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
                    int rowIndex = grdTimers.CurrentCell.RowIndex;
                    long timerID = Convert.ToInt64(grdTimers.Rows[rowIndex].Cells[grdTimers.Columns["ID"].Index].Value);

                    // Stop this specific timer if running, via TimerRuntime
                    timerRuntime.StopTimer(timerID);

                    TimersRepository.DeleteTimer(con, Convert.ToString(timerID));

                    // Remove from existing data source — preserves sort/filter
                    timerRuntime.RemoveTimerState(timerID);
                    var data = (SortableBindingList<Timers.GridData>)grdTimers.DataSource;
                    var item = data.FirstOrDefault(g => g.ID == timerID);
                    if (item != null)
                        data.Remove(item);

                    // Mirror the deletion in the master list so a later
                    // filter rebuild doesn't resurrect the row.
                    if (_allTimers != null)
                    {
                        var masterItem = _allTimers.FirstOrDefault(g => g.ID == timerID);
                        if (masterItem != null)
                            _allTimers.Remove(masterItem);
                        _allTimersVersion++;
                    }

                    RepaintTimerGrid();
                }
            }
        }

        private void btnAddTimer_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = grdTimers.CurrentRow;

            if (row == null || ValidDataTimers(row))
            {
                // Switch to full view if compact so the user can edit all fields
                if (tsbCompactView.Checked)
                {
                    tsbCompactView.Checked = false;
                    compactViewToolStripMenuItem.Checked = false;
                    Database.SetSetting(con, "CompactView", "0");
                    ApplyCompactView(false);
                }

                // Add to the existing data source — preserves sort/filter
                var data = (SortableBindingList<Timers.GridData>)grdTimers.DataSource;

                Timers.GridData gd = new Timers.GridData
                {
                    ID = -1,
                    ActiveYn = 1,
                    Style = "Normal",
                    Scope = "World",
                    DependsOnTimer = "",
                    DependsOnDelay = 0,
                    ClassID = 0,
                    Duration = noTime
                };
                data.Add(gd);

                // Also add to the master list so the new timer survives
                // any subsequent filter rebuild.  Defaults (ActiveYn=1,
                // ClassID=0) are filter-safe under both ShowActiveOnly and
                // class-restricted views, so the row stays visible.
                if (_allTimers != null)
                {
                    _allTimers.Add(gd);
                    _allTimersVersion++;
                }

                // Find the new row (may not be last if a sort is active)
                int newRowIndex = -1;
                for (int r = 0; r < grdTimers.Rows.Count; r++)
                {
                    if (Convert.ToInt64(grdTimers.Rows[r].Cells[grdTimers.Columns["ID"].Index].Value) == -1)
                    {
                        newRowIndex = r;
                        break;
                    }
                }
                if (newRowIndex < 0) newRowIndex = grdTimers.Rows.Count - 1;

                // Save immediately to get a real DB ID
                DataGridViewRow newRow = grdTimers.Rows[newRowIndex];
                grdTimers.EndEdit();
                TimersRepository.SaveTimer(con, grdTimers, newRow);

                // Register in the runtime with the real ID
                timerRuntime.AddTimerState(gd);

                // Apply row color and navigate for editing
                var ts = timerRuntime.GetState(gd.ID);
                if (ts != null)
                    ApplyTimerRowColor(newRow, ts);

                grdTimers.CurrentCell = newRow.Cells[grdTimers.Columns["Name"].Index];
                grdTimers.BeginEdit(true);
            }
        }

        private void btnDeleteCharacter_Click(object sender, EventArgs e)
        {
            charactersController?.DeleteCurrentCharacter();
        }

        private void btnAddCharacter_Click(object sender, EventArgs e)
        {
            charactersController?.AddCharacter();
        }

        private void tscActiveCharacter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ThorneLog.Separator("CHARACTER SWITCH (manual)");
            ThorneLog.Info($"Switch FROM charID={activeCharacterID}");
            ThorneLog.DumpTimerGrid("ManualSwitch-before", grdTimers);
            using (ThorneLog.Time("CharacterSwitch TOTAL (manual)"))
            {

            // Capture OLD character ID before changing activeCharacterID
            long oldCharID = 0;
            long.TryParse(activeCharacterID, out oldCharID);

            // Determine which character is actively logging in LogMonitor
            long loggingCharID = logMonitor.IsRunning ? logMonitor.GetActiveCharacterID() : 0;
            ThorneLog.Info($"LogMonitor active character: charID={loggingCharID}");

            // Save outgoing character's timer state before switching
            // This saves Character/Character+ timer state to DB for the OLD character
            List<TimerState> outgoingStates;
            using (ThorneLog.Time("CharacterSwitch: SaveCharacterState"))
                outgoingStates = timerRuntime.SaveCharacterState();
            using (ThorneLog.Time("CharacterSwitch: TimerStateRepository.SaveTimerStates"))
                TimerStateRepository.SaveTimerStates(con, outgoingStates, activeCharacterID);

            activeCharacterID = (tscActiveCharacter.SelectedItem as ComboBoxItem).Value.ToString();
            ThorneLog.Info($"Switch TO charID={activeCharacterID}");
            Database.SetSetting(con, "ActiveCharacterID", activeCharacterID);

            // Update mini-view character display if views are active
            if (miniViews.MiniViewsActive())
            {
                miniViews.UpdateActiveCharacter(con, activeCharacterID);
            }

            // Tell LogMonitor which character is now active (in UI, not necessarily logging)
            long newCharID = 0;
            long.TryParse(activeCharacterID, out newCharID);
            logMonitor.SetActiveCharacter(newCharID);

            // Temporarily suppress auto-switch for the OLD character (not NEW) so
            // the log monitor doesn't immediately yank back to the previously-playing
            // character.  Only the OLD (outgoing) character is suppressed — a
            // brand-new login on a different character will still trigger a switch.
            // Re-enables automatically when the NEW (active) character's log
            // generates content (see OnLogChunkReceived).
            if (logMonitor.AutoSwitchEnabled && oldCharID > 0)
            {
                autoSwitchSuppressed = true;
                logMonitor.SuppressedAutoSwitchCharacterID = oldCharID;  // FIX: Suppress OLD, not NEW
            }

            // Suppress per-cell repaints during the full reload cycle
            grdTimers.Visible = false;
            BeginGridUpdate();
            try
            {
                // Reload timers and restore incoming character's state
                LoadTimerRuntime();

                ThorneLog.DumpTimerGrid("ManualSwitch-after", grdTimers);

                // Re-apply sort order — LoadTimerRuntime changed cell
                // values so the rows may be in stale order.
                var manualList = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
                if (manualList != null)
                    manualList.ReapplySort();
                using (ThorneLog.Time("CharacterSwitch: RefreshGridAfterSort"))
                    RefreshGridAfterSort();
            }
            finally
            {
                EndGridUpdate();
                grdTimers.Visible = true;
            }

            // Update status bar and browsing indicator if watching
            if (tsbStartStopWatching.Text == stopWatchingText)
            {
                if (newCharID == 0)
                {
                    statusParsing.Text = "Watching: all characters";
                    lblBrowsingIndicator.Visible = false;
                }
                else if (loggingCharID > 0 && loggingCharID != newCharID)
                {
                    // Browsing mode: viewing different character than actively logging one
                    var loggingChar = CharactersRepository.GetCharacter(con, loggingCharID.ToString());
                    var viewingChar = CharactersRepository.GetCharacter(con, activeCharacterID);
                    statusParsing.Text = $"Active: {loggingChar.Name} | Viewing: {viewingChar.Name}";
                    lblBrowsingIndicator.Text = $"⚠ Browsing Mode — {loggingChar.Name} is actively logging. Character-scope timers for {viewingChar.Name} are paused.";
                    lblBrowsingIndicator.Visible = true;
                }
                else
                {
                    // Normal mode: viewing the actively logging character
                    statusParsing.Text = autoSwitchSuppressed
                        ? "Watching: " + Path.GetFileName(logMonitor.FilePath) + " (auto-switch paused)"
                        : "Watching: " + Path.GetFileName(logMonitor.FilePath);
                    lblBrowsingIndicator.Visible = false;
                }
            }
            else
            {
                lblBrowsingIndicator.Visible = false;
            }

            UpdateMiniView();
            }
        }

        private void btnAddCategory_Click(object sender, EventArgs e)
        {
            categoriesController?.AddCategory();
        }

        private void btnDeleteCategory_Click(object sender, EventArgs e)
        {
            categoriesController?.DeleteCurrentCategory();
        }

        private void btnAddView_Click(object sender, EventArgs e)
        {
            viewsController?.AddView();
        }

        private void btnDeleteView_Click(object sender, EventArgs e)
        {
            viewsController?.DeleteCurrentView();
        }

        private void btnAddStyle_Click(object sender, EventArgs e)
        {
            stylesController?.AddStyle();
        }

        private void btnDeleteStyle_Click(object sender, EventArgs e)
        {
            stylesController?.DeleteCurrentStyle();
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
            miniViewSettingsManager.OnFontSizeChanged();
        }

        private void lblWarnPickFore_Click(object sender, EventArgs e)
        {
            miniViewSettingsManager.PickWarningForeColor();
        }

        private void lblWarnPickBack_Click(object sender, EventArgs e)
        {
            miniViewSettingsManager.PickWarningBackColor();
        }

        private void WarningTime_LostFocus(object sender, EventArgs e)
        {
            if (!miniViewSettingsManager.TryCommitWarningTime())
            {
                MessageBox.Show("Invalid Warning Time Format. Use 'MM:SS'", "Error");
                tabCtrlMain.SelectedIndex = 3;
                txtWarningTime.Focus();
            }
        }

        private void tbOpacity_Scroll(object sender, EventArgs e)
        {
            miniViewSettingsManager.OnOpacityChanged();
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

        // v0.6.0: Removed obsolete per-style color pickers (now per-view configuration)

        private void tomeInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string dbPath = Properties.Settings.Default.DatabasePath ?? Database.GetDefaultDatabasePath();
            var infoForm = new FormTomeInfo(con, dbPath)
            {
                StartPosition = FormStartPosition.CenterParent
            };
            infoForm.ShowDialog(this);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var aboutForm = new FormAbout
            {
                StartPosition = FormStartPosition.CenterParent
            };
            aboutForm.ShowDialog(this);
        }

        // v0.6.0: Removed obsolete Ping color pickers, PingTime, and ShowPing checkbox (now per-view)

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            StopAllTimers();
        }

        private void grdTimers_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var list = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
            string dataPropertyName = grdTimers.Columns[e.ColumnIndex].DataPropertyName;
            var prop = (list != null && !string.IsNullOrEmpty(dataPropertyName))
                ? TypeDescriptor.GetProperties(typeof(Timers.GridData))[dataPropertyName]
                : null;

            if (prop != null && list != null)
            {
                // Ctrl+Click: remove column from multi-sort chain
                if (Control.ModifierKeys.HasFlag(Keys.Control))
                {
                    list.RemoveSortColumn(prop);
                }
                // Shift+Click: add to multi-column sort chain or toggle direction
                else if (Control.ModifierKeys.HasFlag(Keys.Shift))
                {
                    list.AddOrToggleSortColumn(prop);
                }
                else
                {
                    // Normal click: single-column sort with asc/desc toggle.
                    // If already sorting by this column alone, toggle direction;
                    // otherwise start a fresh ascending sort.
                    var descs = list.SortDescriptions;
                    ListSortDirection newDir = ListSortDirection.Ascending;

                    if (descs.Count == 1 && descs[0].PropertyDescriptor.Name == prop.Name)
                    {
                        newDir = descs[0].SortDirection == ListSortDirection.Ascending
                            ? ListSortDirection.Descending
                            : ListSortDirection.Ascending;
                    }

                    list.ApplyMultiSort((prop.Name, newDir));
                }

                UpdateSortGlyphs();
                UpdateGroupSortCheckedState();
            }

            RefreshGridAfterSort();
        }

        /// <summary>
        /// Suspends grid layout and disables AutoSizeColumnsMode to prevent
        /// per-cell/per-row repaint during bulk operations.
        /// Supports nesting — only the outermost call changes the grid state.
        /// Always pair with EndGridUpdate().
        /// </summary>
        private void BeginGridUpdate()
        {
            if (_gridUpdateDepth == 0)
            {
                grdTimers.SuspendLayout();
                grdTimers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            }
            _gridUpdateDepth++;
        }

        /// <summary>
        /// Resumes grid layout and restores AutoSizeColumnsMode after a bulk operation.
        /// Only the outermost EndGridUpdate actually restores the grid.
        /// </summary>
        private void EndGridUpdate()
        {
            _gridUpdateDepth--;
            if (_gridUpdateDepth == 0)
            {
                grdTimers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                grdTimers.ResumeLayout(true);
            }
        }

        /// <summary>
        /// Post-sort recovery: reapplies row colors, class-based row visibility,
        /// and column header sort glyphs.  Call after any operation that triggers
        /// a sort (which fires ListChanged Reset, clearing custom cell styles
        /// and row visibility).
        /// </summary>
        private void RefreshGridAfterSort()
        {
            using (ThorneLog.Time("RefreshGridAfterSort TOTAL"))
            {
                // Filter first so SyncRuntimeToGrid only walks visible rows
                // (the filter swaps DataSource, so any prior cell-level edits
                // are discarded anyway).
                using (ThorneLog.Time("RefreshGridAfterSort: RefreshTimerGridDataSource"))
                    RefreshTimerGridDataSource();
                using (ThorneLog.Time("RefreshGridAfterSort: SyncRuntimeToGrid"))
                    SyncRuntimeToGrid();
                using (ThorneLog.Time("RefreshGridAfterSort: UpdateSortGlyphs"))
                    UpdateSortGlyphs();
            }
        }

        /// <summary>
        /// Updates column header sort glyphs to reflect the current sort state.
        /// Uses native OS-styled arrows via SortGlyphDirection.
        /// When multi-column sorting is active (2+ columns), shows a summary
        /// in the status strip; hidden otherwise.
        /// </summary>
        private void UpdateSortGlyphs()
        {
            var list = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
            if (list == null) return;

            var descriptions = list.SortDescriptions;

            // Clear all glyphs
            foreach (DataGridViewColumn col in grdTimers.Columns)
            {
                if (col.SortMode != DataGridViewColumnSortMode.NotSortable)
                    col.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            // Set glyphs on sorted columns
            for (int i = 0; i < descriptions.Count; i++)
            {
                string propName = descriptions[i].PropertyDescriptor.Name;
                foreach (DataGridViewColumn col in grdTimers.Columns)
                {
                    if (col.DataPropertyName == propName)
                    {
                        if (col.SortMode != DataGridViewColumnSortMode.NotSortable)
                            col.HeaderCell.SortGlyphDirection = descriptions[i].SortDirection == ListSortDirection.Ascending
                                ? SortOrder.Ascending
                                : SortOrder.Descending;
                        break;
                    }
                }
            }

            // Show multi-sort summary in status strip only when 2+ columns are sorted
            if (descriptions.Count > 1)
            {
                var parts = new string[descriptions.Count];
                for (int i = 0; i < descriptions.Count; i++)
                {
                    string propName = descriptions[i].PropertyDescriptor.Name;
                    string header = propName;
                    foreach (DataGridViewColumn col in grdTimers.Columns)
                    {
                        if (col.DataPropertyName == propName)
                        {
                            header = col.HeaderText;
                            break;
                        }
                    }
                    string arrow = descriptions[i].SortDirection == ListSortDirection.Ascending ? "\u25B2" : "\u25BC";
                    parts[i] = header + " " + arrow;
                }
                statusSortInfo.Text = "Sort: " + string.Join(" \u2192 ", parts);
                statusSortInfo.Visible = true;
            }
            else
            {
                statusSortInfo.Visible = false;
            }
        }

        /// <summary>
        /// Re-applies the current sort order to the timer grid without reloading data.
        /// Triggered by View → Refresh Sort (F5).
        /// </summary>
        private void refreshTimersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var list = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
            if (list == null) return;

            list.ReapplySort();
            RefreshGridAfterSort();
        }

        /// <summary>
        /// Applies the group sort: Class → Style → Name (all ascending).
        /// Groups timers by class, then by behavior style, then alphabetically —
        /// the most natural view for both gameplay and maintenance.
        /// </summary>
        private void ApplyDefaultSort()
        {
            var list = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
            if (list == null) return;

            list.ApplyMultiSort(
                ("ClassID", ListSortDirection.Ascending),
                ("Style", ListSortDirection.Ascending),
                ("Name", ListSortDirection.Ascending));

            RefreshGridAfterSort();
            UpdateGroupSortCheckedState();
        }

        /// <summary>
        /// Returns true when the current sort order matches the Group Sort
        /// (ClassID Asc → Style Asc → Name Asc).
        /// </summary>
        private bool IsGroupSortActive()
        {
            var list = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
            if (list == null) return false;

            var descs = list.SortDescriptions;
            if (descs == null || descs.Count != 3) return false;

            return descs[0].PropertyDescriptor.Name == "ClassID" && descs[0].SortDirection == ListSortDirection.Ascending
                && descs[1].PropertyDescriptor.Name == "Style" && descs[1].SortDirection == ListSortDirection.Ascending
                && descs[2].PropertyDescriptor.Name == "Name" && descs[2].SortDirection == ListSortDirection.Ascending;
        }

        /// <summary>
        /// Syncs the Group Sort toolbar button and menu item checked state
        /// to reflect whether the current sort order matches the group sort.
        /// </summary>
        private void UpdateGroupSortCheckedState()
        {
            bool active = IsGroupSortActive();
            tsbDefaultSort.Checked = active;
            defaultSortToolStripMenuItem.Checked = active;
        }

        /// <summary>
        /// Persists the pre-Group Sort state to the database so it survives
        /// app restarts.  Uses the existing grid_sort_state table with a
        /// dedicated GridName so no schema changes are needed.
        /// </summary>
        private void SavePreGroupSortState()
        {
            const string gridName = "Timers_PreGroupSort";

            // Clear any existing pre-group sort rows
            Database.SaveSortState(con, gridName, null);

            if (_preGroupSortState == null || _preGroupSortState.Length == 0)
                return;

            // Re-use SaveSortState by building a temporary list and applying it
            // so we get a real ListSortDescriptionCollection.  Simpler: just
            // write the rows directly.
            var cmd = new SQLiteCommand(con);
            for (int i = 0; i < _preGroupSortState.Length; i++)
            {
                cmd.CommandText = "INSERT INTO grid_sort_state (GridName, ColumnName, SortDirection, SortOrder) VALUES (@grid, @col, @dir, @order)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@grid", gridName);
                cmd.Parameters.AddWithValue("@col", _preGroupSortState[i].Item1);
                cmd.Parameters.AddWithValue("@dir", _preGroupSortState[i].Item2 == ListSortDirection.Ascending ? 0 : 1);
                cmd.Parameters.AddWithValue("@order", i);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Restores the pre-Group Sort state from the database so the toggle-off
        /// action can return to the user's previous sort after an app restart.
        /// </summary>
        private void LoadPreGroupSortState()
        {
            try
            {
                var sorts = Database.GetSortState(con, "Timers_PreGroupSort");
                if (sorts.Count > 0)
                {
                    _preGroupSortState = sorts
                        .Select(s => (s.Item1, s.Item2))
                        .ToArray();
                }
                else
                {
                    _preGroupSortState = null;
                }
            }
            catch
            {
                _preGroupSortState = null;
            }
        }

        private void tsbDefaultSort_Click(object sender, EventArgs e)
        {
            if (IsGroupSortActive())
            {
                // Toggle OFF — restore previous sort or fall back to Name ascending
                var list = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
                if (list != null && _preGroupSortState != null && _preGroupSortState.Length > 0)
                {
                    list.ApplyMultiSort(_preGroupSortState);
                }
                else if (list != null)
                {
                    list.ApplyMultiSort(("Name", ListSortDirection.Ascending));
                }

                _preGroupSortState = null;
                SavePreGroupSortState();
                RefreshGridAfterSort();
                UpdateGroupSortCheckedState();
            }
            else
            {
                // Toggle ON — save current sort, then apply group sort
                var list = grdTimers.DataSource as SortableBindingList<Timers.GridData>;
                if (list != null)
                {
                    var descs = list.SortDescriptions;
                    _preGroupSortState = new (string, ListSortDirection)[descs.Count];
                    for (int i = 0; i < descs.Count; i++)
                        _preGroupSortState[i] = (descs[i].PropertyDescriptor.Name, descs[i].SortDirection);
                }

                SavePreGroupSortState();
                ApplyDefaultSort();
            }
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
