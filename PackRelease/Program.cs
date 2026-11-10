using System.Diagnostics;
using PackRelease;

const string usage = """
    Usage: PackRelease <command> [options]

    Commands:
      build      Builds the CurseForge client zip and the server pack from a CurseForge instance
      publish    Uploads the zips of the version to CurseForge, the server pack as an additional file
      release    build, then publish (and deploy with --deploy)
      deploy     Updates the BisectHosting server: chat warning, stop, world archive, mirror of the
                 server pack folders through SFTP, start, then download of the archive

    Options:
      --instance <path>   Instance folder (default: current folder)
      --version <x.y.z>   Version (default: the first heading of changelog.md, which can't be "# Upcoming")
      --out <path>        Output folder (default: "exportDir" in packrelease.json)
      --force             build: overwrite existing zips of the same version
      --dry-run           publish/deploy: check everything and show what would change, without changing anything
      --allow-dirty       publish: allow uncommitted changes in the instance's git repository
      --deploy            release: deploy after publishing
    """;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine(usage);
    return 0;
}

try
{
    string command = args[0];
    Dictionary<string, string?> options = ParseOptions(args[1..]);
    Context context = Context.Create(options);

    switch (command)
    {
        case "build":
            await Build(context, options);
            break;
        case "publish":
            await Publish(context, options);
            break;
        case "release":
            CheckGit(context, options);
            await Build(context, options);
            await Publish(context, options);
            if (options.ContainsKey("deploy"))
                await Deploy(context, options);
            break;
        case "deploy":
            await Deploy(context, options);
            break;
        default:
            throw new PackException($"Unknown command '{command}'{Environment.NewLine}{usage}");
    }
    Log.Step("Done");
    return 0;
}
catch (PackException ex)
{
    Log.Error(ex.Message);
    return 1;
}

static async Task Build(Context context, Dictionary<string, string?> options)
{
    Builder builder = context.Builder;
    builder.CheckOutputs(options.ContainsKey("force"));
    builder.CheckAddons();
    builder.BuildClient();
    await builder.BuildServer();
}

static async Task Publish(Context context, Dictionary<string, string?> options)
{
    bool dryRun = options.ContainsKey("dry-run");
    CurseForgeConfig cf = context.Config.CurseForge ?? throw new PackException($"\"curseForge\" is missing in {PackConfig.FileName}");
    if (cf.ProjectId <= 0)
        throw new PackException($"\"curseForge.projectId\" is missing in {PackConfig.FileName}");
    if (cf.ReleaseType is not ("release" or "beta" or "alpha"))
        throw new PackException($"\"curseForge.releaseType\" must be release, beta or alpha, not '{cf.ReleaseType}'");

    // The changelog section is the file's changelog, so it must be the one of this version
    var (title, body) = Changelog.ReadLatest(context.Instance.Path);
    if (title != context.Version)
        throw new PackException($"The first heading of {Changelog.FileName} is \"# {title}\", not \"# {context.Version}\"");
    if (body.Length == 0)
        throw new PackException($"The \"# {title}\" section of {Changelog.FileName} is empty");
    string changelog = $"# {title}\n{body}";

    CheckGit(context, options);

    Builder builder = context.Builder;
    foreach (string zip in new[] { builder.ClientZip, builder.ServerZip })
        if (!File.Exists(zip))
            throw new PackException($"{zip} not found: run build first");

    string statePath = Path.Combine(context.OutputDir, $"{context.Config.Name}-{context.Version}.curseforge.json");
    PublishState state = PublishState.Load(statePath);

    using CurseForgeClient client = new(CurseForgeClient.ReadToken(cf.TokenVariable));
    List<int> gameVersions = await client.ResolveGameVersions(context.Instance.GameVersion, context.Instance.ModLoaderName);

    Log.Step($"CurseForge project {cf.ProjectId}{(dryRun ? " (dry run, nothing is uploaded)" : "")}");
    Log.Info($"Game versions: {context.Instance.GameVersion}, {context.Instance.ModLoaderName} (ids {string.Join(", ", gameVersions)}), {cf.ReleaseType}");
    Log.Info($"Changelog: {changelog.Split('\n').Length} lines, starting with \"{changelog.Split('\n')[0]}\"");

    if (state.ClientFileId is { } existingClient)
    {
        Log.Info($"Client zip already uploaded as file {existingClient} (from {Path.GetFileName(statePath)})");
    }
    else
    {
        Log.Info($"{(dryRun ? "Would upload" : "Uploading")} {Path.GetFileName(builder.ClientZip)} as \"{context.Version}\" ({SizeMb(builder.ClientZip)})");
        if (!dryRun)
        {
            state.ClientFileId = await client.UploadFile(cf.ProjectId, builder.ClientZip, new UploadMetadata
            {
                Changelog = changelog,
                DisplayName = context.Version,
                ReleaseType = cf.ReleaseType,
                GameVersions = gameVersions
            });
            state.Save(statePath);
            Log.Info($"Uploaded: file {state.ClientFileId}");
        }
    }

    if (state.ServerFileId is { } existingServer)
    {
        Log.Info($"Server pack already uploaded as file {existingServer} (from {Path.GetFileName(statePath)})");
    }
    else
    {
        Log.Info($"{(dryRun ? "Would upload" : "Uploading")} {Path.GetFileName(builder.ServerZip)} as an additional file ({SizeMb(builder.ServerZip)})");
        if (!dryRun)
        {
            state.ServerFileId = await client.UploadFile(cf.ProjectId, builder.ServerZip, new UploadMetadata
            {
                Changelog = changelog,
                ReleaseType = cf.ReleaseType,
                ParentFileId = state.ClientFileId
            });
            state.Save(statePath);
            Log.Info($"Uploaded: file {state.ServerFileId}");
        }
    }
}

