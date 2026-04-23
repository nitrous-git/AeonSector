using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * Responsibility :
 * - given a start tile and move range
 * - return all reachable tiles using BFS
 * - ignore occupied cells except the starting tile
 */
public class ReachableTileService 
{
    public HashSet<GridCoord> GetReachableTiles(GridManager gridManager, GridCoord startCoord, int moveRange)
    {
        HashSet<GridCoord> reachable = new();
        HashSet<GridCoord> visited = new();
        Queue<(GridCoord coord, int distance)> queue = new();

        visited.Add(startCoord);
        queue.Enqueue((startCoord, 0));

        while (queue.Count > 0)
        {
            (GridCoord current, int distance) = queue.Dequeue();

            if (distance > moveRange)
                continue;

            reachable.Add(current);

            if (distance == moveRange)
                continue;

            foreach (GridCoord neighbor in gridManager.GetNeighborsList(current))
            {
                if (visited.Contains(neighbor))
                    continue;

                bool isStartTile = neighbor.x == startCoord.y && neighbor.y == startCoord.y;
                bool canTraverse = isStartTile || gridManager.IsWalkable(neighbor);

                if (!canTraverse)
                    continue;

                visited.Add(neighbor);
                queue.Enqueue((neighbor, distance + 1));
            }
        }

        reachable.Remove(startCoord);
        return reachable;
    }
}
