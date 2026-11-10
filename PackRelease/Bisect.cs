using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PackRelease;

public class BisectConfig
{
    /// <summary>Server identifier, the 8 characters in the panel's server URL.</summary>
    public string ServerId { get; set; } = "";
    public string PanelUrl { get; set; } = "https://games.bisecthosting.com";
    /// <summary>Environment variable with the panel API key (Account > API Credentials).</summary>
    public string ApiTokenVariable { get; set; } = "BISECT_HOSTING_API_TOKEN";
    /// <summary>Environment variable with the SFTP password (the panel password).</summary>
    public string SftpPasswordVariable { get; set; } = "BISECT_HOSTING_SFTP_PASSWORD";
    /// <summary>Server folders archived before the update, like the panel's Archive button.</summary>
    public List<string> BackupFolders { get; set; } = ["world"];
    /// <summary>Folder (relative to the instance) where the archive is downloaded after the update, null to keep it only on the server.</summary>
    public string? BackupDownloadDir { get; set; } = "../../Export/ServerBackups";
    /// <summary>Archives (archive-*.zip in the server root) kept after the update, null to keep all of them.</summary>
    public int? KeepArchives { get; set; } = 3;
    /// <summary>Seconds between the chat warning and the stop, when the server is running.</summary>
    public int WarningSeconds { get; set; } = 60;
}

/// <summary>BisectHosting's panel API (Pterodactyl client API).</summary>
public class BisectClient : IDisposable
{
    private readonly HttpClient http;
    private readonly string server;

