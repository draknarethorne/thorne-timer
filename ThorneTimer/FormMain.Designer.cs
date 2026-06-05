namespace ThorneTimer
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.components = new System.ComponentModel.Container();
            this.cmsTimers = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cmsTimersAdd = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsTimersDuplicate = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsTimersChain = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsTimersDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.tscActiveCharacter = new System.Windows.Forms.ToolStripComboBox();
            this.tsSepCharacter = new System.Windows.Forms.ToolStripSeparator();
            this.tsbStartStopWatching = new System.Windows.Forms.ToolStripButton();
            this.tsbAutoSwitch = new System.Windows.Forms.ToolStripButton();
            this.tsSepWatch = new System.Windows.Forms.ToolStripSeparator();
            this.tsbDefaultSort = new System.Windows.Forms.ToolStripButton();
            this.tsSepSort = new System.Windows.Forms.ToolStripSeparator();
            this.tsbShowAllClasses = new System.Windows.Forms.ToolStripButton();
            this.tsbShowActiveOnly = new System.Windows.Forms.ToolStripButton();
            this.tsSepView = new System.Windows.Forms.ToolStripSeparator();
            this.tsbCompactView = new System.Windows.Forms.ToolStripButton();
            this.tsbMiniViews = new System.Windows.Forms.ToolStripButton();
            this.menuStripMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newDatabaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openDatabaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveDatabaseAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSepSaveRecent = new System.Windows.Forms.ToolStripSeparator();
            this.openRecentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSepRecentExit = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.miniViewsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compactViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewSepCompactFilters = new System.Windows.Forms.ToolStripSeparator();
            this.showAllClassesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showActiveOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewSepFiltersRefresh = new System.Windows.Forms.ToolStripSeparator();
            this.defaultSortToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshTimersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.watchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startStopWatchingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.watchSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.autoSwitchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tomeInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpSepTomeAbout = new System.Windows.Forms.ToolStripSeparator();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabCtrlMain = new System.Windows.Forms.TabControl();
            this.tabTimers = new System.Windows.Forms.TabPage();
            this.btnResetCounts = new System.Windows.Forms.Button();
            this.buttonStopAll = new System.Windows.Forms.Button();
            this.btnAddTimer = new System.Windows.Forms.Button();
            this.btnDeleteTimer = new System.Windows.Forms.Button();
            this.btnDuplicateTimer = new System.Windows.Forms.Button();
            this.btnChainTimer = new System.Windows.Forms.Button();
            this.grdTimers = new System.Windows.Forms.DataGridView();
            this.tabCharacters = new System.Windows.Forms.TabPage();
            this.btnAddCharacter = new System.Windows.Forms.Button();
            this.btnDeleteCharacter = new System.Windows.Forms.Button();
            this.grdCharacters = new System.Windows.Forms.DataGridView();
            this.tabCategories = new System.Windows.Forms.TabPage();
            this.btnAddCategory = new System.Windows.Forms.Button();
            this.btnDeleteCategory = new System.Windows.Forms.Button();
            this.grdCategories = new System.Windows.Forms.DataGridView();
            this.tabStyles = new System.Windows.Forms.TabPage();
            this.btnAddStyle = new System.Windows.Forms.Button();
            this.btnDeleteStyle = new System.Windows.Forms.Button();
            this.grdStyles = new System.Windows.Forms.DataGridView();
            this.tabViews = new System.Windows.Forms.TabPage();
            this.btnDeleteView = new System.Windows.Forms.Button();
            this.btnAddView = new System.Windows.Forms.Button();
            this.grdViews = new System.Windows.Forms.DataGridView();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.gbVoice = new System.Windows.Forms.GroupBox();
            this.lblVoiceEnabled = new System.Windows.Forms.Label();
            this.chkVoiceEnabled = new System.Windows.Forms.CheckBox();
            this.tbVoiceRate = new System.Windows.Forms.TrackBar();
            this.lblVoiceRate = new System.Windows.Forms.Label();
            this.btnTestVolume = new System.Windows.Forms.Button();
            this.tbVolume = new System.Windows.Forms.TrackBar();
            this.lblVolume = new System.Windows.Forms.Label();
            this.cboActiveVoice = new System.Windows.Forms.ComboBox();
            this.lblActiveVoice = new System.Windows.Forms.Label();
            this.grpMiniView = new System.Windows.Forms.GroupBox();
            this.tbFontSize = new System.Windows.Forms.TrackBar();
            this.tbOpacity = new System.Windows.Forms.TrackBar();
            this.lblOpacity = new System.Windows.Forms.Label();
            this.txtWarningTime = new System.Windows.Forms.TextBox();
            this.lblWarningTime = new System.Windows.Forms.Label();
            this.lblWarnPickBack = new System.Windows.Forms.Label();
            this.lblWarnPickFore = new System.Windows.Forms.Label();
            this.lblWarningColors = new System.Windows.Forms.Label();
            this.lblFontSize = new System.Windows.Forms.Label();
            this.lblBuffPickBack = new System.Windows.Forms.Label();
            this.lblBuffPickFore = new System.Windows.Forms.Label();
            this.lblBuffColors = new System.Windows.Forms.Label();
            this.chkShowPing = new System.Windows.Forms.CheckBox();
            this.lblShowPing = new System.Windows.Forms.Label();
            this.txtPingTime = new System.Windows.Forms.TextBox();
            this.lblPingTime = new System.Windows.Forms.Label();
            this.lblPingPickBack = new System.Windows.Forms.Label();
            this.lblPingPickFore = new System.Windows.Forms.Label();
            this.lblPingColors = new System.Windows.Forms.Label();
            this.lblNormPickBack = new System.Windows.Forms.Label();
            this.lblNormPickFore = new System.Windows.Forms.Label();
            this.lblNormalColors = new System.Windows.Forms.Label();
            this.viewSepSortRefresh = new System.Windows.Forms.ToolStripSeparator();
            this.colorDialogPicker = new System.Windows.Forms.ColorDialog();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusTomePath = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusParsing = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusSortInfo = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusTimerStats = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip.SuspendLayout();
            this.menuStripMain.SuspendLayout();
            this.tabCtrlMain.SuspendLayout();
            this.tabTimers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdTimers)).BeginInit();
            this.tabCharacters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdCharacters)).BeginInit();
            this.tabCategories.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdCategories)).BeginInit();
            this.tabStyles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdStyles)).BeginInit();
            this.tabViews.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdViews)).BeginInit();
            this.tabSettings.SuspendLayout();
            this.gbVoice.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbVoiceRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbVolume)).BeginInit();
            this.grpMiniView.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbFontSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbOpacity)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tscActiveCharacter,
            this.tsSepCharacter,
            this.tsbStartStopWatching,
            this.tsbAutoSwitch,
            this.tsSepWatch,
            this.tsbDefaultSort,
            this.tsSepSort,
            this.tsbShowAllClasses,
            this.tsbShowActiveOnly,
            this.tsSepView,
            this.tsbCompactView,
            this.tsbMiniViews});
            this.toolStrip.Location = new System.Drawing.Point(0, 24);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1400, 25);
            this.toolStrip.TabIndex = 21;
            // 
            // tscActiveCharacter
            // 
            this.tscActiveCharacter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscActiveCharacter.Name = "tscActiveCharacter";
            this.tscActiveCharacter.Size = new System.Drawing.Size(140, 25);
            this.tscActiveCharacter.ToolTipText = "Active Character";
            this.tscActiveCharacter.SelectedIndexChanged += new System.EventHandler(this.tscActiveCharacter_SelectedIndexChanged);
            // 
            // tsSepCharacter
            // 
            this.tsSepCharacter.Name = "tsSepCharacter";
            this.tsSepCharacter.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbStartStopWatching
            // 
            this.tsbStartStopWatching.Name = "tsbStartStopWatching";
            this.tsbStartStopWatching.Size = new System.Drawing.Size(89, 22);
            this.tsbStartStopWatching.Text = "Start Watching";
            this.tsbStartStopWatching.ToolTipText = "Start or stop watching log files for timer trigger keywords";
            this.tsbStartStopWatching.Click += new System.EventHandler(this.tsbStartStopWatching_Click);
            // 
            // tsbAutoSwitch
            // 
            this.tsbAutoSwitch.Checked = true;
            this.tsbAutoSwitch.CheckOnClick = true;
            this.tsbAutoSwitch.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsbAutoSwitch.Name = "tsbAutoSwitch";
            this.tsbAutoSwitch.Size = new System.Drawing.Size(77, 22);
            this.tsbAutoSwitch.Text = "Auto-Switch";
            this.tsbAutoSwitch.ToolTipText = "Automatically switch to the character whose log file is actively being written";
            this.tsbAutoSwitch.Click += new System.EventHandler(this.tsbAutoSwitch_Click);
            // 
            // tsSepWatch
            // 
            this.tsSepWatch.Name = "tsSepWatch";
            this.tsSepWatch.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbDefaultSort
            // 
            this.tsbDefaultSort.Name = "tsbDefaultSort";
            this.tsbDefaultSort.Size = new System.Drawing.Size(68, 22);
            this.tsbDefaultSort.Text = "Group Sort";
            this.tsbDefaultSort.ToolTipText = "Sort timers by Class → Style → Name. Groups timers hierarchically for the most na" +
    "tural view.";
            this.tsbDefaultSort.Click += new System.EventHandler(this.tsbDefaultSort_Click);
            // 
            // tsSepSort
            // 
            this.tsSepSort.Name = "tsSepSort";
            this.tsSepSort.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbShowAllClasses
            // 
            this.tsbShowAllClasses.Checked = true;
            this.tsbShowAllClasses.CheckOnClick = true;
            this.tsbShowAllClasses.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsbShowAllClasses.Name = "tsbShowAllClasses";
            this.tsbShowAllClasses.Size = new System.Drawing.Size(66, 22);
            this.tsbShowAllClasses.Text = "All Classes";
            this.tsbShowAllClasses.ToolTipText = "Show timers for all classes. When unchecked, only timers matching the active char" +
    "acter\'s class are shown.";
            this.tsbShowAllClasses.Click += new System.EventHandler(this.tsbShowAllClasses_Click);
            // 
            // tsbShowActiveOnly
            // 
            this.tsbShowActiveOnly.CheckOnClick = true;
            this.tsbShowActiveOnly.Name = "tsbShowActiveOnly";
            this.tsbShowActiveOnly.Size = new System.Drawing.Size(72, 22);
            this.tsbShowActiveOnly.Text = "Active Only";
            this.tsbShowActiveOnly.ToolTipText = "Show only active timers. When unchecked, all timers are shown.";
            this.tsbShowActiveOnly.Click += new System.EventHandler(this.tsbShowActiveOnly_Click);
            // 
            // tsSepView
            // 
            this.tsSepView.Name = "tsSepView";
            this.tsSepView.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbCompactView
            // 
            this.tsbCompactView.CheckOnClick = true;
            this.tsbCompactView.Name = "tsbCompactView";
            this.tsbCompactView.Size = new System.Drawing.Size(60, 22);
            this.tsbCompactView.Text = "Compact";
            this.tsbCompactView.ToolTipText = "Toggle compact view. Hides configuration columns and makes classification columns" +
    " read-only for safe gameplay.";
            this.tsbCompactView.Click += new System.EventHandler(this.tsbCompactView_Click);
            // 
            // tsbMiniViews
            // 
            this.tsbMiniViews.Name = "tsbMiniViews";
            this.tsbMiniViews.Size = new System.Drawing.Size(68, 22);
            this.tsbMiniViews.Text = "Mini Views";
            this.tsbMiniViews.ToolTipText = "Toggle mini view overlay windows for at-a-glance timer monitoring";
            this.tsbMiniViews.Click += new System.EventHandler(this.tsbMiniViews_Click);
            // 
            // menuStripMain
            // 
            this.menuStripMain.BackColor = System.Drawing.SystemColors.MenuBar;
            this.menuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.watchToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStripMain.Location = new System.Drawing.Point(0, 0);
            this.menuStripMain.Name = "menuStripMain";
            this.menuStripMain.ShowItemToolTips = true;
            this.menuStripMain.Size = new System.Drawing.Size(1400, 24);
            this.menuStripMain.TabIndex = 8;
            this.menuStripMain.Text = "menuStripMain";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newDatabaseToolStripMenuItem,
            this.openDatabaseToolStripMenuItem,
            this.saveDatabaseAsToolStripMenuItem,
            this.fileSepSaveRecent,
            this.openRecentToolStripMenuItem,
            this.fileSepRecentExit,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newDatabaseToolStripMenuItem
            // 
            this.newDatabaseToolStripMenuItem.Name = "newDatabaseToolStripMenuItem";
            this.newDatabaseToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.newDatabaseToolStripMenuItem.Text = "&New Tome...";
            this.newDatabaseToolStripMenuItem.Click += new System.EventHandler(this.newDatabaseToolStripMenuItem_Click);
            // 
            // openDatabaseToolStripMenuItem
            // 
            this.openDatabaseToolStripMenuItem.Name = "openDatabaseToolStripMenuItem";
            this.openDatabaseToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.openDatabaseToolStripMenuItem.Text = "&Open Tome...";
            this.openDatabaseToolStripMenuItem.Click += new System.EventHandler(this.openDatabaseToolStripMenuItem_Click);
            // 
            // saveDatabaseAsToolStripMenuItem
            // 
            this.saveDatabaseAsToolStripMenuItem.Name = "saveDatabaseAsToolStripMenuItem";
            this.saveDatabaseAsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.saveDatabaseAsToolStripMenuItem.Text = "&Save Tome As...";
            this.saveDatabaseAsToolStripMenuItem.Click += new System.EventHandler(this.saveDatabaseAsToolStripMenuItem_Click);
            // 
            // fileSepSaveRecent
            // 
            this.fileSepSaveRecent.Name = "fileSepSaveRecent";
            this.fileSepSaveRecent.Size = new System.Drawing.Size(153, 6);
            // 
            // openRecentToolStripMenuItem
            // 
            this.openRecentToolStripMenuItem.Name = "openRecentToolStripMenuItem";
            this.openRecentToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.openRecentToolStripMenuItem.Text = "Open &Recent";
            // 
            // fileSepRecentExit
            // 
            this.fileSepRecentExit.Name = "fileSepRecentExit";
            this.fileSepRecentExit.Size = new System.Drawing.Size(153, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miniViewsToolStripMenuItem,
            this.compactViewToolStripMenuItem,
            this.viewSepCompactFilters,
            this.showAllClassesToolStripMenuItem,
            this.showActiveOnlyToolStripMenuItem,
            this.viewSepFiltersRefresh,
            this.defaultSortToolStripMenuItem,
            this.refreshTimersToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // miniViewsToolStripMenuItem
            // 
            this.miniViewsToolStripMenuItem.Name = "miniViewsToolStripMenuItem";
            this.miniViewsToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.miniViewsToolStripMenuItem.Text = "&Mini Views";
            this.miniViewsToolStripMenuItem.ToolTipText = "Toggle mini view overlay windows for at-a-glance timer monitoring";
            this.miniViewsToolStripMenuItem.Click += new System.EventHandler(this.tsbMiniViews_Click);
            // 
            // compactViewToolStripMenuItem
            // 
            this.compactViewToolStripMenuItem.CheckOnClick = true;
            this.compactViewToolStripMenuItem.Name = "compactViewToolStripMenuItem";
            this.compactViewToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.compactViewToolStripMenuItem.Text = "&Compact View";
            this.compactViewToolStripMenuItem.ToolTipText = "Toggle compact view. Hides configuration columns and makes classification columns" +
    " read-only for safe gameplay.";
            this.compactViewToolStripMenuItem.Click += new System.EventHandler(this.compactViewToolStripMenuItem_Click);
            // 
            // viewSepCompactFilters
            // 
            this.viewSepCompactFilters.Name = "viewSepCompactFilters";
            this.viewSepCompactFilters.Size = new System.Drawing.Size(164, 6);
            // 
            // showAllClassesToolStripMenuItem
            // 
            this.showAllClassesToolStripMenuItem.Checked = true;
            this.showAllClassesToolStripMenuItem.CheckOnClick = true;
            this.showAllClassesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showAllClassesToolStripMenuItem.Name = "showAllClassesToolStripMenuItem";
            this.showAllClassesToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.showAllClassesToolStripMenuItem.Text = "Show &All Classes";
            this.showAllClassesToolStripMenuItem.ToolTipText = "Show timers for all classes. When unchecked, only timers matching the active char" +
    "acter\'s class are shown.";
            this.showAllClassesToolStripMenuItem.Click += new System.EventHandler(this.showAllClassesToolStripMenuItem_Click);
            // 
            // showActiveOnlyToolStripMenuItem
            // 
            this.showActiveOnlyToolStripMenuItem.CheckOnClick = true;
            this.showActiveOnlyToolStripMenuItem.Name = "showActiveOnlyToolStripMenuItem";
            this.showActiveOnlyToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.showActiveOnlyToolStripMenuItem.Text = "Show Acti&ve Only";
            this.showActiveOnlyToolStripMenuItem.ToolTipText = "Show only active timers. When unchecked, all timers are shown.";
            this.showActiveOnlyToolStripMenuItem.Click += new System.EventHandler(this.showActiveOnlyToolStripMenuItem_Click);
            // 
            // viewSepFiltersRefresh
            // 
            this.viewSepFiltersRefresh.Name = "viewSepFiltersRefresh";
            this.viewSepFiltersRefresh.Size = new System.Drawing.Size(164, 6);
            // 
            // defaultSortToolStripMenuItem
            // 
            this.defaultSortToolStripMenuItem.Name = "defaultSortToolStripMenuItem";
            this.defaultSortToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.defaultSortToolStripMenuItem.Text = "&Group Sort";
            this.defaultSortToolStripMenuItem.ToolTipText = "Sort timers by Class → Style → Name. Groups timers hierarchically for the most na" +
    "tural view.";
            this.defaultSortToolStripMenuItem.Click += new System.EventHandler(this.tsbDefaultSort_Click);
            // 
            // refreshTimersToolStripMenuItem
            // 
            this.refreshTimersToolStripMenuItem.Name = "refreshTimersToolStripMenuItem";
            this.refreshTimersToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.refreshTimersToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.refreshTimersToolStripMenuItem.Text = "&Refresh";
            this.refreshTimersToolStripMenuItem.ToolTipText = "Refresh the timer grid — re-applies sorting, painting, and layout (F5).";
            this.refreshTimersToolStripMenuItem.Click += new System.EventHandler(this.refreshTimersToolStripMenuItem_Click);
            // 
            // watchToolStripMenuItem
            // 
            this.watchToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startStopWatchingToolStripMenuItem,
            this.watchSeparator,
            this.autoSwitchToolStripMenuItem});
            this.watchToolStripMenuItem.Name = "watchToolStripMenuItem";
            this.watchToolStripMenuItem.Size = new System.Drawing.Size(53, 20);
            this.watchToolStripMenuItem.Text = "&Watch";
            // 
            // startStopWatchingToolStripMenuItem
            // 
            this.startStopWatchingToolStripMenuItem.Name = "startStopWatchingToolStripMenuItem";
            this.startStopWatchingToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.startStopWatchingToolStripMenuItem.Text = "&Start Watching";
            this.startStopWatchingToolStripMenuItem.ToolTipText = "Start or stop watching log files for timer trigger keywords";
            this.startStopWatchingToolStripMenuItem.Click += new System.EventHandler(this.tsbStartStopWatching_Click);
            // 
            // watchSeparator
            // 
            this.watchSeparator.Name = "watchSeparator";
            this.watchSeparator.Size = new System.Drawing.Size(191, 6);
            // 
            // autoSwitchToolStripMenuItem
            // 
            this.autoSwitchToolStripMenuItem.Checked = true;
            this.autoSwitchToolStripMenuItem.CheckOnClick = true;
            this.autoSwitchToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoSwitchToolStripMenuItem.Name = "autoSwitchToolStripMenuItem";
            this.autoSwitchToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.autoSwitchToolStripMenuItem.Text = "&Auto-Switch Character";
            this.autoSwitchToolStripMenuItem.ToolTipText = "Automatically switch to the character whose log file is actively being written";
            this.autoSwitchToolStripMenuItem.Click += new System.EventHandler(this.autoSwitchToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tomeInfoToolStripMenuItem,
            this.helpSepTomeAbout,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // tomeInfoToolStripMenuItem
            // 
            this.tomeInfoToolStripMenuItem.Name = "tomeInfoToolStripMenuItem";
            this.tomeInfoToolStripMenuItem.Size = new System.Drawing.Size(137, 22);
            this.tomeInfoToolStripMenuItem.Text = "Tome &Info...";
            this.tomeInfoToolStripMenuItem.Click += new System.EventHandler(this.tomeInfoToolStripMenuItem_Click);
            // 
            // helpSepTomeAbout
            // 
            this.helpSepTomeAbout.Name = "helpSepTomeAbout";
            this.helpSepTomeAbout.Size = new System.Drawing.Size(134, 6);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(137, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // tabCtrlMain
            // 
            this.tabCtrlMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabCtrlMain.Controls.Add(this.tabTimers);
            this.tabCtrlMain.Controls.Add(this.tabCharacters);
            this.tabCtrlMain.Controls.Add(this.tabCategories);
            this.tabCtrlMain.Controls.Add(this.tabStyles);
            this.tabCtrlMain.Controls.Add(this.tabViews);
            this.tabCtrlMain.Controls.Add(this.tabSettings);
            this.tabCtrlMain.Location = new System.Drawing.Point(12, 52);
            this.tabCtrlMain.Name = "tabCtrlMain";
            this.tabCtrlMain.SelectedIndex = 0;
            this.tabCtrlMain.Size = new System.Drawing.Size(1376, 609);
            this.tabCtrlMain.TabIndex = 9;
            // 
            // tabTimers
            // 
            this.tabTimers.BackColor = System.Drawing.SystemColors.Control;
            this.tabTimers.Controls.Add(this.btnResetCounts);
            this.tabTimers.Controls.Add(this.buttonStopAll);
            this.tabTimers.Controls.Add(this.btnAddTimer);
            this.tabTimers.Controls.Add(this.btnDeleteTimer);
            this.tabTimers.Controls.Add(this.btnDuplicateTimer);
            this.tabTimers.Controls.Add(this.btnChainTimer);
            this.tabTimers.Controls.Add(this.grdTimers);
            this.tabTimers.Location = new System.Drawing.Point(4, 22);
            this.tabTimers.Name = "tabTimers";
            this.tabTimers.Padding = new System.Windows.Forms.Padding(3);
            this.tabTimers.Size = new System.Drawing.Size(1368, 583);
            this.tabTimers.TabIndex = 0;
            this.tabTimers.Text = "Timers";
            // 
            // btnResetCounts
            // 
            this.btnResetCounts.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnResetCounts.BackColor = System.Drawing.SystemColors.Control;
            this.btnResetCounts.Location = new System.Drawing.Point(1206, 554);
            this.btnResetCounts.Name = "btnResetCounts";
            this.btnResetCounts.Size = new System.Drawing.Size(75, 23);
            this.btnResetCounts.TabIndex = 18;
            this.btnResetCounts.Text = "Reset Count";
            this.btnResetCounts.UseVisualStyleBackColor = true;
            this.btnResetCounts.Click += new System.EventHandler(this.btnResetCounts_Click);
            // 
            // buttonStopAll
            // 
            this.buttonStopAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonStopAll.BackColor = System.Drawing.SystemColors.Control;
            this.buttonStopAll.Location = new System.Drawing.Point(1287, 554);
            this.buttonStopAll.Name = "buttonStopAll";
            this.buttonStopAll.Size = new System.Drawing.Size(75, 23);
            this.buttonStopAll.TabIndex = 17;
            this.buttonStopAll.Text = "Stop All";
            this.buttonStopAll.UseVisualStyleBackColor = true;
            this.buttonStopAll.Click += new System.EventHandler(this.btnStopAll_Click);
            // 
            // btnAddTimer
            // 
            this.btnAddTimer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddTimer.BackColor = System.Drawing.SystemColors.Control;
            this.btnAddTimer.Location = new System.Drawing.Point(6, 554);
            this.btnAddTimer.Name = "btnAddTimer";
            this.btnAddTimer.Size = new System.Drawing.Size(75, 23);
            this.btnAddTimer.TabIndex = 3;
            this.btnAddTimer.Text = "Add";
            this.btnAddTimer.UseVisualStyleBackColor = true;
            this.btnAddTimer.Click += new System.EventHandler(this.btnAddTimer_Click);
            // 
            // btnDeleteTimer
            // 
            this.btnDeleteTimer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteTimer.BackColor = System.Drawing.SystemColors.Control;
            this.btnDeleteTimer.Location = new System.Drawing.Point(249, 554);
            this.btnDeleteTimer.Name = "btnDeleteTimer";
            this.btnDeleteTimer.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteTimer.TabIndex = 6;
            this.btnDeleteTimer.Text = "Delete";
            this.btnDeleteTimer.UseVisualStyleBackColor = true;
            this.btnDeleteTimer.Click += new System.EventHandler(this.btnDeleteTimer_Click);
            // 
            // btnDuplicateTimer
            // 
            this.btnDuplicateTimer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDuplicateTimer.BackColor = System.Drawing.SystemColors.Control;
            this.btnDuplicateTimer.Location = new System.Drawing.Point(87, 554);
            this.btnDuplicateTimer.Name = "btnDuplicateTimer";
            this.btnDuplicateTimer.Size = new System.Drawing.Size(75, 23);
            this.btnDuplicateTimer.TabIndex = 4;
            this.btnDuplicateTimer.Text = "Duplicate";
            this.btnDuplicateTimer.UseVisualStyleBackColor = true;
            this.btnDuplicateTimer.Click += new System.EventHandler(this.btnDuplicateTimer_Click);
            // 
            // btnChainTimer
            // 
            this.btnChainTimer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnChainTimer.BackColor = System.Drawing.SystemColors.Control;
            this.btnChainTimer.Location = new System.Drawing.Point(168, 554);
            this.btnChainTimer.Name = "btnChainTimer";
            this.btnChainTimer.Size = new System.Drawing.Size(75, 23);
            this.btnChainTimer.TabIndex = 5;
            this.btnChainTimer.Text = "Chain";
            this.btnChainTimer.UseVisualStyleBackColor = true;
            this.btnChainTimer.Click += new System.EventHandler(this.btnChainTimer_Click);
            // 
            // cmsTimers
            // 
            this.cmsTimers.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cmsTimersAdd,
            this.cmsTimersDuplicate,
            this.cmsTimersChain,
            this.cmsTimersDelete});
            this.cmsTimers.Name = "cmsTimers";
            this.cmsTimers.Size = new System.Drawing.Size(181, 92);
            this.cmsTimers.Opening += new System.ComponentModel.CancelEventHandler(this.cmsTimers_Opening);
            // 
            // cmsTimersAdd
            // 
            this.cmsTimersAdd.Name = "cmsTimersAdd";
            this.cmsTimersAdd.Size = new System.Drawing.Size(180, 22);
            this.cmsTimersAdd.Text = "Add";
            this.cmsTimersAdd.Click += new System.EventHandler(this.btnAddTimer_Click);
            // 
            // cmsTimersDuplicate
            // 
            this.cmsTimersDuplicate.Name = "cmsTimersDuplicate";
            this.cmsTimersDuplicate.Size = new System.Drawing.Size(180, 22);
            this.cmsTimersDuplicate.Text = "Duplicate";
            this.cmsTimersDuplicate.Click += new System.EventHandler(this.btnDuplicateTimer_Click);
            // 
            // cmsTimersChain
            // 
            this.cmsTimersChain.Name = "cmsTimersChain";
            this.cmsTimersChain.Size = new System.Drawing.Size(180, 22);
            this.cmsTimersChain.Text = "Chain";
            this.cmsTimersChain.Click += new System.EventHandler(this.btnChainTimer_Click);
            // 
            // cmsTimersDelete
            // 
            this.cmsTimersDelete.Name = "cmsTimersDelete";
            this.cmsTimersDelete.Size = new System.Drawing.Size(180, 22);
            this.cmsTimersDelete.Text = "Delete";
            this.cmsTimersDelete.Click += new System.EventHandler(this.btnDeleteTimer_Click);
            // 
            // grdTimers
            // 
            this.grdTimers.AllowUserToAddRows = false;
            this.grdTimers.AllowUserToDeleteRows = false;
            this.grdTimers.AllowUserToResizeRows = false;
            this.grdTimers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdTimers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdTimers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdTimers.ContextMenuStrip = this.cmsTimers;
            this.grdTimers.Location = new System.Drawing.Point(6, 6);
            this.grdTimers.Name = "grdTimers";
            this.grdTimers.Size = new System.Drawing.Size(1356, 542);
            this.grdTimers.TabIndex = 1;
            this.grdTimers.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grdTimers_CellClick);
            this.grdTimers.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.grdTimers_CellMouseDown);
            this.grdTimers.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.grdTimers_ColumnHeaderMouseClick);
            // 
            // tabCharacters
            // 
            this.tabCharacters.BackColor = System.Drawing.SystemColors.Control;
            this.tabCharacters.Controls.Add(this.btnAddCharacter);
            this.tabCharacters.Controls.Add(this.btnDeleteCharacter);
            this.tabCharacters.Controls.Add(this.grdCharacters);
            this.tabCharacters.Location = new System.Drawing.Point(4, 22);
            this.tabCharacters.Name = "tabCharacters";
            this.tabCharacters.Padding = new System.Windows.Forms.Padding(3);
            this.tabCharacters.Size = new System.Drawing.Size(1368, 583);
            this.tabCharacters.TabIndex = 1;
            this.tabCharacters.Text = "Characters";
            // 
            // btnAddCharacter
            // 
            this.btnAddCharacter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddCharacter.Location = new System.Drawing.Point(6, 544);
            this.btnAddCharacter.Name = "btnAddCharacter";
            this.btnAddCharacter.Size = new System.Drawing.Size(75, 23);
            this.btnAddCharacter.TabIndex = 5;
            this.btnAddCharacter.Text = "Add";
            this.btnAddCharacter.UseVisualStyleBackColor = true;
            this.btnAddCharacter.Click += new System.EventHandler(this.btnAddCharacter_Click);
            // 
            // btnDeleteCharacter
            // 
            this.btnDeleteCharacter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteCharacter.Location = new System.Drawing.Point(87, 544);
            this.btnDeleteCharacter.Name = "btnDeleteCharacter";
            this.btnDeleteCharacter.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteCharacter.TabIndex = 4;
            this.btnDeleteCharacter.Text = "Delete";
            this.btnDeleteCharacter.UseVisualStyleBackColor = true;
            this.btnDeleteCharacter.Click += new System.EventHandler(this.btnDeleteCharacter_Click);
            // 
            // grdCharacters
            // 
            this.grdCharacters.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdCharacters.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdCharacters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdCharacters.Location = new System.Drawing.Point(6, 6);
            this.grdCharacters.Name = "grdCharacters";
            this.grdCharacters.Size = new System.Drawing.Size(1314, 532);
            this.grdCharacters.TabIndex = 0;
            // 
            // tabCategories
            // 
            this.tabCategories.BackColor = System.Drawing.SystemColors.Control;
            this.tabCategories.Controls.Add(this.btnAddCategory);
            this.tabCategories.Controls.Add(this.btnDeleteCategory);
            this.tabCategories.Controls.Add(this.grdCategories);
            this.tabCategories.Location = new System.Drawing.Point(4, 22);
            this.tabCategories.Name = "tabCategories";
            this.tabCategories.Padding = new System.Windows.Forms.Padding(3);
            this.tabCategories.Size = new System.Drawing.Size(1368, 583);
            this.tabCategories.TabIndex = 2;
            this.tabCategories.Text = "Categories";
            // 
            // btnAddCategory
            // 
            this.btnAddCategory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddCategory.Location = new System.Drawing.Point(6, 544);
            this.btnAddCategory.Name = "btnAddCategory";
            this.btnAddCategory.Size = new System.Drawing.Size(75, 23);
            this.btnAddCategory.TabIndex = 6;
            this.btnAddCategory.Text = "Add";
            this.btnAddCategory.UseVisualStyleBackColor = true;
            this.btnAddCategory.Click += new System.EventHandler(this.btnAddCategory_Click);
            // 
            // btnDeleteCategory
            // 
            this.btnDeleteCategory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteCategory.Location = new System.Drawing.Point(87, 544);
            this.btnDeleteCategory.Name = "btnDeleteCategory";
            this.btnDeleteCategory.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteCategory.TabIndex = 5;
            this.btnDeleteCategory.Text = "Delete";
            this.btnDeleteCategory.UseVisualStyleBackColor = true;
            this.btnDeleteCategory.Click += new System.EventHandler(this.btnDeleteCategory_Click);
            // 
            // grdCategories
            // 
            this.grdCategories.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdCategories.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdCategories.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdCategories.Location = new System.Drawing.Point(6, 6);
            this.grdCategories.Name = "grdCategories";
            this.grdCategories.Size = new System.Drawing.Size(1314, 532);
            this.grdCategories.TabIndex = 1;
            // 
            // tabStyles
            // 
            this.tabStyles.BackColor = System.Drawing.SystemColors.Control;
            this.tabStyles.Controls.Add(this.btnAddStyle);
            this.tabStyles.Controls.Add(this.btnDeleteStyle);
            this.tabStyles.Controls.Add(this.grdStyles);
            this.tabStyles.Location = new System.Drawing.Point(4, 22);
            this.tabStyles.Name = "tabStyles";
            this.tabStyles.Padding = new System.Windows.Forms.Padding(3);
            this.tabStyles.Size = new System.Drawing.Size(1368, 583);
            this.tabStyles.TabIndex = 5;
            this.tabStyles.Text = "Styles";
            // 
            // btnAddStyle
            // 
            this.btnAddStyle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddStyle.Location = new System.Drawing.Point(6, 544);
            this.btnAddStyle.Name = "btnAddStyle";
            this.btnAddStyle.Size = new System.Drawing.Size(75, 23);
            this.btnAddStyle.TabIndex = 1;
            this.btnAddStyle.Text = "Add";
            this.btnAddStyle.UseVisualStyleBackColor = true;
            this.btnAddStyle.Click += new System.EventHandler(this.btnAddStyle_Click);
            // 
            // btnDeleteStyle
            // 
            this.btnDeleteStyle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteStyle.Location = new System.Drawing.Point(87, 544);
            this.btnDeleteStyle.Name = "btnDeleteStyle";
            this.btnDeleteStyle.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteStyle.TabIndex = 2;
            this.btnDeleteStyle.Text = "Delete";
            this.btnDeleteStyle.UseVisualStyleBackColor = true;
            this.btnDeleteStyle.Click += new System.EventHandler(this.btnDeleteStyle_Click);
            // 
            // grdStyles
            // 
            this.grdStyles.AllowUserToAddRows = false;
            this.grdStyles.AllowUserToDeleteRows = false;
            this.grdStyles.AllowUserToResizeRows = false;
            this.grdStyles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdStyles.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdStyles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdStyles.Location = new System.Drawing.Point(6, 6);
            this.grdStyles.Name = "grdStyles";
            this.grdStyles.Size = new System.Drawing.Size(1356, 532);
            this.grdStyles.TabIndex = 0;
            // 
            // tabViews
            // 
            this.tabViews.BackColor = System.Drawing.SystemColors.Control;
            this.tabViews.Controls.Add(this.btnDeleteView);
            this.tabViews.Controls.Add(this.btnAddView);
            this.tabViews.Controls.Add(this.grdViews);
            this.tabViews.Location = new System.Drawing.Point(4, 22);
            this.tabViews.Name = "tabViews";
            this.tabViews.Padding = new System.Windows.Forms.Padding(3);
            this.tabViews.Size = new System.Drawing.Size(1368, 583);
            this.tabViews.TabIndex = 3;
            this.tabViews.Text = "Views";
            // 
            // btnDeleteView
            // 
            this.btnDeleteView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteView.Location = new System.Drawing.Point(87, 544);
            this.btnDeleteView.Name = "btnDeleteView";
            this.btnDeleteView.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteView.TabIndex = 8;
            this.btnDeleteView.Text = "Delete";
            this.btnDeleteView.UseVisualStyleBackColor = true;
            this.btnDeleteView.Click += new System.EventHandler(this.btnDeleteView_Click);
            // 
            // btnAddView
            // 
            this.btnAddView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddView.Location = new System.Drawing.Point(6, 544);
            this.btnAddView.Name = "btnAddView";
            this.btnAddView.Size = new System.Drawing.Size(75, 23);
            this.btnAddView.TabIndex = 7;
            this.btnAddView.Text = "Add";
            this.btnAddView.UseVisualStyleBackColor = true;
            this.btnAddView.Click += new System.EventHandler(this.btnAddView_Click);
            // 
            // grdViews
            // 
            this.grdViews.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdViews.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdViews.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdViews.Location = new System.Drawing.Point(6, 6);
            this.grdViews.Name = "grdViews";
            this.grdViews.Size = new System.Drawing.Size(1356, 532);
            this.grdViews.TabIndex = 2;
            // 
            // tabSettings
            // 
            this.tabSettings.BackColor = System.Drawing.SystemColors.Control;
            this.tabSettings.Controls.Add(this.gbVoice);
            this.tabSettings.Controls.Add(this.grpMiniView);
            this.tabSettings.Location = new System.Drawing.Point(4, 22);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabSettings.Size = new System.Drawing.Size(1368, 583);
            this.tabSettings.TabIndex = 4;
            this.tabSettings.Text = "Settings";
            // 
            // gbVoice
            // 
            this.gbVoice.Controls.Add(this.lblVoiceEnabled);
            this.gbVoice.Controls.Add(this.chkVoiceEnabled);
            this.gbVoice.Controls.Add(this.tbVoiceRate);
            this.gbVoice.Controls.Add(this.lblVoiceRate);
            this.gbVoice.Controls.Add(this.btnTestVolume);
            this.gbVoice.Controls.Add(this.tbVolume);
            this.gbVoice.Controls.Add(this.lblVolume);
            this.gbVoice.Controls.Add(this.cboActiveVoice);
            this.gbVoice.Controls.Add(this.lblActiveVoice);
            this.gbVoice.Location = new System.Drawing.Point(6, 6);
            this.gbVoice.Name = "gbVoice";
            this.gbVoice.Size = new System.Drawing.Size(287, 172);
            this.gbVoice.TabIndex = 17;
            this.gbVoice.TabStop = false;
            this.gbVoice.Text = "Voice Options";
            // 
            // lblVoiceEnabled
            // 
            this.lblVoiceEnabled.AutoSize = true;
            this.lblVoiceEnabled.Location = new System.Drawing.Point(8, 136);
            this.lblVoiceEnabled.Name = "lblVoiceEnabled";
            this.lblVoiceEnabled.Size = new System.Drawing.Size(79, 13);
            this.lblVoiceEnabled.TabIndex = 36;
            this.lblVoiceEnabled.Text = "Voice Enabled:";
            // 
            // chkVoiceEnabled
            // 
            this.chkVoiceEnabled.AutoSize = true;
            this.chkVoiceEnabled.Location = new System.Drawing.Point(92, 136);
            this.chkVoiceEnabled.Name = "chkVoiceEnabled";
            this.chkVoiceEnabled.Size = new System.Drawing.Size(15, 14);
            this.chkVoiceEnabled.TabIndex = 36;
            this.chkVoiceEnabled.UseVisualStyleBackColor = true;
            this.chkVoiceEnabled.Click += new System.EventHandler(this.chkVoiceEnabled_Click);
            // 
            // tbVoiceRate
            // 
            this.tbVoiceRate.Location = new System.Drawing.Point(86, 95);
            this.tbVoiceRate.Minimum = -10;
            this.tbVoiceRate.Name = "tbVoiceRate";
            this.tbVoiceRate.Size = new System.Drawing.Size(104, 45);
            this.tbVoiceRate.TabIndex = 26;
            this.tbVoiceRate.Value = -2;
            this.tbVoiceRate.Scroll += new System.EventHandler(this.tbVoiceRate_Scroll);
            // 
            // lblVoiceRate
            // 
            this.lblVoiceRate.AutoSize = true;
            this.lblVoiceRate.Location = new System.Drawing.Point(8, 95);
            this.lblVoiceRate.Name = "lblVoiceRate";
            this.lblVoiceRate.Size = new System.Drawing.Size(81, 13);
            this.lblVoiceRate.TabIndex = 25;
            this.lblVoiceRate.Text = "Speaking Rate:";
            // 
            // btnTestVolume
            // 
            this.btnTestVolume.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnTestVolume.Location = new System.Drawing.Point(209, 52);
            this.btnTestVolume.Name = "btnTestVolume";
            this.btnTestVolume.Size = new System.Drawing.Size(68, 23);
            this.btnTestVolume.TabIndex = 24;
            this.btnTestVolume.Text = "Test Voice";
            this.btnTestVolume.UseVisualStyleBackColor = true;
            this.btnTestVolume.Click += new System.EventHandler(this.btnTestVolume_Click);
            // 
            // tbVolume
            // 
            this.tbVolume.Location = new System.Drawing.Point(86, 52);
            this.tbVolume.Maximum = 100;
            this.tbVolume.Name = "tbVolume";
            this.tbVolume.Size = new System.Drawing.Size(104, 45);
            this.tbVolume.TabIndex = 23;
            this.tbVolume.Value = 100;
            this.tbVolume.Scroll += new System.EventHandler(this.tbVolume_Scroll);
            // 
            // lblVolume
            // 
            this.lblVolume.AutoSize = true;
            this.lblVolume.Location = new System.Drawing.Point(8, 52);
            this.lblVolume.Name = "lblVolume";
            this.lblVolume.Size = new System.Drawing.Size(45, 13);
            this.lblVolume.TabIndex = 13;
            this.lblVolume.Text = "Volume:";
            // 
            // cboActiveVoice
            // 
            this.cboActiveVoice.FormattingEnabled = true;
            this.cboActiveVoice.Location = new System.Drawing.Point(83, 19);
            this.cboActiveVoice.Name = "cboActiveVoice";
            this.cboActiveVoice.Size = new System.Drawing.Size(193, 21);
            this.cboActiveVoice.TabIndex = 11;
            this.cboActiveVoice.SelectedIndexChanged += new System.EventHandler(this.cboActiveVoice_SelectedIndexChanged);
            // 
            // lblActiveVoice
            // 
            this.lblActiveVoice.AutoSize = true;
            this.lblActiveVoice.Location = new System.Drawing.Point(7, 23);
            this.lblActiveVoice.Name = "lblActiveVoice";
            this.lblActiveVoice.Size = new System.Drawing.Size(70, 13);
            this.lblActiveVoice.TabIndex = 12;
            this.lblActiveVoice.Text = "Active Voice:";
            // 
            // grpMiniView
            // 
            this.grpMiniView.Controls.Add(this.tbFontSize);
            this.grpMiniView.Controls.Add(this.tbOpacity);
            this.grpMiniView.Controls.Add(this.lblOpacity);
            this.grpMiniView.Controls.Add(this.txtWarningTime);
            this.grpMiniView.Controls.Add(this.lblWarningTime);
            this.grpMiniView.Controls.Add(this.lblWarnPickBack);
            this.grpMiniView.Controls.Add(this.lblWarnPickFore);
            this.grpMiniView.Controls.Add(this.lblWarningColors);
            this.grpMiniView.Controls.Add(this.lblFontSize);
            this.grpMiniView.Location = new System.Drawing.Point(299, 6);
            this.grpMiniView.Name = "grpMiniView";
            this.grpMiniView.Size = new System.Drawing.Size(287, 172);
            this.grpMiniView.TabIndex = 16;
            this.grpMiniView.TabStop = false;
            this.grpMiniView.Text = "Mini View Options";
            // 
            // tbFontSize
            // 
            this.tbFontSize.Location = new System.Drawing.Point(86, 71);
            this.tbFontSize.Maximum = 30;
            this.tbFontSize.Minimum = 6;
            this.tbFontSize.Name = "tbFontSize";
            this.tbFontSize.Size = new System.Drawing.Size(104, 45);
            this.tbFontSize.TabIndex = 15;
            this.tbFontSize.Value = 6;
            this.tbFontSize.Scroll += new System.EventHandler(this.tbFontSize_Scroll);
            // 
            // tbOpacity
            // 
            this.tbOpacity.Location = new System.Drawing.Point(83, 19);
            this.tbOpacity.Maximum = 100;
            this.tbOpacity.Name = "tbOpacity";
            this.tbOpacity.Size = new System.Drawing.Size(104, 45);
            this.tbOpacity.TabIndex = 22;
            this.tbOpacity.Value = 100;
            this.tbOpacity.Scroll += new System.EventHandler(this.tbOpacity_Scroll);
            // 
            // lblOpacity
            // 
            this.lblOpacity.AutoSize = true;
            this.lblOpacity.Location = new System.Drawing.Point(7, 19);
            this.lblOpacity.Name = "lblOpacity";
            this.lblOpacity.Size = new System.Drawing.Size(46, 13);
            this.lblOpacity.TabIndex = 21;
            this.lblOpacity.Text = "Opacity:";
            // 
            // txtWarningTime
            // 
            this.txtWarningTime.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtWarningTime.Location = new System.Drawing.Point(93, 145);
            this.txtWarningTime.Name = "txtWarningTime";
            this.txtWarningTime.Size = new System.Drawing.Size(43, 20);
            this.txtWarningTime.TabIndex = 20;
            this.txtWarningTime.Text = "00:30";
            this.txtWarningTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblWarningTime
            // 
            this.lblWarningTime.AutoSize = true;
            this.lblWarningTime.Location = new System.Drawing.Point(10, 149);
            this.lblWarningTime.Name = "lblWarningTime";
            this.lblWarningTime.Size = new System.Drawing.Size(76, 13);
            this.lblWarningTime.TabIndex = 19;
            this.lblWarningTime.Text = "Warning Time:";
            // 
            // lblWarnPickBack
            // 
            this.lblWarnPickBack.AutoSize = true;
            this.lblWarnPickBack.BackColor = System.Drawing.Color.Red;
            this.lblWarnPickBack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWarnPickBack.Location = new System.Drawing.Point(113, 126);
            this.lblWarnPickBack.MinimumSize = new System.Drawing.Size(12, 12);
            this.lblWarnPickBack.Name = "lblWarnPickBack";
            this.lblWarnPickBack.Size = new System.Drawing.Size(12, 15);
            this.lblWarnPickBack.TabIndex = 18;
            this.lblWarnPickBack.Click += new System.EventHandler(this.lblWarnPickBack_Click);
            // 
            // lblWarnPickFore
            // 
            this.lblWarnPickFore.AutoSize = true;
            this.lblWarnPickFore.BackColor = System.Drawing.Color.White;
            this.lblWarnPickFore.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWarnPickFore.Location = new System.Drawing.Point(95, 126);
            this.lblWarnPickFore.MinimumSize = new System.Drawing.Size(12, 12);
            this.lblWarnPickFore.Name = "lblWarnPickFore";
            this.lblWarnPickFore.Size = new System.Drawing.Size(12, 15);
            this.lblWarnPickFore.TabIndex = 17;
            this.lblWarnPickFore.Click += new System.EventHandler(this.lblWarnPickFore_Click);
            // 
            // lblWarningColors
            // 
            this.lblWarningColors.AutoSize = true;
            this.lblWarningColors.Location = new System.Drawing.Point(4, 126);
            this.lblWarningColors.Name = "lblWarningColors";
            this.lblWarningColors.Size = new System.Drawing.Size(82, 13);
            this.lblWarningColors.TabIndex = 16;
            this.lblWarningColors.Text = "Warning Colors:";
            // 
            // lblFontSize
            // 
            this.lblFontSize.AutoSize = true;
            this.lblFontSize.Location = new System.Drawing.Point(6, 71);
            this.lblFontSize.Name = "lblFontSize";
            this.lblFontSize.Size = new System.Drawing.Size(54, 13);
            this.lblFontSize.TabIndex = 13;
            this.lblFontSize.Text = "Font Size:";
            // 
            // lblBuffPickBack
            // 
            this.lblBuffPickBack.AutoSize = true;
            this.lblBuffPickBack.BackColor = System.Drawing.Color.Black;
            this.lblBuffPickBack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBuffPickBack.Location = new System.Drawing.Point(113, 148);
            this.lblBuffPickBack.MinimumSize = new System.Drawing.Size(12, 12);
            this.lblBuffPickBack.Name = "lblBuffPickBack";
            this.lblBuffPickBack.Size = new System.Drawing.Size(12, 15);
            this.lblBuffPickBack.TabIndex = 35;
            // 
            // lblBuffPickFore
            // 
            this.lblBuffPickFore.AutoSize = true;
            this.lblBuffPickFore.BackColor = System.Drawing.Color.Orange;
            this.lblBuffPickFore.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBuffPickFore.Location = new System.Drawing.Point(95, 148);
            this.lblBuffPickFore.MinimumSize = new System.Drawing.Size(12, 12);
            this.lblBuffPickFore.Name = "lblBuffPickFore";
            this.lblBuffPickFore.Size = new System.Drawing.Size(12, 15);
            this.lblBuffPickFore.TabIndex = 34;
            // 
            // lblBuffColors
            // 
            this.lblBuffColors.AutoSize = true;
            this.lblBuffColors.Location = new System.Drawing.Point(22, 148);
            this.lblBuffColors.Name = "lblBuffColors";
            this.lblBuffColors.Size = new System.Drawing.Size(61, 13);
            this.lblBuffColors.TabIndex = 33;
            this.lblBuffColors.Text = "Buff Colors:";
            // 
            // chkShowPing
            // 
            this.chkShowPing.AutoSize = true;
            this.chkShowPing.Location = new System.Drawing.Point(222, 148);
            this.chkShowPing.Name = "chkShowPing";
            this.chkShowPing.Size = new System.Drawing.Size(15, 14);
            this.chkShowPing.TabIndex = 32;
            this.chkShowPing.UseVisualStyleBackColor = true;
            // 
            // lblShowPing
            // 
            this.lblShowPing.AutoSize = true;
            this.lblShowPing.Location = new System.Drawing.Point(153, 148);
            this.lblShowPing.Name = "lblShowPing";
            this.lblShowPing.Size = new System.Drawing.Size(61, 13);
            this.lblShowPing.TabIndex = 31;
            this.lblShowPing.Text = "Show Ping:";
            // 
            // txtPingTime
            // 
            this.txtPingTime.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPingTime.Location = new System.Drawing.Point(222, 188);
            this.txtPingTime.Name = "txtPingTime";
            this.txtPingTime.Size = new System.Drawing.Size(43, 20);
            this.txtPingTime.TabIndex = 30;
            this.txtPingTime.Text = "00:30";
            this.txtPingTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblPingTime
            // 
            this.lblPingTime.AutoSize = true;
            this.lblPingTime.Location = new System.Drawing.Point(159, 191);
            this.lblPingTime.Name = "lblPingTime";
            this.lblPingTime.Size = new System.Drawing.Size(57, 13);
            this.lblPingTime.TabIndex = 29;
            this.lblPingTime.Text = "Ping Time:";
            // 
            // lblPingPickBack
            // 
            this.lblPingPickBack.AutoSize = true;
            this.lblPingPickBack.BackColor = System.Drawing.Color.Black;
            this.lblPingPickBack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPingPickBack.Location = new System.Drawing.Point(240, 168);
            this.lblPingPickBack.MinimumSize = new System.Drawing.Size(12, 12);
            this.lblPingPickBack.Name = "lblPingPickBack";
            this.lblPingPickBack.Size = new System.Drawing.Size(12, 15);
            this.lblPingPickBack.TabIndex = 28;
            // 
            // lblPingPickFore
            // 
            this.lblPingPickFore.AutoSize = true;
            this.lblPingPickFore.BackColor = System.Drawing.Color.LightGreen;
            this.lblPingPickFore.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPingPickFore.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblPingPickFore.Location = new System.Drawing.Point(222, 168);
            this.lblPingPickFore.MinimumSize = new System.Drawing.Size(12, 12);
            this.lblPingPickFore.Name = "lblPingPickFore";
            this.lblPingPickFore.Size = new System.Drawing.Size(12, 15);
            this.lblPingPickFore.TabIndex = 27;
            // 
            // lblPingColors
            // 
            this.lblPingColors.AutoSize = true;
            this.lblPingColors.Location = new System.Drawing.Point(153, 168);
            this.lblPingColors.Name = "lblPingColors";
            this.lblPingColors.Size = new System.Drawing.Size(63, 13);
            this.lblPingColors.TabIndex = 26;
            this.lblPingColors.Text = "Ping Colors:";
            // 
            // lblNormPickBack
            // 
            this.lblNormPickBack.AutoSize = true;
            this.lblNormPickBack.BackColor = System.Drawing.Color.Black;
            this.lblNormPickBack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNormPickBack.Location = new System.Drawing.Point(113, 128);
            this.lblNormPickBack.MinimumSize = new System.Drawing.Size(12, 12);
            this.lblNormPickBack.Name = "lblNormPickBack";
            this.lblNormPickBack.Size = new System.Drawing.Size(12, 15);
            this.lblNormPickBack.TabIndex = 25;
            // 
            // lblNormPickFore
            // 
            this.lblNormPickFore.AutoSize = true;
            this.lblNormPickFore.BackColor = System.Drawing.Color.Yellow;
            this.lblNormPickFore.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNormPickFore.Location = new System.Drawing.Point(95, 128);
            this.lblNormPickFore.MinimumSize = new System.Drawing.Size(12, 12);
            this.lblNormPickFore.Name = "lblNormPickFore";
            this.lblNormPickFore.Size = new System.Drawing.Size(12, 15);
            this.lblNormPickFore.TabIndex = 24;
            // 
            // lblNormalColors
            // 
            this.lblNormalColors.AutoSize = true;
            this.lblNormalColors.Location = new System.Drawing.Point(10, 128);
            this.lblNormalColors.Name = "lblNormalColors";
            this.lblNormalColors.Size = new System.Drawing.Size(75, 13);
            this.lblNormalColors.TabIndex = 23;
            this.lblNormalColors.Text = "Normal Colors:";
            // 
            // viewSepSortRefresh
            // 
            this.viewSepSortRefresh.Name = "viewSepSortRefresh";
            this.viewSepSortRefresh.Size = new System.Drawing.Size(157, 6);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusTomePath,
            this.statusParsing,
            this.statusSortInfo,
            this.statusTimerStats});
            this.statusStrip.Location = new System.Drawing.Point(0, 676);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1400, 24);
            this.statusStrip.TabIndex = 20;
            // 
            // statusTomePath
            // 
            this.statusTomePath.Name = "statusTomePath";
            this.statusTomePath.Size = new System.Drawing.Size(1165, 19);
            this.statusTomePath.Spring = true;
            this.statusTomePath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // statusParsing
            // 
            this.statusParsing.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusParsing.Name = "statusParsing";
            this.statusParsing.Size = new System.Drawing.Size(30, 19);
            this.statusParsing.Text = "Idle";
            // 
            // statusSortInfo
            // 
            this.statusSortInfo.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusSortInfo.Name = "statusSortInfo";
            this.statusSortInfo.Size = new System.Drawing.Size(4, 19);
            this.statusSortInfo.Visible = false;
            // 
            // statusTimerStats
            // 
            this.statusTimerStats.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusTimerStats.Name = "statusTimerStats";
            this.statusTimerStats.Size = new System.Drawing.Size(190, 19);
            this.statusTimerStats.Text = "Timers: 0/0   Active: 0   Running: 0";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(1400, 700);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.tabCtrlMain);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.menuStripMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStripMain;
            this.MinimumSize = new System.Drawing.Size(800, 550);
            this.Name = "FormMain";
            this.Text = "Thorne Timer";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.menuStripMain.ResumeLayout(false);
            this.menuStripMain.PerformLayout();
            this.tabCtrlMain.ResumeLayout(false);
            this.tabTimers.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdTimers)).EndInit();
            this.tabCharacters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdCharacters)).EndInit();
            this.tabCategories.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdCategories)).EndInit();
            this.tabStyles.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdStyles)).EndInit();
            this.tabViews.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdViews)).EndInit();
            this.tabSettings.ResumeLayout(false);
            this.gbVoice.ResumeLayout(false);
            this.gbVoice.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbVoiceRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbVolume)).EndInit();
            this.grpMiniView.ResumeLayout(false);
            this.grpMiniView.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbFontSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbOpacity)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripComboBox tscActiveCharacter;
        private System.Windows.Forms.ToolStripSeparator tsSepCharacter;
        private System.Windows.Forms.ToolStripButton tsbStartStopWatching;
        private System.Windows.Forms.ToolStripSeparator tsSepWatch;
        private System.Windows.Forms.ToolStripButton tsbMiniViews;
        private System.Windows.Forms.ToolStripButton tsbAutoSwitch;
        private System.Windows.Forms.ToolStripButton tsbShowAllClasses;
        private System.Windows.Forms.ToolStripButton tsbShowActiveOnly;
        private System.Windows.Forms.ToolStripButton tsbCompactView;
        private System.Windows.Forms.MenuStrip menuStripMain;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TabControl tabCtrlMain;
        private System.Windows.Forms.TabPage tabTimers;
        private System.Windows.Forms.Button btnAddTimer;
        private System.Windows.Forms.Button btnDeleteTimer;
        private System.Windows.Forms.Button btnDuplicateTimer;
        private System.Windows.Forms.Button btnChainTimer;
        private System.Windows.Forms.ContextMenuStrip cmsTimers;
        private System.Windows.Forms.ToolStripMenuItem cmsTimersAdd;
        private System.Windows.Forms.ToolStripMenuItem cmsTimersDuplicate;
        private System.Windows.Forms.ToolStripMenuItem cmsTimersChain;
        private System.Windows.Forms.ToolStripMenuItem cmsTimersDelete;
        private System.Windows.Forms.DataGridView grdTimers;
        private System.Windows.Forms.TabPage tabCharacters;
        private System.Windows.Forms.DataGridView grdCharacters;
        private System.Windows.Forms.Button btnAddCharacter;
        private System.Windows.Forms.Button btnDeleteCharacter;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem miniViewsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compactViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshTimersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem watchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startStopWatchingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoSwitchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showAllClassesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showActiveOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator viewSepCompactFilters;
        private System.Windows.Forms.ToolStripSeparator viewSepFiltersRefresh;
        private System.Windows.Forms.ToolStripSeparator viewSepSortRefresh;
        private System.Windows.Forms.ToolStripSeparator watchSeparator;
        private System.Windows.Forms.ToolStripButton tsbDefaultSort;
        private System.Windows.Forms.ToolStripMenuItem defaultSortToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator tsSepView;
        private System.Windows.Forms.ToolStripSeparator tsSepSort;
        private System.Windows.Forms.TabPage tabCategories;
        private System.Windows.Forms.Button btnAddCategory;
        private System.Windows.Forms.Button btnDeleteCategory;
        private System.Windows.Forms.DataGridView grdCategories;
        private System.Windows.Forms.ComboBox cboActiveVoice;
        private System.Windows.Forms.Label lblActiveVoice;
        private System.Windows.Forms.TabPage tabSettings;
        private System.Windows.Forms.TrackBar tbFontSize;
        private System.Windows.Forms.Label lblFontSize;
        private System.Windows.Forms.GroupBox grpMiniView;
        private System.Windows.Forms.GroupBox gbVoice;
        private System.Windows.Forms.Label lblWarnPickFore;
        private System.Windows.Forms.Label lblWarningColors;
        private System.Windows.Forms.ColorDialog colorDialogPicker;
        private System.Windows.Forms.TextBox txtWarningTime;
        private System.Windows.Forms.Label lblWarningTime;
        private System.Windows.Forms.Label lblWarnPickBack;
        private System.Windows.Forms.Label lblOpacity;
        private System.Windows.Forms.TrackBar tbOpacity;
        private System.Windows.Forms.TrackBar tbVolume;
        private System.Windows.Forms.Label lblVolume;
        private System.Windows.Forms.Button btnTestVolume;
        private System.Windows.Forms.TrackBar tbVoiceRate;
        private System.Windows.Forms.Label lblVoiceRate;
        private System.Windows.Forms.Label lblNormPickBack;
        private System.Windows.Forms.Label lblNormPickFore;
        private System.Windows.Forms.Label lblNormalColors;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tomeInfoToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator helpSepTomeAbout;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.Label lblPingColors;
        private System.Windows.Forms.Label lblPingPickBack;
        private System.Windows.Forms.Label lblPingPickFore;
        private System.Windows.Forms.CheckBox chkShowPing;
        private System.Windows.Forms.Label lblShowPing;
        private System.Windows.Forms.TextBox txtPingTime;
        private System.Windows.Forms.Label lblPingTime;
        private System.Windows.Forms.Button buttonStopAll;
        private System.Windows.Forms.Label lblBuffPickBack;
        private System.Windows.Forms.Label lblBuffPickFore;
        private System.Windows.Forms.Label lblBuffColors;
        private System.Windows.Forms.TabPage tabViews;
        private System.Windows.Forms.DataGridView grdViews;
        private System.Windows.Forms.TabPage tabStyles;
        private System.Windows.Forms.DataGridView grdStyles;
        private System.Windows.Forms.Button btnAddStyle;
        private System.Windows.Forms.Button btnDeleteStyle;
        private System.Windows.Forms.Button btnDeleteView;
        private System.Windows.Forms.Button btnAddView;
        private System.Windows.Forms.Button btnResetCounts;
        private System.Windows.Forms.Label lblVoiceEnabled;
        private System.Windows.Forms.CheckBox chkVoiceEnabled;
        private System.Windows.Forms.ToolStripMenuItem newDatabaseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDatabaseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveDatabaseAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openRecentToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator fileSepSaveRecent;
        private System.Windows.Forms.ToolStripSeparator fileSepRecentExit;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusTomePath;
        private System.Windows.Forms.ToolStripStatusLabel statusParsing;
        private System.Windows.Forms.ToolStripStatusLabel statusTimerStats;
        private System.Windows.Forms.ToolStripStatusLabel statusSortInfo;
    }
}

