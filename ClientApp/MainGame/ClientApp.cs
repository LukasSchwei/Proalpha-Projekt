using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using ClassLibrary.Coordinates;
using ClientApp.Communication;
using ClassLibrary.GlobalVariables;
using ClassLibrary.Responses;
using ClassLibrary.Dialog;
using ClassLibrary.TextureManager;
using ClassLibrary.Algorithm.BFS;
using ClassLibrary.Algorithm.AStar;
using ClassLibrary.MapSaver;
using ClassLibrary.Variables;
using ClassLibrary.Map;
using Microsoft.JSInterop.Infrastructure;
using ClassLibrary.Wins;

namespace ClientApp;

public partial class ClientApp : Form
{
    public enum Algorithm
    {
        AStar,
        BFS
    }
    public static IWin32Window owner;
    public static System.Windows.Forms.Timer loadingTimer;
    public static bool centerOnPlayer = true;

    #region Constructor

    /// <summary>
    /// Initializes the form and its components.
    /// </summary>
    public ClientApp()
    {
        InitializeComponent();
        //DoubleBuffered to not flicker while resizing
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        this.UpdateStyles();

        // Initialize loading timer
        loadingTimer = new System.Windows.Forms.Timer();
        loadingTimer.Interval = 10; // Update every 10ms for smooth animation
        loadingTimer.Tick += (s, e) =>
        {
            if (V.isLoading)
            {
                lock (V.loadingLock)
                {
                    // Update progress based on time elapsed since login
                    float progress = (float)(DateTime.Now - V.loginTime).TotalMilliseconds / V.LOGIN_DELAY;
                    V.loadingProgress = Math.Min(progress, 1.0f);
                }
                Invalidate(); // Force redraw to update progress bar
            }
        };

        string iconPath = Path.Combine("Textures", "icon.ico");
        if (File.Exists(iconPath))
        {
            this.Icon = new Icon(iconPath);
        }

        this.BackColor = Color.Black;
        this.Text = "MoleThief";

        //# this.Resize += (s, e) => CenterOnPlayer();
        this.KeyPreview = true;
        this.KeyDown += ClientApp_KeyDown;
        this.WindowState = FormWindowState.Maximized;

        // Initialize textures
        TextureManager.Initialize();

        // Set up the redraw trigger for the state machine
        PlayerStateHandler.TriggerRedraw = () => this.Invalidate();

        if (centerOnPlayer)
            CenterOnPlayer(); // Center on player by default
        else
            CenterOnMap(); // Center on map if not centered on player

        V.currentPosition = new CurrentPosition() { AbsoluteX = 0, AbsoluteY = 0 };

        // Start the login process when the form loads
        this.Load += async (sender, e) => await Login(GV.CurrentMap);
        owner = this;
        V.actioncounter = 0;
    }

    /*
    /// <summary>
    /// Shows the action counter in a dialog.
    /// </summary>
    private async Task DEBUGshowActionCounter()
    {
        Dialog.CreateGenericDialog("Action Counter", $"Action Counter: {V.actioncounter}", "Ok", owner);
    }
    */

    #endregion
    /////////////////////
    /////////////////////WINFORMS////////////////////////
    /////////////////////
    #region WinForms

    /// <summary>
    /// Handles key down events for player actions.
    /// </summary>
    private async void ClientApp_KeyDown(object sender, KeyEventArgs e)
    {
        if (DateTime.Now - V.loginTime < TimeSpan.FromMilliseconds(V.LOGIN_DELAY)) return;

        switch (e.KeyCode)
        {
            case Keys.W: if (V.isMoving) return; await Move(0, -1); break;
            case Keys.A: if (V.isMoving) return; await Move(-1, 0); break;
            case Keys.S: if (V.isMoving) return; await Move(0, 1); break;
            case Keys.D: if (V.isMoving) return; await Move(1, 0); break;
            case Keys.Q: if (V.isMoving) return; await Move(-1, -1); break;
            case Keys.E: if (V.isMoving) return; await Move(1, -1); break;
            case Keys.Y: if (V.isMoving) return; await Move(-1, 1); break;
            case Keys.X: if (V.isMoving) return; await Move(1, 1); break;
            case Keys.F1: if (V.isAlgorithmRunning) return; await AlgorithmicSolve(V.map, Algorithm.BFS); break;
            case Keys.F2: if (V.isAlgorithmRunning) return; await AlgorithmicSolve(V.map, Algorithm.AStar); break;
            case Keys.F3: if (V.isAlgorithmRunning) return; await RevealWholeMap(); break;
            case Keys.L: await Look(); break;
            case Keys.C: await Collect(); break;
            case Keys.G: await Quit(); break;
            case Keys.F: await Finish(); break;
            case Keys.R: centerOnPlayer = !centerOnPlayer; CenterOnMap(); break;
                //MAPSAVINGCHEAT NICHT BENUTZEN!
                //case Keys.F8: await AlgorithmicSolveBFS(MapSavingCheat.ReadMap()); break;
                //case Keys.F12: await DEBUGshowActionCounter(); break;
        }
    }