    public BisectClient(string panelUrl, string serverId, string token)
    {
        server = $"/api/client/servers/{serverId}";
        // Archiving a big world can take a while
        http = new HttpClient { BaseAddress = new Uri(panelUrl), Timeout = TimeSpan.FromMinutes(30) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> GetUsername() => (await Send(HttpMethod.Get, "/api/client/account"))!["attributes"]!["username"]!.GetValue<string>();

    public async Task<(string Host, int Port)> GetSftp()
    {
        JsonNode sftp = (await Send(HttpMethod.Get, server))!["attributes"]!["sftp_details"]!;
        return (sftp["ip"]!.GetValue<string>(), sftp["port"]!.GetValue<int>());
    }

    /// <summary>offline, starting, running or stopping</summary>
    public async Task<string> GetState() => (await Send(HttpMethod.Get, $"{server}/resources"))!["attributes"]!["current_state"]!.GetValue<string>();

    public async Task<string?> GetStartupVariable(string name)
    {
        JsonNode startup = (await Send(HttpMethod.Get, $"{server}/startup"))!;
        return startup["data"]!.AsArray()
            .Select(v => v!["attributes"]!)
            .FirstOrDefault(a => a["env_variable"]!.GetValue<string>() == name)?["server_value"]?.GetValue<string>();
    }

    public Task SendCommand(string command) => Send(HttpMethod.Post, $"{server}/command", new { command });
    public Task SetPower(string signal) => Send(HttpMethod.Post, $"{server}/power", new { signal });

    /// <summary>Same as the file manager's Archive button: creates archive-&lt;date&gt;.zip in <paramref name="root"/>.</summary>
    public async Task<(string Name, long Size)> Compress(string root, List<string> files)
    {
        JsonNode file = (await Send(HttpMethod.Post, $"{server}/files/compress", new { root, files }))!["attributes"]!;
        return (file["name"]!.GetValue<string>(), file["size"]!.GetValue<long>());
    }

    /// <summary>Names of the files (not folders) in a server directory.</summary>
    public async Task<List<string>> ListFiles(string directory)
    {
        JsonNode list = (await Send(HttpMethod.Get, $"{server}/files/list?directory={Uri.EscapeDataString(directory)}"))!;
        return list["data"]!.AsArray()
            .Select(f => f!["attributes"]!)
            .Where(a => a["is_file"]!.GetValue<bool>())
            .Select(a => a["name"]!.GetValue<string>())
            .ToList();
    }

    public Task DeleteFiles(string root, List<string> files) => Send(HttpMethod.Post, $"{server}/files/delete", new { root, files });

    /// <summary>Downloads a server file through the panel's signed download URL.</summary>
    public async Task Download(string file, string destination)
    {
        string url = (await Send(HttpMethod.Get, $"{server}/files/download?file={Uri.EscapeDataString(file)}"))!["attributes"]!["url"]!.GetValue<string>();
        using HttpClient download = new() { Timeout = Timeout.InfiniteTimeSpan };
        using var response = await download.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
            throw new PackException($"Download of {file} failed ({(int)response.StatusCode} {response.ReasonPhrase})");

        // Download to a temporary file first, so an interrupted download never looks complete
        string partial = destination + ".part";
        await using (var stream = await response.Content.ReadAsStreamAsync())
        await using (var output = File.Create(partial))
        {
            await stream.CopyToAsync(output);
        }
        File.Move(partial, destination, true);
    }

    private async Task<JsonNode?> Send(HttpMethod method, string path, object? body = null)
    {
        using HttpRequestMessage request = new(method, path);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request);
        string text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new PackException($"Bisect {method} {path} failed ({(int)response.StatusCode} {response.ReasonPhrase}): {text}");
        return text.Length == 0 ? null : JsonNode.Parse(text);
    }

    public void Dispose() => http.Dispose();
}

/// <summary>Updates the server: warning, stop, world archive, SFTP mirror of the server pack folders, start.</summary>
internal class Deployer(Context context, BisectConfig bisect, bool dryRun)
{
    public async Task Run()
    {
        string serverDir = context.Builder.ServerDir;
        if (!Directory.Exists(serverDir))
            throw new PackException($"{serverDir} not found: run build first");
        // Only the folders in the server pack: a folder missing from it is left alone on the server
        List<string> folders = context.Config.ServerFolders.Where(f => Directory.Exists(Path.Combine(serverDir, f))).ToList();

        using BisectClient client = new(bisect.PanelUrl, bisect.ServerId, ReadSecret(bisect.ApiTokenVariable, "Bisect API key"));
        string sftpPassword = ReadSecret(bisect.SftpPasswordVariable, "Bisect SFTP password");
        var (host, port) = await client.GetSftp();
        string sftpUser = $"{await client.GetUsername()}.{bisect.ServerId}";
        string state = await client.GetState();
        Log.Step($"Bisect server {bisect.ServerId} ({state}), SFTP {sftpUser}@{host}:{port}{(dryRun ? " (dry run, nothing is changed)" : "")}");

        // The panel installs the loader: the pack can't update it, so it must already match
        string loaderVersion = context.Instance.ModLoaderId[(context.Instance.ModLoaderId.IndexOf('-') + 1)..];
        string? jar = await client.GetStartupVariable("SERVER_JARFILE");
        if (jar == null || !jar.Contains(loaderVersion))
            throw new PackException($"The server runs '{jar}', not {context.Instance.ModLoaderName} {loaderVersion}: update it from the panel first");
        Log.Info($"Loader: {jar}");

        Rclone rclone = new(host, port, sftpUser, sftpPassword);
        if (dryRun)
        {
            // Sizes only, so the preview lists the files that really change (the deploy re-uploads all files but mods)
            foreach (string folder in folders)
            {
                Log.Step($"Changes in {folder}/ (by size)");
                await rclone.Sync(Path.Combine(serverDir, folder), folder, ["--size-only", "--dry-run", "-v"]);
            }
            // The deploy creates one more archive before pruning
            await PruneArchives(client, 1);
            return;
        }

        if (state != "offline")
        {
            await Warn(client);
            Log.Step("Stopping the server");
            await client.SetPower("stop");
            await WaitForState(client, "offline", TimeSpan.FromMinutes(5));
        }

        Log.Step($"Archiving {string.Join(", ", bisect.BackupFolders)}");
        string archive;
        try
        {
            (archive, long size) = await client.Compress("/", bisect.BackupFolders);
            Log.Info($"{archive} ({size / 1024.0 / 1024.0:0} MB)");
        }
        catch (Exception ex)
        {
            Log.Error($"Archive failed, nothing was changed: starting the server again");
            await client.SetPower("start");
            throw new PackException($"Archive failed: {ex.Message}");
        }

        foreach (string folder in folders)
        {
            Log.Step($"Mirroring {folder}/");
            try
            {
                // Mods have the version in the file name, the other folders are small enough to upload again,
                // so the sync doesn't rely on SFTP keeping the modification times
                string[] flags = folder == "mods" ? ["--size-only", "-v"] : ["--ignore-times"];
                await rclone.Sync(Path.Combine(serverDir, folder), folder, flags);
            }
            catch (PackException ex)
            {
                throw new PackException($"{ex.Message}{Environment.NewLine}The server is left offline, as its files may be half updated: run deploy again");
            }
        }

        Log.Step("Starting the server");
        await client.SetPower("start");
        await WaitForState(client, "running", TimeSpan.FromMinutes(15));

        // After the start, so the download doesn't add to the downtime
        if (bisect.BackupDownloadDir != null)
        {
            string directory = Path.GetFullPath(Path.Combine(context.Instance.Path, bisect.BackupDownloadDir));
            Directory.CreateDirectory(directory);
            string destination = Path.Combine(directory, archive);
            Log.Step($"Downloading {archive} to {directory}");
            try
            {
                await client.Download(archive, destination);
                Log.Info($"Downloaded ({new FileInfo(destination).Length / 1024.0 / 1024.0:0} MB)");
            }
            catch (Exception ex) when (ex is PackException or HttpRequestException or IOException)
            {
                Log.Warn($"Download failed, the archive is still on the server: {ex.Message}");
            }
        }

        await PruneArchives(client, 0);
    }

