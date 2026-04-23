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
    public HashSet<GridCoord> GetReachableTiles(TilemapBoardAdapter board, GridCoord startCoord, int moveRange)
    {
        HashSet<GridCoord> reachable = new();
        Queue<(GridCoord coord, int cost)> queue = new();

        reachable.Add(startCoord);
        queue.Enqueue((startCoord, 0));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            GridCoord currentCoord = current.coord;
            int currentCost = current.cost;

            if (currentCost >= moveRange)
                continue;

            foreach (GridCoord neighbor in board.GetNeighborsList(currentCoord))
            {
                if (reachable.Contains(neighbor))
                    continue;

                bool isStartTile = neighbor.x == startCoord.x && neighbor.y == startCoord.y;

                if (!board.HasBaseWalkable(neighbor))
                    continue;

                if (!isStartTile && board.IsOccupied(neighbor))
                    continue;

                reachable.Add(neighbor);
                queue.Enqueue((neighbor, currentCost + 1));
            }
        }

        return reachable;
    }
}