    /// <summary>
    /// Centers the player on the screen.
    /// </summary>
    private void CenterOnPlayer()
    {
        if (V.currentPosition != null)
        {
            // Calculate the center position of the screen
            int centerX = this.ClientSize.Width / 2;
            int centerY = this.ClientSize.Height / 2;

            // Calculate the offset needed to center the player
            V.offsetX = centerX - (V.currentPosition.AbsoluteX * V.baseCellSize * V.zoomLevel);
            V.offsetY = centerY - (V.currentPosition.AbsoluteY * V.baseCellSize * V.zoomLevel);

            Invalidate();
        }
    }

    /// <summary>
    /// Centers the view on the middle of the map (INFO_STARTPOS).
    /// </summary>
    private void CenterOnMap()
    {
        // Find the INFO_STARTPOS in the map
        var startPos = V.map.Values.FirstOrDefault(obj => obj.Type == "INFO_STARTPOS");

        if (startPos != null)
        {
            // Calculate the center position of the screen
            int centerX = this.ClientSize.Width / 2;
            int centerY = this.ClientSize.Height / 2;

            // Calculate the offset needed to center the map on the start position
            V.offsetX = centerX - (startPos.AbsoluteX * V.baseCellSize * V.zoomLevel);
            V.offsetY = centerY - (startPos.AbsoluteY * V.baseCellSize * V.zoomLevel);

            Invalidate();
        }
    }

    /// <summary>
    /// Handles mouse wheel events for zooming.
    /// </summary>
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        // Store the current zoom level before changing it
        float oldZoom = V.zoomLevel;

        // Calculate the new zoom level
        V.zoomLevel += e.Delta * V.ZOOM_SPEED / 120;
        V.zoomLevel = Math.Max(V.ZOOM_MIN, Math.Min(V.ZOOM_MAX, V.zoomLevel));

