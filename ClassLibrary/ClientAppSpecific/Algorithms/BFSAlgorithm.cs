using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Xml;
using ClassLibrary.Coordinates;
using ClassLibrary.Dialog;
using ClassLibrary.Responses;

namespace ClassLibrary.Algorithm.BFS;

public static class BFSPathfinding
{
    /// <summary>
    /// Finds the shortest path from the current position to the nearest object whose
    /// <see cref="AbsoluteObject.Type"/> matches <paramref name="type"/> using the BTS-Algorithm.
    /// </summary>
    /// <param name="objects">Dictionary containing all known map objects indexed by absolute coordinates.</param>
    /// <param name="type">Desired object type (e.g. "ORE").</param>
    /// <param name="currentPosition">Current absolute position of the player.</param>
    /// <param name="globalMapMinX">Left map boundary.</param>
    /// <param name="globalMapMinY">Top map boundary.</param>
    /// <param name="globalMapMaxX">Right map boundary.</param>
    /// <param name="globalMapMaxY">Bottom map boundary.</param>
    /// <returns>
    /// Sequence of relative moves (dx, dy) leading from start to the target or <c>null</c> if no path exists.
    /// </returns>
    public static List<(int x, int y)>? FindPath(Dictionary<(int x, int y), AbsoluteObject> objects, string? type, CurrentPosition currentPosition, int globalMapMinX, int globalMapMinY, int globalMapMaxX, int globalMapMaxY)
    {
        if (string.IsNullOrEmpty(type)) return null;

        var start = (currentPosition.AbsoluteX, currentPosition.AbsoluteY);

        // Identifies a tile as a valid target: it contains the desired object type and is not the starting tile.
        Func<(int x, int y), bool> isTarget = pos =>
            // TryGetValue returns true if the key is found, and false otherwise.
            objects.TryGetValue(pos, out var obj) && obj.Type == type && pos != start;

        // Checks if a tile can be traversed (i.e., not an obstacle).
        Func<(int x, int y), bool> isWalkable = pos =>
            !IsObstacle(objects, pos.x, pos.y, globalMapMinX, globalMapMinY, globalMapMaxX, globalMapMaxY);

        return BfsPath(start, isTarget, isWalkable/*, true*/);
    }

    /// <summary>
    /// Returns a path to the nearest tile that is still unknown (not present in <paramref name="objects"/>)
    /// and is not an obstacle. Useful for exploring fog-of-war areas.
    /// </summary>
    public static List<(int x, int y)>? FindPathToUnknown(Dictionary<(int x, int y), AbsoluteObject> objects, CurrentPosition currentPosition, int globalMapMinX, int globalMapMinY, int globalMapMaxX, int globalMapMaxY, IWin32Window owner)
    {
        var start = (currentPosition.AbsoluteX, currentPosition.AbsoluteY);

        // Ensures coordinates stay within the global map boundaries.
        bool WithinBounds((int x, int y) p) => p.x >= globalMapMinX && p.x <= globalMapMaxX && p.y >= globalMapMinY && p.y <= globalMapMaxY;

        // Tile is a target if it is within bounds and not yet discovered.
        Func<(int x, int y), bool> isTarget = pos => WithinBounds(pos) && !objects.ContainsKey(pos);

        // Tile is walkable when within bounds AND (unknown OR known but not an obstacle).
        Func<(int x, int y), bool> isWalkable = pos =>
            WithinBounds(pos) && (
                !objects.ContainsKey(pos) ||
                !IsObstacle(objects, pos.x, pos.y, globalMapMinX, globalMapMinY, globalMapMaxX, globalMapMaxY)
            );

        return BfsPath(start, isTarget, isWalkable/*, false*/);
    }

    /// <summary>
    /// Determines whether the specified coordinate should be treated as an unwalkable obstacle.
    /// Solid rock/walls as well as positions outside the map bounds are considered blocking.
    /// </summary>
    private static bool IsObstacle(Dictionary<(int x, int y), AbsoluteObject> objects, int x, int y, int mapMinX, int mapMinY, int mapMaxX, int mapMaxY)
    {
        if (objects.ContainsKey((x, y)))
        {
            if (x < mapMinX || x > mapMaxX || y < mapMinY || y > mapMaxY) return true;
            if (objects[(x, y)].Type == "SOLID_ROCK" || objects[(x, y)].Type == "SOLID_WALL") return true;
            return false;
        }
        return true;
    }

    /// <summary>
    /// The eight immediate neighbours (4-orthogonal + 4 diagonal) that can be reached in one step.
    /// </summary>
    private static readonly (int dx, int dy)[] Directions = new (int, int)[]
    {
        (-1, 0), (1, 0), (0, -1), (0, 1),   // orthogonal moves
        (-1, -1), (1, -1), (-1, 1), (1, 1)  // diagonal moves
    };

    /// <summary>
    /// Breadth-First Search implementation.
    /// </summary>
    /// <param name="start">Starting coordinate (absolute).</param>
    /// <param name="isTarget">Predicate that decides whether the current coordinate is a valid target.</param>
    /// <param name="isWalkable">Predicate that decides whether a coordinate is walkable.</param>
    /// <returns>List of relative moves (dx, dy) to the found target or <c>null</c> if unreachable.</returns>
    public static List<(int x, int y)>? BfsPath(
        (int x, int y) start,
        Func<(int x, int y), bool> isTarget,
        Func<(int x, int y), bool> isWalkable/*, bool goToEnd*/)
    {
        // FIFO queue holding frontier coordinates that still need to be explored.
        Queue<(int x, int y)> queue = new();
        // Set of already visited coordinates to prevent revisiting the same tile.
        HashSet<(int x, int y)> visited = new();
        // Parent dictionary – for every discovered coordinate we store the coordinate from which we reached it.
        // This allows efficient reconstruction of the path once the goal is reached.
        Dictionary<(int x, int y), (int x, int y)> parent = new();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (isTarget(current))
            {
                return ReconstructPath(start, current, parent/*, goToEnd*/);
            }

            // Explore neighbours of the current coordinate.
            foreach (var (dx, dy) in Directions) // iterate over the 8 neighbouring offsets
            {
                var next = (current.x + dx, current.y + dy);
                if (!visited.Contains(next) && isWalkable(next))
                {
                    visited.Add(next);
                    parent[next] = current;
                    queue.Enqueue(next);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Reconstructs the list of relative moves from <paramref name="start"/> to <paramref name="end"/>
    /// using the supplied parent dictionary.
    /// </summary>
    private static List<(int x, int y)>? ReconstructPath((int x, int y) start, (int x, int y) end, Dictionary<(int x, int y), (int x, int y)> parent/*, bool goToEnd*/)
    {
        List<(int x, int y)> moves = new();
        var current = end;
        while (current != start)
        {
            var prev = parent[current];
            moves.Add((current.x - prev.x, current.y - prev.y)); // store relative move (dx, dy)
            current = prev;
        }
        //# if (!goToEnd)
        //# moves.Remove(moves.Last()); // remove the last move if the player searches for unknown so the player don't end up running into a wall
        moves.Reverse();
        return moves.Count == 0 ? null : moves;
    }
}