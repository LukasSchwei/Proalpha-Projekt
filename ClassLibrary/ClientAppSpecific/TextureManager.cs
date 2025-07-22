using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Timers;
using ClassLibrary.GlobalVariables;

namespace ClassLibrary.TextureManager;

#region Enums
/// <summary>
/// Represents the possible player states in the game.
/// </summary>
public enum PlayerState
{
    Spawn,      // Shows player.png on login
    Moving,     // Shows directional movement textures
    Collecting, // Shows player_coin.png when collecting
    Idle        // Alternates between player_idle1.png and player_idle2.png
}

/// <summary>
/// Represents the possible directions a player can face in the game.
/// Each direction has two states (1 and 2) for animation purposes.
/// </summary>
public enum PlayerDirection
{
    North1,
    North2,
    NorthEast1,
    NorthEast2,
    East1,
    East2,
    SouthEast1,
    SouthEast2,
    South1,
    South2,
    SouthWest1,
    SouthWest2,
    West1,
    West2,
    NorthWest1,
    NorthWest2,
    Idle1,
    Idle2,
    Default
}
#endregion

#region PlayerStateHandler
/// <summary>
/// Manages the player's state machine including direction, animations, and texture handling.
/// Handles loading and caching of player textures based on current state and direction.
/// </summary>
public static class PlayerStateHandler
{
    //////////////////////////////////////
    //////////////VARIABLES///////////////
    //////////////////////////////////////
    private static PlayerState currentState = PlayerState.Spawn;
    private static PlayerDirection currentDirection = PlayerDirection.Default;
    private static readonly Dictionary<PlayerDirection, string> directionTextures = new();
    private static readonly Dictionary<PlayerDirection, Image> cachedTextures = new();
    private static System.Timers.Timer? idleTimer;
    private static System.Timers.Timer? idleAnimationTimer;
    private static bool idleAnimationState = true; // true = idle1, false = idle2
    private static bool moveAnimationState = false; // toggles between 1 and 2 for movement

    public static PlayerState CurrentState => currentState;
    public static PlayerDirection CurrentDirection => currentDirection;

    // Event to trigger redraw when texture changes
    public static Action? TriggerRedraw { get; set; }

    //////////////////////////////////////
    //////////////METHODS/////////////////
    //////////////////////////////////////

    /// <summary>
    /// Initializes the state machine by setting up texture mappings and loading textures.
    /// Creates texture cache and sets up timers for state transitions.
    /// </summary>
    public static void Initialize()
    {
        // Initialize directional textures mapping
        directionTextures[PlayerDirection.North1] = "player_north1.png";
        directionTextures[PlayerDirection.North2] = "player_north2.png";
        directionTextures[PlayerDirection.NorthEast1] = "player_northeast1.png";
        directionTextures[PlayerDirection.NorthEast2] = "player_northeast2.png";
        directionTextures[PlayerDirection.East1] = "player_east1.png";
        directionTextures[PlayerDirection.East2] = "player_east2.png";
        directionTextures[PlayerDirection.SouthEast1] = "player_southeast1.png";
        directionTextures[PlayerDirection.SouthEast2] = "player_southeast2.png";
        directionTextures[PlayerDirection.South1] = "player_south1.png";
        directionTextures[PlayerDirection.South2] = "player_south2.png";
        directionTextures[PlayerDirection.SouthWest1] = "player_southwest1.png";
        directionTextures[PlayerDirection.SouthWest2] = "player_southwest2.png";
        directionTextures[PlayerDirection.West1] = "player_west1.png";
        directionTextures[PlayerDirection.West2] = "player_west2.png";
        directionTextures[PlayerDirection.NorthWest1] = "player_northwest1.png";
        directionTextures[PlayerDirection.NorthWest2] = "player_northwest2.png";
        directionTextures[PlayerDirection.Idle1] = "player_idle1.png";
        directionTextures[PlayerDirection.Idle2] = "player_idle2.png";
        directionTextures[PlayerDirection.Default] = "player.png";

        // Load all textures into cache
        foreach (var pair in directionTextures)
        {
            string filePath = TextureManager.GetTexturePath(pair.Value);
            if (File.Exists(filePath))
            {
                cachedTextures[pair.Key] = Image.FromFile(filePath);
            }
        }

        // Also load the coin collection texture
        string coinTexturePath = TextureManager.GetTexturePath("player_coin.png");
        if (File.Exists(coinTexturePath))
        {
            // Using a special key for the coin texture
            cachedTextures[PlayerDirection.Default] = Image.FromFile(coinTexturePath);
        }

        // Initialize timers
        SetupTimers();

        // Start in spawn state and stay there until first movement
        SetState(PlayerState.Spawn);
    }