        // Only update if zoom actually changed
        if (Math.Abs(oldZoom - V.zoomLevel) > 0.01f)
        {
            // Get the player's position in world coordinates
            var playerPos = V.currentPosition ?? V.lastValidPosition;
            var startPos = V.map.Values.FirstOrDefault(obj => obj.Type == "INFO_STARTPOS");
            if ((playerPos != null && centerOnPlayer) || (startPos != null && !centerOnPlayer))
            {
                if (centerOnPlayer)
                {
                    V.offsetX = (this.ClientSize.Width / 2) - (playerPos.AbsoluteX * V.baseCellSize * V.zoomLevel);
                    V.offsetY = (this.ClientSize.Height / 2) - (playerPos.AbsoluteY * V.baseCellSize * V.zoomLevel);
                }
                else
                {
                    V.offsetX = (this.ClientSize.Width / 2) - (startPos.AbsoluteX * V.baseCellSize * V.zoomLevel);
                    V.offsetY = (this.ClientSize.Height / 2) - (startPos.AbsoluteY * V.baseCellSize * V.zoomLevel);
                }

                Invalidate();
            }
        }
    }

    /// <summary>
    /// Handles resize events for centering the player.
    /// </summary>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (centerOnPlayer)
            CenterOnPlayer(); // Center player when window is resized
        else
            CenterOnMap(); // Center map when window is resized
    }

    #endregion
    /////////////////////
    /////////////////////GAME COMMANDS////////////////////////
    /////////////////////
    #region Game Commands

    /// <summary>
    /// Moves the player in the specified direction.
    /// </summary>
    private async Task Move(int x, int y)
    {
        V.actioncounter++;
        if (V.isMoving) return;
        V.isMoving = true;

        try
        {
            // Update player direction and state before moving
            PlayerStateHandler.SetDirection(x, y);

            ClientAPICom clientAPICom = new ClientAPICom();
            Response response = await clientAPICom.MoveAsync(x, y);

            if (response != null)
            {
                // Only update position if the server confirms the move
                if (response.CurrentPosition != null)
                {
                    V.currentPosition = response.CurrentPosition;
                    V.lastValidPosition = response.CurrentPosition;
                    if (centerOnPlayer)
                        CenterOnPlayer(); // Center camera on player after move
                    else
                        CenterOnMap(); // Center camera on map after move
                }

                // Always update the map with the latest objects
                if (response.Objects != null)
                {
                    await Map.Update(response.Objects);
                    Invalidate();
                    if (response.Objects.Count < 8)
                    {
                        await Map.AdjustBounds(response.Objects, 1);
                    }
                }
            }
        }
        finally
        {
            V.isMoving = false;
            Invalidate(); // Redraw to show updated player texture
        }
    }

    /// <summary>
    /// Logs the player in and initializes the game.
    /// </summary>
    private async Task Login(string Map)
    {
        // Reset loading state
        lock (V.loadingLock)
        {
            V.isLoading = true;
            V.loadingProgress = 0f;
        }

        V.loginTime = DateTime.Now;
        loadingTimer.Start();

        try
        {
            // Set spawn state on login, showing player.png
            PlayerStateHandler.SetState(PlayerState.Spawn);
            V.actioncounter = 0;
            GV.finished = false;
            V.map.Clear();
            //# V.map = MapSavingCheat.ReadMap(); // Load the map from the cheat file
            ClientAPICom clientAPICom = new ClientAPICom();

            // Start the login process
            var loginTask = clientAPICom.LoginAsync(Map);

            // Wait for either the login to complete or the minimum delay
            var delayTask = Task.Delay(V.LOGIN_DELAY);
            var completedTask = await Task.WhenAny(loginTask, delayTask);

            // If login completed first, wait for the remaining time
            if (completedTask == loginTask && delayTask.IsCompleted == false)
            {
                await delayTask;
            }

            // Ensure we have the login result
            string result = await loginTask;

            // Force progress to 100% before finishing
            lock (V.loadingLock)
            {
                V.loadingProgress = 1.0f;
            }
            Invalidate(); // Force a final redraw with 100% progress
            await Task.Delay(100); // Small delay to show 100% progress
        }
        finally
        {
            lock (V.loadingLock)
            {
                V.isLoading = false;
            }
            loadingTimer.Stop();

            // Stay in spawn state - don't transition to idle until first movement

            Invalidate(); // Force redraw to remove loading screen
        }
    }

    /// <summary>
    /// Looks at the current position and updates the map.
    /// </summary>
    private async Task Look()
    {
        V.actioncounter++;
        ClientAPICom clientAPICom = new ClientAPICom();
        Response response = await clientAPICom.LookAsync();
        if (response != null)
        {
            if (response.CurrentPosition != null)
            {
                // Update current player position only when the API returns a valid coordinate
                V.currentPosition = response.CurrentPosition;
            }

            if (response.Objects != null)
            {
                await Map.Update(response.Objects);
                Invalidate();
                if (response.Objects.Count < 61)
                {
                    await Map.AdjustBounds(response.Objects, 5);
                }

            }
        }
        Invalidate();
    }

    /// <summary>
    /// Quits the game and logs out the player.
    /// </summary>
    private async Task Quit()
    {

        ClientAPICom clientAPICom = new ClientAPICom();
        Response response = await clientAPICom.QuitAsync();
        Application.Restart();
    }

    /// <summary>
    /// Tries to finish the game.
    /// </summary>
    private async Task Finish()
    {
        V.actioncounter++;
        ClientAPICom clientAPICom = new ClientAPICom();
        Response response = await clientAPICom.FinishAsync();
        if (response != null)
        {
            if (GV.finished)
            {
                int winNumber = Wins.Read() + 1;
                bool result = Dialog.CreateQuitDialog("Game Finished", $"You have finished the game in {V.actioncounter} actions.\n It took you a total time of {DateTime.Now - V.loginTime}.\n This is your {GetOrdinalSuffix(winNumber)} win.", "Exit", owner);
                if (result)
                {
                    //# Wins.Save(0);
                    if (Wins.Read() > 0)
                    {
                        Wins.Save(Wins.Read() + 1);
                    }
                    else
                    {
                        Wins.Save(1);
                    }

                    Application.Restart();
                }
            }
            else
            {
                Dialog.CreateGenericDialog("Game Not Finished", "You have not finished the game. There are still coins left.", "Ok", owner);
            }
        }
    }

    /// <summary>
    /// Tries to collect the object at the current position.
    /// </summary>
    private async Task Collect()
    {
        V.actioncounter++;
        ClientAPICom clientAPICom = new ClientAPICom();
        Response response = await clientAPICom.CollectAsync();
        if (response != null && (V.map[(V.currentPosition.AbsoluteX, V.currentPosition.AbsoluteY)].Type == "COLLECTIBLE_COIN" || V.map[(V.currentPosition.AbsoluteX, V.currentPosition.AbsoluteY)].Type == "COLLECTIBLE_DYNAMITE"))
        {
            // Update player state to show coin collection effect
            if (V.map[(V.currentPosition.AbsoluteX, V.currentPosition.AbsoluteY)].Type == "COLLECTIBLE_COIN")
            {
                PlayerStateHandler.Collect();
            }

            await Map.Update(new List<AbsoluteObject>() { new AbsoluteObject() { AbsoluteX = V.currentPosition.AbsoluteX, AbsoluteY = V.currentPosition.AbsoluteY, Type = "NONE" } });
            Invalidate(); // Redraw to show updated player texture
        }
    }

    /// <summary>
    /// Adds ordinal suffix to a number (st, nd, rd, th).
    /// </summary>
    private string GetOrdinalSuffix(int number)
    {
        if (number % 100 >= 11 && number % 100 <= 13)
        {
            return number + "th";
        }

        return (number % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }

    #endregion
    /////////////////////
    /////////////////////ALGORITHMS////////////////////////
    /////////////////////
    #region Algorithms

    /// <summary>
    /// Solves the game using the algorithm specified (default is BFS).
    /// </summary>
    /// <param name="map">The map to solve.</param>
    /// <param name="algorithm">The algorithm to use.</param>
    private async Task AlgorithmicSolve(Dictionary<(int x, int y), AbsoluteObject> map, Algorithm algorithm)
    {
        V.isAlgorithmRunning = true;
        int changeToUnknown;
        int tryFinish;
        int ignoreUnknownOnX;
        int ignoreUnknownOnY;
        int finishCoinCounter;
        int lookThreshold;
        // Load configuration settings based on the current map from an xml file
        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
        AppSettingsSection appSettingSection = (AppSettingsSection)config.GetSection(config.AppSettings.SectionInformation.Name);
        switch (GV.CurrentMap)
        {
            case GV.MAP_4:
                changeToUnknown = Convert.ToInt32(appSettingSection.Settings["ChangeToUnknownMap4"].Value);
                tryFinish = Convert.ToInt32(appSettingSection.Settings["TryFinishMap4"].Value);
                ignoreUnknownOnX = Convert.ToInt32(appSettingSection.Settings["IgnoreUnknownXMap4"].Value);
                ignoreUnknownOnY = Convert.ToInt32(appSettingSection.Settings["IgnoreUnknownYMap4"].Value);
                finishCoinCounter = Convert.ToInt32(appSettingSection.Settings["FinishCoinCounterMap4"].Value);
                lookThreshold = Convert.ToInt32(appSettingSection.Settings["LookThresholdMap4"].Value);
                break;
            case GV.MAP_3:
                changeToUnknown = Convert.ToInt32(appSettingSection.Settings["ChangeToUnknownMap3"].Value);
                tryFinish = Convert.ToInt32(appSettingSection.Settings["TryFinishMap3"].Value);
                ignoreUnknownOnX = Convert.ToInt32(appSettingSection.Settings["IgnoreUnknownXMap3"].Value);
                ignoreUnknownOnY = Convert.ToInt32(appSettingSection.Settings["IgnoreUnknownYMap3"].Value);
                finishCoinCounter = Convert.ToInt32(appSettingSection.Settings["FinishCoinCounterMap3"].Value);
                lookThreshold = Convert.ToInt32(appSettingSection.Settings["LookThresholdMap3"].Value);
                break;
            case GV.MAP_2:
                changeToUnknown = Convert.ToInt32(appSettingSection.Settings["ChangeToUnknownMap2"].Value);
                tryFinish = Convert.ToInt32(appSettingSection.Settings["TryFinishMap2"].Value);
                ignoreUnknownOnX = Convert.ToInt32(appSettingSection.Settings["IgnoreUnknownXMap2"].Value);
                ignoreUnknownOnY = Convert.ToInt32(appSettingSection.Settings["IgnoreUnknownYMap2"].Value);
                finishCoinCounter = Convert.ToInt32(appSettingSection.Settings["FinishCoinCounterMap2"].Value);
                lookThreshold = Convert.ToInt32(appSettingSection.Settings["LookThresholdMap2"].Value);
                break;
            case GV.MAP_1:
                changeToUnknown = Convert.ToInt32(appSettingSection.Settings["ChangeToUnknownMap1"].Value);
                tryFinish = Convert.ToInt32(appSettingSection.Settings["TryFinishMap1"].Value);
                ignoreUnknownOnX = Convert.ToInt32(appSettingSection.Settings["IgnoreUnknownXMap1"].Value);
                ignoreUnknownOnY = Convert.ToInt32(appSettingSection.Settings["IgnoreUnknownYMap1"].Value);
                finishCoinCounter = Convert.ToInt32(appSettingSection.Settings["FinishCoinCounterMap1"].Value);
                lookThreshold = Convert.ToInt32(appSettingSection.Settings["LookThresholdMap1"].Value);
                break;
            default:
                changeToUnknown = 20;
                tryFinish = 999;
                ignoreUnknownOnX = 0;
                ignoreUnknownOnY = 0;
                finishCoinCounter = 999;
                lookThreshold = 4;
                break;
        }
        //# Dialog.CreateGenericDialog("Algorithmic Solve", $"Change to Unknown: {changeToUnknown}, " + $"Try Finish: {tryFinish}, " + $"Ignore Unknown X: {ignoreUnknownOnX},\n" + $"Ignore Unknown Y: {ignoreUnknownOnY}, " + "Finish Coin Counter: {finishCoinCounter}, " + $"Look Threshold: {lookThreshold}", "Ok", owner);
        await Look();
        int coinCollectedCounter = 0;
        while (V.map.Any(obj => obj.Value.Type == "COLLECTIBLE_COIN") ||
        BFSPathfinding.FindPathToUnknown(V.map, V.currentPosition, V.globalMapMinX + ignoreUnknownOnX,
        V.globalMapMinY + ignoreUnknownOnY, V.globalMapMaxX - ignoreUnknownOnX, V.globalMapMaxY - ignoreUnknownOnY, owner) != null)
        {
            List<(int x, int y)>? path;
            if (algorithm == Algorithm.AStar)
            {
                path = AStarPathfinding.FindPath(V.map, "COLLECTIBLE_COIN", V.currentPosition, V.globalMapMinX, V.globalMapMinY, V.globalMapMaxX, V.globalMapMaxY);
            }
            else
            {
                path = BFSPathfinding.FindPath(V.map, "COLLECTIBLE_COIN", V.currentPosition, V.globalMapMinX, V.globalMapMinY, V.globalMapMaxX, V.globalMapMaxY);
            }
            List<(int x, int y)>? unknownPath = null;

            if (coinCollectedCounter >= finishCoinCounter)
            {
                //DEBUGshowActionCounter();
                await Finish();
            }
            if (path == null)
            {
                int lookLock = 0;
                if (map.ContainsKey((V.currentPosition.AbsoluteX + 5, V.currentPosition.AbsoluteY + 5)))
                {
                    lookLock++;
                }
                if (map.ContainsKey((V.currentPosition.AbsoluteX - 5, V.currentPosition.AbsoluteY + 5)))
                {
                    lookLock++;
                }
                if (map.ContainsKey((V.currentPosition.AbsoluteX + 5, V.currentPosition.AbsoluteY - 5)))
                {
                    lookLock++;
                }
                if (map.ContainsKey((V.currentPosition.AbsoluteX - 5, V.currentPosition.AbsoluteY - 5)))
                {
                    lookLock++;
                }
                if (lookLock < lookThreshold)
                {
                    await Look();
                }
                if (algorithm == Algorithm.AStar)
                {
                    path = AStarPathfinding.FindPath(V.map, "COLLECTIBLE_COIN", V.currentPosition, V.globalMapMinX, V.globalMapMinY, V.globalMapMaxX, V.globalMapMaxY);
                    unknownPath = AStarPathfinding.FindPathToUnknown(V.map, V.currentPosition, V.globalMapMinX + ignoreUnknownOnX,
                    V.globalMapMinY + ignoreUnknownOnY, V.globalMapMaxX - ignoreUnknownOnX, V.globalMapMaxY - ignoreUnknownOnY);
                }
                else
                {
                    path = BFSPathfinding.FindPath(V.map, "COLLECTIBLE_COIN", V.currentPosition, V.globalMapMinX, V.globalMapMinY, V.globalMapMaxX, V.globalMapMaxY);
                    unknownPath = BFSPathfinding.FindPathToUnknown(V.map, V.currentPosition, V.globalMapMinX + ignoreUnknownOnX,
                    V.globalMapMinY + ignoreUnknownOnY, V.globalMapMaxX - ignoreUnknownOnX, V.globalMapMaxY - ignoreUnknownOnY, owner);
                }

            }
            else if (path.Count > changeToUnknown && map != MapSavingCheat.ReadMap())
            {
                if (algorithm == Algorithm.AStar)
                {
                    unknownPath = AStarPathfinding.FindPathToUnknown(V.map, V.currentPosition, V.globalMapMinX + ignoreUnknownOnX,
                    V.globalMapMinY + ignoreUnknownOnY, V.globalMapMaxX - ignoreUnknownOnX, V.globalMapMaxY - ignoreUnknownOnY);
                }
                else
                {
                    unknownPath = BFSPathfinding.FindPathToUnknown(V.map, V.currentPosition, V.globalMapMinX + ignoreUnknownOnX,
                    V.globalMapMinY + ignoreUnknownOnY, V.globalMapMaxX - ignoreUnknownOnX, V.globalMapMaxY - ignoreUnknownOnY, owner);
                }
                if (unknownPath != null && unknownPath.Count < path.Count)
                {
                    path = null;
                }
            }
            if (path != null)
            {
                V.currentPath = path;
                V.showPath = true;

                // Calculate and store absolute positions for the path
                V.absolutePathPoints.Clear();
                int currentX = V.currentPosition.AbsoluteX;
                int currentY = V.currentPosition.AbsoluteY;

                // The first point is the current position
                V.absolutePathPoints.Add((currentX, currentY));

                // Calculate absolute positions for each step
                foreach (var step in path)
                {
                    currentX += step.x;
                    currentY += step.y;
                    V.absolutePathPoints.Add((currentX, currentY));
                }

                Invalidate();
                foreach (var step in path)
                {
                    await Move(step.x, step.y);
                    Invalidate();

                    if (map[(V.currentPosition.AbsoluteX, V.currentPosition.AbsoluteY)].Type == "COLLECTIBLE_COIN")
                    {
                        coinCollectedCounter++;
                        await Collect();
                        map[(V.currentPosition.AbsoluteX, V.currentPosition.AbsoluteY)] = new AbsoluteObject() { AbsoluteX = V.currentPosition.AbsoluteX, AbsoluteY = V.currentPosition.AbsoluteY, Type = "NONE" };
                    }
                }
                V.showPath = false;
                V.currentPath.Clear();
                Invalidate();
            }
            else if (unknownPath != null)
            {
                if (unknownPath.Count > tryFinish && coinCollectedCounter >= finishCoinCounter)
                {
                    //DEBUGshowActionCounter();
                    await Finish();
                }

                V.currentPath = unknownPath;
                V.showPath = true;

                // Calculate and store absolute positions for the path
                V.absolutePathPoints.Clear();
                int currentX = V.currentPosition.AbsoluteX;
                int currentY = V.currentPosition.AbsoluteY;

                // The first point is the current position
                V.absolutePathPoints.Add((currentX, currentY));

                // Calculate absolute positions for each step
                foreach (var step in unknownPath)
                {
                    currentX += step.x;
                    currentY += step.y;
                    V.absolutePathPoints.Add((currentX, currentY));
                }

                Invalidate();
                foreach (var step in unknownPath)
                {
                    await Move(step.x, step.y);
                    Invalidate();

                    if (map[(V.currentPosition.AbsoluteX, V.currentPosition.AbsoluteY)].Type == "COLLECTIBLE_COIN")
                    {
                        coinCollectedCounter++;
                        await Collect();
                        map[(V.currentPosition.AbsoluteX, V.currentPosition.AbsoluteY)] = new AbsoluteObject() { AbsoluteX = V.currentPosition.AbsoluteX, AbsoluteY = V.currentPosition.AbsoluteY, Type = "NONE" };
                    }
                }
                V.showPath = false;
                V.currentPath.Clear();
                Invalidate();
            }
        }
        if (!V.isLoading)
        {
            //DEBUGshowActionCounter();
            await Finish();
        }
        else
        {
            return;
        }
        V.isAlgorithmRunning = false;
    }

    /// <summary>
    /// Reveal the whole map.
    /// </summary>
    private async Task RevealWholeMap()
    {
        V.isAlgorithmRunning = true;
        await Look();
        while (BFSPathfinding.FindPathToUnknown(V.map, V.currentPosition, V.globalMapMinX, V.globalMapMinY, V.globalMapMaxX, V.globalMapMaxY, owner) != null)
        {
            await Look();
            if (BFSPathfinding.FindPathToUnknown(V.map, V.currentPosition, V.globalMapMinX, V.globalMapMinY, V.globalMapMaxX, V.globalMapMaxY, owner) == null)
            {
                break;
            }
            foreach (var step in BFSPathfinding.FindPathToUnknown(V.map, V.currentPosition, V.globalMapMinX, V.globalMapMinY, V.globalMapMaxX, V.globalMapMaxY, owner))
            {
                await Move(step.x, step.y);
                Invalidate();
                Thread.Sleep(50);
            }
        }
        Dialog.CreateGenericDialog("Map revealed", "Whole Map revealed. On this map are " + V.map.Count(obj => obj.Value.Type == "COLLECTIBLE_COIN") + " coins.", "OK", this);
        MapSavingCheat.SaveMap(V.map);
    }

    #endregion
    /////////////////////
    /////////////////////VISUAL////////////////////
    /////////////////////
    #region Visual

    /// <summary>
    /// Paints the game map.
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;

        // Draw loading screen if active
        bool shouldShowLoading;
        float currentProgress;

        lock (V.loadingLock)
        {
            shouldShowLoading = V.isLoading || V.loadingProgress < 1.0f;
            currentProgress = V.loadingProgress;
        }

        if (shouldShowLoading)
        {
            // Try to load main_menu.png directly
            string loadingBackgroundPath = Path.Combine("Textures", GV.CurrentSkin.ToString(), "main_menu.png");
            if (File.Exists(loadingBackgroundPath))
            {

                using (var loadingBackgroundImage = Image.FromFile(loadingBackgroundPath))
                {
                    g.DrawImage(loadingBackgroundImage, 0, 0, loadingBackgroundImage.Width, loadingBackgroundImage.Height);
                }

            }

            // Draw progress bar background
            int progressBarHeight = 130;
            int progressBarWidth = (int)(this.ClientSize.Width);
            int progressBarX = (this.ClientSize.Width - progressBarWidth);
            int progressBarY = this.ClientSize.Height - 130;

            // Draw progress bar background
            g.FillRectangle(Brushes.Black, progressBarX, progressBarY, progressBarWidth, progressBarHeight);

            // Draw progress bar fill (ensure at least 1px width when not empty)
            int fillWidth = currentProgress <= 0 ? 0 : Math.Max(1, (int)(progressBarWidth * currentProgress));
            if (fillWidth > 0)
            {
                using (var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(progressBarX, progressBarY, fillWidth, progressBarHeight),
                    Color.FromArgb(0, 150, 255), // Blue
                    Color.FromArgb(0, 100, 200), // Darker blue
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(gradient, progressBarX, progressBarY, fillWidth, progressBarHeight);
                }
            }

            return; // Skip the rest of the drawing while loading
        }

        // Apply high-quality rendering settings
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

        // Draw repeating background texture with zoom
        if (TextureManager.BackgroundTexture != null)
        {
            using (var bgBrush = new TextureBrush(TextureManager.BackgroundTexture, System.Drawing.Drawing2D.WrapMode.Tile))
            {
                // Adjust background position based on zoom and offset
                bgBrush.TranslateTransform(V.offsetX, V.offsetY);
                bgBrush.ScaleTransform(V.zoomLevel, V.zoomLevel);

                // Fill the visible area with the tiled background
                g.FillRectangle(bgBrush, this.ClientRectangle);
            }
        }

        // Calculate scaled cell size
        float scaledCellSize = V.baseCellSize * V.zoomLevel;

        // Draw all seen tiles
        foreach (var tile in V.map.Values)
        {
            float screenX = V.offsetX + tile.AbsoluteX * scaledCellSize;
            float screenY = V.offsetY + tile.AbsoluteY * scaledCellSize;

            // Only draw tiles that are within the visible area
            if (screenX + scaledCellSize < 0 || screenX > this.ClientSize.Width ||
                screenY + scaledCellSize < 0 || screenY > this.ClientSize.Height)
            {
                continue;
            }

            // Draw the tile texture with scaling
            var texture = TextureManager.GetTexture(tile.Type, tile.AbsoluteX, tile.AbsoluteY);
            if (texture != null)
            {
                g.DrawImage(texture, screenX, screenY, scaledCellSize, scaledCellSize);
            }
            else
            {
                // Fallback to solid color if texture is missing
                using var brush = new SolidBrush(tile.Type switch
                {
                    "COLLECTIBLE_COIN" => Color.Yellow,
                    "COLLECTIBLE_DYNAMITE" => Color.Red,
                    "SOLID_WALL" => Color.Black,
                    "SOLID_ROCK" => Color.Gray,
                    "INFO_STARTPOS" => Color.Blue,
                    "NONE" => Color.White,
                    _ => Color.White
                });
                g.FillRectangle(brush, screenX, screenY, scaledCellSize, scaledCellSize);
                g.DrawRectangle(Pens.Black, screenX, screenY, scaledCellSize, scaledCellSize);
            }
        }

        // Draw the path if visible - using absolute positions
        if (V.showPath && V.absolutePathPoints.Count > 1)
        {
            using (var pathPen = new Pen(Color.Yellow, 4f * V.zoomLevel))
            using (var pointBrush = new SolidBrush(Color.Red))
            {
                // Draw lines between absolute path points
                for (int i = 0; i < V.absolutePathPoints.Count - 1; i++)
                {
                    var start = V.absolutePathPoints[i];
                    var end = V.absolutePathPoints[i + 1];

                    float startX = V.offsetX + start.x * scaledCellSize + scaledCellSize / 2;
                    float startY = V.offsetY + start.y * scaledCellSize + scaledCellSize / 2;
                    float endX = V.offsetX + end.x * scaledCellSize + scaledCellSize / 2;
                    float endY = V.offsetY + end.y * scaledCellSize + scaledCellSize / 2;

                    // Draw line between points
                    g.DrawLine(pathPen, startX, startY, endX, endY);

                    // Draw point at the start of each segment
                    float pointSize = 8f * V.zoomLevel;
                    g.FillEllipse(pointBrush, startX - pointSize / 2, startY - pointSize / 2, pointSize, pointSize);

                    // Draw point at the end of the last segment
                    if (i == V.absolutePathPoints.Count - 2)
                    {
                        g.FillEllipse(pointBrush, endX - pointSize / 2, endY - pointSize / 2, pointSize, pointSize);
                    }
                }
            }
        }

        // Draw the player last (on top of everything else)
        if (V.currentPosition != null)
        {
            float playerScreenX = V.currentPosition.AbsoluteX * V.baseCellSize * V.zoomLevel + V.offsetX;
            float playerScreenY = V.currentPosition.AbsoluteY * V.baseCellSize * V.zoomLevel + V.offsetY;
            float playerScaledCellSize = V.baseCellSize * V.zoomLevel;

            // Get the appropriate player texture based on state
            var playerTexture = TextureManager.GetPlayerTexture();

            if (playerTexture != null)
            {
                g.DrawImage(playerTexture, playerScreenX, playerScreenY, playerScaledCellSize, playerScaledCellSize);
            }
            else
            {
                // Fallback to simple colored rectangle if texture is missing
                int margin = (int)(playerScaledCellSize * 0.15f);
                g.FillRectangle(Brushes.DeepSkyBlue,
                    playerScreenX + margin, playerScreenY + margin,
                    playerScaledCellSize - 2 * margin, playerScaledCellSize - 2 * margin);
                g.DrawRectangle(Pens.Black,
                    playerScreenX + margin, playerScreenY + margin,
                    playerScaledCellSize - 2 * margin, playerScaledCellSize - 2 * margin);
            }
        }
    }
    #endregion
}