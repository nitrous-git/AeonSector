using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBoardAdapter : MonoBehaviour
{

    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap blockTilemap;
    [SerializeField] private Tilemap highlightTilemap;
    [SerializeField] private Tilemap pathTilemap;

    [Header("Highlight Tiles")]
    [SerializeField] private TileBase moveHighlightTile;
    [SerializeField] private TileBase pathHighlightTile;
    [SerializeField] private TileBase attackHighlightTile;

    private BoundsInt bounds;

    // Keep the convention :
    // [row, col] == [i, j] == [height, width] == [y, x] : For storage loops
    // and (x, y) for GridCoord and world conversion 
    private bool[,] baseWalkable;
    private CombatUnit[,] occupancy;

    public int Width => bounds.size.x;
    public int Height => bounds.size.y;
    public BoundsInt Bounds => bounds;

    private void Awake()
    {
        BuildBoard();
    }

    public void BuildBoard()
    {
        if (groundTilemap == null)
        {
            Debug.LogError("TilemapBoardAdapter: Ground Tilemap is not assigned");
            return;
        }

        groundTilemap.CompressBounds();
        bounds = groundTilemap.cellBounds;

        Debug.Log($"size x {bounds.size.x}, size y {bounds.size.y}" );

        baseWalkable = new bool[bounds.size.y, bounds.size.x];
        occupancy = new CombatUnit[bounds.size.y, bounds.size.x];

        for (int cellY = bounds.yMin; cellY < bounds.yMax; cellY++)
        {
            for (int cellX = bounds.xMin; cellX < bounds.xMax; cellX++)
            {
                Vector3Int cell = new Vector3Int(cellX, cellY, 0);

                bool hasGround = groundTilemap.HasTile(cell);
                bool blocked = blockTilemap != null && blockTilemap.HasTile(cell);

                int localX = cellX - bounds.xMin;
                int localY = cellY - bounds.yMin;

                baseWalkable[localY, localX] = hasGround && !blocked;
            }
        }

        ClearHighlights();
    }

    // --------------------------------------------------
    // Board queries
    // --------------------------------------------------

    public bool IsInside(GridCoord coord)
    {
        return coord.x >= 0 && coord.x < Width && coord.y >= 0 && coord.y < Height;
    }

    public bool IsOccupied(GridCoord coord)
    {
        if (!IsInside(coord)) { return false; }
        return occupancy[coord.y, coord.x] != null;
    }

    public CombatUnit GetUnitAt(GridCoord coord)
    {
        if (!IsInside(coord)) 
        { 
            return null; 
        }
        return occupancy[coord.y, coord.x];
    }

    // Does this cell have valid ground, no blocking tile, and no unit currently on it?
    public bool IsWalkable(GridCoord coord)
    {
        return IsInside(coord) && baseWalkable[coord.y, coord.x] && occupancy[coord.y, coord.x] == null;
    }

    // Does this cell have valid ground and no blocking tile?
    public bool HasBaseWalkable(GridCoord coord)
    {
        if (!IsInside(coord)) 
        { 
            return false; 
        }
        return baseWalkable[coord.y, coord.x];
    }

    // --------------------------------------------------
    // Unit occupancy
    // --------------------------------------------------

    public bool TryPlaceUnit(CombatUnit unit, GridCoord coord)
    {
        if (unit == null || !IsWalkable(coord)) 
        {
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

        if (occupancy[coord.y, coord.x] == unit) 
        {
            occupancy[coord.y, coord.x] = null;
        }
    }

    // --------------------------------------------------
    // Neighbors
    // --------------------------------------------------

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

    // --------------------------------------------------
    // Grid <-> World conversion
    // GridCoord is normalized board-space:
    // (0,0) mapping to tilemap cell (bounds.xMin, bounds.yMin)
    // --------------------------------------------------

    public Vector3 ConvertGridToWorld(GridCoord coord)
    {
        Vector3Int tileCell = new Vector3Int(coord.x + bounds.xMin, coord.y + bounds.yMin,0);

        return groundTilemap.GetCellCenterWorld(tileCell);
    }

    public GridCoord ConvertWorldToGrid(Vector3 worldPosition)
    {
        Vector3Int tileCell = groundTilemap.WorldToCell(worldPosition);

        int localX = tileCell.x - bounds.xMin;
        int localY = tileCell.y - bounds.yMin;

        return new GridCoord(localX, localY);
    }

    // --------------------------------------------------
    // Highlights
    // --------------------------------------------------

    public void ClearHighlights()
    {
        if (highlightTilemap != null)
        {
            highlightTilemap.ClearAllTiles();
        }

        if (pathTilemap != null)
        {
            pathTilemap.ClearAllTiles();
        }
    }

    public void ClearPath()
    {
        if (pathTilemap != null)
        {
            pathTilemap.ClearAllTiles();
        }
    }

    public void ShowCells(IEnumerable<GridCoord> cells)
    {
        if (highlightTilemap == null || moveHighlightTile == null)
        {
            return;
        }

        highlightTilemap.ClearAllTiles();

        foreach (GridCoord coord in cells)
        {
            if (!IsInside(coord))
            {
                continue;
            }

            if (!HasBaseWalkable(coord))
            {
                continue;
            }

            Vector3Int tileCell = new Vector3Int(coord.x + bounds.xMin, coord.y + bounds.yMin, 0);
            //Debug.Log($"Set highlight tile at : ({tileCell.x}, {tileCell.y})");
            highlightTilemap.SetTile(tileCell, moveHighlightTile);
        }
    }

    public void ShowPath(IEnumerable<GridCoord> path)
    {
        if (pathTilemap == null || pathHighlightTile == null)
        {
            return;
        }

        pathTilemap.ClearAllTiles();

        foreach (GridCoord coord in path)
        {
            if (!IsInside(coord))
            {
                continue;
            }

            Vector3Int tileCell = new Vector3Int(coord.x + bounds.xMin, coord.y + bounds.yMin, 0);
            pathTilemap.SetTile(tileCell, pathHighlightTile);
        }
    }

    public void ShowSelected(GridCoord coord)
    {
        Vector3Int tileCell = new Vector3Int(coord.x + bounds.xMin, coord.y + bounds.yMin, 0);
        highlightTilemap.SetTile(tileCell, moveHighlightTile);
    }

    // Duplicate of showCells but with the attackHighlightTile (Generalize this later)
    public void ShowAttackCells(IEnumerable<GridCoord> cells)
    {
        if (highlightTilemap == null || attackHighlightTile == null)
        {
            return;
        }

        highlightTilemap.ClearAllTiles();

        foreach (GridCoord coord in cells)
        {
            if (!IsInside(coord))
            {
                continue;
            }

            if (!HasBaseWalkable(coord))
            {
                continue;
            }

            Vector3Int tileCell = new Vector3Int(coord.x + bounds.xMin, coord.y + bounds.yMin,0);
            highlightTilemap.SetTile(tileCell, attackHighlightTile);
        }
    }

    // --------------------------------------------------
    // Blocking validation
    // --------------------------------------------------
    public bool BlocksAttackLine(GridCoord coord)
    {
        if (!IsInside(coord))
            return true;

        if (!HasBaseWalkable(coord))
            return true;

        return false;
    }

    // --------------------------------------------------
    // Debug print
    // --------------------------------------------------

    [ContextMenu("Debug Print Occupancy")]
    public void DebugPrintOccupancy()
    {
        for (int row = 0; row < Height; row++)
        {
            string line = "";

            for (int col = 0; col < Width; col++)
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

    [ContextMenu("Debug Print baseWalkable")]
    public void DebugPrintBaseWalkable()
    {
        for (int row = 0; row < Height; row++)
        {
            string line = "";

            for (int col = 0; col < Width; col++)
            {
                if (baseWalkable[row, col] == false)
                {
                    line += "[ ]";

                }
                else
                {
                    line += "[X]";
                }
            }

            Debug.Log(line);
        }
    }

}