    /// <summary>
    /// Sets up the idle and idle animation timers.
    /// </summary>
    private static void SetupTimers()
    {
        // Timer to transition from Moving to Idle after 1000ms of no movement
        idleTimer = new System.Timers.Timer(1000);
        idleTimer.Elapsed += (sender, e) =>
        {
            if (currentState == PlayerState.Moving)
            {
                SetState(PlayerState.Idle);
            }
        };
        idleTimer.AutoReset = false; // Only fire once

        // Timer for idle animation (switches between idle1 and idle2 every 1000ms)
        idleAnimationTimer = new System.Timers.Timer(1000);
        idleAnimationTimer.Elapsed += (sender, e) =>
        {
            // Only animate if we're in idle state
            if (currentState == PlayerState.Idle)
            {
                idleAnimationState = !idleAnimationState;
                currentDirection = idleAnimationState ? PlayerDirection.Idle1 : PlayerDirection.Idle2;
                // Trigger a redraw to show the new texture
                TriggerRedraw?.Invoke();
            }
        };
        idleAnimationTimer.AutoReset = true; // Repeat continuously
    }

    /// <summary>
    /// Sets the player's state and handles state-specific logic.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    public static void SetState(PlayerState newState)
    {
        // Only stop the idle timer, keep idle animation timer running
        idleTimer?.Stop();

        var oldState = currentState;
        currentState = newState;

        switch (newState)
        {
            case PlayerState.Spawn:
                currentDirection = PlayerDirection.Default;
                break;

            case PlayerState.Moving:
                // The direction is already set by SetDirection method
                // Start idle timer to transition to idle if no further movement
                idleTimer?.Start();
                break;

            case PlayerState.Collecting:
                // Keep current direction, just change how we render
                Task.Delay(500).ContinueWith(_ =>
                {
                    if (currentState == PlayerState.Collecting)
                    {
                        // Return to idle state after collecting
                        SetState(PlayerState.Idle);
                    }
                });
                break;

            case PlayerState.Idle:
                // Start with idle1 and begin animation
                idleAnimationState = true;
                currentDirection = PlayerDirection.Idle1;
                // Start the idle animation timer if it's not running yet
                if (idleAnimationTimer != null && !idleAnimationTimer.Enabled)
                {
                    idleAnimationTimer.Start();
                }
                // Trigger immediate redraw to show idle state
                TriggerRedraw?.Invoke();
                break;
        }
    }

    /// <summary>
    /// Handles movement input and updates the player's direction and state.
    /// </summary>
    /// <param name="deltaX">Change in X position.</param>
    /// <param name="deltaY">Change in Y position.</param>
    public static void SetDirection(int deltaX, int deltaY)
    {
        // If no movement, don't change anything
        if (deltaX == 0 && deltaY == 0) return;

        // Toggle movement animation state
        moveAnimationState = !moveAnimationState;

        // Determine the direction based on delta and animation state
        currentDirection = (deltaX, deltaY, moveAnimationState) switch
        {
            (0, -1, true) => PlayerDirection.North1,
            (0, -1, false) => PlayerDirection.North2,
            (1, -1, true) => PlayerDirection.NorthEast1,
            (1, -1, false) => PlayerDirection.NorthEast2,
            (1, 0, true) => PlayerDirection.East1,
            (1, 0, false) => PlayerDirection.East2,
            (1, 1, true) => PlayerDirection.SouthEast1,
            (1, 1, false) => PlayerDirection.SouthEast2,
            (0, 1, true) => PlayerDirection.South1,
            (0, 1, false) => PlayerDirection.South2,
            (-1, 1, true) => PlayerDirection.SouthWest1,
            (-1, 1, false) => PlayerDirection.SouthWest2,
            (-1, 0, true) => PlayerDirection.West1,
            (-1, 0, false) => PlayerDirection.West2,
            (-1, -1, true) => PlayerDirection.NorthWest1,
            (-1, -1, false) => PlayerDirection.NorthWest2,
            _ => PlayerDirection.Default
        };

        // Set state to Moving
        SetState(PlayerState.Moving);
    }

