using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace ServerGenerator;

public partial class MainForm : Form
{
    dynamic json;
    private string _forgeVersion = null;

    public MainForm()
    {
        InitializeComponent();
    }

    private readonly List<string> commonDirectories = new()
    {
        "config", "datapacks", "defaultconfigs", "global_packs", "mods", "scripts", "thingpacks",
    };

    private string? modPackPath;
    List<string> filesToIgnore = new();

    private void btnSelectModPackFolder_Click(object sender, EventArgs e)
    {
        FolderBrowserDialog folderBrowserDialog = new()
        {
            AutoUpgradeEnabled = true,
            UseDescriptionForTitle = true,
            Description = "Select the folder containing the ModPack"
        };

        if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            return;

        chkFolders.Items.Clear();
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

            if (Path.GetFileName(file).Equals("clientmods.txt"))
            {
                filesToIgnore = new List<string>(File.ReadAllLines(file));
            }

            if (Path.GetFileName(file).Equals("minecraftinstance.json"))
            {
                json = JObject.Parse(File.ReadAllText(file));
                _forgeVersion = Path.GetFileNameWithoutExtension(json["baseModLoader"]["filename"].ToObject<string>()).Replace("forge-", "");
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
        if (string.IsNullOrEmpty(modPackPath))
        {
            MessageBox.Show("No ModPack selected.");
            return;
        }

        this.Cursor = Cursors.WaitCursor;

        string tmpPath = Path.Combine(modPackPath, "../tmp");
        SaveFileDialog saveFileDialog = new()
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
            if (chkKeepTmpFolder.Checked)
                Copy(tmpPath, Path.Combine(Path.GetDirectoryName(saveFileDialog.FileName), Path.GetFileNameWithoutExtension(saveFileDialog.FileName)));
            Directory.Delete(tmpPath, true);
            txtLog.AppendText($"Zip created{Environment.NewLine}");
            this.Cursor = Cursors.Default;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private async Task DownloadForgeInstaller(string path)
    {
        HttpClient client = new();
        var httpResult = await client.GetAsync($"https://maven.minecraftforge.net/net/minecraftforge/forge/{_forgeVersion}/forge-{_forgeVersion}-installer.jar");
        if (!httpResult.IsSuccessStatusCode)
        {
            txtLog.AppendText($"Failed to download Forge installer{Environment.NewLine}");
            return;
        }

        using var resultStream = await httpResult.Content.ReadAsStreamAsync();
        using var fileStream = File.Create(Path.Combine(path, $"forge-{_forgeVersion}-installer.jar"));
        resultStream.CopyTo(fileStream);
    }

    //TY Stackoverflow
    public void Copy(string sourceDirectory, string targetDirectory)
    {
        DirectoryInfo diSource = new(sourceDirectory);
        DirectoryInfo diTarget = new(targetDirectory);

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
        DirectoryInfo diSource = new(sourceDirectory);
        DirectoryInfo diTarget = new(targetDirectory);

        CopyAllIgnoring(diSource, diTarget, toIgnore);
    }

    public void CopyAllIgnoring(DirectoryInfo source, DirectoryInfo target, List<string> toIgnore)
    {
        List<FileInfo> fileInfoToIgnore = new();
        foreach (string patternToIgnore in toIgnore)
        {
            foreach (FileInfo f in source.GetFiles($"{patternToIgnore}*"))
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