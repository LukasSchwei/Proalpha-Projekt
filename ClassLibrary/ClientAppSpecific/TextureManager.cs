using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using ClassLibrary.GlobalVariables;

namespace ClassLibrary.TextureManager;

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
    Default
}

/// <summary>
/// Manages the player's state including direction, animations, and texture handling.
/// Handles loading and caching of player textures based on direction and game state.
/// </summary>
public static class PlayerStateHandler
{
    //////////////////////////////////////
    //////////////VARIABLES///////////////
    //////////////////////////////////////
    private static PlayerDirection currentDirection = PlayerDirection.Default;
    private static bool hasCollectedCoin = false;
    private static readonly Dictionary<PlayerDirection, string> directionTextures = new();
    private static readonly Dictionary<PlayerDirection, Image> cachedTextures = new();
    public static PlayerDirection CurrentDirection => currentDirection;

    /// <summary>
    /// Tracks animation state (alternates between true/false for walking animation).
    /// </summary>
    private static bool movemode = false;

    //////////////////////////////////////
    //////////////METHODS/////////////////
    //////////////////////////////////////

    /// <summary>
    /// Initializes the texture manager by setting up texture mappings and loading textures.
    /// Creates Textures directory if it doesn't exist and loads all player textures.
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
        directionTextures[PlayerDirection.Default] = "player.png";

        // Load all textures into cache
        foreach (var pair in directionTextures)
        {
            string filePath = TextureManager.GetTexturePath(pair.Value);
            if (File.Exists(filePath))
            {
                try
                {
                    cachedTextures[pair.Key] = Image.FromFile(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load texture {filePath}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Sets the player's direction based on the given delta X and Y values.
    /// Updates the current direction and game state accordingly.
    /// </summary>
    /// <param name="deltaX">Change in X position.</param>
    /// <param name="deltaY">Change in Y position.</param>
    public static void SetDirection(int deltaX, int deltaY)
    {
        movemode = !movemode;
        currentDirection = (deltaX, deltaY, movemode) switch
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
            (0, 0, true) => PlayerDirection.Default,
            (0, 0, false) => PlayerDirection.Default,
            _ => PlayerDirection.Default
        };
    }

    /// <summary>
    /// Sets the player's coin collection state to true.
    /// Resets the state after a short delay to show the coin collection effect.
    /// </summary>
    public static void SetCollectedCoin()
    {
        hasCollectedCoin = true;
        // Reset after a short delay to show the coin collection effect
        Task.Delay(500).ContinueWith(_ =>
        {
            hasCollectedCoin = false;
            // Refresh the texture after coin collection effect ends
            var currentTexture = directionTextures[currentDirection];
        });
    }

    /// <summary>
    /// Returns the current texture based on the player's direction and game state.
    /// If the player has collected a coin, returns the coin texture.
    /// Otherwise, returns the texture corresponding to the current direction.
    /// </summary>
    /// <returns>The current texture.</returns>
    public static Image GetCurrentTexture()
    {
        // If the player has collected a coin, try to get the coin texture
        if (hasCollectedCoin && cachedTextures.TryGetValue(PlayerDirection.Default, out var coinTexture))
        {
            string coinTexturePath = TextureManager.GetTexturePath("player_coin.png");
            try
            {
                if (File.Exists(coinTexturePath))
                {
                    var img = Image.FromFile(coinTexturePath);
                    // Cache the coin texture
                    if (!cachedTextures.ContainsKey(PlayerDirection.Default))
                        cachedTextures[PlayerDirection.Default] = img;
                    return img;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading coin texture: {ex.Message}");
            }
        }

        // Otherwise, get the current direction's texture
        if (cachedTextures.TryGetValue(currentDirection, out var texture))
        {
            return texture;
        }

        // Fallback to default texture if current direction's texture is missing
        cachedTextures.TryGetValue(PlayerDirection.Default, out var defaultTexture);
        return defaultTexture;
    }

    /// <summary>
    /// Clears cached textures and reloads them – used when the user switches the skin at runtime.
    /// </summary>
    public static void ReloadTextures()
    {
        cachedTextures.Clear();
        Initialize();
    }
}

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

    public static Image BackgroundTexture { get; private set; }
    public static Image PlayerTexture { get; private set; }

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
    public static Image GetPlayerTexture()
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