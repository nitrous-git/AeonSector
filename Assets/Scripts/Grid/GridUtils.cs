using UnityEngine;

public class GridUtils
{
    public static int ManhattanDistance(GridCoord a, GridCoord b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
