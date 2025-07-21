using System;
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

    public static int actioncounter;

    ///////////////
    ///////////////Zoom Variables/////////////
    ///////////////

    public static float zoomLevel = 4.0f;
    public const float ZOOM_MIN = 0.5f;
    public const float ZOOM_MAX = 6.0f;
    public const float ZOOM_SPEED = 0.1f;

    ///////////////
    ///////////////Algorithm Logic FasterSolve Constants/////////////
    ///////////////

    public const int CHANGE_TO_UNKNOWN_MAP1 = 5;
    public const int CHANGE_TO_UNKNOWN_MAP2 = 10;
    public const int CHANGE_TO_UNKNOWN_MAP3 = 20;

    public const int TRY_FINISH_MAP1 = 999;
    public const int TRY_FINISH_MAP2 = 999;
    public const int TRY_FINISH_MAP3 = 20;

    public const int IGNORE_UNKNOWN_ON_Y_MAP1 = 0;
    public const int IGNORE_UNKNOWN_ON_Y_MAP2 = 0;
    public const int IGNORE_UNKNOWN_ON_Y_MAP3 = 2;

    public const int IGNORE_UNKNOWN_ON_X_MAP1 = 0;
    public const int IGNORE_UNKNOWN_ON_X_MAP2 = 0;
    public const int IGNORE_UNKNOWN_ON_X_MAP3 = 5;

    public const int FINISH_COIN_COUNTER_MAP1 = 404;
    public const int FINISH_COIN_COUNTER_MAP2 = 332;
    public const int FINISH_COIN_COUNTER_MAP3 = 20;

    public const int LOOK_THREASHOLD_MAP1 = 3;
    public const int LOOK_THREASHOLD_MAP2 = 2;
    public const int LOOK_THREASHOLD_MAP3 = 4;

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