namespace ServerGenerator
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSelectModPackFolder = new System.Windows.Forms.Button();
            this.chkFolders = new System.Windows.Forms.CheckedListBox();
            this.btnGenerateServerZip = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.cmbGameVersions = new System.Windows.Forms.ComboBox();
            this.cmbForgeVersion = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // btnSelectModPackFolder
            // 
            this.btnSelectModPackFolder.Location = new System.Drawing.Point(12, 12);
            this.btnSelectModPackFolder.Name = "btnSelectModPackFolder";
            this.btnSelectModPackFolder.Size = new System.Drawing.Size(461, 23);
            this.btnSelectModPackFolder.TabIndex = 0;
            this.btnSelectModPackFolder.Text = "Select ModPack Folder";
            this.btnSelectModPackFolder.UseVisualStyleBackColor = true;
            this.btnSelectModPackFolder.Click += new System.EventHandler(this.btnSelectModPackFolder_Click);
            // 
            // chkFolders
            // 
            this.chkFolders.FormattingEnabled = true;
            this.chkFolders.Location = new System.Drawing.Point(12, 83);
            this.chkFolders.Name = "chkFolders";
            this.chkFolders.Size = new System.Drawing.Size(461, 310);
            this.chkFolders.TabIndex = 2;
            // 
            // btnGenerateServerZip
            // 
            this.btnGenerateServerZip.Location = new System.Drawing.Point(12, 556);
            this.btnGenerateServerZip.Name = "btnGenerateServerZip";
            this.btnGenerateServerZip.Size = new System.Drawing.Size(461, 23);
            this.btnGenerateServerZip.TabIndex = 3;
            this.btnGenerateServerZip.Text = "Generate Server Zip";
            this.btnGenerateServerZip.UseVisualStyleBackColor = true;
            this.btnGenerateServerZip.Click += new System.EventHandler(this.btnGenerateServerZip_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(12, 399);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(461, 151);
            this.txtLog.TabIndex = 4;
            // 
            // cmbGameVersions
            // 
            this.cmbGameVersions.FormattingEnabled = true;
            this.cmbGameVersions.Location = new System.Drawing.Point(12, 41);
            this.cmbGameVersions.Name = "cmbGameVersions";
            this.cmbGameVersions.Size = new System.Drawing.Size(220, 23);
            this.cmbGameVersions.TabIndex = 5;
            this.cmbGameVersions.SelectedIndexChanged += new System.EventHandler(this.cmbGameVersions_SelectedIndexChanged);
            // 
            // cmbForgeVersion
            // 
            this.cmbForgeVersion.FormattingEnabled = true;
            this.cmbForgeVersion.Location = new System.Drawing.Point(248, 41);
            this.cmbForgeVersion.Name = "cmbForgeVersion";
            this.cmbForgeVersion.Size = new System.Drawing.Size(225, 23);
            this.cmbForgeVersion.TabIndex = 6;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(485, 591);
            this.Controls.Add(this.cmbForgeVersion);
            this.Controls.Add(this.cmbGameVersions);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnGenerateServerZip);
            this.Controls.Add(this.chkFolders);
            this.Controls.Add(this.btnSelectModPackFolder);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "ModPack Server Generator";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button btnSelectModPackFolder;
        private CheckedListBox chkFolders;
        private Button btnGenerateServerZip;
        private TextBox txtLog;
        private ComboBox cmbGameVersions;
        private ComboBox cmbForgeVersion;
    }
}