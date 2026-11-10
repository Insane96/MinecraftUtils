using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PackRelease;

/// <summary>Builds the CurseForge client zip and the server pack from the instance folder.</summary>
public class Builder(Instance instance, PackConfig config, string version, string outputDir)
{
    public string ClientZip => Path.Combine(outputDir, $"{config.Name}-{version}.zip");
    public string ServerDir => Path.Combine(outputDir, $"{config.Name}-{version}_ServerPack");
    public string ServerZip => ServerDir + ".zip";

    private string InstancePath => instance.Path;

    public void CheckOutputs(bool force)
    {
        string[] existing = new[] { ClientZip, ServerZip, ServerDir }.Where(p => File.Exists(p) || Directory.Exists(p)).ToArray();
        if (existing.Length > 0 && !force)
            throw new PackException($"Already exists (use --force to overwrite):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", existing)}");
    }

    public void CheckAddons()
    {
        HashSet<string> tracked = instance.Addons.Select(a => a.RelativePath).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        string modsDir = Path.Combine(InstancePath, "mods");
        if (Directory.Exists(modsDir))
        {
            foreach (string jar in Directory.GetFiles(modsDir).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal))
                if (!tracked.Contains($"mods/{jar}"))
                    Log.Warn($"mods/{jar} is not a CurseForge addon: it is in the server pack but not in the client zip");
        }

        foreach (Addon addon in instance.Addons)
            if (addon.RelativePath != null && !File.Exists(Path.Combine(InstancePath, addon.RelativePath)))
                Log.Warn($"{addon.Name}: {addon.RelativePath} is in minecraftinstance.json but not on disk");
    }

    public void BuildClient()
    {
        Log.Step($"Client zip: {ClientZip}");
        HashSet<string> addonFiles = instance.Addons.Select(a => a.RelativePath).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        WriteZip(ClientZip, zip =>
        {
            AddEntry(zip, "manifest.json", BuildManifest());
            AddEntry(zip, "modlist.html", BuildModList());

            int count = 0;
            foreach (string folder in config.ClientOverrides)
            {
                foreach (var (relative, full) in EnumerateFiles(folder))
                {
                    // CurseForge leaves out the files it downloads itself (e.g. resource packs)
                    if (addonFiles.Contains(relative) || config.IsExcluded(Path.GetFileName(relative)))
                        continue;
                    zip.CreateEntryFromFile(full, $"overrides/{relative}", CompressionLevel.Optimal);
                    count++;
                }
            }
            Log.Info($"{instance.Addons.Count} addons in the manifest, {count} files in overrides");
        });
    }

    public async Task BuildServer()
    {
        Log.Step($"Server pack: {ServerZip}");
        List<string> clientMods = ReadClientMods();

        if (Directory.Exists(ServerDir))
            Directory.Delete(ServerDir, true);
        Directory.CreateDirectory(ServerDir);

        int count = 0;
        List<string> ignored = [];
        foreach (string folder in config.ServerFolders)
        {
            foreach (var (relative, full) in EnumerateFiles(folder))
            {
                string name = Path.GetFileName(relative);
                if (config.IsExcluded(name))
                    continue;
                if (clientMods.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    ignored.Add(relative);
                    continue;
                }
                string destination = Path.Combine(ServerDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(full, destination);
                count++;
            }
        }
        Log.Info($"{count} files, {ignored.Count} client-only files left out ({string.Join(", ", ignored.GroupBy(f => f.Split('/')[0]).Select(g => $"{g.Key} {g.Count()}"))})");

        string installer = await LoaderInstaller.CopyTo(instance.ModLoaderId, ServerDir);
        Log.Info($"Installer: {Path.GetFileName(installer)}");

        WriteZip(ServerZip, zip =>
        {
            foreach (var (relative, full) in EnumerateFiles("", ServerDir))
                zip.CreateEntryFromFile(full, relative, CompressionLevel.Optimal);
        });
    }

    /// <summary>Same format as CurseForge's export: 2 spaces, LF, no BOM, no trailing newline.</summary>
    public byte[] BuildManifest()
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter w = new(stream, new JsonWriterOptions
               {
                   Indented = true,
                   NewLine = "\n",
                   Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
               }))
        {
            w.WriteStartObject();
            w.WriteStartObject("minecraft");
            w.WriteString("version", instance.GameVersion);
            w.WriteStartArray("modLoaders");
            w.WriteStartObject();
            w.WriteString("id", instance.ModLoaderId);
            w.WriteBoolean("primary", true);
            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteNumber("recommendedRam", instance.AllocatedMemory);
            w.WriteEndObject();
            w.WriteString("manifestType", "minecraftModpack");
            w.WriteNumber("manifestVersion", 1);
            w.WriteString("name", config.Name);
            w.WriteString("version", version);
            w.WriteString("author", config.Author);
            w.WriteString("overrides", "overrides");
            w.WriteStartArray("files");
            foreach (Addon addon in instance.Addons)
            {
                w.WriteStartObject();
                w.WriteNumber("projectID", addon.ProjectId);
                w.WriteNumber("fileID", addon.FileId);
                w.WriteBoolean("required", true);
                w.WriteBoolean("isLocked", addon.IsLocked);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        return stream.ToArray();
    }

    /// <summary>Same format as CurseForge's export: UTF-8 BOM, CRLF.</summary>
    public byte[] BuildModList()
    {
        StringBuilder sb = new();
        sb.Append("<ul>\r\n");
        foreach (Addon addon in instance.Addons)
            sb.Append($"<li><a href=\"{addon.WebsiteUrl}\">{addon.Name} (by {addon.Author})</a></li>\r\n");
        sb.Append("</ul>\r\n");
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(sb.ToString())];
    }

    private List<string> ReadClientMods()
    {
        string path = Path.Combine(InstancePath, config.ClientModsFile);
        if (!File.Exists(path))
        {
            Log.Warn($"{config.ClientModsFile} not found, no client-only mods left out of the server pack");
            return [];
        }
        // Skip empty lines and comments: an empty prefix would match every file
        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToList();
    }

    /// <summary>Files under <paramref name="folder"/> as (path relative to <paramref name="root"/> with '/', full path), sorted.</summary>
    private IEnumerable<(string Relative, string Full)> EnumerateFiles(string folder, string? root = null)
    {
        root ??= InstancePath;
        string directory = Path.Combine(root, folder);
        if (!Directory.Exists(directory))
            return [];
        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(full => (Path.GetRelativePath(root, full).Replace('\\', '/'), full))
            .OrderBy(f => f.Item1, StringComparer.Ordinal);
    }

    private static void AddEntry(ZipArchive zip, string name, byte[] content)
    {
        using Stream stream = zip.CreateEntry(name, CompressionLevel.Optimal).Open();
        stream.Write(content);
    }

    /// <summary>Writes to a .part file first, so a failed build never leaves a zip that looks complete.</summary>
    private static void WriteZip(string path, Action<ZipArchive> fill)
    {
        string partial = path + ".part";
        try
        {
            using (FileStream file = File.Create(partial))
            using (ZipArchive zip = new(file, ZipArchiveMode.Create))
            {
                fill(zip);
            }
            File.Move(partial, path, true);
            Log.Info($"Written {new FileInfo(path).Length / 1024.0 / 1024.0:0.0} MB");
        }
        finally
        {
            if (File.Exists(partial))
                File.Delete(partial);
        }
    }
}
