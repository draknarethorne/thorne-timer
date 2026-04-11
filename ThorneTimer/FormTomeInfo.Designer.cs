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
            this.groupStats = new System.Windows.Forms.GroupBox();
            this.labelTimerCaption = new System.Windows.Forms.Label();
            this.labelTimerCount = new System.Windows.Forms.Label();
            this.labelActiveTimerCaption = new System.Windows.Forms.Label();
            this.labelActiveTimerCount = new System.Windows.Forms.Label();
            this.labelCharacterCaption = new System.Windows.Forms.Label();
            this.labelCharacterCount = new System.Windows.Forms.Label();
            this.labelCategoryCaption = new System.Windows.Forms.Label();
            this.labelCategoryCount = new System.Windows.Forms.Label();
            this.labelViewCaption = new System.Windows.Forms.Label();
            this.labelViewCount = new System.Windows.Forms.Label();
            this.labelClassCaption = new System.Windows.Forms.Label();
            this.labelClassCount = new System.Windows.Forms.Label();
            this.groupBreakdown = new System.Windows.Forms.GroupBox();
            this.labelStyleCaption = new System.Windows.Forms.Label();
            this.labelStyleBreakdown = new System.Windows.Forms.Label();
            this.labelScopeCaption = new System.Windows.Forms.Label();
            this.labelScopeBreakdown = new System.Windows.Forms.Label();
            this.buttonOk = new System.Windows.Forms.Button();
            this.panelHeader.SuspendLayout();
            this.groupFile.SuspendLayout();
            this.groupStats.SuspendLayout();
            this.groupBreakdown.SuspendLayout();
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
            this.panelHeader.Size = new System.Drawing.Size(424, 56);
            this.panelHeader.TabIndex = 0;
            // 
            // labelTomeName
            // 
            this.labelTomeName.AutoSize = true;
            this.labelTomeName.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.labelTomeName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelTomeName.Location = new System.Drawing.Point(14, 8);
            this.labelTomeName.Name = "labelTomeName";
            this.labelTomeName.Size = new System.Drawing.Size(120, 21);
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
            this.labelTomePath.Size = new System.Drawing.Size(396, 16);
            this.labelTomePath.TabIndex = 1;
            this.labelTomePath.Text = "C:\\path\\to\\tome";
            // 
            // panelSeparator1
            // 
            this.panelSeparator1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.panelSeparator1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSeparator1.Location = new System.Drawing.Point(0, 56);
            this.panelSeparator1.Name = "panelSeparator1";
            this.panelSeparator1.Size = new System.Drawing.Size(424, 1);
            this.panelSeparator1.TabIndex = 1;
            // 
            // groupFile — File Information
            // 
            this.groupFile.Controls.Add(this.labelFileSizeCaption);
            this.groupFile.Controls.Add(this.labelFileSize);
            this.groupFile.Controls.Add(this.labelCreatedCaption);
            this.groupFile.Controls.Add(this.labelCreated);
            this.groupFile.Controls.Add(this.labelModifiedCaption);
            this.groupFile.Controls.Add(this.labelModified);
            this.groupFile.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.groupFile.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.groupFile.Location = new System.Drawing.Point(14, 64);
            this.groupFile.Name = "groupFile";
            this.groupFile.Size = new System.Drawing.Size(396, 76);
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
            this.labelFileSizeCaption.Size = new System.Drawing.Size(55, 13);
            this.labelFileSizeCaption.TabIndex = 0;
            this.labelFileSizeCaption.Text = "File Size:";
            // 
            // labelFileSize
            // 
            this.labelFileSize.AutoSize = true;
            this.labelFileSize.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelFileSize.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelFileSize.Location = new System.Drawing.Point(100, 22);
            this.labelFileSize.Name = "labelFileSize";
            this.labelFileSize.Size = new System.Drawing.Size(20, 13);
            this.labelFileSize.TabIndex = 1;
            this.labelFileSize.Text = "—";
            // 
            // labelCreatedCaption
            // 
            this.labelCreatedCaption.AutoSize = true;
            this.labelCreatedCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCreatedCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCreatedCaption.Location = new System.Drawing.Point(12, 40);
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
            this.labelCreated.Location = new System.Drawing.Point(100, 40);
            this.labelCreated.Name = "labelCreated";
            this.labelCreated.Size = new System.Drawing.Size(20, 13);
            this.labelCreated.TabIndex = 3;
            this.labelCreated.Text = "—";
            // 
            // labelModifiedCaption
            // 
            this.labelModifiedCaption.AutoSize = true;
            this.labelModifiedCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelModifiedCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelModifiedCaption.Location = new System.Drawing.Point(12, 58);
            this.labelModifiedCaption.Name = "labelModifiedCaption";
            this.labelModifiedCaption.Size = new System.Drawing.Size(55, 13);
            this.labelModifiedCaption.TabIndex = 4;
            this.labelModifiedCaption.Text = "Modified:";
            // 
            // labelModified
            // 
            this.labelModified.AutoSize = true;
            this.labelModified.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelModified.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelModified.Location = new System.Drawing.Point(100, 58);
            this.labelModified.Name = "labelModified";
            this.labelModified.Size = new System.Drawing.Size(20, 13);
            this.labelModified.TabIndex = 5;
            this.labelModified.Text = "—";
            // 
            // groupStats — Tome Contents
            // 
            this.groupStats.Controls.Add(this.labelTimerCaption);
            this.groupStats.Controls.Add(this.labelTimerCount);
            this.groupStats.Controls.Add(this.labelActiveTimerCaption);
            this.groupStats.Controls.Add(this.labelActiveTimerCount);
            this.groupStats.Controls.Add(this.labelCharacterCaption);
            this.groupStats.Controls.Add(this.labelCharacterCount);
            this.groupStats.Controls.Add(this.labelCategoryCaption);
            this.groupStats.Controls.Add(this.labelCategoryCount);
            this.groupStats.Controls.Add(this.labelViewCaption);
            this.groupStats.Controls.Add(this.labelViewCount);
            this.groupStats.Controls.Add(this.labelClassCaption);
            this.groupStats.Controls.Add(this.labelClassCount);
            this.groupStats.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.groupStats.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.groupStats.Location = new System.Drawing.Point(14, 146);
            this.groupStats.Name = "groupStats";
            this.groupStats.Size = new System.Drawing.Size(396, 112);
            this.groupStats.TabIndex = 3;
            this.groupStats.TabStop = false;
            this.groupStats.Text = "Tome Contents";
            // 
            // labelTimerCaption
            // 
            this.labelTimerCaption.AutoSize = true;
            this.labelTimerCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelTimerCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelTimerCaption.Location = new System.Drawing.Point(12, 22);
            this.labelTimerCaption.Name = "labelTimerCaption";
            this.labelTimerCaption.Size = new System.Drawing.Size(45, 13);
            this.labelTimerCaption.TabIndex = 0;
            this.labelTimerCaption.Text = "Timers:";
            // 
            // labelTimerCount
            // 
            this.labelTimerCount.AutoSize = true;
            this.labelTimerCount.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelTimerCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelTimerCount.Location = new System.Drawing.Point(100, 22);
            this.labelTimerCount.Name = "labelTimerCount";
            this.labelTimerCount.Size = new System.Drawing.Size(14, 13);
            this.labelTimerCount.TabIndex = 1;
            this.labelTimerCount.Text = "0";
            // 
            // labelActiveTimerCaption
            // 
            this.labelActiveTimerCaption.AutoSize = true;
            this.labelActiveTimerCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelActiveTimerCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelActiveTimerCaption.Location = new System.Drawing.Point(12, 40);
            this.labelActiveTimerCaption.Name = "labelActiveTimerCaption";
            this.labelActiveTimerCaption.Size = new System.Drawing.Size(82, 13);
            this.labelActiveTimerCaption.TabIndex = 2;
            this.labelActiveTimerCaption.Text = "Active Timers:";
            // 
            // labelActiveTimerCount
            // 
            this.labelActiveTimerCount.AutoSize = true;
            this.labelActiveTimerCount.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelActiveTimerCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelActiveTimerCount.Location = new System.Drawing.Point(100, 40);
            this.labelActiveTimerCount.Name = "labelActiveTimerCount";
            this.labelActiveTimerCount.Size = new System.Drawing.Size(14, 13);
            this.labelActiveTimerCount.TabIndex = 3;
            this.labelActiveTimerCount.Text = "0";
            // 
            // labelCharacterCaption
            // 
            this.labelCharacterCaption.AutoSize = true;
            this.labelCharacterCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCharacterCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCharacterCaption.Location = new System.Drawing.Point(12, 58);
            this.labelCharacterCaption.Name = "labelCharacterCaption";
            this.labelCharacterCaption.Size = new System.Drawing.Size(68, 13);
            this.labelCharacterCaption.TabIndex = 4;
            this.labelCharacterCaption.Text = "Characters:";
            // 
            // labelCharacterCount
            // 
            this.labelCharacterCount.AutoSize = true;
            this.labelCharacterCount.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelCharacterCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelCharacterCount.Location = new System.Drawing.Point(100, 58);
            this.labelCharacterCount.Name = "labelCharacterCount";
            this.labelCharacterCount.Size = new System.Drawing.Size(14, 13);
            this.labelCharacterCount.TabIndex = 5;
            this.labelCharacterCount.Text = "0";
            // 
            // labelCategoryCaption
            // 
            this.labelCategoryCaption.AutoSize = true;
            this.labelCategoryCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelCategoryCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCategoryCaption.Location = new System.Drawing.Point(12, 76);
            this.labelCategoryCaption.Name = "labelCategoryCaption";
            this.labelCategoryCaption.Size = new System.Drawing.Size(65, 13);
            this.labelCategoryCaption.TabIndex = 6;
            this.labelCategoryCaption.Text = "Categories:";
            // 
            // labelCategoryCount
            // 
            this.labelCategoryCount.AutoSize = true;
            this.labelCategoryCount.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelCategoryCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelCategoryCount.Location = new System.Drawing.Point(100, 76);
            this.labelCategoryCount.Name = "labelCategoryCount";
            this.labelCategoryCount.Size = new System.Drawing.Size(14, 13);
            this.labelCategoryCount.TabIndex = 7;
            this.labelCategoryCount.Text = "0";
            // 
            // labelViewCaption
            // 
            this.labelViewCaption.AutoSize = true;
            this.labelViewCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelViewCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelViewCaption.Location = new System.Drawing.Point(200, 22);
            this.labelViewCaption.Name = "labelViewCaption";
            this.labelViewCaption.Size = new System.Drawing.Size(40, 13);
            this.labelViewCaption.TabIndex = 8;
            this.labelViewCaption.Text = "Views:";
            // 
            // labelViewCount
            // 
            this.labelViewCount.AutoSize = true;
            this.labelViewCount.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelViewCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelViewCount.Location = new System.Drawing.Point(280, 22);
            this.labelViewCount.Name = "labelViewCount";
            this.labelViewCount.Size = new System.Drawing.Size(14, 13);
            this.labelViewCount.TabIndex = 9;
            this.labelViewCount.Text = "0";
            // 
            // labelClassCaption
            // 
            this.labelClassCaption.AutoSize = true;
            this.labelClassCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelClassCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelClassCaption.Location = new System.Drawing.Point(200, 40);
            this.labelClassCaption.Name = "labelClassCaption";
            this.labelClassCaption.Size = new System.Drawing.Size(48, 13);
            this.labelClassCaption.TabIndex = 10;
            this.labelClassCaption.Text = "Classes:";
            // 
            // labelClassCount
            // 
            this.labelClassCount.AutoSize = true;
            this.labelClassCount.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelClassCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelClassCount.Location = new System.Drawing.Point(280, 40);
            this.labelClassCount.Name = "labelClassCount";
            this.labelClassCount.Size = new System.Drawing.Size(14, 13);
            this.labelClassCount.TabIndex = 11;
            this.labelClassCount.Text = "0";
            // 
            // groupBreakdown — Timer Breakdown
            // 
            this.groupBreakdown.Controls.Add(this.labelStyleCaption);
            this.groupBreakdown.Controls.Add(this.labelStyleBreakdown);
            this.groupBreakdown.Controls.Add(this.labelScopeCaption);
            this.groupBreakdown.Controls.Add(this.labelScopeBreakdown);
            this.groupBreakdown.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.groupBreakdown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.groupBreakdown.Location = new System.Drawing.Point(14, 264);
            this.groupBreakdown.Name = "groupBreakdown";
            this.groupBreakdown.Size = new System.Drawing.Size(396, 60);
            this.groupBreakdown.TabIndex = 4;
            this.groupBreakdown.TabStop = false;
            this.groupBreakdown.Text = "Timer Breakdown";
            // 
            // labelStyleCaption
            // 
            this.labelStyleCaption.AutoSize = true;
            this.labelStyleCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelStyleCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelStyleCaption.Location = new System.Drawing.Point(12, 20);
            this.labelStyleCaption.Name = "labelStyleCaption";
            this.labelStyleCaption.Size = new System.Drawing.Size(52, 13);
            this.labelStyleCaption.TabIndex = 0;
            this.labelStyleCaption.Text = "By Style:";
            // 
            // labelStyleBreakdown
            // 
            this.labelStyleBreakdown.AutoSize = true;
            this.labelStyleBreakdown.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelStyleBreakdown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelStyleBreakdown.Location = new System.Drawing.Point(100, 20);
            this.labelStyleBreakdown.Name = "labelStyleBreakdown";
            this.labelStyleBreakdown.Size = new System.Drawing.Size(20, 13);
            this.labelStyleBreakdown.TabIndex = 1;
            this.labelStyleBreakdown.Text = "—";
            // 
            // labelScopeCaption
            // 
            this.labelScopeCaption.AutoSize = true;
            this.labelScopeCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelScopeCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelScopeCaption.Location = new System.Drawing.Point(12, 38);
            this.labelScopeCaption.Name = "labelScopeCaption";
            this.labelScopeCaption.Size = new System.Drawing.Size(58, 13);
            this.labelScopeCaption.TabIndex = 2;
            this.labelScopeCaption.Text = "By Scope:";
            // 
            // labelScopeBreakdown
            // 
            this.labelScopeBreakdown.AutoSize = true;
            this.labelScopeBreakdown.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelScopeBreakdown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelScopeBreakdown.Location = new System.Drawing.Point(100, 38);
            this.labelScopeBreakdown.Name = "labelScopeBreakdown";
            this.labelScopeBreakdown.Size = new System.Drawing.Size(20, 13);
            this.labelScopeBreakdown.TabIndex = 3;
            this.labelScopeBreakdown.Text = "—";
            // 
            // buttonOk
            // 
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.Location = new System.Drawing.Point(330, 336);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(80, 28);
            this.buttonOk.TabIndex = 5;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // FormTomeInfo
            // 
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(424, 376);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.groupBreakdown);
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
            this.Text = "Tome Information";
            this.Load += new System.EventHandler(this.FormTomeInfo_Load);
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.groupFile.ResumeLayout(false);
            this.groupFile.PerformLayout();
            this.groupStats.ResumeLayout(false);
            this.groupStats.PerformLayout();
            this.groupBreakdown.ResumeLayout(false);
            this.groupBreakdown.PerformLayout();
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
        private System.Windows.Forms.GroupBox groupStats;
        private System.Windows.Forms.Label labelTimerCaption;
        private System.Windows.Forms.Label labelTimerCount;
        private System.Windows.Forms.Label labelActiveTimerCaption;
        private System.Windows.Forms.Label labelActiveTimerCount;
        private System.Windows.Forms.Label labelCharacterCaption;
        private System.Windows.Forms.Label labelCharacterCount;
        private System.Windows.Forms.Label labelCategoryCaption;
        private System.Windows.Forms.Label labelCategoryCount;
        private System.Windows.Forms.Label labelViewCaption;
        private System.Windows.Forms.Label labelViewCount;
        private System.Windows.Forms.Label labelClassCaption;
        private System.Windows.Forms.Label labelClassCount;
        private System.Windows.Forms.GroupBox groupBreakdown;
        private System.Windows.Forms.Label labelStyleCaption;
        private System.Windows.Forms.Label labelStyleBreakdown;
        private System.Windows.Forms.Label labelScopeCaption;
        private System.Windows.Forms.Label labelScopeBreakdown;
        private System.Windows.Forms.Button buttonOk;
    }
}