    /// <summary>Deletes the oldest archives in the server root, keeping <see cref="BisectConfig.KeepArchives"/>.</summary>
    /// <param name="upcoming">Archives the deploy will still create, counted as already there</param>
    private async Task PruneArchives(BisectClient client, int upcoming)
    {
        if (bisect.KeepArchives is not { } keep)
            return;
        // The names have a sortable timestamp: archive-2026-11-09T133828Z.zip
        List<string> archives = (await client.ListFiles("/"))
            .Where(name => name.StartsWith("archive-") && name.EndsWith(".zip"))
            .OrderDescending(StringComparer.Ordinal)
            .ToList();
        List<string> old = archives.Skip(Math.Max(0, keep - upcoming)).ToList();
        if (old.Count == 0)
            return;

        Log.Step($"{(dryRun ? "Would delete" : "Deleting")} {old.Count} old archives, keeping the newest {keep}");
        foreach (string name in old)
            Log.Info(name);
        if (dryRun)
            return;
        try
        {
            await client.DeleteFiles("/", old);
        }
        catch (PackException ex)
        {
            Log.Warn($"Deleting the old archives failed: {ex.Message}");
        }
    }

    private async Task Warn(BisectClient client)
    {
        int remaining = bisect.WarningSeconds;
        foreach (int at in new[] { remaining, 30, 10 }.Where(s => s <= remaining).Distinct().OrderDescending())
        {
            await Task.Delay(TimeSpan.FromSeconds(remaining - at));
            remaining = at;
            string message = $"Server restarting in {at} seconds to update {context.Config.Name} to {context.Version}";
            await client.SendCommand($"say {message}");
            Log.Info($"say {message}");
        }
        await Task.Delay(TimeSpan.FromSeconds(remaining));
    }

    private static async Task WaitForState(BisectClient client, string wanted, TimeSpan timeout)
    {
        Stopwatch watch = Stopwatch.StartNew();
        string last = "";
        while (true)
        {
            string state = await client.GetState();
            if (state != last)
                Log.Info($"State: {state}");
            last = state;
            if (state == wanted)
                return;
            // Offline while waiting to run means the server stopped or crashed during startup
            if (wanted == "running" && state == "offline" && watch.Elapsed > TimeSpan.FromSeconds(20))
                throw new PackException("The server went offline while starting: check the console in the panel");
            if (watch.Elapsed > timeout)
                throw new PackException($"The server isn't {wanted} after {timeout.TotalMinutes:0} minutes (state: {state})");
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    private static string ReadSecret(string variable, string description)
    {
        string? value = Environment.GetEnvironmentVariable(variable)
                        ?? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User)
                        ?? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Machine);
        if (string.IsNullOrWhiteSpace(value))
            throw new PackException($"Environment variable {variable} with the {description} not found");
        return value.Trim();
    }
}

/// <summary>rclone sync to an SFTP remote, configured through environment variables so no secret is on the command line.</summary>
public class Rclone(string host, int port, string user, string password)
{
    private string? obscuredPassword;

