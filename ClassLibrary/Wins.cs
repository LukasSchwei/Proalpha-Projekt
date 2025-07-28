using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassLibrary.Coordinates;
using ClassLibrary.GlobalVariables;

namespace ClassLibrary.Wins;

public static class Wins
{
    private static readonly string SaveFilePath = "ClassLibrary/wins.json";

    public static void Save(int wins)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string jsonString = JsonSerializer.Serialize(wins, options);
        File.WriteAllText(SaveFilePath, jsonString);
    }
    public static int Read()
    {
        string jsonString = File.ReadAllText(SaveFilePath);
        return JsonSerializer.Deserialize<int>(jsonString);
    }
}
