namespace PackRelease;

/// <summary>Downloads the mod loader's server installer, cached like ServerGenerator does.</summary>
public static class LoaderInstaller
{
    private static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ServerGenerator", "installers");

    /// <summary>Copies the installer for <paramref name="modLoaderId"/> (e.g. "neoforge-21.1.265") to <paramref name="directory"/>.</summary>
    public static async Task<string> CopyTo(string modLoaderId, string directory)
    {
        var (loader, url) = Resolve(modLoaderId);
        string fileName = $"{modLoaderId}-installer.jar";
        string cacheDirectory = Path.Combine(CachePath, loader);
        string cachedFile = Path.Combine(cacheDirectory, fileName);

        if (File.Exists(cachedFile))
        {
            Log.Info($"{loader} installer taken from cache");
        }
        else
        {
            using HttpClient client = new();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                throw new PackException($"Failed to download the {loader} installer ({(int)response.StatusCode} {response.ReasonPhrase}): {url}");

            // Download to a temporary file first, so an interrupted download never ends up in the cache
            Directory.CreateDirectory(cacheDirectory);
            string partialFile = cachedFile + ".part";
            await using (var stream = await response.Content.ReadAsStreamAsync())
            await using (var file = File.Create(partialFile))
            {
                await stream.CopyToAsync(file);
            }
            File.Move(partialFile, cachedFile, true);
            Log.Info($"{loader} installer downloaded");
        }

        string destination = Path.Combine(directory, fileName);
        File.Copy(cachedFile, destination, true);
        return destination;
    }

    private static (string Loader, string Url) Resolve(string modLoaderId)
    {
        // "neoforge-" must be checked first, as "forge-" is a substring of it
        if (modLoaderId.StartsWith("neoforge-", StringComparison.OrdinalIgnoreCase))
        {
            string version = modLoaderId["neoforge-".Length..];
            return ("NeoForge", $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{version}/neoforge-{version}-installer.jar");
        }
        if (modLoaderId.StartsWith("forge-", StringComparison.OrdinalIgnoreCase))
        {
            string version = modLoaderId["forge-".Length..];
            return ("Forge", $"https://maven.minecraftforge.net/net/minecraftforge/forge/{version}/forge-{version}-installer.jar");
        }
        throw new PackException($"Unrecognized mod loader '{modLoaderId}' in minecraftinstance.json");
    }
}
