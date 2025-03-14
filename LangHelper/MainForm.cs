using System.Text.Json;

namespace LangHelper;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void btnUploadOriginal_Click(object sender, EventArgs e)
    {
        DialogResult dialogResult = openFileDialog1.ShowDialog();
        if (dialogResult != DialogResult.OK) 
            return;
        string json = File.ReadAllText(openFileDialog1.FileName);
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) 
            return;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            dataGridView1.Rows.Add(property.Name, property.Value.ToString(), "");
        }
    }

    private void btnUploadTranslation_Click(object sender, EventArgs e)
    {
        DialogResult dialogResult = openFileDialog1.ShowDialog();
        if (dialogResult != DialogResult.OK) 
            return;
        string json = File.ReadAllText(openFileDialog1.FileName);
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) 
            return;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            // Try to find a row whose first cell matches the property name.
            var row = dataGridView1.Rows
                .Cast<DataGridViewRow>()
                .FirstOrDefault(r => r.Cells[0].Value?.ToString() == property.Name);

            if (row != null)
                // Found the row: update the value in the third cell.
                row.Cells[2].Value = property.Value.ToString();
            else
                // No matching row found: add a new row.
                dataGridView1.Rows.Add(property.Name, "", property.Value.ToString());
        }
    }
}