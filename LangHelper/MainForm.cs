using System.Text.Json;
using System.Text.Json.Nodes;

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

    private void btnSaveTranslation_Click(object sender, EventArgs e)
    {
        JsonObject jObject = new();

        foreach (DataGridViewRow row in dataGridView1.Rows)
        {
            if (row.IsNewRow)
                continue;
            
            // Check if the "original" column (column 1) is empty.
            string original = row.Cells[1].Value?.ToString();
            if (string.IsNullOrWhiteSpace(original))
                continue;
            string translation = row.Cells[2].Value?.ToString();
            if (string.IsNullOrWhiteSpace(translation))
                continue;
            
            // Assuming column 0 holds the key and column 2 holds the translation.
            string key = row.Cells[0].Value?.ToString();
    
            if (!string.IsNullOrEmpty(key))
            {
                // Add the property with the key and translation value.
                jObject[key] = translation;
            }
        }

        // Serialize the JsonObject to a JSON string with indentation.
        string jsonString = jObject.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        SaveFileDialog saveFileDialog = new();
        saveFileDialog.Filter = "Json file|*.json";
        if (saveFileDialog.ShowDialog() != DialogResult.OK) 
            return;
        File.WriteAllText(saveFileDialog.FileName, jsonString);
    }
}