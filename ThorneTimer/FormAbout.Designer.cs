
namespace ThorneTimer
{
    partial class FormAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAbout));
            this.pictureIcon = new System.Windows.Forms.PictureBox();
            this.labelAppName = new System.Windows.Forms.Label();
            this.labelVersion = new System.Windows.Forms.Label();
            this.labelCopyright = new System.Windows.Forms.Label();
            this.panelHeader = new System.Windows.Forms.Panel();
            this.panelSeparator1 = new System.Windows.Forms.Panel();
            this.labelDescription = new System.Windows.Forms.Label();
            this.labelFeatures = new System.Windows.Forms.Label();
            this.panelSeparator2 = new System.Windows.Forms.Panel();
            this.labelFramework = new System.Windows.Forms.Label();
            this.labelDatabase = new System.Windows.Forms.Label();
            this.labelLicense = new System.Windows.Forms.Label();
            this.linkGitHub = new System.Windows.Forms.LinkLabel();
            this.panelSeparator3 = new System.Windows.Forms.Panel();
            this.labelTagline = new System.Windows.Forms.Label();
            this.buttonOk = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIcon)).BeginInit();
            this.panelHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.White;
            this.panelHeader.Controls.Add(this.pictureIcon);
            this.panelHeader.Controls.Add(this.labelAppName);
            this.panelHeader.Controls.Add(this.labelVersion);
            this.panelHeader.Controls.Add(this.labelCopyright);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(464, 88);
            this.panelHeader.TabIndex = 10;
            // 
            // pictureIcon
            // 
            this.pictureIcon.Image = global::ThorneTimer.Properties.Resources.ThorneTimer;
            this.pictureIcon.Location = new System.Drawing.Point(16, 14);
            this.pictureIcon.Name = "pictureIcon";
            this.pictureIcon.Size = new System.Drawing.Size(60, 60);
            this.pictureIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureIcon.TabIndex = 0;
            this.pictureIcon.TabStop = false;
            // 
            // labelAppName
            // 
            this.labelAppName.AutoSize = true;
            this.labelAppName.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.labelAppName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.labelAppName.Location = new System.Drawing.Point(86, 14);
            this.labelAppName.Name = "labelAppName";
            this.labelAppName.Size = new System.Drawing.Size(172, 30);
            this.labelAppName.TabIndex = 1;
            this.labelAppName.Text = "Thorne Timer";
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelVersion.Location = new System.Drawing.Point(89, 47);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(86, 15);
            this.labelVersion.TabIndex = 2;
            this.labelVersion.Text = "Version x.x.x.x";
            // 
            // labelCopyright
            // 
            this.labelCopyright.AutoSize = true;
            this.labelCopyright.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelCopyright.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.labelCopyright.Location = new System.Drawing.Point(89, 64);
            this.labelCopyright.Name = "labelCopyright";
            this.labelCopyright.Size = new System.Drawing.Size(135, 15);
            this.labelCopyright.TabIndex = 3;
            this.labelCopyright.Text = "Copyright \u00A9 2026";
            // 
            // panelSeparator1
            // 
            this.panelSeparator1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.panelSeparator1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSeparator1.Location = new System.Drawing.Point(0, 88);
            this.panelSeparator1.Name = "panelSeparator1";
            this.panelSeparator1.Size = new System.Drawing.Size(464, 1);
            this.panelSeparator1.TabIndex = 11;
            // 
            // labelDescription
            // 
            this.labelDescription.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.labelDescription.Location = new System.Drawing.Point(20, 98);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(424, 50);
            this.labelDescription.TabIndex = 12;
            this.labelDescription.Text = "Your EverQuest companion \u2014 real-time log parsing, overlay timers, and voice alerts" +
                " crafted for the way you play.";
            // 
            // labelFeatures
            // 
            this.labelFeatures.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelFeatures.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.labelFeatures.Location = new System.Drawing.Point(20, 148);
            this.labelFeatures.Name = "labelFeatures";
            this.labelFeatures.Size = new System.Drawing.Size(424, 100);
            this.labelFeatures.TabIndex = 13;
            this.labelFeatures.Text = "";
            // 
            // panelSeparator2
            // 
            this.panelSeparator2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.panelSeparator2.Location = new System.Drawing.Point(20, 254);
            this.panelSeparator2.Name = "panelSeparator2";
            this.panelSeparator2.Size = new System.Drawing.Size(424, 1);
            this.panelSeparator2.TabIndex = 14;
            // 
            // labelFramework
            // 
            this.labelFramework.AutoSize = true;
            this.labelFramework.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelFramework.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.labelFramework.Location = new System.Drawing.Point(20, 264);
            this.labelFramework.Name = "labelFramework";
            this.labelFramework.Size = new System.Drawing.Size(100, 13);
            this.labelFramework.TabIndex = 15;
            this.labelFramework.Text = "Framework:";
            // 
            // labelDatabase
            // 
            this.labelDatabase.AutoSize = true;
            this.labelDatabase.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelDatabase.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.labelDatabase.Location = new System.Drawing.Point(20, 282);
            this.labelDatabase.Name = "labelDatabase";
            this.labelDatabase.Size = new System.Drawing.Size(100, 13);
            this.labelDatabase.TabIndex = 16;
            this.labelDatabase.Text = "Database:";
            // 
            // labelLicense
            // 
            this.labelLicense.AutoSize = true;
            this.labelLicense.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelLicense.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.labelLicense.Location = new System.Drawing.Point(20, 300);
            this.labelLicense.Name = "labelLicense";
            this.labelLicense.Size = new System.Drawing.Size(100, 13);
            this.labelLicense.TabIndex = 17;
            this.labelLicense.Text = "License: MIT";
            // 
            // linkGitHub
            // 
            this.linkGitHub.AutoSize = true;
            this.linkGitHub.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.linkGitHub.Location = new System.Drawing.Point(20, 318);
            this.linkGitHub.Name = "linkGitHub";
            this.linkGitHub.Size = new System.Drawing.Size(200, 13);
            this.linkGitHub.TabIndex = 18;
            this.linkGitHub.TabStop = true;
            this.linkGitHub.Text = "github.com/draknarethorne/thorne-timer";
            this.linkGitHub.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkGitHub_LinkClicked);
            // 
            // panelSeparator3
            // 
            this.panelSeparator3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.panelSeparator3.Location = new System.Drawing.Point(20, 340);
            this.panelSeparator3.Name = "panelSeparator3";
            this.panelSeparator3.Size = new System.Drawing.Size(424, 1);
            this.panelSeparator3.TabIndex = 19;
            // 
            // labelTagline
            // 
            this.labelTagline.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic);
            this.labelTagline.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.labelTagline.Location = new System.Drawing.Point(20, 348);
            this.labelTagline.Name = "labelTagline";
            this.labelTagline.Size = new System.Drawing.Size(424, 30);
            this.labelTagline.TabIndex = 20;
            this.labelTagline.Text = "Built for the Project Quarm community by Draknar\u00E9 Thorne  \u2694\uFE0F See you in Norrath";
            this.labelTagline.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonOk
            // 
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.Location = new System.Drawing.Point(369, 386);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(80, 28);
            this.buttonOk.TabIndex = 4;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.ButtonOk_Click);
            // 
            // FormAbout
            // 
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 424);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.labelTagline);
            this.Controls.Add(this.panelSeparator3);
            this.Controls.Add(this.linkGitHub);
            this.Controls.Add(this.labelLicense);
            this.Controls.Add(this.labelDatabase);
            this.Controls.Add(this.labelFramework);
            this.Controls.Add(this.panelSeparator2);
            this.Controls.Add(this.labelFeatures);
            this.Controls.Add(this.labelDescription);
            this.Controls.Add(this.panelSeparator1);
            this.Controls.Add(this.panelHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormAbout";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "About Thorne Timer";
            this.Load += new System.EventHandler(this.FormAbout_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureIcon)).EndInit();
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.PictureBox pictureIcon;
        private System.Windows.Forms.Label labelAppName;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Label labelCopyright;
        private System.Windows.Forms.Panel panelSeparator1;
        private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.Label labelFeatures;
        private System.Windows.Forms.Panel panelSeparator2;
        private System.Windows.Forms.Label labelFramework;
        private System.Windows.Forms.Label labelDatabase;
        private System.Windows.Forms.Label labelLicense;
        private System.Windows.Forms.LinkLabel linkGitHub;
        private System.Windows.Forms.Panel panelSeparator3;
        private System.Windows.Forms.Label labelTagline;
        private System.Windows.Forms.Button buttonOk;
    }
}