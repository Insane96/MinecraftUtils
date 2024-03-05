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
            this.chkKeepTmpFolder = new System.Windows.Forms.CheckBox();
            this.btnGenerateServerZip = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnSelectModPackFolder
            // 
            this.btnSelectModPackFolder.Location = new System.Drawing.Point(12, 12);
            this.btnSelectModPackFolder.Name = "btnSelectModPackFolder";
            this.btnSelectModPackFolder.Size = new System.Drawing.Size(461, 23);
            this.btnSelectModPackFolder.TabIndex = 0;
            this.btnSelectModPackFolder.Text = "Create unzipped folder";
            this.btnSelectModPackFolder.UseVisualStyleBackColor = true;
            this.btnSelectModPackFolder.Click += new System.EventHandler(this.btnSelectModPackFolder_Click);
            // 
            // chkFolders
            // 
            this.chkFolders.FormattingEnabled = true;
            this.chkFolders.Location = new System.Drawing.Point(12, 83);
            this.chkFolders.Name = "chkFolders";
            this.chkFolders.Size = new System.Drawing.Size(461, 290);
            this.chkFolders.TabIndex = 2;
            // 
            // chkKeepTmpFolder
            // 
            this.chkKeepTmpFolder.Location = new System.Drawing.Point(12, 370);
            this.chkKeepTmpFolder.Name = "chkKeepTmpFolder";
            this.chkKeepTmpFolder.Size = new System.Drawing.Size(461, 20);
            this.chkKeepTmpFolder.Text = "Keep folder";
            this.chkKeepTmpFolder.TabIndex = 3;
            // 
            // btnGenerateServerZip
            // 
            this.btnGenerateServerZip.Location = new System.Drawing.Point(12, 556);
            this.btnGenerateServerZip.Name = "btnGenerateServerZip";
            this.btnGenerateServerZip.Size = new System.Drawing.Size(461, 23);
            this.btnGenerateServerZip.TabIndex = 4;
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
            this.txtLog.TabIndex = 5;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(485, 591);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnGenerateServerZip);
            this.Controls.Add(this.chkFolders);
            this.Controls.Add(this.chkKeepTmpFolder);
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
        private CheckBox chkKeepTmpFolder;
        private Button btnGenerateServerZip;
        private TextBox txtLog;
    }
}