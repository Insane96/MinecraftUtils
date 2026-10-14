using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace ServerGenerator;

public enum ModLoader
{
    Forge,
    NeoForge
}

public partial class MainForm : Form
{
    dynamic json;
    private ModLoader? _modLoader = null;
    private string? _loaderVersion = null;

    public MainForm()
    {
        InitializeComponent();
    }

    private readonly List<string> commonDirectories = new()
    {
        "config", "datapacks", "defaultconfigs", "mods", "optional_datapacks", "scripts",
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
        filesToIgnore = new List<string>();
        _modLoader = null;
        _loaderVersion = null;

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
                // Skip empty lines and comments: an empty pattern would match every file
                filesToIgnore = File.ReadAllLines(file)
                    .Select(line => line.Trim())
                    .Where(line => line.Length > 0 && !line.StartsWith("#"))
                    .ToList();
            }

            if (Path.GetFileName(file).Equals("minecraftinstance.json"))
            {
                try
                {
                    json = JObject.Parse(File.ReadAllText(file));
                    string loaderFileName = Path.GetFileNameWithoutExtension(json["baseModLoader"]["filename"].ToObject<string>());
                    ParseModLoader(loaderFileName);
                    if (_modLoader == null)
                        ShowError($"Unrecognized mod loader '{loaderFileName}' in minecraftinstance.json");
                }
                catch (Exception ex)
                {
                    ShowError($"Failed to read the mod loader from minecraftinstance.json: {ex.Message}");
                }
            }
        }

        if (!File.Exists(Path.Combine(modPackPath, "minecraftinstance.json")))
            ShowError("minecraftinstance.json not found in the selected folder, can't detect the mod loader");
        else if (_modLoader != null)
            txtLog.AppendText($"Mod loader: {_modLoader} {_loaderVersion}{Environment.NewLine}");

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

        if (_modLoader == null || string.IsNullOrEmpty(_loaderVersion))
        {
            ShowError("Mod loader not detected, can't download the server installer.");
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

        try
        {
            // Leftovers from an interrupted run would end up in the zip
            if (Directory.Exists(tmpPath))
                Directory.Delete(tmpPath, true);
            Directory.CreateDirectory(tmpPath);
            foreach (var item in chkFolders.CheckedItems)
            {
                CopyIgnoring(Path.Combine(modPackPath, item.ToString()), Path.Combine(tmpPath, item.ToString()), filesToIgnore);
            }
        }
        catch (Exception ex)
        {
            txtLog.AppendText($"Failed to copy the ModPack files: {ex.Message}{Environment.NewLine}");
            DeleteTmpFolder(tmpPath);
            this.Cursor = Cursors.Default;
            return;
        }

        Task<bool> t = DownloadLoaderInstaller(tmpPath, _modLoader.Value, _loaderVersion);
        t.ContinueWith(task =>
        {
            try
            {
                if (!task.Result)
                {
                    txtLog.AppendText($"Zip not created{Environment.NewLine}");
                    return;
                }

                // CreateFromDirectory throws if the destination file already exists
                if (File.Exists(saveFileDialog.FileName))
                    File.Delete(saveFileDialog.FileName);
                ZipFile.CreateFromDirectory(tmpPath, saveFileDialog.FileName);
                if (chkKeepTmpFolder.Checked)
                    Copy(tmpPath, Path.Combine(Path.GetDirectoryName(saveFileDialog.FileName), Path.GetFileNameWithoutExtension(saveFileDialog.FileName)));
                txtLog.AppendText($"Zip created{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"Failed to create the zip: {ex.Message}{Environment.NewLine}");
            }
            finally
            {
                DeleteTmpFolder(tmpPath);
                this.Cursor = Cursors.Default;
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void ParseModLoader(string loaderFileName)
    {
        // "neoforge-" must be checked first, as "forge-" is a substring of it
        if (loaderFileName.StartsWith("neoforge-", StringComparison.OrdinalIgnoreCase))
        {
            _modLoader = ModLoader.NeoForge;
            _loaderVersion = loaderFileName.Substring("neoforge-".Length);
        }
        else if (loaderFileName.StartsWith("forge-", StringComparison.OrdinalIgnoreCase))
        {
            _modLoader = ModLoader.Forge;
            _loaderVersion = loaderFileName.Substring("forge-".Length);
        }
    }

    private static string GetInstallerFileName(ModLoader modLoader, string version)
    {
        return modLoader switch
        {
            ModLoader.NeoForge => $"neoforge-{version}-installer.jar",
            _ => $"forge-{version}-installer.jar"
        };
    }

    private static string GetInstallerUrl(ModLoader modLoader, string version)
    {
        return modLoader switch
        {
            ModLoader.NeoForge => $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{version}/{GetInstallerFileName(modLoader, version)}",
            _ => $"https://maven.minecraftforge.net/net/minecraftforge/forge/{version}/{GetInstallerFileName(modLoader, version)}"
        };
    }

    private async Task<bool> DownloadLoaderInstaller(string path, ModLoader modLoader, string version)
    {
        string url = GetInstallerUrl(modLoader, version);
        try
        {
            using HttpClient client = new();
            using var httpResult = await client.GetAsync(url);
            if (!httpResult.IsSuccessStatusCode)
            {
                txtLog.AppendText($"Failed to download {modLoader} installer ({(int)httpResult.StatusCode} {httpResult.ReasonPhrase}): {url}{Environment.NewLine}");
                return false;
            }

            using var resultStream = await httpResult.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(Path.Combine(path, GetInstallerFileName(modLoader, version)));
            await resultStream.CopyToAsync(fileStream);
            txtLog.AppendText($"{modLoader} installer downloaded{Environment.NewLine}");
            return true;
        }
        catch (Exception ex)
        {
            txtLog.AppendText($"Failed to download {modLoader} installer: {ex.Message}{Environment.NewLine}");
            return false;
        }
    }

    private void DeleteTmpFolder(string tmpPath)
    {
        try
        {
            if (Directory.Exists(tmpPath))
                Directory.Delete(tmpPath, true);
        }
        catch (Exception ex)
        {
            txtLog.AppendText($"Failed to delete the tmp folder '{tmpPath}': {ex.Message}{Environment.NewLine}");
        }
    }

    private void ShowError(string message)
    {
        txtLog.AppendText($"Error: {message}{Environment.NewLine}");
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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