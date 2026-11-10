namespace PackRelease;

public static class Log
{
    public static void Step(string message) => Write(ConsoleColor.Cyan, $"==> {message}");
    public static void Info(string message) => Console.WriteLine($"    {message}");
    public static void Warn(string message) => Write(ConsoleColor.Yellow, $"    WARNING: {message}");
    public static void Error(string message) => Write(ConsoleColor.Red, $"ERROR: {message}");

    private static void Write(ConsoleColor color, string message)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
