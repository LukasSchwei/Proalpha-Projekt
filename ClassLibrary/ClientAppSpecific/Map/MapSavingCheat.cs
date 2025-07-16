using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassLibrary.Coordinates;
using ClassLibrary.GlobalVariables;

namespace ClassLibrary.MapSaver;

public class MapSavingCheat
{
    private static readonly string SaveFilePath = "ClassLibrary/ClientAppSpecific/Map/Saves/map_save_" + GV.CurrentMap + ".json";

    public static void SaveMap(Dictionary<(int x, int y), AbsoluteObject> map)
    {
        // Convert the dictionary with tuple keys to a serializable format
        var serializableMap = new Dictionary<string, AbsoluteObject>();
        foreach (var kvp in map)
        {
            string key = $"{kvp.Key.x},{kvp.Key.y}";
            serializableMap[key] = kvp.Value;
        }

        // Serialize to JSON with pretty printing
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string jsonString = JsonSerializer.Serialize(serializableMap, options);
        File.WriteAllText(SaveFilePath, jsonString);
    }

    public static Dictionary<(int x, int y), AbsoluteObject> ReadMap()
    {
        if (!File.Exists(SaveFilePath))
        {
            Console.WriteLine("No saved map file found.");
            return new Dictionary<(int x, int y), AbsoluteObject>();
        }

        string jsonString = File.ReadAllText(SaveFilePath);
        var serializableMap = JsonSerializer.Deserialize<Dictionary<string, AbsoluteObject>>(jsonString);

        // Convert back to dictionary with tuple keys
        var map = new Dictionary<(int x, int y), AbsoluteObject>();
        foreach (var kvp in serializableMap)
        {
            var coords = kvp.Key.Split(',');
            if (coords.Length == 2 &&
                int.TryParse(coords[0], out int x) &&
                int.TryParse(coords[1], out int y))
            {
                map[(x, y)] = kvp.Value;
            }
        }

        return map;
    }
}
