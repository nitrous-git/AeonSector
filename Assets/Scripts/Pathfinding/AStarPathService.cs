using System.Collections.Generic;
using UnityEngine;

public class AStarPathService 
{
    // -------------------------
    //  PathNode 
    // -------------------------
    // This is a inner path node class for simplicity 
    private class PathNode
    {
        public GridCoord coord;
        public bool isWalkable;

        public int gCost;             // Distance from start
        public int hCost;             // Distance (heuristic) to end
        public int fCost;             // gCost + hCost
        public PathNode cameFromNode; // For path reconstruction

        public PathNode(GridCoord coord, bool isWalkable)
        {
            this.coord = coord;
            this.isWalkable = isWalkable;
            gCost = int.MaxValue;
            hCost = 0;
            fCost = int.MaxValue;
            cameFromNode = null;
        }

        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }
    }

    public List<GridCoord> FindPath(TilemapBoardAdapter board, GridCoord start, GridCoord goal)
    {
        List<GridCoord> empty = new();

        if (!board.IsInside(start) || !board.IsInside(goal))
        {
            Debug.LogWarning("AStarPathService: Start or goal is out of map bounds.");
            return empty;
        }

        // Start can be occupied by the selected unit, so only check base walkability.
        if (!board.HasBaseWalkable(start))
        {
            Debug.LogWarning("AStarPathService: Start tile has no base walkable tile.");
            return empty;
        }

        // Goal should be fully walkable: base walkable + not occupied.
        if (!board.IsWalkable(goal))
        {
            Debug.LogWarning("AStarPathService: Goal tile is blocked.");
            return empty;
        }

        if (start.x == goal.x && start.y == goal.y)
        {
            return new List<GridCoord> { start };
        }

        int rows = board.Height;
        int cols = board.Width;

        // nodes[y, x]
        PathNode[,] nodes = new PathNode[rows, cols];

        // Create all nodes 
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                GridCoord coord = new GridCoord(j, i);

                // First tile
                bool isStart = coord.x == start.x && coord.y == start.y;

                // Important:
                // Start tile is occupied by the moving unit, so it must be allowed as the path origin.
                bool walkable = isStart ? board.HasBaseWalkable(coord) : board.IsWalkable(coord);

                nodes[i, j] = new PathNode(coord, walkable);
            }
        }

        // Prepare lists
        List<PathNode> openList = new();
        HashSet<PathNode> closedSet = new();

        //// Initialize all nodes
        //for (int i = 0; i < rows; i++)
        //{
        //    for (int j = 0; j < cols; j++)
        //    {
        //        PathNode node = nodes[i, j];
        //        node.gCost = int.MaxValue;
        //        node.cameFromNode = null;
        //        node.CalculateFCost();
        //    }
        //}

        // Setup start/end goal
        PathNode startNode = nodes[start.y, start.x];
        PathNode endNode = nodes[goal.y, goal.x];

        // Start node init
        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, endNode);
        startNode.CalculateFCost();

        // Put start node in openList
        openList.Add(startNode);

        // -------------------------
        // A* search loop
        // -------------------------
        while (openList.Count > 0)
        {
            // 1) Get node with lowest fCost
            PathNode currentNode = GetLowestFCostNode(openList);

            // 2) If this is our endNode => path found
            if (currentNode == endNode)
            {
                // Reached the final node
                return CalculatePath(endNode);
            }

            // 3) Move currentNode from openList to closedList
            openList.Remove(currentNode);
            closedSet.Add(currentNode);

            // 4) Check neighbors
            foreach (GridCoord neighborCoord in board.GetNeighborsList(currentNode.coord))
            {
                PathNode neighborNode = nodes[neighborCoord.y, neighborCoord.x];

                // a) If neighbor is in closedList => skip
                if (closedSet.Contains(neighborNode)) { continue; }

                // b) If not walkable => move to closedList & skip
                if (!neighborNode.isWalkable) { continue; }

                // c) Compute tentative GCost
                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighborNode);

                // d) If we found a cheaper path to neighbor => update & add to openList
                if (tentativeGCost < neighborNode.gCost)
                {
                    neighborNode.cameFromNode = currentNode;
                    neighborNode.gCost = tentativeGCost;
                    neighborNode.hCost = CalculateDistanceCost(neighborNode, endNode);
                    neighborNode.CalculateFCost();

                    if (!openList.Contains(neighborNode))
                    {
                        openList.Add(neighborNode);
                    }
                }
            }
        }

        // Out of nodes on the openList
        return empty;
    }

    // -------------------------
    //  Utilities
    // -------------------------

    // this is the Manhattan distance 
    private int CalculateDistanceCost(PathNode a, PathNode b)
    {
        int xDistance = Mathf.Abs(a.coord.x - b.coord.x);
        int yDistance = Mathf.Abs(a.coord.y - b.coord.y);
        // For purely orthogonal movement, Manhattan is simplest:
        return xDistance + yDistance;
    }

    private PathNode GetLowestFCostNode(List<PathNode> nodeList)
    {
        PathNode lowestFCostNode = nodeList[0];
        for (int i = 1; i < nodeList.Count; i++)
        {
            PathNode candidate = nodeList[i];

            // Tie-breaker using hCost.
            // When two nodes have the same fCost, choose the one closer to the goal
            if (candidate.fCost < lowestFCostNode.fCost)
            {
                lowestFCostNode = candidate;
            }
            else if (candidate.fCost == lowestFCostNode.fCost && candidate.hCost < lowestFCostNode.hCost)
            {
                lowestFCostNode = candidate;
            }
        }
        return lowestFCostNode;
    }

    // Path Reconstruction
    private List<GridCoord> CalculatePath(PathNode endNode)
    {
        // Reconstruct path by going backwards from endNode
        List<GridCoord> path = new List<GridCoord>();
        PathNode currentNode = endNode;
        while (currentNode != null)
        {
            path.Add(currentNode.coord);
            currentNode = currentNode.cameFromNode;
        }
        path.Reverse();
        return path;
    }
}