    /// <summary>
    /// Triggers the collecting state when the player collects something.
    /// </summary>
    public static void Collect()
    {
        SetState(PlayerState.Collecting);
    }

    /// <summary>
    /// Returns the current texture based on the player's state and direction.
    /// </summary>
    /// <returns>The current texture or null if not found.</returns>
    public static Image? GetCurrentTexture()
    {
        switch (currentState)
        {
            case PlayerState.Spawn:
                // Show default player.png texture
                string playerTexturePath = TextureManager.GetTexturePath("player.png");
                if (File.Exists(playerTexturePath))
                {
                    return Image.FromFile(playerTexturePath);
                }
                break;

            case PlayerState.Collecting:
                // Show coin collection texture
                string coinTexturePath = TextureManager.GetTexturePath("player_coin.png");
                if (File.Exists(coinTexturePath))
                {
                    return Image.FromFile(coinTexturePath);
                }
                break;

            case PlayerState.Moving:
                // Show movement animation texture based on current direction
                if (cachedTextures.TryGetValue(currentDirection, out var movingTexture))
                {
                    return movingTexture;
                }
                break;

            case PlayerState.Idle:
                // Show idle animation texture (idle1 or idle2)
                if (cachedTextures.TryGetValue(currentDirection, out var idleTexture))
                {
                    return idleTexture;
                }
                break;
        }

        // Fallback to default texture
        string defaultTexturePath = TextureManager.GetTexturePath("player.png");
        return Image.FromFile(defaultTexturePath);
    }

    /// <summary>
    /// Clears cached textures and reloads them – used when the user switches the skin at runtime.
    /// </summary>
    public static void ReloadTextures()
    {
        // Stop timers during reload
        idleTimer?.Stop();
        idleAnimationTimer?.Stop();

        // Clear cache and reinitialize
        cachedTextures.Clear();
        Initialize();
    }

    /// <summary>
    /// Cleanup method to dispose of timers when needed.
    /// </summary>
    public static void Dispose()
    {
        idleTimer?.Stop();
        idleTimer?.Dispose();
        idleAnimationTimer?.Stop();
        idleAnimationTimer?.Dispose();
    }
}
#endregion

#region TextureManager
public static class TextureManager
{
    //////////////////////////////////////
    //////////////VARIABLES///////////////
    //////////////////////////////////////
    private static Dictionary<string, Image> textures = new Dictionary<string, Image>();
    private static Dictionary<string, int> textureVariants = new Dictionary<string, int>();
    private static Random random = new Random();
    private static bool initialized = false;
    public const string TEXTURES_FOLDER = "Textures";

    /// <summary>
    /// Combines the textures root folder with the currently selected skin (1-5) and file name.
    /// </summary>
    public static string GetTexturePath(string fileName) => Path.Combine(TEXTURES_FOLDER, GV.CurrentSkin.ToString(), fileName);

