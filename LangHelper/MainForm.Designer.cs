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
        btnLoadOriginal = new System.Windows.Forms.Button();
        btnLoadTranslation = new System.Windows.Forms.Button();
        openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
        txtSelectedCell = new System.Windows.Forms.TextBox();
        btnSaveTranslation = new System.Windows.Forms.Button();
        btnTranslate = new System.Windows.Forms.Button();
        btnTranslateSelectedRows = new System.Windows.Forms.Button();
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
        dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
        {
            key, original, translation
        });
        dataGridView1.Location = new System.Drawing.Point(12, 47);
        dataGridView1.Name = "dataGridView1";
        dataGridView1.Size = new System.Drawing.Size(1034, 612);
        dataGridView1.TabIndex = 0;
        dataGridView1.Text = "dataGridView1";
        dataGridView1.CellEndEdit += dataGridView1_CellEndEdit;
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
        // btnLoadOriginal
        // 
        btnLoadOriginal.Location = new System.Drawing.Point(12, 10);
        btnLoadOriginal.Name = "btnLoadOriginal";
        btnLoadOriginal.Size = new System.Drawing.Size(193, 31);
        btnLoadOriginal.TabIndex = 1;
        btnLoadOriginal.Text = "Open Original";
        btnLoadOriginal.UseVisualStyleBackColor = true;
        btnLoadOriginal.Click += btnLoadOriginal_Click;
        // 
        // btnLoadTranslation
        // 
        btnLoadTranslation.Location = new System.Drawing.Point(211, 10);
        btnLoadTranslation.Name = "btnLoadTranslation";
        btnLoadTranslation.Size = new System.Drawing.Size(193, 31);
        btnLoadTranslation.TabIndex = 2;
        btnLoadTranslation.Text = "Open Translation";
        btnLoadTranslation.UseVisualStyleBackColor = true;
        btnLoadTranslation.Click += btnLoadTranslation_Click;
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
        btnSaveTranslation.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
        btnSaveTranslation.Location = new System.Drawing.Point(853, 10);
        btnSaveTranslation.Name = "btnSaveTranslation";
        btnSaveTranslation.Size = new System.Drawing.Size(193, 31);
        btnSaveTranslation.TabIndex = 4;
        btnSaveTranslation.Text = "Save Translation";
        btnSaveTranslation.UseVisualStyleBackColor = true;
        btnSaveTranslation.Click += btnSaveTranslation_Click;
        // 
        // btnTranslate
        // 
        btnTranslate.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
        btnTranslate.Location = new System.Drawing.Point(654, 10);
        btnTranslate.Name = "btnTranslate";
        btnTranslate.Size = new System.Drawing.Size(193, 31);
        btnTranslate.TabIndex = 4;
        btnTranslate.Text = "Translate Missing";
        btnTranslate.UseVisualStyleBackColor = true;
        btnTranslate.Click += btnTranslate_Click;
        // 
        // btnTranslateSelectedRows
        // 
        btnTranslateSelectedRows.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
        btnTranslateSelectedRows.Location = new System.Drawing.Point(455, 10);
        btnTranslateSelectedRows.Name = "btnTranslateSelectedRows";
        btnTranslateSelectedRows.Size = new System.Drawing.Size(193, 31);
        btnTranslateSelectedRows.TabIndex = 4;
        btnTranslateSelectedRows.Text = "Translate Selected Rows";
        btnTranslateSelectedRows.UseVisualStyleBackColor = true;
        btnTranslateSelectedRows.Click += btnTranslateSelectedRows_Click;
        // 
        // MainForm
        // 
        ClientSize = new System.Drawing.Size(1057, 730);
        Controls.Add(btnTranslateSelectedRows);
        Controls.Add(btnTranslate);
        Controls.Add(btnSaveTranslation);
        Controls.Add(txtSelectedCell);
        Controls.Add(btnLoadTranslation);
        Controls.Add(btnLoadOriginal);
        Controls.Add(dataGridView1);
        ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
    private System.Windows.Forms.Button btnTranslateSelectedRows;
    private System.Windows.Forms.Button btnTranslate;

    private System.Windows.Forms.Button btnSaveTranslation;

    private System.Windows.Forms.TextBox txtSelectedCell;

    private System.Windows.Forms.OpenFileDialog openFileDialog1;

    private System.Windows.Forms.DataGridViewTextBoxColumn key;

    private System.Windows.Forms.Button btnLoadOriginal;
    private System.Windows.Forms.Button btnLoadTranslation;

    private System.Windows.Forms.DataGridViewTextBoxColumn original;
    private System.Windows.Forms.DataGridViewTextBoxColumn translation;

    private System.Windows.Forms.DataGridView dataGridView1;

    #endregion
}