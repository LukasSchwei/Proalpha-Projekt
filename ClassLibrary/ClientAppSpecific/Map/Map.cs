using System;
using ClassLibrary.Coordinates;
using ClassLibrary.Variables;

namespace ClassLibrary.Map;

public static class Map
{
    /// <summary>
    /// Updates the map with the received objects.
    /// </summary>
    public static async Task Update(List<AbsoluteObject> objects)
    {
        // Update all received objects in the global map
        foreach (AbsoluteObject obj in objects)
        {
            V.map[(obj.AbsoluteX, obj.AbsoluteY)] = obj;
        }
    }

    /// <summary>
    /// Adjusts the global map bounds based on the player's current position.
    /// </summary>
    /// <param name="size">The size of the bounds to adjust (Look is 5 and Move is 1).</param>
    public static async Task AdjustBounds(List<AbsoluteObject> objects, int size)
    {
        for (int i = -size; i < 0; i++)
        {
            if (!V.map.ContainsKey((V.currentPosition.AbsoluteX + i, V.currentPosition.AbsoluteY)))
            {
                V.globalMapMinX = V.currentPosition.AbsoluteX + i + 1;
                //Dialog.CreateGenericDialog("GlobalMapMinX: " + globalMapMinX, "", "", this);
            }
            if (!V.map.ContainsKey((V.currentPosition.AbsoluteX, V.currentPosition.AbsoluteY + i)))
            {
                V.globalMapMinY = V.currentPosition.AbsoluteY + i + 1;
                //Dialog.CreateGenericDialog("GlobalMapMinY: " + globalMapMinY, "", "", this);
            }
        }
        for (int i = size; i > 0; i--)
        {
            if (!V.map.ContainsKey((V.currentPosition.AbsoluteX + i, V.currentPosition.AbsoluteY)))
            {
                V.globalMapMaxX = V.currentPosition.AbsoluteX + i - 1;
                //Dialog.CreateGenericDialog("GlobalMapMaxX: " + globalMapMaxX, "", "", this);
            }
            if (!V.map.ContainsKey((V.currentPosition.AbsoluteX, V.currentPosition.AbsoluteY + i)))
            {
                V.globalMapMaxY = V.currentPosition.AbsoluteY + i - 1;
                //Dialog.CreateGenericDialog("GlobalMapMaxY: " + globalMapMaxY, "", "", this);
            }
        }
    }
}