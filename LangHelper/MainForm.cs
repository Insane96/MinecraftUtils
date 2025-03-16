using System.Collections;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepL;
using DeepL.Model;

namespace LangHelper;

public partial class MainForm : Form
{
    static Translator? DeepLTranslator;
    
    public MainForm()
    {
        InitializeComponent();
    }

    private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
        ColorRowBasedOnContent(dataGridView1.Rows[e.RowIndex]);
    }

    private void ColorRowBasedOnContent(DataGridViewRow row)
    {
        string? original = row.Cells["original"].Value?.ToString()?.Trim();
        string? translation = row.Cells["translation"].Value?.ToString()?.Trim();

        if (string.IsNullOrEmpty(original))
        {
            row.DefaultCellStyle.BackColor = Color.Moccasin; // Soft yellow
            row.DefaultCellStyle.ForeColor = Color.Black;
        }
        else if (string.IsNullOrEmpty(translation))
        {
            row.DefaultCellStyle.BackColor = Color.LightSalmon; // Soft red
            row.DefaultCellStyle.ForeColor = Color.Black;
        }
        else
        {
            row.DefaultCellStyle.BackColor = Color.White; // Reset to default
            row.DefaultCellStyle.ForeColor = Color.Black;
        }
    }
    
    private void ColorRowsBasedOnContent()
    {
        foreach (DataGridViewRow row in dataGridView1.Rows)
        {
            ColorRowBasedOnContent(row);
        }
    }

    private void ImportOriginalFromJson(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) 
            return;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            dataGridView1.Rows.Add(property.Name, property.Value.ToString(), "");
        }
        ColorRowsBasedOnContent();
    }

    private void ImportTranslationFromJson(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) 
            return;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            // Try to find a row whose first cell matches the property name.
            DataGridViewRow? row = dataGridView1.Rows
                .Cast<DataGridViewRow>()
                .FirstOrDefault(r => r.Cells[0].Value?.ToString() == property.Name);

            if (row != null)
                // Found the row: update the value in the third cell.
                row.Cells[2].Value = property.Value.ToString();
            else
                // No matching row found: add a new row.
                dataGridView1.Rows.Add(property.Name, "", property.Value.ToString());
        }
        ColorRowsBasedOnContent();
    }

    private void btnLoadOriginal_Click(object sender, EventArgs e)
    {
        ContextMenuStrip menu = new();
        
        ToolStripMenuItem fromFile = new("From File");
        fromFile.Click += delegate
        {
            DialogResult dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult != DialogResult.OK) 
                return;
            ImportOriginalFromJson(File.ReadAllText(openFileDialog1.FileName));
        };
        menu.Items.Add(fromFile);
        
        ToolStripMenuItem fromClipboard = new("From Clipboard");
        fromClipboard.Click += delegate
        {
            ImportOriginalFromJson(Clipboard.GetText());
        };
        menu.Items.Add(fromClipboard);
        
        ToolStripMenuItem fromUrl = new("From URL");
        fromUrl.Click += async (_, _) =>
        {
            string? json = await GetStringFromUrl(Clipboard.GetText());
            if (json == null)
            {
                MessageBox.Show("Failed to get load from URL");
                return;
            }
            ImportOriginalFromJson(json);
        };
        menu.Items.Add(fromUrl);
        
        menu.Show(Cursor.Position);
    }

    private void btnLoadTranslation_Click(object sender, EventArgs e)
    {
        ContextMenuStrip menu = new();
        
        ToolStripMenuItem fromFile = new("From File");
        fromFile.Click += delegate
        {
            DialogResult dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult != DialogResult.OK) 
                return;
            ImportTranslationFromJson(File.ReadAllText(openFileDialog1.FileName));
        };
        menu.Items.Add(fromFile);
        
        ToolStripMenuItem fromClipboard = new("From Clipboard");
        fromClipboard.Click += delegate
        {
            ImportTranslationFromJson(Clipboard.GetText());
        };
        menu.Items.Add(fromClipboard);
        
        ToolStripMenuItem fromUrl = new("From URL");
        fromUrl.Click += async delegate
        {
            string? json = await GetStringFromUrl(Clipboard.GetText());
            if (json == null)
            {
                MessageBox.Show("Failed to get load from URL");
                return;
            }
            ImportTranslationFromJson(json);
        };
        menu.Items.Add(fromUrl);
        
        menu.Show(Cursor.Position);
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
        string jsonString = jObject.ToJsonString(new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        SaveFileDialog saveFileDialog = new();
        saveFileDialog.Filter = "Json file|*.json";
        if (saveFileDialog.ShowDialog() != DialogResult.OK) 
            return;
        File.WriteAllText(saveFileDialog.FileName, jsonString);
    }
    
    private async void btnTranslate_Click(object sender, EventArgs e)
    {
        await TranslateParallelAsync(dataGridView1.Rows);
    }
    
    private async void btnTranslateSelectedRows_Click(object sender, EventArgs e)
    {
        await TranslateParallelAsync(dataGridView1.SelectedRows);
    }
    
    private async Task TranslateParallelAsync(IList rows)
    {
        if (GetTranslator() == null)
            return;
        List<Task> translationTasks = [];

        foreach (DataGridViewRow row in rows)
        {
            string? original = row.Cells["original"].Value?.ToString()?.Trim();
            string? translation = row.Cells["translation"].Value?.ToString()?.Trim();

            if (!string.IsNullOrEmpty(translation) || string.IsNullOrEmpty(original))
                continue;

            // Create a task for each translation
            Task task = Task.Run(async () =>
            {
                TextResult translatedText = await GetTranslator().TranslateTextAsync(original, "EN", "IT");

                // Update the DataGridView with the translation
                row.Cells["translation"].Value = translatedText.Text;
                Debug.WriteLine(translatedText.BilledCharacters);
                ColorRowBasedOnContent(row);
            });

            translationTasks.Add(task);
        }

        await Task.WhenAll(translationTasks); // Wait for all translations to complete
        MessageBox.Show("Translation completed!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    
    public static async Task<string?> GetStringFromUrl(string url)
    {
        try
        {
            using HttpClient client = new();
            return await client.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error fetching JSON: {ex.Message}");
        }
        return null;
    }
    
    private Translator? GetTranslator()
    {
        if (DeepLTranslator != null)
            return DeepLTranslator;
        
        string? apiKey = Environment.GetEnvironmentVariable("deepl_api_key");
        if (apiKey != null)
        {
            DeepLTranslator = new(apiKey);
            return DeepLTranslator;
        };
        MessageBox.Show("Please set the 'deepl_api_key' environment variable.");
        return DeepLTranslator;
    }
}