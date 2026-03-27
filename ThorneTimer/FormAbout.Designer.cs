
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
            this.buttonOk = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureIcon
            // 
            this.pictureIcon.Image = ((System.Drawing.Image)(resources.GetObject("pictureIcon.Image")));
            this.pictureIcon.Location = new System.Drawing.Point(12, 26);
            this.pictureIcon.Name = "pictureIcon";
            this.pictureIcon.Size = new System.Drawing.Size(56, 59);
            this.pictureIcon.TabIndex = 0;
            this.pictureIcon.TabStop = false;
            // 
            // labelAppName
            // 
            this.labelAppName.AutoSize = true;
            this.labelAppName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAppName.Location = new System.Drawing.Point(74, 26);
            this.labelAppName.Name = "labelAppName";
            this.labelAppName.Size = new System.Drawing.Size(59, 13);
            this.labelAppName.TabIndex = 1;
            this.labelAppName.Text = "EQ Timer";
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.Location = new System.Drawing.Point(74, 48);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(74, 13);
            this.labelVersion.TabIndex = 2;
            this.labelVersion.Text = "Version x.x.x.x";
            // 
            // labelCopyright
            // 
            this.labelCopyright.AutoSize = true;
            this.labelCopyright.Location = new System.Drawing.Point(74, 72);
            this.labelCopyright.Name = "labelCopyright";
            this.labelCopyright.Size = new System.Drawing.Size(180, 13);
            this.labelCopyright.TabIndex = 3;
            this.labelCopyright.Text = "Copyright 2020-2024 Stolistic Games";
            // 
            // buttonOk
            // 
            this.buttonOk.Location = new System.Drawing.Point(213, 103);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 4;
            this.buttonOk.Text = "Ok";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.ButtonOk_Click);
            // 
            // FormAbout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 138);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.labelCopyright);
            this.Controls.Add(this.labelVersion);
            this.Controls.Add(this.labelAppName);
            this.Controls.Add(this.pictureIcon);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormAbout";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "About";
            this.Load += new System.EventHandler(this.FormAbout_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureIcon;
        private System.Windows.Forms.Label labelAppName;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Label labelCopyright;
        private System.Windows.Forms.Button buttonOk;
    }
}