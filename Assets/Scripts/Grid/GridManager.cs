using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;

    [Header("World Layout")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 originWorldPosition = Vector3.zero;

    private CombatUnit[,] occupancy;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public Vector3 OriginWorldPosition => originWorldPosition;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Keep the convention :

        // [row, col] == [i, j] == [height, width] == [y, x]
        // For storage loops
        occupancy = new CombatUnit[height, width];

        // and (x, y) for GridCoord and world conversion 
    }

    // Helper methods 
    // ----------------------------------------------

    public bool IsInside(GridCoord coord)
    {
        return coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;
    }
    public bool IsOccupied(GridCoord coord)
    {
        if (!IsInside(coord)) { return false; }
        return occupancy[coord.y, coord.x] != null;
    }

    public CombatUnit GetUnitAt(GridCoord coord)
    {
        if (!IsInside(coord)) { return null; }

        return occupancy[coord.y, coord.x];
    }

    public bool IsWalkable(GridCoord coord)
    {
        return IsInside(coord) && !IsOccupied(coord);
    }

    public bool TryPlaceUnit(CombatUnit unit, GridCoord coord)
    {
        if (unit == null || !IsWalkable(coord)) {
            return false;
        }

        occupancy[coord.y, coord.x] = unit;
        unit.SetGridPosition(coord);

        return true;
    }

    public bool TryMoveUnit(CombatUnit unit, GridCoord targetCoord)
    {
        if (unit == null || !unit.IsAlive) { return false; }
        if (!IsWalkable(targetCoord)) { return false; }

        GridCoord from = unit.GridPosition;

        if (!IsInside(from)) { return false; }
        if (occupancy[from.y, from.x] != unit) { return false; }

        occupancy[from.y, from.x] = null;
        occupancy[targetCoord.y, targetCoord.x] = unit;
        unit.SetGridPosition(targetCoord);

        return true;
    }

    public void RemoveUnit(CombatUnit unit)
    {
        if (unit == null) { return; }

        GridCoord coord = unit.GridPosition;

        if (!IsInside(coord)) { return; }

        if (occupancy[coord.y, coord.x] == unit) {
            occupancy[coord.y, coord.x] = null;
        }
    }

    public List<GridCoord> GetNeighborsList(GridCoord coord)
    {
        List<GridCoord> neighbors = new();

        // Generates neighbors in board space
        GridCoord up = new(coord.x, coord.y + 1);
        GridCoord down = new(coord.x, coord.y - 1);
        GridCoord right = new(coord.x + 1, coord.y);
        GridCoord left = new(coord.x - 1, coord.y);

        if (IsInside(right)) neighbors.Add(right);
        if (IsInside(left)) neighbors.Add(left);
        if (IsInside(up)) neighbors.Add(up);
        if (IsInside(down)) neighbors.Add(down);

        return neighbors;
    }

    // World coordinates (float)
    public Vector3 ConvertGridToWorld(GridCoord coord)
    {
        return originWorldPosition + new Vector3(coord.x * cellSize, 0f, coord.y * cellSize);
    }

    // Grid coordinate (int)
    public GridCoord ConvertWorldToGrid(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - originWorldPosition;

        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.z / cellSize);

        return new GridCoord(x, y);
    }

    [ContextMenu("Debug Print Occupancy")]
    public void DebugPrintOccupancy()
    {
        for (int row = 0; row < height; row++)
        {
            string line = "";

            for (int col = 0; col < width; col++)
            {
                if (occupancy[row, col] == null)
                {
                    line += "[ ]";
 
                }
                else
                {
                    if (occupancy[row, col].OwnerFaction.Type.Equals(FactionType.Player))
                    {
                        line += "[P]";
                    }
                    else
                    {
                        line += "[E]";
                    }
                }

            }

            Debug.Log(line);
        }
    }

}
