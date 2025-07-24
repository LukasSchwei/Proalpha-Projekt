using System;
using System.Reflection.Metadata;
using ClassLibrary.Coordinates;

namespace ClassLibrary.Variables;

public static class V
{
    ///////////////
    ///////////////Drawing Variables/////////////
    ///////////////
    public static int baseCellSize = 50;

    public static float offsetX;
    public static float offsetY;

    public static Dictionary<(int x, int y), AbsoluteObject> map = new();
    public static CurrentPosition currentPosition;
    public static CurrentPosition? lastValidPosition;

    ///////////////
    ///////////////Game Variables/////////////
    ///////////////

    public static int globalMapMinX = -999;
    public static int globalMapMaxX = 999;
    public static int globalMapMinY = -999;
    public static int globalMapMaxY = 999;

    public static bool isMoving = false;
    public static bool isAlgorithmRunning = false;

    public static int actioncounter;

    ///////////////
    ///////////////Zoom Variables/////////////
    ///////////////

    public static float zoomLevel = 3.0f;
    public const float ZOOM_MIN = 0.2f;
    public const float ZOOM_MAX = 6.0f;
    public const float ZOOM_SPEED = 0.1f;

    ///////////////
    ///////////////Login Variables/////////////
    ///////////////

    public static DateTime loginTime;
    public const int LOGIN_DELAY = 5000;

    ///////////////
    ///////////////Loading Variables/////////////
    ///////////////

    public static bool isLoading = false;
    public static float loadingProgress = 0f;
    public static readonly object loadingLock = new object();

    ///////////////
    ///////////////Path-Drawing Variables/////////////
    ///////////////

    public static List<(int x, int y)> currentPath = new List<(int x, int y)>();
    public static bool showPath = false;
    public static List<(int x, int y)> absolutePathPoints = new List<(int x, int y)>();
}