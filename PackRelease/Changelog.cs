namespace PackRelease;

public static class Changelog
{
    public const string FileName = "changelog.md";
    public const string Upcoming = "Upcoming";

    /// <summary>Title and body of the first "# " section of changelog.md.</summary>
    public static (string Title, string Body) ReadLatest(string instancePath)
    {
        string path = Path.Combine(instancePath, FileName);
        if (!File.Exists(path))
            throw new PackException($"{FileName} not found in {instancePath}");

        string? title = null;
        List<string> body = [];
        foreach (string line in File.ReadLines(path))
        {
            if (line.StartsWith("# "))
            {
                if (title != null)
                    break;
                title = line[2..].Trim();
                continue;
            }
            if (title != null)
                body.Add(line);
        }

        if (title == null)
            throw new PackException($"No \"# <version>\" heading in {FileName}");
        return (title, string.Join("\n", body).Trim());
    }
}
