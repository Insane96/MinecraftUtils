using System.Text.Json.Nodes;

namespace PackRelease;

public record Addon(
    int ProjectId,
    int FileId,
    bool IsLocked,
    int PackageType,
    string Name,
    string Author,
    string WebsiteUrl,
    string FileName)
{
    public const int PackageTypeMod = 6;
    public const int PackageTypeResourcePack = 3;

    /// <summary>Path of the addon's file relative to the instance folder, null for unknown package types.</summary>
    public string? RelativePath => PackageType switch
    {
        PackageTypeMod => $"mods/{FileName}",
        PackageTypeResourcePack => $"resourcepacks/{FileName}",
        _ => null
    };
}

/// <summary>The parts of CurseForge's minecraftinstance.json used for the release.</summary>
public class Instance
{
    public required string Path { get; init; }
    public required string GameVersion { get; init; }
    /// <summary>e.g. "neoforge-21.1.265"</summary>
    public required string ModLoaderId { get; init; }
    /// <summary>The loader's name as CurseForge calls it, e.g. "NeoForge"</summary>
    public string ModLoaderName =>
        // "neoforge-" must be checked first, as "forge-" is a substring of it
        ModLoaderId.StartsWith("neoforge-", StringComparison.OrdinalIgnoreCase) ? "NeoForge"
        : ModLoaderId.StartsWith("forge-", StringComparison.OrdinalIgnoreCase) ? "Forge"
        : throw new PackException($"Unrecognized mod loader '{ModLoaderId}' in minecraftinstance.json");
    public required int AllocatedMemory { get; init; }
    public required List<Addon> Addons { get; init; }

    public static Instance Load(string instancePath)
    {
        string file = System.IO.Path.Combine(instancePath, "minecraftinstance.json");
        if (!File.Exists(file))
            throw new PackException($"minecraftinstance.json not found in {instancePath}");
        JsonNode root = JsonNode.Parse(File.ReadAllText(file))!;

        var addons = root["installedAddons"]!.AsArray().Select(a =>
        {
            JsonNode installed = a!["installedFile"]!;
            return new Addon(
                a["addonID"]!.GetValue<int>(),
                installed["id"]!.GetValue<int>(),
                a["isLocked"]?.GetValue<bool>() ?? false,
                a["packageType"]!.GetValue<int>(),
                a["name"]!.GetValue<string>(),
                a["primaryAuthor"]?.GetValue<string>() ?? "",
                a["webSiteURL"]?.GetValue<string>() ?? "",
                installed["fileNameOnDisk"]?.GetValue<string>() ?? installed["fileName"]!.GetValue<string>());
        }).ToList();

        return new Instance
        {
            Path = instancePath,
            GameVersion = root["gameVersion"]!.GetValue<string>(),
            ModLoaderId = root["baseModLoader"]!["name"]!.GetValue<string>(),
            AllocatedMemory = root["allocatedMemory"]!.GetValue<int>(),
            Addons = addons
        };
    }
}