    private static readonly Dictionary<string, string> textureFiles = new Dictionary<string, string>
    {
        { "COLLECTIBLE_COIN-1", "coin-1.png" },
        { "COLLECTIBLE_COIN-2", "coin-2.png" },
        { "COLLECTIBLE_COIN-3", "coin-3.png" },
        { "COLLECTIBLE_DYNAMITE", "dynamite.png" },
        { "SOLID_WALL-1", "wall-1.png" },
        { "SOLID_WALL-2", "wall-2.png" },
        { "SOLID_WALL-3", "wall-3.png" },
        { "SOLID_ROCK-1", "rock-1.png" },
        { "SOLID_ROCK-2", "rock-2.png" },
        { "SOLID_ROCK-3", "rock-3.png" },
        { "INFO_STARTPOS", "start.png" },
        { "NONE-1", "none-1.png" },
        { "NONE-2", "none-2.png" },
        { "NONE-3", "none-3.png" },
        { "BACKGROUND", "background.png" },
        { "PLAYER", "player.png" }
    };

    public static Image? BackgroundTexture { get; private set; }
    public static Image? PlayerTexture { get; private set; }

    //////////////////////////////////////
    //////////////METHODS/////////////////
    //////////////////////////////////////

    /// <summary>
    /// Initializes the texture manager.
    /// </summary>
    public static void Initialize()
    {
        if (initialized) return;

        // Initialize player state handler
        PlayerStateHandler.Initialize();

        // Load all textures
        foreach (var pair in textureFiles)
        {
            string filePath = GetTexturePath(pair.Value);
            if (File.Exists(filePath))
            {
                try
                {
                    var image = Image.FromFile(filePath);
                    textures[pair.Key] = image;

                    // Store background and player texture separately for easy access
                    if (pair.Key == "BACKGROUND")
                    {
                        BackgroundTexture = image;
                    }
                    else if (pair.Key == "PLAYER")
                    {
                        PlayerTexture = image;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load texture {filePath}: {ex.Message}");
                }
            }
        }

        initialized = true;
    }

    /// <summary>
    /// Gets a texture by type.
    /// </summary>
    /// <param name="type">Type of texture to retrieve.</param>
    /// <param name="x">X position of the texture.</param>
    /// <param name="y">Y position of the texture.</param>
    /// <returns>The requested texture.</returns>
    public static Image GetTexture(string type, int x, int y)
    {
        if (type == "COLLECTIBLE_DYNAMITE")
        {
            return textures["COLLECTIBLE_DYNAMITE"];
        }
        else if (type == "INFO_STARTPOS")
        {
            return textures["INFO_STARTPOS"];
        }

        // Use a seeded random based on position for consistent but random variants
        int seed = (GV.GameId.GetHashCode() * x * y) - GV.CurrentMap.GetHashCode();
        int variant = new Random(seed).Next(1, 4);

        string textureKey = $"{type}-{variant}";
        if (textures.TryGetValue(textureKey, out var texture))
        {
            return texture;
        }
        // Fallback to variant 1 if the calculated variant doesn't exist
        textureKey = $"{type}-1";
        if (textures.TryGetValue(textureKey, out texture))
        {
            return texture;
        }
        // Return a default texture if the requested one doesn't exist
        return textures.ContainsKey("NONE-1") ? textures["NONE-1"] : null;
    }

    /// <summary>
    /// Gets the player texture.
    /// </summary>
    /// <returns>The player texture.</returns>
    public static Image? GetPlayerTexture()
    {
        return PlayerStateHandler.GetCurrentTexture();
    }

    /// <summary>
    /// Reload every texture after skin switch.
    /// </summary>
    public static void ReloadTextures()
    {
        textures.Clear();
        BackgroundTexture = null;
        PlayerTexture = null;
        initialized = false;

        // also refresh player directional textures
        PlayerStateHandler.ReloadTextures();

        Initialize();
    }

    /// <summary>
    /// Convenience helper – cycles through the available skins and reloads all textures.
    /// </summary>
    public static void ChangeSkin(int newSkinId)
    {
        // clamp & store
        if (newSkinId < 1) newSkinId = 1;
        if (newSkinId > 5) newSkinId = 5;

        if (GV.CurrentSkin == newSkinId) return;

        GV.CurrentSkin = newSkinId;
        ReloadTextures();
    }
}
#endregion