static async Task Deploy(Context context, Dictionary<string, string?> options)
{
    BisectConfig bisect = context.Config.Bisect ?? throw new PackException($"\"bisect\" is missing in {PackConfig.FileName}");
    if (string.IsNullOrWhiteSpace(bisect.ServerId))
        throw new PackException($"\"bisect.serverId\" is missing in {PackConfig.FileName}");
    await new Deployer(context, bisect, options.ContainsKey("dry-run")).Run();
}

static void CheckGit(Context context, Dictionary<string, string?> options)
{
    if (options.ContainsKey("allow-dirty"))
        return;
    ProcessStartInfo info = new("git", ["-C", context.Instance.Path, "status", "--porcelain"])
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    using Process git = Process.Start(info) ?? throw new PackException("Can't run git");
    string output = git.StandardOutput.ReadToEnd();
    git.WaitForExit();
    if (git.ExitCode != 0)
        throw new PackException($"git status failed: {git.StandardError.ReadToEnd()}");
    if (output.Trim().Length > 0)
        throw new PackException($"Uncommitted changes in the instance (commit them or pass --allow-dirty):{Environment.NewLine}{output.TrimEnd()}");
}

static string SizeMb(string path) => $"{new FileInfo(path).Length / 1024.0 / 1024.0:0.0} MB";

static Dictionary<string, string?> ParseOptions(string[] args)
{
    string[] flags = ["force", "dry-run", "allow-dirty", "deploy"];
    Dictionary<string, string?> options = new();
    for (int i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--"))
            throw new PackException($"Unexpected argument '{args[i]}'");
        string name = args[i][2..];
        if (flags.Contains(name))
            options[name] = null;
        else if (i + 1 < args.Length)
            options[name] = args[++i];
        else
            throw new PackException($"Missing value for --{name}");
    }
    return options;
}

class Context
{
    public required PackConfig Config { get; init; }
    public required Instance Instance { get; init; }
    public required string Version { get; init; }
    public required string OutputDir { get; init; }
    public required Builder Builder { get; init; }

    public static Context Create(Dictionary<string, string?> options)
    {
        string instancePath = Path.GetFullPath(options.GetValueOrDefault("instance") ?? Environment.CurrentDirectory);
        PackConfig config = PackConfig.Load(instancePath);
        Instance instance = Instance.Load(instancePath);

        string version = options.GetValueOrDefault("version") ?? ReadVersion(instancePath);
        string outputDir = Path.GetFullPath(options.GetValueOrDefault("out") ?? Path.Combine(instancePath, config.ExportDir));
        Directory.CreateDirectory(outputDir);

        Log.Step($"{config.Name} {version} ({instance.GameVersion}, {instance.ModLoaderId})");
        return new Context
        {
            Config = config,
            Instance = instance,
            Version = version,
            OutputDir = outputDir,
            Builder = new Builder(instance, config, version, outputDir)
        };
    }

    private static string ReadVersion(string instancePath)
    {
        var (title, _) = Changelog.ReadLatest(instancePath);
        if (title.Equals(Changelog.Upcoming, StringComparison.OrdinalIgnoreCase))
            throw new PackException($"The first heading of {Changelog.FileName} is \"# {Changelog.Upcoming}\": rename it to the version or pass --version");
        return title;
    }
}
