using System.IO.Enumeration;
using System.Text.Json;

namespace PackRelease;

/// <summary>Non-secret settings, read from packrelease.json in the instance folder.</summary>
public class PackConfig
{
    public const string FileName = "packrelease.json";

    /// <summary>Modpack name, used in manifest.json and in the zip names.</summary>
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    /// <summary>Folder where the zips are written, relative to the instance folder.</summary>
    public string ExportDir { get; set; } = "../../Export";
    /// <summary>Instance folders copied in the client zip's overrides.</summary>
    public List<string> ClientOverrides { get; set; } = ["config", "datapacks", "resourcepacks"];
    /// <summary>Instance folders copied in the server pack.</summary>
    public List<string> ServerFolders { get; set; } = ["config", "datapacks", "defaultconfigs", "mods", "optional_datapacks", "scripts"];
    /// <summary>
    /// Patterns (* and ?) of files left out of both zips: matched against the file name, or against the path
    /// relative to the instance (with '/') when the pattern contains a '/', e.g. "config/spark/tmp-client/*".
    /// </summary>
    public List<string> Exclude { get; set; } = ["*.toml.bak"];
    /// <summary>File with the name prefixes of client-only mods, left out of the server pack.</summary>
    public string ClientModsFile { get; set; } = "clientmods.txt";
    public CurseForgeConfig? CurseForge { get; set; }
    public BisectConfig? Bisect { get; set; }

    public static PackConfig Load(string instancePath)
    {
        string path = Path.Combine(instancePath, FileName);
        if (!File.Exists(path))
            throw new PackException($"{FileName} not found in {instancePath}");
        var config = JsonSerializer.Deserialize<PackConfig>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }) ?? throw new PackException($"{FileName} is empty");
        if (string.IsNullOrWhiteSpace(config.Name))
            throw new PackException($"\"name\" is missing in {FileName}");
        return config;
    }

    /// <param name="relativePath">Path relative to the instance, with '/'</param>
    public bool IsExcluded(string relativePath) =>
        Exclude.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern,
            pattern.Contains('/') ? relativePath : Path.GetFileName(relativePath)));
}

public class PackException(string message) : Exception(message);
