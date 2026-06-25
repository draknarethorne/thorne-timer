namespace ThorneTimer
{
    partial class FormTomeInfo
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolTipPath = new System.Windows.Forms.ToolTip(this.components);
            this.panelHeader = new System.Windows.Forms.Panel();
            this.labelTomeName = new System.Windows.Forms.Label();
            this.labelTomePath = new System.Windows.Forms.Label();
            this.panelSeparator1 = new System.Windows.Forms.Panel();
            this.groupFile = new System.Windows.Forms.GroupBox();
            this.labelFileSizeCaption = new System.Windows.Forms.Label();
            this.labelFileSize = new System.Windows.Forms.Label();
            this.labelCreatedCaption = new System.Windows.Forms.Label();
            this.labelCreated = new System.Windows.Forms.Label();
            this.labelModifiedCaption = new System.Windows.Forms.Label();
            this.labelModified = new System.Windows.Forms.Label();
            this.labelAppVersionCaption = new System.Windows.Forms.Label();
            this.labelAppVersion = new System.Windows.Forms.Label();
            this.labelTomeVersionCaption = new System.Windows.Forms.Label();
            this.labelTomeVersion = new System.Windows.Forms.Label();
            this.labelCreatedByCaption = new System.Windows.Forms.Label();
            this.labelCreatedBy = new System.Windows.Forms.Label();
            this.groupStats = new System.Windows.Forms.GroupBox();
            this.labelTimersCaption = new System.Windows.Forms.Label();
            this.labelTimersValue = new System.Windows.Forms.Label();
            this.labelActiveTimersCaption = new System.Windows.Forms.Label();
            this.labelActiveTimersValue = new System.Windows.Forms.Label();
            this.labelRunningTimersCaption = new System.Windows.Forms.Label();
            this.labelRunningTimersValue = new System.Windows.Forms.Label();
            this.labelCountCharactersCap = new System.Windows.Forms.Label();
            this.labelCountCharacters = new System.Windows.Forms.Label();
            this.labelCountCategoriesCap = new System.Windows.Forms.Label();
            this.labelCountCategories = new System.Windows.Forms.Label();
            this.labelCountStylesCap = new System.Windows.Forms.Label();
            this.labelCountStyles = new System.Windows.Forms.Label();
            this.labelCountViewsCap = new System.Windows.Forms.Label();
            this.labelCountViews = new System.Windows.Forms.Label();
            this.labelCountClassesCap = new System.Windows.Forms.Label();
            this.labelCountClasses = new System.Windows.Forms.Label();
            this.groupUsage = new System.Windows.Forms.GroupBox();
            this.listUsage = new System.Windows.Forms.ListView();
            this.colUsageFeature = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUsageCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUsagePct = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBreakdown = new System.Windows.Forms.GroupBox();
            this.tableBreakdown = new System.Windows.Forms.TableLayoutPanel();
            this.listByCategory = new System.Windows.Forms.ListView();
            this.colCatName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colCatCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listByStyle = new System.Windows.Forms.ListView();
            this.colStyleName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colStyleCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listByClass = new System.Windows.Forms.ListView();
            this.colClassName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colClassCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listByScope = new System.Windows.Forms.ListView();
            this.colScopeName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colScopeCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonOk = new System.Windows.Forms.Button();
            this.panelHeader.SuspendLayout();
            this.groupFile.SuspendLayout();
            this.groupStats.SuspendLayout();
            this.groupUsage.SuspendLayout();
            this.groupBreakdown.SuspendLayout();
            this.tableBreakdown.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.White;
            this.panelHeader.Controls.Add(this.labelTomeName);
            this.panelHeader.Controls.Add(this.labelTomePath);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(800, 56);
            this.panelHeader.TabIndex = 0;
            // 
            // labelTomeName
            // 
            this.labelTomeName.AutoSize = true;
            this.labelTomeName.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.labelTomeName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelTomeName.Location = new System.Drawing.Point(14, 8);
            this.labelTomeName.Name = "labelTomeName";
            this.labelTomeName.Size = new System.Drawing.Size(137, 21);
            this.labelTomeName.TabIndex = 0;
            this.labelTomeName.Text = "ThorneTimer.tdb";
            // 
            // labelTomePath
            // 
            this.labelTomePath.AutoEllipsis = true;
            this.labelTomePath.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.labelTomePath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.labelTomePath.Location = new System.Drawing.Point(14, 32);
            this.labelTomePath.Name = "labelTomePath";
            this.labelTomePath.Size = new System.Drawing.Size(772, 16);
            this.labelTomePath.TabIndex = 1;
            this.labelTomePath.Text = "C:\\path\\to\\tome";
            // 
            // panelSeparator1
            // 
            this.panelSeparator1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.panelSeparator1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSeparator1.Location = new System.Drawing.Point(0, 56);
            this.panelSeparator1.Name = "panelSeparator1";
            this.panelSeparator1.Size = new System.Drawing.Size(800, 1);
            this.panelSeparator1.TabIndex = 1;
            // 
            // groupFile
            // 
            this.groupFile.Controls.Add(this.labelFileSizeCaption);
            this.groupFile.Controls.Add(this.labelFileSize);
            this.groupFile.Controls.Add(this.labelCreatedCaption);
            this.groupFile.Controls.Add(this.labelCreated);
            this.groupFile.Controls.Add(this.labelModifiedCaption);
            this.groupFile.Controls.Add(this.labelModified);
            this.groupFile.Controls.Add(this.labelAppVersionCaption);
            this.groupFile.Controls.Add(this.labelAppVersion);
            this.groupFile.Controls.Add(this.labelTomeVersionCaption);
            this.groupFile.Controls.Add(this.labelTomeVersion);
            this.groupFile.Controls.Add(this.labelCreatedByCaption);
            this.groupFile.Controls.Add(this.labelCreatedBy);
            this.groupFile.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.groupFile.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.groupFile.Location = new System.Drawing.Point(14, 64);
            this.groupFile.Name = "groupFile";
            this.groupFile.Size = new System.Drawing.Size(772, 92);
            this.groupFile.TabIndex = 2;
            this.groupFile.TabStop = false;
            this.groupFile.Text = "File Information";
            // 
            // labelFileSizeCaption
            // 
            this.labelFileSizeCaption.AutoSize = true;
            this.labelFileSizeCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelFileSizeCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelFileSizeCaption.Location = new System.Drawing.Point(12, 22);
            this.labelFileSizeCaption.Name = "labelFileSizeCaption";
            this.labelFileSizeCaption.Size = new System.Drawing.Size(51, 13);
            this.labelFileSizeCaption.TabIndex = 0;
            this.labelFileSizeCaption.Text = "File Size:";
            // 
            // labelFileSize
            // 
            this.labelFileSize.AutoSize = true;
            this.labelFileSize.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelFileSize.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelFileSize.Location = new System.Drawing.Point(95, 22);
            this.labelFileSize.Name = "labelFileSize";
            this.labelFileSize.Size = new System.Drawing.Size(18, 13);
            this.labelFileSize.TabIndex = 1;
            this.labelFileSize.Text = "—";
            // 
            // labelCreatedCaption
            // 
            this.labelCreatedCaption.AutoSize = true;
            this.labelCreatedCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCreatedCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCreatedCaption.Location = new System.Drawing.Point(12, 42);
            this.labelCreatedCaption.Name = "labelCreatedCaption";
            this.labelCreatedCaption.Size = new System.Drawing.Size(50, 13);
            this.labelCreatedCaption.TabIndex = 2;
            this.labelCreatedCaption.Text = "Created:";
            // 
            // labelCreated
            // 
            this.labelCreated.AutoSize = true;
            this.labelCreated.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCreated.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelCreated.Location = new System.Drawing.Point(95, 42);
            this.labelCreated.Name = "labelCreated";
            this.labelCreated.Size = new System.Drawing.Size(18, 13);
            this.labelCreated.TabIndex = 3;
            this.labelCreated.Text = "—";
            // 
            // labelModifiedCaption
            // 
            this.labelModifiedCaption.AutoSize = true;
            this.labelModifiedCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelModifiedCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelModifiedCaption.Location = new System.Drawing.Point(260, 22);
            this.labelModifiedCaption.Name = "labelModifiedCaption";
            this.labelModifiedCaption.Size = new System.Drawing.Size(57, 13);
            this.labelModifiedCaption.TabIndex = 4;
            this.labelModifiedCaption.Text = "Modified:";
            // 
            // labelModified
            // 
            this.labelModified.AutoSize = true;
            this.labelModified.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelModified.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelModified.Location = new System.Drawing.Point(335, 22);
            this.labelModified.Name = "labelModified";
            this.labelModified.Size = new System.Drawing.Size(18, 13);
            this.labelModified.TabIndex = 5;
            this.labelModified.Text = "—";
            // 
            // labelAppVersionCaption
            // 
            this.labelAppVersionCaption.AutoSize = true;
            this.labelAppVersionCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelAppVersionCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelAppVersionCaption.Location = new System.Drawing.Point(495, 42);
            this.labelAppVersionCaption.Name = "labelAppVersionCaption";
            this.labelAppVersionCaption.Size = new System.Drawing.Size(72, 13);
            this.labelAppVersionCaption.TabIndex = 6;
            this.labelAppVersionCaption.Text = "App Version:";
            // 
            // labelAppVersion
            // 
            this.labelAppVersion.AutoSize = true;
            this.labelAppVersion.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelAppVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelAppVersion.Location = new System.Drawing.Point(590, 42);
            this.labelAppVersion.Name = "labelAppVersion";
            this.labelAppVersion.Size = new System.Drawing.Size(18, 13);
            this.labelAppVersion.TabIndex = 7;
            this.labelAppVersion.Text = "—";
            // 
            // labelTomeVersionCaption
            // 
            this.labelTomeVersionCaption.AutoSize = true;
            this.labelTomeVersionCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelTomeVersionCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelTomeVersionCaption.Location = new System.Drawing.Point(495, 22);
            this.labelTomeVersionCaption.Name = "labelTomeVersionCaption";
            this.labelTomeVersionCaption.Size = new System.Drawing.Size(77, 13);
            this.labelTomeVersionCaption.TabIndex = 8;
            this.labelTomeVersionCaption.Text = "Tome Version:";
            // 
            // labelTomeVersion
            // 
            this.labelTomeVersion.AutoSize = true;
            this.labelTomeVersion.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelTomeVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelTomeVersion.Location = new System.Drawing.Point(590, 22);
            this.labelTomeVersion.Name = "labelTomeVersion";
            this.labelTomeVersion.Size = new System.Drawing.Size(18, 13);
            this.labelTomeVersion.TabIndex = 9;
            this.labelTomeVersion.Text = "—";
            // 
            // labelCreatedByCaption
            // 
            this.labelCreatedByCaption.AutoSize = true;
            this.labelCreatedByCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCreatedByCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCreatedByCaption.Location = new System.Drawing.Point(260, 42);
            this.labelCreatedByCaption.Name = "labelCreatedByCaption";
            this.labelCreatedByCaption.Size = new System.Drawing.Size(65, 13);
            this.labelCreatedByCaption.TabIndex = 10;
            this.labelCreatedByCaption.Text = "Created By:";
            // 
            // labelCreatedBy
            // 
            this.labelCreatedBy.AutoSize = true;
            this.labelCreatedBy.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCreatedBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelCreatedBy.Location = new System.Drawing.Point(335, 42);
            this.labelCreatedBy.Name = "labelCreatedBy";
            this.labelCreatedBy.Size = new System.Drawing.Size(18, 13);
            this.labelCreatedBy.TabIndex = 11;
            this.labelCreatedBy.Text = "—";
            // 
            // groupStats
            // 
            this.groupStats.Controls.Add(this.labelTimersCaption);
            this.groupStats.Controls.Add(this.labelTimersValue);
            this.groupStats.Controls.Add(this.labelActiveTimersCaption);
            this.groupStats.Controls.Add(this.labelActiveTimersValue);
            this.groupStats.Controls.Add(this.labelRunningTimersCaption);
            this.groupStats.Controls.Add(this.labelRunningTimersValue);
            this.groupStats.Controls.Add(this.labelCountCharactersCap);
            this.groupStats.Controls.Add(this.labelCountCharacters);
            this.groupStats.Controls.Add(this.labelCountCategoriesCap);
            this.groupStats.Controls.Add(this.labelCountCategories);
            this.groupStats.Controls.Add(this.labelCountStylesCap);
            this.groupStats.Controls.Add(this.labelCountStyles);
            this.groupStats.Controls.Add(this.labelCountViewsCap);
            this.groupStats.Controls.Add(this.labelCountViews);
            this.groupStats.Controls.Add(this.labelCountClassesCap);
            this.groupStats.Controls.Add(this.labelCountClasses);
            this.groupStats.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.groupStats.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.groupStats.Location = new System.Drawing.Point(14, 162);
            this.groupStats.Name = "groupStats";
            this.groupStats.Size = new System.Drawing.Size(772, 76);
            this.groupStats.TabIndex = 3;
            this.groupStats.TabStop = false;
            this.groupStats.Text = "Tome Contents";
            // 
            // labelTimersCaption
            // 
            this.labelTimersCaption.AutoSize = false;
            this.labelTimersCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelTimersCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelTimersCaption.Location = new System.Drawing.Point(12, 22);
            this.labelTimersCaption.Name = "labelTimersCaption";
            this.labelTimersCaption.Size = new System.Drawing.Size(75, 18);
            this.labelTimersCaption.TabIndex = 0;
            this.labelTimersCaption.Text = "Timers:";
            this.labelTimersCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelTimersValue
            // 
            this.labelTimersValue.AutoSize = false;
            this.labelTimersValue.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelTimersValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelTimersValue.Location = new System.Drawing.Point(91, 22);
            this.labelTimersValue.Name = "labelTimersValue";
            this.labelTimersValue.Size = new System.Drawing.Size(40, 18);
            this.labelTimersValue.TabIndex = 1;
            this.labelTimersValue.Text = "0";
            this.labelTimersValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelActiveTimersCaption
            // 
            this.labelActiveTimersCaption.AutoSize = false;
            this.labelActiveTimersCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelActiveTimersCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelActiveTimersCaption.Location = new System.Drawing.Point(164, 22);
            this.labelActiveTimersCaption.Name = "labelActiveTimersCaption";
            this.labelActiveTimersCaption.Size = new System.Drawing.Size(75, 18);
            this.labelActiveTimersCaption.TabIndex = 2;
            this.labelActiveTimersCaption.Text = "Active:";
            this.labelActiveTimersCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelActiveTimersValue
            // 
            this.labelActiveTimersValue.AutoSize = false;
            this.labelActiveTimersValue.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelActiveTimersValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelActiveTimersValue.Location = new System.Drawing.Point(243, 22);
            this.labelActiveTimersValue.Name = "labelActiveTimersValue";
            this.labelActiveTimersValue.Size = new System.Drawing.Size(40, 18);
            this.labelActiveTimersValue.TabIndex = 3;
            this.labelActiveTimersValue.Text = "0";
            this.labelActiveTimersValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelRunningTimersCaption
            // 
            this.labelRunningTimersCaption.AutoSize = false;
            this.labelRunningTimersCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelRunningTimersCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelRunningTimersCaption.Location = new System.Drawing.Point(316, 22);
            this.labelRunningTimersCaption.Name = "labelRunningTimersCaption";
            this.labelRunningTimersCaption.Size = new System.Drawing.Size(75, 18);
            this.labelRunningTimersCaption.TabIndex = 4;
            this.labelRunningTimersCaption.Text = "Running:";
            this.labelRunningTimersCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelRunningTimersValue
            // 
            this.labelRunningTimersValue.AutoSize = false;
            this.labelRunningTimersValue.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelRunningTimersValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelRunningTimersValue.Location = new System.Drawing.Point(395, 22);
            this.labelRunningTimersValue.Name = "labelRunningTimersValue";
            this.labelRunningTimersValue.Size = new System.Drawing.Size(40, 18);
            this.labelRunningTimersValue.TabIndex = 5;
            this.labelRunningTimersValue.Text = "0";
            this.labelRunningTimersValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCountCharactersCap
            // 
            this.labelCountCharactersCap.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCountCharactersCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCountCharactersCap.Location = new System.Drawing.Point(12, 50);
            this.labelCountCharactersCap.Name = "labelCountCharactersCap";
            this.labelCountCharactersCap.Size = new System.Drawing.Size(75, 18);
            this.labelCountCharactersCap.TabIndex = 2;
            this.labelCountCharactersCap.Text = "Characters:";
            this.labelCountCharactersCap.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCountCharacters
            // 
            this.labelCountCharacters.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelCountCharacters.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelCountCharacters.Location = new System.Drawing.Point(91, 50);
            this.labelCountCharacters.Name = "labelCountCharacters";
            this.labelCountCharacters.Size = new System.Drawing.Size(40, 18);
            this.labelCountCharacters.TabIndex = 3;
            this.labelCountCharacters.Text = "0";
            this.labelCountCharacters.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCountCategoriesCap
            // 
            this.labelCountCategoriesCap.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCountCategoriesCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCountCategoriesCap.Location = new System.Drawing.Point(164, 50);
            this.labelCountCategoriesCap.Name = "labelCountCategoriesCap";
            this.labelCountCategoriesCap.Size = new System.Drawing.Size(75, 18);
            this.labelCountCategoriesCap.TabIndex = 4;
            this.labelCountCategoriesCap.Text = "Categories:";
            this.labelCountCategoriesCap.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCountCategories
            // 
            this.labelCountCategories.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelCountCategories.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelCountCategories.Location = new System.Drawing.Point(243, 50);
            this.labelCountCategories.Name = "labelCountCategories";
            this.labelCountCategories.Size = new System.Drawing.Size(40, 18);
            this.labelCountCategories.TabIndex = 5;
            this.labelCountCategories.Text = "0";
            this.labelCountCategories.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCountStylesCap
            // 
            this.labelCountStylesCap.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCountStylesCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCountStylesCap.Location = new System.Drawing.Point(316, 50);
            this.labelCountStylesCap.Name = "labelCountStylesCap";
            this.labelCountStylesCap.Size = new System.Drawing.Size(75, 18);
            this.labelCountStylesCap.TabIndex = 6;
            this.labelCountStylesCap.Text = "Styles:";
            this.labelCountStylesCap.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCountStyles
            // 
            this.labelCountStyles.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelCountStyles.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelCountStyles.Location = new System.Drawing.Point(395, 50);
            this.labelCountStyles.Name = "labelCountStyles";
            this.labelCountStyles.Size = new System.Drawing.Size(40, 18);
            this.labelCountStyles.TabIndex = 7;
            this.labelCountStyles.Text = "0";
            this.labelCountStyles.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCountViewsCap
            // 
            this.labelCountViewsCap.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCountViewsCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCountViewsCap.Location = new System.Drawing.Point(468, 50);
            this.labelCountViewsCap.Name = "labelCountViewsCap";
            this.labelCountViewsCap.Size = new System.Drawing.Size(75, 18);
            this.labelCountViewsCap.TabIndex = 8;
            this.labelCountViewsCap.Text = "Views:";
            this.labelCountViewsCap.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCountViews
            // 
            this.labelCountViews.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelCountViews.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelCountViews.Location = new System.Drawing.Point(547, 50);
            this.labelCountViews.Name = "labelCountViews";
            this.labelCountViews.Size = new System.Drawing.Size(40, 18);
            this.labelCountViews.TabIndex = 9;
            this.labelCountViews.Text = "0";
            this.labelCountViews.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCountClassesCap
            // 
            this.labelCountClassesCap.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCountClassesCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCountClassesCap.Location = new System.Drawing.Point(620, 50);
            this.labelCountClassesCap.Name = "labelCountClassesCap";
            this.labelCountClassesCap.Size = new System.Drawing.Size(75, 18);
            this.labelCountClassesCap.TabIndex = 10;
            this.labelCountClassesCap.Text = "Classes:";
            this.labelCountClassesCap.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCountClasses
            // 
            this.labelCountClasses.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelCountClasses.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelCountClasses.Location = new System.Drawing.Point(699, 50);
            this.labelCountClasses.Name = "labelCountClasses";
            this.labelCountClasses.Size = new System.Drawing.Size(60, 18);
            this.labelCountClasses.TabIndex = 11;
            this.labelCountClasses.Text = "0";
            this.labelCountClasses.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupUsage
            // 
            this.groupUsage.Controls.Add(this.listUsage);
            this.groupUsage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.groupUsage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.groupUsage.Location = new System.Drawing.Point(14, 244);
            this.groupUsage.Name = "groupUsage";
            this.groupUsage.Size = new System.Drawing.Size(310, 280);
            this.groupUsage.TabIndex = 4;
            this.groupUsage.TabStop = false;
            this.groupUsage.Text = "Feature Usage";
            // 
            // listUsage
            // 
            this.listUsage.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colUsageFeature,
            this.colUsageCount,
            this.colUsagePct});
            this.listUsage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listUsage.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.listUsage.FullRowSelect = true;
            this.listUsage.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Clickable;
            this.listUsage.HideSelection = false;
            this.listUsage.Location = new System.Drawing.Point(3, 19);
            this.listUsage.MultiSelect = false;
            this.listUsage.Name = "listUsage";
            this.listUsage.Size = new System.Drawing.Size(304, 258);
            this.listUsage.TabIndex = 0;
            this.listUsage.UseCompatibleStateImageBehavior = false;
            this.listUsage.View = System.Windows.Forms.View.Details;
            // 
            // colUsageFeature
            // 
            this.colUsageFeature.Text = "Feature";
            this.colUsageFeature.Width = 175;
            // 
            // colUsageCount
            // 
            this.colUsageCount.Text = "Count";
            this.colUsageCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colUsageCount.Width = 55;
            // 
            // colUsagePct
            // 
            this.colUsagePct.Text = "%";
            this.colUsagePct.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colUsagePct.Width = 50;
            // 
            // groupBreakdown
            // 
            this.groupBreakdown.Controls.Add(this.tableBreakdown);
            this.groupBreakdown.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.groupBreakdown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.groupBreakdown.Location = new System.Drawing.Point(330, 244);
            this.groupBreakdown.Name = "groupBreakdown";
            this.groupBreakdown.Size = new System.Drawing.Size(456, 280);
            this.groupBreakdown.TabIndex = 5;
            this.groupBreakdown.TabStop = false;
            this.groupBreakdown.Text = "Timer Breakdown";
            // 
            // tableBreakdown
            // 
            this.tableBreakdown.ColumnCount = 2;
            this.tableBreakdown.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableBreakdown.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableBreakdown.Controls.Add(this.listByCategory, 0, 0);
            this.tableBreakdown.Controls.Add(this.listByStyle, 1, 0);
            this.tableBreakdown.Controls.Add(this.listByClass, 0, 1);
            this.tableBreakdown.Controls.Add(this.listByScope, 1, 1);
            this.tableBreakdown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableBreakdown.Location = new System.Drawing.Point(3, 19);
            this.tableBreakdown.Name = "tableBreakdown";
            this.tableBreakdown.Padding = new System.Windows.Forms.Padding(4, 16, 4, 4);
            this.tableBreakdown.RowCount = 2;
            this.tableBreakdown.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableBreakdown.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableBreakdown.Size = new System.Drawing.Size(450, 258);
            this.tableBreakdown.TabIndex = 0;
            // 
            // listByCategory
            // 
            this.listByCategory.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colCatName,
            this.colCatCount});
            this.listByCategory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listByCategory.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.listByCategory.FullRowSelect = true;
            this.listByCategory.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Clickable;
            this.listByCategory.HideSelection = false;
            this.listByCategory.Location = new System.Drawing.Point(6, 18);
            this.listByCategory.Margin = new System.Windows.Forms.Padding(2);
            this.listByCategory.MultiSelect = false;
            this.listByCategory.Name = "listByCategory";
            this.listByCategory.Size = new System.Drawing.Size(217, 115);
            this.listByCategory.TabIndex = 0;
            this.listByCategory.UseCompatibleStateImageBehavior = false;
            this.listByCategory.View = System.Windows.Forms.View.Details;
            this.listByCategory.SelectedIndexChanged += new System.EventHandler(this.listByCategory_SelectedIndexChanged);
            // 
            // colCatName
            // 
            this.colCatName.Text = "By Category";
            this.colCatName.Width = 140;
            // 
            // colCatCount
            // 
            this.colCatCount.Text = "#";
            this.colCatCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colCatCount.Width = 40;
            // 
            // listByStyle
            // 
            this.listByStyle.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colStyleName,
            this.colStyleCount});
            this.listByStyle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listByStyle.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.listByStyle.FullRowSelect = true;
            this.listByStyle.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Clickable;
            this.listByStyle.HideSelection = false;
            this.listByStyle.Location = new System.Drawing.Point(227, 18);
            this.listByStyle.Margin = new System.Windows.Forms.Padding(2);
            this.listByStyle.MultiSelect = false;
            this.listByStyle.Name = "listByStyle";
            this.listByStyle.Size = new System.Drawing.Size(217, 115);
            this.listByStyle.TabIndex = 1;
            this.listByStyle.UseCompatibleStateImageBehavior = false;
            this.listByStyle.View = System.Windows.Forms.View.Details;
            // 
            // colStyleName
            // 
            this.colStyleName.Text = "By Style";
            this.colStyleName.Width = 140;
            // 
            // colStyleCount
            // 
            this.colStyleCount.Text = "#";
            this.colStyleCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colStyleCount.Width = 40;
            // 
            // listByClass
            // 
            this.listByClass.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colClassName,
            this.colClassCount});
            this.listByClass.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listByClass.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.listByClass.FullRowSelect = true;
            this.listByClass.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Clickable;
            this.listByClass.HideSelection = false;
            this.listByClass.Location = new System.Drawing.Point(6, 137);
            this.listByClass.Margin = new System.Windows.Forms.Padding(2);
            this.listByClass.MultiSelect = false;
            this.listByClass.Name = "listByClass";
            this.listByClass.Size = new System.Drawing.Size(217, 115);
            this.listByClass.TabIndex = 2;
            this.listByClass.UseCompatibleStateImageBehavior = false;
            this.listByClass.View = System.Windows.Forms.View.Details;
            // 
            // colClassName
            // 
            this.colClassName.Text = "By Class";
            this.colClassName.Width = 140;
            // 
            // colClassCount
            // 
            this.colClassCount.Text = "#";
            this.colClassCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colClassCount.Width = 40;
            // 
            // listByScope
            // 
            this.listByScope.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colScopeName,
            this.colScopeCount});
            this.listByScope.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listByScope.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.listByScope.FullRowSelect = true;
            this.listByScope.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Clickable;
            this.listByScope.HideSelection = false;
            this.listByScope.Location = new System.Drawing.Point(227, 137);
            this.listByScope.Margin = new System.Windows.Forms.Padding(2);
            this.listByScope.MultiSelect = false;
            this.listByScope.Name = "listByScope";
            this.listByScope.Size = new System.Drawing.Size(217, 115);
            this.listByScope.TabIndex = 3;
            this.listByScope.UseCompatibleStateImageBehavior = false;
            this.listByScope.View = System.Windows.Forms.View.Details;
            // 
            // colScopeName
            // 
            this.colScopeName.Text = "By Scope";
            this.colScopeName.Width = 140;
            // 
            // colScopeCount
            // 
            this.colScopeCount.Text = "#";
            this.colScopeCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colScopeCount.Width = 40;
            // 
            // buttonOk
            // 
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.Location = new System.Drawing.Point(706, 534);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(80, 28);
            this.buttonOk.TabIndex = 6;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // FormTomeInfo
            // 
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 574);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.groupBreakdown);
            this.Controls.Add(this.groupUsage);
            this.Controls.Add(this.groupStats);
            this.Controls.Add(this.groupFile);
            this.Controls.Add(this.panelSeparator1);
            this.Controls.Add(this.panelHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormTomeInfo";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Tome Information";
            this.Load += new System.EventHandler(this.FormTomeInfo_Load);
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.groupFile.ResumeLayout(false);
            this.groupFile.PerformLayout();
            this.groupStats.ResumeLayout(false);
            this.groupStats.PerformLayout();
            this.groupUsage.ResumeLayout(false);
            this.groupBreakdown.ResumeLayout(false);
            this.tableBreakdown.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolTip toolTipPath;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label labelTomeName;
        private System.Windows.Forms.Label labelTomePath;
        private System.Windows.Forms.Panel panelSeparator1;

        private System.Windows.Forms.GroupBox groupFile;
        private System.Windows.Forms.Label labelFileSizeCaption;
        private System.Windows.Forms.Label labelFileSize;
        private System.Windows.Forms.Label labelCreatedCaption;
        private System.Windows.Forms.Label labelCreated;
        private System.Windows.Forms.Label labelModifiedCaption;
        private System.Windows.Forms.Label labelModified;
        private System.Windows.Forms.Label labelAppVersionCaption;
        private System.Windows.Forms.Label labelAppVersion;
        private System.Windows.Forms.Label labelTomeVersionCaption;
        private System.Windows.Forms.Label labelTomeVersion;
        private System.Windows.Forms.Label labelCreatedByCaption;
        private System.Windows.Forms.Label labelCreatedBy;

        private System.Windows.Forms.GroupBox groupStats;
        private System.Windows.Forms.Label labelTimersCaption;
        private System.Windows.Forms.Label labelTimersValue;
        private System.Windows.Forms.Label labelActiveTimersCaption;
        private System.Windows.Forms.Label labelActiveTimersValue;
        private System.Windows.Forms.Label labelRunningTimersCaption;
        private System.Windows.Forms.Label labelRunningTimersValue;
        private System.Windows.Forms.Label labelCountCharactersCap;
        private System.Windows.Forms.Label labelCountCharacters;
        private System.Windows.Forms.Label labelCountCategoriesCap;
        private System.Windows.Forms.Label labelCountCategories;
        private System.Windows.Forms.Label labelCountStylesCap;
        private System.Windows.Forms.Label labelCountStyles;
        private System.Windows.Forms.Label labelCountViewsCap;
        private System.Windows.Forms.Label labelCountViews;
        private System.Windows.Forms.Label labelCountClassesCap;
        private System.Windows.Forms.Label labelCountClasses;

        private System.Windows.Forms.GroupBox groupUsage;
        private System.Windows.Forms.ListView listUsage;
        private System.Windows.Forms.ColumnHeader colUsageFeature;
        private System.Windows.Forms.ColumnHeader colUsageCount;
        private System.Windows.Forms.ColumnHeader colUsagePct;

        private System.Windows.Forms.GroupBox groupBreakdown;
        private System.Windows.Forms.TableLayoutPanel tableBreakdown;
        private System.Windows.Forms.ListView listByCategory;
        private System.Windows.Forms.ColumnHeader colCatName;
        private System.Windows.Forms.ColumnHeader colCatCount;
        private System.Windows.Forms.ListView listByStyle;
        private System.Windows.Forms.ColumnHeader colStyleName;
        private System.Windows.Forms.ColumnHeader colStyleCount;
        private System.Windows.Forms.ListView listByClass;
        private System.Windows.Forms.ColumnHeader colClassName;
        private System.Windows.Forms.ColumnHeader colClassCount;
        private System.Windows.Forms.ListView listByScope;
        private System.Windows.Forms.ColumnHeader colScopeName;
        private System.Windows.Forms.ColumnHeader colScopeCount;

        private System.Windows.Forms.Button buttonOk;
    }
}
