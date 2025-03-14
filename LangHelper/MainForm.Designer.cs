namespace LangHelper;

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
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        dataGridView1 = new System.Windows.Forms.DataGridView();
        key = new System.Windows.Forms.DataGridViewTextBoxColumn();
        original = new System.Windows.Forms.DataGridViewTextBoxColumn();
        translation = new System.Windows.Forms.DataGridViewTextBoxColumn();
        btnUploadOriginal = new System.Windows.Forms.Button();
        btnUploadTranslation = new System.Windows.Forms.Button();
        openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
        txtSelectedCell = new System.Windows.Forms.TextBox();
        btnSaveTranslation = new System.Windows.Forms.Button();
        ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
        SuspendLayout();
        // 
        // dataGridView1
        // 
        dataGridView1.AllowUserToAddRows = false;
        dataGridView1.AllowUserToDeleteRows = false;
        dataGridView1.AllowUserToOrderColumns = true;
        dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { key, original, translation });
        dataGridView1.Location = new System.Drawing.Point(12, 47);
        dataGridView1.Name = "dataGridView1";
        dataGridView1.Size = new System.Drawing.Size(1034, 612);
        dataGridView1.TabIndex = 0;
        dataGridView1.Text = "dataGridView1";
        // 
        // key
        // 
        key.HeaderText = "key";
        key.Name = "key";
        key.ReadOnly = true;
        // 
        // original
        // 
        original.HeaderText = "original";
        original.Name = "original";
        original.ReadOnly = true;
        // 
        // translation
        // 
        translation.HeaderText = "translation";
        translation.Name = "translation";
        // 
        // btnUploadOriginal
        // 
        btnUploadOriginal.Location = new System.Drawing.Point(12, 10);
        btnUploadOriginal.Name = "btnUploadOriginal";
        btnUploadOriginal.Size = new System.Drawing.Size(193, 31);
        btnUploadOriginal.TabIndex = 1;
        btnUploadOriginal.Text = "Open Original";
        btnUploadOriginal.UseVisualStyleBackColor = true;
        btnUploadOriginal.Click += btnUploadOriginal_Click;
        // 
        // btnUploadTranslation
        // 
        btnUploadTranslation.Location = new System.Drawing.Point(211, 10);
        btnUploadTranslation.Name = "btnUploadTranslation";
        btnUploadTranslation.Size = new System.Drawing.Size(193, 31);
        btnUploadTranslation.TabIndex = 2;
        btnUploadTranslation.Text = "Open Translation";
        btnUploadTranslation.UseVisualStyleBackColor = true;
        btnUploadTranslation.Click += btnUploadTranslation_Click;
        // 
        // openFileDialog1
        // 
        openFileDialog1.FileName = "openFileDialog";
        // 
        // txtSelectedCell
        // 
        txtSelectedCell.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        txtSelectedCell.Location = new System.Drawing.Point(12, 665);
        txtSelectedCell.Multiline = true;
        txtSelectedCell.Name = "txtSelectedCell";
        txtSelectedCell.Size = new System.Drawing.Size(1034, 57);
        txtSelectedCell.TabIndex = 3;
        // 
        // btnSaveTranslation
        // 
        btnSaveTranslation.Location = new System.Drawing.Point(853, 10);
        btnSaveTranslation.Name = "btnSaveTranslation";
        btnSaveTranslation.Size = new System.Drawing.Size(193, 31);
        btnSaveTranslation.TabIndex = 4;
        btnSaveTranslation.Text = "Save Translation";
        btnSaveTranslation.UseVisualStyleBackColor = true;
        // 
        // MainForm
        // 
        ClientSize = new System.Drawing.Size(1057, 730);
        Controls.Add(btnSaveTranslation);
        Controls.Add(txtSelectedCell);
        Controls.Add(btnUploadTranslation);
        Controls.Add(btnUploadOriginal);
        Controls.Add(dataGridView1);
        ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Button btnSaveTranslation;

    private System.Windows.Forms.TextBox txtSelectedCell;

    private System.Windows.Forms.OpenFileDialog openFileDialog1;

    private System.Windows.Forms.DataGridViewTextBoxColumn key;

    private System.Windows.Forms.Button btnUploadOriginal;
    private System.Windows.Forms.Button btnUploadTranslation;

    private System.Windows.Forms.DataGridViewTextBoxColumn original;
    private System.Windows.Forms.DataGridViewTextBoxColumn translation;

    private System.Windows.Forms.DataGridView dataGridView1;

    #endregion
}