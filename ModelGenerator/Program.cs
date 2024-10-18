Console.Write("Path: ");
string? path = Console.ReadLine();

if (string.IsNullOrEmpty(path) || !File.Exists(path))
{
    Console.WriteLine("Failed to read path");
    return;
}

Console.WriteLine("Folder path: " + path);
string json = File.ReadAllText(path);

for (int i = 1; i <= 16; i++)
{
    string jsonPath = Path.Combine(Path.GetDirectoryName(path), $"scute_{i}.json");
    string newJson = json
        .Replace("#1#", (i - 1).ToString())
        .Replace("#2#", (i).ToString());

    Console.WriteLine(newJson);
    //File.WriteAllText(jsonPath, newJson);
}