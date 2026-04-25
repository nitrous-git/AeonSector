using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private TilemapBoardAdapter board;
    [SerializeField] private Camera mainCamera;

    [Header("Selection")]
    [SerializeField] private LayerMask unitLayerMask;
 
    private readonly ReachableTileService reachableTileService = new();
    private readonly AStarPathService pathService = new();

    private HashSet<GridCoord> reachableCells = new();
    public CombatUnit selectedUnit;

    private PlayerInputMode inputMode = PlayerInputMode.None;
    private GridCoord? hoveredCoord = null;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (turnManager == null || board == null || mainCamera == null)
            return;

        if (!turnManager.IsPlayerTurn())
            return;

        if (inputMode == PlayerInputMode.UnitMoving)
            return;

        HandleHoverPreview();

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelCurrentMode();
        }
    }

    private void HandleLeftClick()
    {
        Vector3 worldPos = GetMouseWorldPosition();

        if (inputMode == PlayerInputMode.ChoosingMoveTarget && selectedUnit != null)
        {
            GridCoord clickedCoord = board.ConvertWorldToGrid(worldPos);

            if (reachableCells.Contains(clickedCoord) && !(clickedCoord.x == selectedUnit.GridPosition.x && clickedCoord.y == selectedUnit.GridPosition.y))
            {
                CommitMove(clickedCoord);
                return;
            }
        }

        Collider2D hit = Physics2D.OverlapPoint(worldPos, unitLayerMask);
        if (hit == null)
        {
            ClearSelection();
            return;
        }

        CombatUnit clickedUnit = hit.GetComponent<CombatUnit>();
        if (clickedUnit == null)
        {
            ClearSelection();
            return;
        }

        if (clickedUnit.OwnerFaction != turnManager.PlayerFaction)
        {
            ClearSelection();
            return;
        }

        if (!clickedUnit.IsAlive)
        {
            ClearSelection();
            return;
        }

        //Debug.Log($"SelectedUnit : {clickedUnit.name}");
        SelectUnit(clickedUnit);
    }

    // ----------------------------
    // Selection
    // ----------------------------

    private void SelectUnit(CombatUnit clickedUnit)
    {
        selectedUnit = clickedUnit;

        //GridCoord inferredFromWorld = board.ConvertWorldToGrid(clickedUnit.transform.position);
        //Debug.Log(
        //    $"Selected {clickedUnit.name} | Stored Grid:{clickedUnit.GridPosition} | " +
        //    $"Inferred From World:{inferredFromWorld} | World:{clickedUnit.transform.position}"
        //);

        reachableCells = reachableTileService.GetReachableTiles(board, clickedUnit.GridPosition, clickedUnit.Stats.MoveRange);

        board.ClearHighlights();
        board.ShowCells(reachableCells);
        //Debug.Log($"Selected : {clickedUnit.name} at position : {clickedUnit.GridPosition}");

        inputMode = clickedUnit.CanMove ? PlayerInputMode.ChoosingMoveTarget : PlayerInputMode.UnitSelected;

        hoveredCoord = null; 
    }


    // ----------------------------
    // Move Flow
    // ----------------------------

    private void CommitMove(GridCoord targetCoord)
    {
        if (selectedUnit == null)
        {
            return;
        }

        if (!selectedUnit.CanMove)
        {
            return;
        }

        List<GridCoord> path = pathService.FindPath(board, selectedUnit.GridPosition, targetCoord);
        if (path.Count <= 1)
        {
            return;
        }

        StartCoroutine(ExecuteMove(selectedUnit, path));
    }

    private IEnumerator ExecuteMove(CombatUnit unit, List<GridCoord> path)
    {
        inputMode = PlayerInputMode.UnitMoving;

        board.ShowPath(path);

        GridCoord destination = path[path.Count - 1];

        UnitMover mover = selectedUnit.GetComponent<UnitMover>();
        yield return mover.MoveAlongPath(board, path);

        bool moved = board.TryMoveUnit(unit, destination);
        if (!moved)
        {
            Debug.LogWarning($"Failed to finalize move for {unit.name} to {destination}");
        }
        else
        {
            Debug.Log("MarkMoved for this unit.");
            unit.MarkMoved();
        }

        board.ClearHighlights();
        board.ClearPath();
        reachableCells.Clear();
        hoveredCoord = null;

        selectedUnit = unit;

        if (unit.CanMove)
        {
            SelectUnit(unit);
        }
        else
        {
            inputMode = PlayerInputMode.UnitSelected;
        }
    }

    // --------------------
    // Tile overlay preview
    // --------------------
    private void HandleHoverPreview()
    {
        if (inputMode != PlayerInputMode.ChoosingMoveTarget || selectedUnit == null)
            return;

        GridCoord coord = board.ConvertWorldToGrid(GetMouseWorldPosition());

        if (hoveredCoord.HasValue && hoveredCoord.Value.x == coord.x && hoveredCoord.Value.y == coord.y)
        {
            return;
        }

        hoveredCoord = coord;
        board.ClearPath();

        if (!reachableCells.Contains(coord))
            return;

        if (coord.x == selectedUnit.GridPosition.x && coord.y == selectedUnit.GridPosition.y)
            return;

        List<GridCoord> path = pathService.FindPath(board, selectedUnit.GridPosition, coord);
        if (path.Count > 0)
        {
            board.ShowPath(path);
        }
    }


    // --------------------
    // Helpers 
    // --------------------
    private void CancelCurrentMode()
    {
        if (inputMode == PlayerInputMode.UnitMoving)
            return;

        ClearSelection();
    }

    private void ClearSelection()
    {
        selectedUnit = null;
        reachableCells.Clear();
        hoveredCoord = null;
        inputMode = PlayerInputMode.None;
        board.ClearHighlights();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -mainCamera.transform.position.z;
        Vector3 world = mainCamera.ScreenToWorldPoint(mouse);
        world.z = 0f;
        return world;
    }

    // { }
}



