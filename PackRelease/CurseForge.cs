using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PackRelease;

public class CurseForgeConfig
{
    public int ProjectId { get; set; }
    /// <summary>"release", "beta" or "alpha"</summary>
    public string ReleaseType { get; set; } = "release";
    /// <summary>Environment variable holding the API token (authors.curseforge.com > API Tokens).</summary>
    public string TokenVariable { get; set; } = "CF_API_TOKEN";
}

/// <summary>File IDs of a version already uploaded, so a failed or repeated publish doesn't upload twice.</summary>
public class PublishState
{
    public int? ClientFileId { get; set; }
    public int? ServerFileId { get; set; }

    public static PublishState Load(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<PublishState>(File.ReadAllText(path)) ?? new() : new();

    public void Save(string path) =>
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
}

/// <summary>CurseForge Upload API (https://support.curseforge.com/en/support/solutions/articles/9000197321).</summary>
public class CurseForgeClient : IDisposable
{
    private const string BaseUrl = "https://minecraft.curseforge.com";
    private readonly HttpClient http;

    public CurseForgeClient(string token)
    {
        http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromMinutes(30) };
        http.DefaultRequestHeaders.Add("X-Api-Token", token);
    }

    public static string ReadToken(string variable)
    {
        // Machine and user variables are also read from the registry, as processes started before they were set don't see them
        string? token = Environment.GetEnvironmentVariable(variable)
                        ?? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User)
                        ?? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Machine);
        if (string.IsNullOrWhiteSpace(token))
            throw new PackException($"Environment variable {variable} with the CurseForge API token not found");
        return token.Trim();
    }

    /// <summary>IDs of the Minecraft version and of the mod loader, e.g. "1.21.1" and "NeoForge".</summary>
    public async Task<List<int>> ResolveGameVersions(string minecraftVersion, string modLoader)
    {
        JsonArray types = await GetArray("/api/game/version-types");
        JsonArray versions = await GetArray("/api/game/versions");
        Dictionary<int, string> typeNames = types.ToDictionary(t => t!["id"]!.GetValue<int>(), t => t!["name"]!.GetValue<string>());

        int Find(string name, Func<string, bool> typeFilter, string description)
        {
            var matches = versions
                .Where(v => v!["name"]!.GetValue<string>().Equals(name, StringComparison.OrdinalIgnoreCase)
                            && typeFilter(typeNames.GetValueOrDefault(v["gameVersionTypeID"]!.GetValue<int>(), "")))
                .Select(v => v!["id"]!.GetValue<int>()).ToList();
            if (matches.Count != 1)
                throw new PackException($"Expected one CurseForge game version for {description} '{name}', found {matches.Count}");
            return matches[0];
        }

        // "1.21.1" exists for several version types (e.g. Addons): the Minecraft one is in "Minecraft 1.21"
        return
        [
            Find(minecraftVersion, t => t.StartsWith("Minecraft ", StringComparison.OrdinalIgnoreCase), "Minecraft"),
            Find(modLoader, t => t.Equals("Modloader", StringComparison.OrdinalIgnoreCase), "mod loader")
        ];
    }

    public async Task<int> UploadFile(int projectId, string filePath, UploadMetadata metadata)
    {
        string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        using MultipartFormDataContent content = new();
        content.Add(new StringContent(json, Encoding.UTF8, "application/json"), "metadata");
        StreamContent file = new(File.OpenRead(filePath));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(file, "file", Path.GetFileName(filePath));

        using var response = await http.PostAsync($"/api/projects/{projectId}/upload-file", content);
        string body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new PackException($"CurseForge upload of {Path.GetFileName(filePath)} failed ({(int)response.StatusCode} {response.ReasonPhrase}): {body}");
        return JsonNode.Parse(body)?["id"]?.GetValue<int>()
               ?? throw new PackException($"CurseForge upload: no file id in the response: {body}");
    }

    private async Task<JsonArray> GetArray(string path)
    {
        using var response = await http.GetAsync(path);
        string body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new PackException($"CurseForge {path} failed ({(int)response.StatusCode} {response.ReasonPhrase}): {body}");
        return JsonNode.Parse(body)!.AsArray();
    }

    public void Dispose() => http.Dispose();
}

public class UploadMetadata
{
    [JsonPropertyName("changelog")] public string Changelog { get; set; } = "";
    [JsonPropertyName("changelogType")] public string ChangelogType { get; set; } = "markdown";
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("releaseType")] public string ReleaseType { get; set; } = "release";
    /// <summary>Not allowed together with <see cref="ParentFileId"/>: additional files take the parent's versions.</summary>
    [JsonPropertyName("gameVersions")] public List<int>? GameVersions { get; set; }
    [JsonPropertyName("parentFileID")] public int? ParentFileId { get; set; }
}
