// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

//TODO listing items is AND and not OR
const string advancement = """
                           {
                               "parent": "minecraft:recipes/root",
                               "criteria": {
                                   "has_item": {
                                       "trigger": "minecraft:inventory_changed",
                                       "conditions": {
                                           "items": [
                                                #list_of_items
                                           ]
                                       }
                                   },
                                   "has_the_recipe": {
                                       "trigger": "minecraft:recipe_unlocked",
                                       "conditions": {
                                           "recipe": "#recipe"
                                       }
                                   }
                               },
                               "requirements": [
                                   [
                                       "has_item",
                                       "has_the_recipe"
                                   ]
                               ],
                               "rewards": {
                                   "recipes": [
                                       "#recipe"
                                   ]
                               }
                           }
                           """;

Console.Write("Folder path: ");
string path = @"C:\Users\delvi\Desktop\Quark";//Console.ReadLine();
if (string.IsNullOrEmpty(path) || !Path.Exists(path))
{
    KeyPressAndClose("Failed to read path");
    return;
}

if (!Directory.GetDirectories(path).Contains(Path.Combine(path, "data")))
{
    KeyPressAndClose("Failed to get data folder");
    return;
}
path = Path.Combine(path, "data");

List<string> validTypes = ["minecraft:crafting_shaped", "minecraft:crafting_shapeless", "minecraft:smelting", "minecraft:blasting", "minecraft:smoking", "minecraft:stonecutting", "minecraft:campfire_cooking", "minecraft:smithing_transform", "quark:smithing_rune"];
//List<string> types = [];
foreach (string file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
{
    string[] splitPath = Path.GetRelativePath(path, file).Split(Path.DirectorySeparatorChar);
    if (!splitPath.Contains("recipes") || splitPath.Contains("advancements"))
    {
        //Console.WriteLine("Folder isn't in the recipes folder");
        continue;
    }
    string modId = splitPath[0];
    string recipePath = Path.GetRelativePath(Path.Combine(path, modId, "recipes"), file);
    string recipeId = $"{modId}:{Path.GetDirectoryName(recipePath)}\\{Path.GetFileNameWithoutExtension(file)}";
    Console.WriteLine(recipeId);
    JsonNode? recipe = JsonSerializer.Deserialize<JsonNode>(File.ReadAllText(file));
    if (recipe == null)
    {
        Console.WriteLine("Failed to deserialize");
        continue;
    }

    string? type = recipe["type"]?.GetValue<string>();
    if (type == null)
    {
        Console.WriteLine("Failed to get type");
        continue;
    }

    if (!validTypes.Contains(type))
    {
        Console.WriteLine("Invalid type");
        continue;
    }

    List<string> ingredientsList = [];
    switch (type)
    {
        case "minecraft:crafting_shaped":
            JsonNode? keys = recipe["key"];
            if (keys == null)
            {
                Console.WriteLine("Failed to get keys");
                continue;
            }
            foreach (KeyValuePair<string, JsonNode?> obj in keys.AsObject())
            {
                //Console.WriteLine($"\t{getItemOrTag(obj.Value)}");
                ingredientsList.Add(getItemOrTag(obj.Value));
            }
            break;
        case "minecraft:crafting_shapeless":
            JsonNode? ingredients = recipe["ingredients"];
            if (ingredients == null)
            {
                Console.WriteLine("Failed to get ingredients");
                continue;
            }
            foreach (JsonNode? jsonNode in ingredients.AsArray())
            {
                if (jsonNode == null)
                    continue;
                ingredientsList.Add(getItemOrTag(jsonNode));
            }
            break;
        case "minecraft:smelting":
        case "minecraft:smoking":
        case "minecraft:blasting":
        case "minecraft:stonecutting":
        case "minecraft:campfire_cooking":
            JsonNode? ingredient = recipe["ingredient"];
            if (ingredient == null)
            {
                Console.WriteLine("Failed to get ingredient");
                continue;
            }
            ingredientsList.Add(getItemOrTag(ingredient));
            break;
        case "minecraft:smithing_transform":
            JsonNode? baseNode = recipe["base"];
            if (baseNode == null)
            {
                Console.WriteLine("Failed to get base");
                continue;
            }
            ingredientsList.Add(getItemOrTag(baseNode));
            break;
        case "minecraft:smithing_rune":
            JsonNode? template = recipe["template"];
            if (template == null)
            {
                Console.WriteLine("Failed to get template");
                continue;
            }
            ingredientsList.Add(getItemOrTag(template));
            break;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(path, modId, "advancements", "recipes", $"{recipePath}")));
    StringBuilder ingredientsString = new();
    bool first = true;
    foreach (string ingr in ingredientsList)
    {
        if (!first)
            ingredientsString.Append(", ");
        if (ingr.StartsWith('#'))
            ingredientsString.Append($"{{\"tag\": \"{ingr.AsSpan(1, ingr.Length - 1)}\"}}");
        else 
            ingredientsString.Append($"{{\"items\": [\"{ingr}\"]}}");
        first = false;
    }
    //Directory.CreateDirectory(Path.Combine(path, modId, "advancements", "recipes"));
    File.WriteAllText(Path.Combine(path, modId, "advancements", "recipes", $"{recipePath}"), advancement.Replace("#list_of_items", ingredientsString.ToString()).Replace("#recipe", recipeId.Replace('\\', '/')));
    //if (!types.Contains(type))
    //    types.Add(type);
    //Console.WriteLine("\t" + type);
}
//types.ForEach(type => Console.WriteLine(type));

static string? getItemOrTag(JsonNode node)
{
    if (node is JsonObject)
    {
        if (node["item"] != null)
            return node["item"].ToString();
        if (node["tag"] != null)
            return $"#{node["tag"]}";
    }
    else if (node is JsonArray)
    {
        foreach (JsonNode? jsonNode2 in node.AsArray())
        {
            if (jsonNode2 == null)
                continue;
            if (jsonNode2["item"] != null)
                return jsonNode2["item"].ToString();
            if (jsonNode2["tag"] != null)
                return $"#{jsonNode2["tag"]}";
        }
    }

    return null;
}
static string? GetResult(JsonNode recipe)
{
    JsonNode? result = recipe["result"];
    if (result == null)
        return "Failed to get result";

    if (result is JsonObject)
        return getItemOrTag(result);
    else if (result is JsonValue)
        return result.GetValue<string>();

    return null;
}

static void KeyPressAndClose(string extraMessage = "")
{
    if (!string.IsNullOrEmpty(extraMessage))
        Console.WriteLine(extraMessage);
    Console.WriteLine("Press any key to exit");
    Environment.Exit(0);
}