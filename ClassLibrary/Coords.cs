using System;
using System.Collections.Generic;
using System.Linq;
using ClassLibrary.Objects;

namespace ClassLibrary.Coordinates
{
    /// <summary>
    /// Static class for managing coordinate conversion between relative and absolute coordinate systems,
    /// tracking player position, and maintaining a global map of discovered objects.
    /// </summary>
    public static class CoordinateConverter
    {
        public static int CurrentAbsoluteX { get; set; } = 0;
        public static int CurrentAbsoluteY { get; set; } = 0;

        /// <summary>
        /// Global map storage for all discovered objects, indexed by their absolute coordinates.
        /// Key: (x, y) representing absolute coordinates
        /// Value: AbsoluteObject containing all object information
        /// </summary>
        private static readonly Dictionary<(int x, int y), AbsoluteObject> discoveredObjects = new();

        /// <summary>
        /// Updates the player's absolute position by adding delta values (movement).
        /// Called when the player successfully moves in the game.
        /// </summary>
        /// <param name="deltaX">Change in X position (positive = right, negative = left)</param>
        /// <param name="deltaY">Change in Y position (positive = up, negative = down)</param>
        public static void UpdatePlayerPosition(int deltaX, int deltaY)
        {
            CurrentAbsoluteX += deltaX;
            CurrentAbsoluteY += deltaY;
            Console.WriteLine($"Player moved to absolute position: ({CurrentAbsoluteX}, {CurrentAbsoluteY})");
        }

        /// <summary>
        /// Used for initialization (Setting the player's absolute position to 0, 0).
        /// </summary>
        /// <param name="absoluteX">New absolute X position</param>
        /// <param name="absoluteY">New absolute Y position</param>
        public static void SetPlayerPosition(int absoluteX, int absoluteY)
        {
            CurrentAbsoluteX = absoluteX;
            CurrentAbsoluteY = absoluteY;
            Console.WriteLine($"Player position set to: ({CurrentAbsoluteX}, {CurrentAbsoluteY})");
        }

        /// <summary>
        /// Converts a list of objects with relative coordinates to objects with absolute coordinates.
        /// Each object's relative position is converted to world coordinates based on current player position.
        /// </summary>
        /// <param name="relativeObjects">List of objects from API responses with relative coordinates</param>
        /// <returns>List of AbsoluteObject instances with both relative and absolute coordinates</returns>
        public static List<AbsoluteObject> ConvertToAbsolute(List<ClassLibrary.Objects.Object> relativeObjects)
        {
            return relativeObjects.Select(obj =>
            {
                int absX = CurrentAbsoluteX + obj.CoordX;
                int absY = CurrentAbsoluteY + obj.CoordY;
                return new AbsoluteObject
                {
                    AbsoluteX = absX,
                    AbsoluteY = absY,
                    RelativeX = obj.CoordX,
                    RelativeY = obj.CoordY,
                    Name = obj.ObjectName,
                    Type = obj.ObjectType
                };
            }).ToList();
        }

        /// <summary>
        /// Searches for the starting position marker (INFO_STARTPOS) in a list of objects
        /// and uses it to establish the absolute coordinate system. The start position
        /// is used as a reference point to calibrate absolute coordinates.
        /// </summary>
        /// <param name="lookObjects">List of objects from a Look API response</param>
        /// <returns>True if start position was found and player position was set, false otherwise</returns>
        public static bool FindAndSetStartPosition(List<ClassLibrary.Objects.Object> lookObjects)
        {
            // Find the start position marker in the object list
            var startPos = lookObjects.FirstOrDefault(obj => obj.ObjectType == "INFO_STARTPOS");
            if (startPos != null)
            {
                // Use the start position's relative coordinates to calculate our absolute position
                SetPlayerPosition(-startPos.CoordX, -startPos.CoordY);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds or updates objects in the global discovery map. 
        /// Objects at the same coordinates will be overwritten with new information.
        /// </summary>
        /// <param name="objects">List of AbsoluteObject instances to add to the global map</param>
        public static void AddToGlobalMap(List<AbsoluteObject> objects)
        {
            foreach (AbsoluteObject obj in objects)
            {
                var key = (obj.AbsoluteX, obj.AbsoluteY);
                discoveredObjects[key] = obj; // Overwrites existing object at same coordinates
            }
            Console.WriteLine($"Global map now contains {discoveredObjects.Count} discovered objects");
        }

        /// <summary>
        /// Clears all discovered objects from the global map. 
        /// Should be called when starting a new game to ensure the map doesn't contain data from previous sessions.
        /// </summary>
        public static void ClearGlobalMap()
        {
            discoveredObjects.Clear();
            Console.WriteLine("Global map cleared");
        }

        /// <summary>
        /// Generates statistics about the discovered objects in the global map.
        /// Counts objects by type and provides a total count.
        /// </summary>
        /// <returns>Dictionary with object type names as keys and counts as values, plus "Total Objects"</returns>
        public static Dictionary<string, int> GetMapStatistics()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();

            // Group objects by type and count each group
            var objectTypes = discoveredObjects.Values
                .GroupBy(obj => obj.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            // Add each object type count to the statistics
            foreach (var kvp in objectTypes)
            {
                stats[kvp.Key] = kvp.Value;
            }

            // Add total object count for quick reference
            stats["Total Objects"] = discoveredObjects.Count;
            return stats;
        }
    }

    /// <summary>
    /// Represents a game object with both relative and absolute coordinate information.
    /// </summary>
    public class AbsoluteObject
    {
        /// <summary>
        /// Absolute X coordinate in the world coordinate system
        /// </summary>
        public int AbsoluteX { get; set; }

        /// <summary>
        /// Absolute Y coordinate in the world coordinate system
        /// </summary>
        public int AbsoluteY { get; set; }

        /// <summary>
        /// Original relative X coordinate from the API response
        /// </summary>
        public int RelativeX { get; set; }

        /// <summary>
        /// Original relative Y coordinate from the API response
        /// </summary>
        public int RelativeY { get; set; }

        /// <summary>
        /// Name of the object
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of the object
        /// </summary>
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// Player's current position from API
    /// </summary>
    public class CurrentPosition
    {
        public int AbsoluteX { get; set; }
        public int AbsoluteY { get; set; }
    }
}
