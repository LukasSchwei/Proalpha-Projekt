using System.ComponentModel.DataAnnotations;
using ClassLibrary.Coordinates;

namespace ClassLibrary.GlobalVariables;

public static class GV
{
    public static string Host { get; set; } = "ready2run.proalpha.cloud";
    public static string Port { get; set; } = "8080";
    public static string User { get; set; } = "lukasschwei";
    public static string HashedPassword { get; set; } = "e10adc3949ba59abbe56e057f20f883e";
    public static string GameId { get; set; } = "0";
    public static bool finished { get; set; } = false;
    public static string CurrentMap { get; set; } = MAP_1;
    public static int CurrentSkin { get; set; } = 1;
    public const string MAP_TEST = "m00_12345.map.json";
    public const string MAP_1 = "m01_xt13=1.map.json";
    public const string MAP_2 = "m02_z391e.map.json";
    public const string MAP_3 = "m03_87!sd6x.map.json";
    public const string MAP_4 = "m04.json";
}