    public async Task Sync(string localDir, string remoteDir, string[] flags)
    {
        obscuredPassword ??= await Obscure(password);
        string knownHosts = await EnsureHostKey();
        // --inplace: Pterodactyl's SFTP can't rename over an existing file, so rclone's default
        // upload to a .partial file and rename fails for every file that is replaced
        await Run(["sync", localDir, $":sftp:{remoteDir}", "--stats-one-line", "--stats", "0", "--inplace", .. flags], new()
        {
            ["RCLONE_SFTP_HOST"] = host,
            ["RCLONE_SFTP_PORT"] = port.ToString(),
            ["RCLONE_SFTP_USER"] = user,
            ["RCLONE_SFTP_PASS"] = obscuredPassword,
            // Pterodactyl's SFTP has no shell and doesn't need modification times (see the sync flags)
            ["RCLONE_SFTP_SHELL_TYPE"] = "none",
            ["RCLONE_SFTP_SET_MODTIME"] = "false",
            ["RCLONE_SFTP_KNOWN_HOSTS_FILE"] = knownHosts
        });
    }

    private static readonly string KnownHostsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PackRelease", "known_hosts");

    /// <summary>
    /// Trust on first use: the first connection to a host saves its key with ssh-keyscan, then rclone refuses
    /// to connect if the server presents a different key.
    /// </summary>
    private async Task<string> EnsureHostKey()
    {
        string entry = port == 22 ? host : $"[{host}]:{port}";
        if (File.Exists(KnownHostsPath) && File.ReadLines(KnownHostsPath).Any(line => line.StartsWith(entry + " ")))
            return KnownHostsPath;

        string keyscan = Path.Combine(Environment.SystemDirectory, "OpenSSH", "ssh-keyscan.exe");
        ProcessStartInfo info = new(File.Exists(keyscan) ? keyscan : "ssh-keyscan", ["-p", port.ToString(), host])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using Process process = Process.Start(info) ?? throw new PackException("Can't start ssh-keyscan");
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        List<string> keys = output.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith(entry + " ")).ToList();
        if (keys.Count == 0)
            throw new PackException($"ssh-keyscan returned no host key for {entry}: {await process.StandardError.ReadToEndAsync()}");

        Directory.CreateDirectory(Path.GetDirectoryName(KnownHostsPath)!);
        await File.AppendAllLinesAsync(KnownHostsPath, keys);
        Log.Info($"Saved the SFTP host key of {entry} to {KnownHostsPath}: {string.Join(", ", keys.Select(k => k.Split(' ')[1]))}");
        return KnownHostsPath;
    }

    private static async Task<string> Obscure(string password)
    {
        ProcessStartInfo info = new("rclone", ["obscure", "-"])
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using Process process = Start(info);
        await process.StandardInput.WriteAsync(password);
        process.StandardInput.Close();
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new PackException($"rclone obscure failed: {await process.StandardError.ReadToEndAsync()}");
        return output.Trim();
    }

    private static async Task Run(string[] args, Dictionary<string, string> environment)
    {
        ProcessStartInfo info = new("rclone", args);
        foreach (var (key, value) in environment)
            info.Environment[key] = value;
        using Process process = Start(info);
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new PackException($"rclone {args[0]} {args[2]} failed (exit code {process.ExitCode})");
    }

    private static Process Start(ProcessStartInfo info)
    {
        try
        {
            return Process.Start(info) ?? throw new PackException("Can't start rclone");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new PackException("rclone not found: install it (winget install Rclone.Rclone) or add it to the PATH");
        }
    }
}
