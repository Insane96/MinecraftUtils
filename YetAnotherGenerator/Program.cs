// See https://aka.ms/new-console-template for more information

using System.Text;

string recipe = """
                {
                    "type": "minecraft:item",
                    "weight": 1,
                    "name": "minecraft:enchanted_book",
                    "functions": [
                        {
                            "function": "minecraft:set_enchantments",
                            "enchantments": {
                                "#enchantment#": 1
                            }
                        }
                    ]
                }
                """;

string? path = Console.ReadLine();
if (path == null)
    return;
string[] lines = File.ReadAllLines(path);
StringBuilder output = new();
foreach (string line in lines)
{
    string book = new(recipe);
    book = book.Replace("#enchantment#", line);
    output.Append($"{book},{Environment.NewLine}");
    //File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), line.Replace(":", "_") + ".json"), output);
}
Console.WriteLine(output.ToString());
