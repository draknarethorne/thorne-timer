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
            this.lblActiveChar = new System.Windows.Forms.Label();
            this.btnStartStopLog = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabCtrlMain = new System.Windows.Forms.TabControl();
            this.tabTimers = new System.Windows.Forms.TabPage();
            this.btnResetCounts = new System.Windows.Forms.Button();
            this.buttonStopAll = new System.Windows.Forms.Button();
            this.labelTimerCount = new System.Windows.Forms.Label();
            this.btnAddTimer = new System.Windows.Forms.Button();
            this.btnDeleteTimer = new System.Windows.Forms.Button();
            this.grdTimers = new System.Windows.Forms.DataGridView();
            this.tabCharacters = new System.Windows.Forms.TabPage();
            this.btnAddCharacter = new System.Windows.Forms.Button();
            this.btnDeleteCharacter = new System.Windows.Forms.Button();
            this.grdCharacters = new System.Windows.Forms.DataGridView();
            this.tabCategories = new System.Windows.Forms.TabPage();
            this.btnAddCategory = new System.Windows.Forms.Button();
            this.btnDeleteCategory = new System.Windows.Forms.Button();
            this.grdCategories = new System.Windows.Forms.DataGridView();
            this.tabViews = new System.Windows.Forms.TabPage();
            this.btnDeleteView = new System.Windows.Forms.Button();
            this.btnAddView = new System.Windows.Forms.Button();
            this.grdViews = new System.Windows.Forms.DataGridView();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.gbVoice = new System.Windows.Forms.GroupBox();
            this.tbVoiceRate = new System.Windows.Forms.TrackBar();
            this.lblVoiceRate = new System.Windows.Forms.Label();
            this.btnTestVolume = new System.Windows.Forms.Button();
            this.tbVolume = new System.Windows.Forms.TrackBar();
            this.lblVolume = new System.Windows.Forms.Label();
            this.cboActiveVoice = new System.Windows.Forms.ComboBox();
            this.lblActiveVoice = new System.Windows.Forms.Label();
            this.grpMiniView = new System.Windows.Forms.GroupBox();
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
            this.tbFontSize = new System.Windows.Forms.TrackBar();
            this.lblNormalColors = new System.Windows.Forms.Label();
            this.tbOpacity = new System.Windows.Forms.TrackBar();
            this.lblOpacity = new System.Windows.Forms.Label();
            this.txtWarningTime = new System.Windows.Forms.TextBox();
            this.lblWarningTime = new System.Windows.Forms.Label();
            this.lblWarnPickBack = new System.Windows.Forms.Label();
            this.lblWarnPickFore = new System.Windows.Forms.Label();
            this.lblWarningColors = new System.Windows.Forms.Label();
            this.lblFontSize = new System.Windows.Forms.Label();
            this.cboActiveCharacter = new System.Windows.Forms.ComboBox();
            this.btnMiniView = new System.Windows.Forms.Button();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.labelLogFile = new System.Windows.Forms.Label();
            this.chkVoiceEnabled = new System.Windows.Forms.CheckBox();
            this.lblVoiceEnabled = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.tabCtrlMain.SuspendLayout();
            this.tabTimers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdTimers)).BeginInit();
            this.tabCharacters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdCharacters)).BeginInit();
            this.tabCategories.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdCategories)).BeginInit();
            this.tabViews.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdViews)).BeginInit();
            this.tabSettings.SuspendLayout();
            this.gbVoice.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbVoiceRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbVolume)).BeginInit();
            this.grpMiniView.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbFontSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbOpacity)).BeginInit();
            this.SuspendLayout();
            // 
            // lblActiveChar
            // 
            this.lblActiveChar.AutoSize = true;
            this.lblActiveChar.Location = new System.Drawing.Point(9, 37);
            this.lblActiveChar.Name = "lblActiveChar";
            this.lblActiveChar.Size = new System.Drawing.Size(89, 13);
            this.lblActiveChar.TabIndex = 3;
            this.lblActiveChar.Text = "Active Character:";
            // 
            // btnStartStopLog
            // 
            this.btnStartStopLog.BackColor = System.Drawing.SystemColors.Control;
            this.btnStartStopLog.Location = new System.Drawing.Point(231, 32);
            this.btnStartStopLog.Name = "btnStartStopLog";
            this.btnStartStopLog.Size = new System.Drawing.Size(116, 23);
            this.btnStartStopLog.TabIndex = 6;
            this.btnStartStopLog.Text = "Start Parsing Log";
            this.btnStartStopLog.UseVisualStyleBackColor = true;
            this.btnStartStopLog.Click += new System.EventHandler(this.btnStartStopLog_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.SystemColors.MenuBar;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1152, 24);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(93, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
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
            this.tabCtrlMain.Controls.Add(this.tabViews);
            this.tabCtrlMain.Controls.Add(this.tabSettings);
            this.tabCtrlMain.Location = new System.Drawing.Point(12, 61);
            this.tabCtrlMain.Name = "tabCtrlMain";
            this.tabCtrlMain.SelectedIndex = 0;
            this.tabCtrlMain.Size = new System.Drawing.Size(1128, 433);
            this.tabCtrlMain.TabIndex = 9;
            // 
            // tabTimers
            // 
            this.tabTimers.BackColor = System.Drawing.SystemColors.Control;
            this.tabTimers.Controls.Add(this.btnResetCounts);
            this.tabTimers.Controls.Add(this.buttonStopAll);
            this.tabTimers.Controls.Add(this.labelTimerCount);
            this.tabTimers.Controls.Add(this.btnAddTimer);
            this.tabTimers.Controls.Add(this.btnDeleteTimer);
            this.tabTimers.Controls.Add(this.grdTimers);
            this.tabTimers.Location = new System.Drawing.Point(4, 22);
            this.tabTimers.Name = "tabTimers";
            this.tabTimers.Padding = new System.Windows.Forms.Padding(3);
            this.tabTimers.Size = new System.Drawing.Size(1120, 407);
            this.tabTimers.TabIndex = 0;
            this.tabTimers.Text = "Timers";
            // 
            // btnResetCounts
            // 
            this.btnResetCounts.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnResetCounts.BackColor = System.Drawing.SystemColors.Control;
            this.btnResetCounts.Location = new System.Drawing.Point(87, 378);
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
            this.buttonStopAll.Location = new System.Drawing.Point(958, 378);
            this.buttonStopAll.Name = "buttonStopAll";
            this.buttonStopAll.Size = new System.Drawing.Size(75, 23);
            this.buttonStopAll.TabIndex = 17;
            this.buttonStopAll.Text = "Stop All";
            this.buttonStopAll.UseVisualStyleBackColor = true;
            this.buttonStopAll.Click += new System.EventHandler(this.btnStopAll_Click);
            // 
            // labelTimerCount
            // 
            this.labelTimerCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTimerCount.AutoSize = true;
            this.labelTimerCount.Location = new System.Drawing.Point(766, 383);
            this.labelTimerCount.Name = "labelTimerCount";
            this.labelTimerCount.Size = new System.Drawing.Size(162, 13);
            this.labelTimerCount.TabIndex = 16;
            this.labelTimerCount.Text = "Timers: 0   Active: 0   Running: 0";
            this.labelTimerCount.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // btnAddTimer
            // 
            this.btnAddTimer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddTimer.BackColor = System.Drawing.SystemColors.Control;
            this.btnAddTimer.Location = new System.Drawing.Point(1039, 378);
            this.btnAddTimer.Name = "btnAddTimer";
            this.btnAddTimer.Size = new System.Drawing.Size(75, 23);
            this.btnAddTimer.TabIndex = 4;
            this.btnAddTimer.Text = "Add";
            this.btnAddTimer.UseVisualStyleBackColor = true;
            this.btnAddTimer.Click += new System.EventHandler(this.btnAddTimer_Click);
            // 
            // btnDeleteTimer
            // 
            this.btnDeleteTimer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteTimer.BackColor = System.Drawing.SystemColors.Control;
            this.btnDeleteTimer.Location = new System.Drawing.Point(6, 378);
            this.btnDeleteTimer.Name = "btnDeleteTimer";
            this.btnDeleteTimer.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteTimer.TabIndex = 3;
            this.btnDeleteTimer.Text = "Delete";
            this.btnDeleteTimer.UseVisualStyleBackColor = true;
            this.btnDeleteTimer.Click += new System.EventHandler(this.btnDeleteTimer_Click);
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
            this.grdTimers.Location = new System.Drawing.Point(6, 6);
            this.grdTimers.Name = "grdTimers";
            this.grdTimers.Size = new System.Drawing.Size(1108, 366);
            this.grdTimers.TabIndex = 1;
            this.grdTimers.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grdTimers_CellClick);
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
            this.tabCharacters.Size = new System.Drawing.Size(1120, 407);
            this.tabCharacters.TabIndex = 1;
            this.tabCharacters.Text = "Characters";
            // 
            // btnAddCharacter
            // 
            this.btnAddCharacter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddCharacter.Location = new System.Drawing.Point(997, 368);
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
            this.btnDeleteCharacter.Location = new System.Drawing.Point(6, 368);
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
            this.grdCharacters.Size = new System.Drawing.Size(1066, 356);
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
            this.tabCategories.Size = new System.Drawing.Size(1120, 407);
            this.tabCategories.TabIndex = 2;
            this.tabCategories.Text = "Categories";
            // 
            // btnAddCategory
            // 
            this.btnAddCategory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddCategory.Location = new System.Drawing.Point(997, 368);
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
            this.btnDeleteCategory.Location = new System.Drawing.Point(6, 368);
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
            this.grdCategories.Size = new System.Drawing.Size(1066, 356);
            this.grdCategories.TabIndex = 1;
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
            this.tabViews.Size = new System.Drawing.Size(1120, 407);
            this.tabViews.TabIndex = 3;
            this.tabViews.Text = "Views";
            // 
            // btnDeleteView
            // 
            this.btnDeleteView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteView.Location = new System.Drawing.Point(6, 368);
            this.btnDeleteView.Name = "btnDeleteView";
            this.btnDeleteView.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteView.TabIndex = 8;
            this.btnDeleteView.Text = "Delete";
            this.btnDeleteView.UseVisualStyleBackColor = true;
            this.btnDeleteView.Click += new System.EventHandler(this.btnDeleteView_Click);
            // 
            // btnAddView
            // 
            this.btnAddView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddView.Location = new System.Drawing.Point(997, 368);
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
            this.grdViews.Size = new System.Drawing.Size(1066, 356);
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
            this.tabSettings.Size = new System.Drawing.Size(1120, 407);
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
            this.grpMiniView.Controls.Add(this.lblBuffPickBack);
            this.grpMiniView.Controls.Add(this.lblBuffPickFore);
            this.grpMiniView.Controls.Add(this.lblBuffColors);
            this.grpMiniView.Controls.Add(this.chkShowPing);
            this.grpMiniView.Controls.Add(this.lblShowPing);
            this.grpMiniView.Controls.Add(this.txtPingTime);
            this.grpMiniView.Controls.Add(this.lblPingTime);
            this.grpMiniView.Controls.Add(this.lblPingPickBack);
            this.grpMiniView.Controls.Add(this.lblPingPickFore);
            this.grpMiniView.Controls.Add(this.lblPingColors);
            this.grpMiniView.Controls.Add(this.lblNormPickBack);
            this.grpMiniView.Controls.Add(this.lblNormPickFore);
            this.grpMiniView.Controls.Add(this.tbFontSize);
            this.grpMiniView.Controls.Add(this.lblNormalColors);
            this.grpMiniView.Controls.Add(this.tbOpacity);
            this.grpMiniView.Controls.Add(this.lblOpacity);
            this.grpMiniView.Controls.Add(this.txtWarningTime);
            this.grpMiniView.Controls.Add(this.lblWarningTime);
            this.grpMiniView.Controls.Add(this.lblWarnPickBack);
            this.grpMiniView.Controls.Add(this.lblWarnPickFore);
            this.grpMiniView.Controls.Add(this.lblWarningColors);
            this.grpMiniView.Controls.Add(this.lblFontSize);
            this.grpMiniView.Location = new System.Drawing.Point(6, 184);
            this.grpMiniView.Name = "grpMiniView";
            this.grpMiniView.Size = new System.Drawing.Size(287, 217);
            this.grpMiniView.TabIndex = 16;
            this.grpMiniView.TabStop = false;
            this.grpMiniView.Text = "Mini View Options";
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
            this.lblBuffPickBack.Click += new System.EventHandler(this.lblBuffPickBack_Click);
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
            this.lblBuffPickFore.Click += new System.EventHandler(this.lblBuffPickFore_Click);
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
            this.chkShowPing.Click += new System.EventHandler(this.chkShowPing_Click);
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
            this.lblPingPickBack.Click += new System.EventHandler(this.lblPingPickBack_Click);
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
            this.lblPingPickFore.Click += new System.EventHandler(this.lblPingPickFore_Click);
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
            this.lblNormPickBack.Click += new System.EventHandler(this.lblNormPickBack_Click);
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
            this.lblNormPickFore.Click += new System.EventHandler(this.lblNormPickFore_Click);
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
            // lblNormalColors
            // 
            this.lblNormalColors.AutoSize = true;
            this.lblNormalColors.Location = new System.Drawing.Point(10, 128);
            this.lblNormalColors.Name = "lblNormalColors";
            this.lblNormalColors.Size = new System.Drawing.Size(75, 13);
            this.lblNormalColors.TabIndex = 23;
            this.lblNormalColors.Text = "Normal Colors:";
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
            this.txtWarningTime.Location = new System.Drawing.Point(93, 187);
            this.txtWarningTime.Name = "txtWarningTime";
            this.txtWarningTime.Size = new System.Drawing.Size(43, 20);
            this.txtWarningTime.TabIndex = 20;
            this.txtWarningTime.Text = "00:30";
            this.txtWarningTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblWarningTime
            // 
            this.lblWarningTime.AutoSize = true;
            this.lblWarningTime.Location = new System.Drawing.Point(10, 191);
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
            this.lblWarnPickBack.Location = new System.Drawing.Point(113, 168);
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
            this.lblWarnPickFore.Location = new System.Drawing.Point(95, 168);
            this.lblWarnPickFore.MinimumSize = new System.Drawing.Size(12, 12);
            this.lblWarnPickFore.Name = "lblWarnPickFore";
            this.lblWarnPickFore.Size = new System.Drawing.Size(12, 15);
            this.lblWarnPickFore.TabIndex = 17;
            this.lblWarnPickFore.Click += new System.EventHandler(this.lblWarnPickFore_Click);
            // 
            // lblWarningColors
            // 
            this.lblWarningColors.AutoSize = true;
            this.lblWarningColors.Location = new System.Drawing.Point(4, 168);
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
            // cboActiveCharacter
            // 
            this.cboActiveCharacter.FormattingEnabled = true;
            this.cboActiveCharacter.Location = new System.Drawing.Point(104, 32);
            this.cboActiveCharacter.Name = "cboActiveCharacter";
            this.cboActiveCharacter.Size = new System.Drawing.Size(121, 21);
            this.cboActiveCharacter.TabIndex = 10;
            this.cboActiveCharacter.SelectedIndexChanged += new System.EventHandler(this.cboActiveCharacter_SelectedIndexChanged);
            // 
            // btnMiniView
            // 
            this.btnMiniView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMiniView.BackColor = System.Drawing.SystemColors.Control;
            this.btnMiniView.Location = new System.Drawing.Point(1055, 32);
            this.btnMiniView.Name = "btnMiniView";
            this.btnMiniView.Size = new System.Drawing.Size(75, 23);
            this.btnMiniView.TabIndex = 13;
            this.btnMiniView.Text = "Mini View";
            this.btnMiniView.UseVisualStyleBackColor = true;
            this.btnMiniView.Click += new System.EventHandler(this.btnMiniView_Click);
            // 
            // labelLogFile
            // 
            this.labelLogFile.AutoSize = true;
            this.labelLogFile.BackColor = System.Drawing.SystemColors.Window;
            this.labelLogFile.Location = new System.Drawing.Point(362, 37);
            this.labelLogFile.Name = "labelLogFile";
            this.labelLogFile.Size = new System.Drawing.Size(24, 13);
            this.labelLogFile.TabIndex = 15;
            this.labelLogFile.Text = "Idle";
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
            // lblVoiceEnabled
            // 
            this.lblVoiceEnabled.AutoSize = true;
            this.lblVoiceEnabled.Location = new System.Drawing.Point(8, 136);
            this.lblVoiceEnabled.Name = "lblVoiceEnabled";
            this.lblVoiceEnabled.Size = new System.Drawing.Size(79, 13);
            this.lblVoiceEnabled.TabIndex = 36;
            this.lblVoiceEnabled.Text = "Voice Enabled:";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(1152, 524);
            this.Controls.Add(this.tabCtrlMain);
            this.Controls.Add(this.labelLogFile);
            this.Controls.Add(this.btnMiniView);
            this.Controls.Add(this.cboActiveCharacter);
            this.Controls.Add(this.btnStartStopLog);
            this.Controls.Add(this.lblActiveChar);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormMain";
            this.Text = "Thorne Timer";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabCtrlMain.ResumeLayout(false);
            this.tabTimers.ResumeLayout(false);
            this.tabTimers.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdTimers)).EndInit();
            this.tabCharacters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdCharacters)).EndInit();
            this.tabCategories.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdCategories)).EndInit();
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
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblActiveChar;
        private System.Windows.Forms.Button btnStartStopLog;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TabControl tabCtrlMain;
        private System.Windows.Forms.TabPage tabTimers;
        private System.Windows.Forms.Button btnAddTimer;
        private System.Windows.Forms.Button btnDeleteTimer;
        private System.Windows.Forms.DataGridView grdTimers;
        private System.Windows.Forms.TabPage tabCharacters;
        private System.Windows.Forms.DataGridView grdCharacters;
        private System.Windows.Forms.Button btnAddCharacter;
        private System.Windows.Forms.Button btnDeleteCharacter;
        private System.Windows.Forms.ComboBox cboActiveCharacter;
        private System.Windows.Forms.TabPage tabCategories;
        private System.Windows.Forms.Button btnAddCategory;
        private System.Windows.Forms.Button btnDeleteCategory;
        private System.Windows.Forms.DataGridView grdCategories;
        private System.Windows.Forms.ComboBox cboActiveVoice;
        private System.Windows.Forms.Label lblActiveVoice;
        private System.Windows.Forms.Button btnMiniView;
        private System.Windows.Forms.TabPage tabSettings;
        private System.Windows.Forms.TrackBar tbFontSize;
        private System.Windows.Forms.Label lblFontSize;
        private System.Windows.Forms.GroupBox grpMiniView;
        private System.Windows.Forms.GroupBox gbVoice;
        private System.Windows.Forms.Label lblWarnPickFore;
        private System.Windows.Forms.Label lblWarningColors;
        private System.Windows.Forms.ColorDialog colorDialog1;
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
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.Label labelLogFile;
        private System.Windows.Forms.Label labelTimerCount;
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
        private System.Windows.Forms.Button btnDeleteView;
        private System.Windows.Forms.Button btnAddView;
        private System.Windows.Forms.Button btnResetCounts;
        private System.Windows.Forms.Label lblVoiceEnabled;
        private System.Windows.Forms.CheckBox chkVoiceEnabled;
    }
}

