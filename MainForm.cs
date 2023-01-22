using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Net;
using System.Net.Http;

namespace ServerGenerator;

public partial class MainForm : Form
{

    dynamic json;
    public MainForm()
    {
        InitializeComponent();
        LoadForgeVersions();
    }

    public async void LoadForgeVersions()
    {
        HttpClient client = new();
        string result = await client.GetStringAsync("https://meta.modrinth.com/gamedata/forge/v0/manifest.json");
        json = JObject.Parse(result);

        foreach (JObject gameVersion in json.gameVersions)
        {
            string? value = gameVersion["id"].Value<string>();
            cmbGameVersions.Items.Add(value);
        }
    }

    private void cmbGameVersions_SelectedIndexChanged(object sender, EventArgs e)
    {
        cmbForgeVersion.Items.Clear();
        foreach (JObject gameVersion in json.gameVersions)
        {
            string? id = gameVersion["id"].Value<string>();
            if (id == cmbGameVersions.SelectedItem.ToString())
            {
                JArray? value = gameVersion["loaders"].Value<JArray>();
                foreach (var item in value)
                {
                    cmbForgeVersion.Items.Add(item["id"].Value<string>());
                }
                return;
            }
        }
    }

    private readonly List<string> commonDirectories = new()
        {
            "config", "defaultconfigs", "global_packs", "mods", "scripts", "thingpacks", 
        };

    private string modPackPath;
    List<string> filesToIgnore = new();
    private void btnSelectModPackFolder_Click(object sender, EventArgs e)
    {
        FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog()
        {
            AutoUpgradeEnabled = true,
            UseDescriptionForTitle = true,
            Description = "Select the folder containing the ModPack"
        };

        if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            return;

        modPackPath = folderBrowserDialog.SelectedPath;

        string[] folders = Directory.GetDirectories(modPackPath);
        foreach (string folder in folders)
        {
            bool chkd = commonDirectories.Any(s => Path.GetFileName(folder).Equals(s));
            chkFolders.Items.Add(Path.GetFileName(folder), chkd);
        }

        string[] files = Directory.GetFiles(modPackPath);
        foreach (string file in files)
        {
            bool chkd = commonDirectories.Any(s => Path.GetFileName(file).Equals(s));
            chkFolders.Items.Add(Path.GetFileName(file), chkd);

            if (Path.GetFileName(file).Equals("filesToIgnore.txt"))
            {
                filesToIgnore = new List<string>(File.ReadAllLines(file));
            }
        }

        txtLog.AppendText($"ModPack Folder selected. Common modpacks folders have been already checked{Environment.NewLine}");
        if (filesToIgnore.Count > 0)
        {
            string patternsToIgnore = "";
            foreach (string fileToIgnore in filesToIgnore)
            {
                patternsToIgnore += $"\"{fileToIgnore}\", ";
            }
            txtLog.AppendText($"Files with this pattern will be ignored: {patternsToIgnore}{Environment.NewLine}");
        }
    }

    private void btnGenerateServerZip_Click(object sender, EventArgs e)
    {
        if (cmbGameVersions.SelectedItem == null)
        {
            MessageBox.Show("No Forge version selected.");
            return;
        }
        if (string.IsNullOrEmpty(modPackPath))
        {
            MessageBox.Show("No ModPack selected.");
            return;
        }
        this.Cursor = Cursors.WaitCursor;

        string tmpPath = Path.Combine(modPackPath, "../tmp");
        SaveFileDialog saveFileDialog = new SaveFileDialog()
        {
            InitialDirectory = Path.Combine(modPackPath, ".."),
            Filter = "Zip File|*.zip"
        };
        if (saveFileDialog.ShowDialog() != DialogResult.OK)
        {
            txtLog.AppendText($"Generation cancelled{Environment.NewLine}");
            this.Cursor = Cursors.Default;
            return;
        }
        Directory.CreateDirectory(tmpPath);
        foreach (var item in chkFolders.CheckedItems)
        {
            CopyIgnoring(Path.Combine(modPackPath, item.ToString()), Path.Combine(tmpPath, item.ToString()), filesToIgnore);
        }

        Task t = DownloadForgeInstaller(tmpPath);
        t.ContinueWith(task =>
        {
            ZipFile.CreateFromDirectory(tmpPath, saveFileDialog.FileName);
            Directory.Delete(tmpPath, true);
            txtLog.AppendText($"Zip created{Environment.NewLine}");
            this.Cursor = Cursors.Default;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private async Task DownloadForgeInstaller(string path)
    {
        HttpClient client = new();
        var httpResult = await client.GetAsync($"https://maven.minecraftforge.net/net/minecraftforge/forge/{cmbForgeVersion.SelectedItem}/forge-{cmbForgeVersion.SelectedItem}-installer.jar");
        if (!httpResult.IsSuccessStatusCode)
        {
            txtLog.AppendText($"Failed to download Forge installer{Environment.NewLine}");
            return;
        }
        using var resultStream = await httpResult.Content.ReadAsStreamAsync();
        using var fileStream = File.Create(Path.Combine(path, $"forge-{cmbForgeVersion.SelectedItem}-installer.jar"));
        resultStream.CopyTo(fileStream);
    }

    //TY Stackoverflow
    public void Copy(string sourceDirectory, string targetDirectory)
    {
        DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
        DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    public void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

    public void CopyIgnoring(string sourceDirectory, string targetDirectory, List<string> toIgnore)
    {
        DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
        DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

        CopyAllIgnoring(diSource, diTarget, toIgnore);
    }

    public void CopyAllIgnoring(DirectoryInfo source, DirectoryInfo target, List<string> toIgnore)
    {
        List<FileInfo> fileInfoToIgnore = new List<FileInfo>();
        foreach (string patternToIgnore in toIgnore)
        {
            foreach (FileInfo f in source.GetFiles(patternToIgnore))
            {
                fileInfoToIgnore.Add(f);
            }
        }

        Directory.CreateDirectory(target.FullName);
        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            if (fileInfoToIgnore.Any(fileInfo => fi.FullName.Equals(fileInfo.FullName)))
            {
                txtLog.AppendText($"File '{fi.Name}' ignored{Environment.NewLine}");
                continue;
            }
            Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAllIgnoring(diSourceSubDir, nextTargetSubDir, toIgnore);
        }
    }
}