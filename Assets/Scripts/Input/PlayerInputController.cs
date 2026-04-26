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
 
    // Pathfind Services
    private readonly ReachableTileService reachableTileService = new();
    private readonly AStarPathService pathService = new();
    private readonly AttackRangeService attackRangeService = new();

    private HashSet<GridCoord> reachableCells = new();
    private GridCoord? hoveredCoord = null;

    // CombatUnit Action Command
    public CombatUnit selectedUnit;

    private CommandMode commandMode = CommandMode.None;
    private bool isResolvingAction = false;

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

        if (isResolvingAction)
            return;

        HandleCommandHotkeys();
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

        if (commandMode == CommandMode.Move && selectedUnit != null)
        {
            GridCoord clickedCoord = board.ConvertWorldToGrid(worldPos);

            if (reachableCells.Contains(clickedCoord) && !(clickedCoord.x == selectedUnit.GridPosition.x && clickedCoord.y == selectedUnit.GridPosition.y))
            {
                CommitMove(clickedCoord);
                return;
            }
        }

        if (IsAttackMode(commandMode) && selectedUnit != null)
        {
            GridCoord clickedCoord = board.ConvertWorldToGrid(worldPos);
            TryCommitAttackAt(clickedCoord);
            return; 
        }

        Collider2D hit = Physics2D.OverlapPoint(worldPos, unitLayerMask);
        if (hit == null)
            return;

        CombatUnit clickedUnit = hit.GetComponent<CombatUnit>();
        if (clickedUnit == null)
            return;

        if (clickedUnit.OwnerFaction != turnManager.PlayerFaction)
            return;

        if (!clickedUnit.IsAlive)
            return;

        //Debug.Log($"SelectedUnit : {clickedUnit.name}");
        SelectUnit(clickedUnit);
    }

    private void HandleCommandHotkeys()
    {
        if (selectedUnit == null)
            return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            EnterMoveMode();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            EnterMeleeAttackMode();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            EnterRangedAttackMode();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryEndPlayerTurn();
            return;
        }
    }

    // ----------------------------
    // Selection
    // ----------------------------
    private void SelectUnit(CombatUnit clickedUnit)
    {
        selectedUnit = clickedUnit;

        commandMode = CommandMode.None;
        reachableCells.Clear();
        hoveredCoord = null;

        board.ClearHighlights();
        board.ShowSelected(clickedUnit.GridPosition);
        Debug.Log($"Selected {clickedUnit.name}");
    }

    // ----------------------------
    // Move Flow
    // ----------------------------
    private void EnterMoveMode()
    {
        if (selectedUnit == null)
            return;

        if (!selectedUnit.CanMove)
        {
            Debug.Log("Selected unit cannot move.");
            return;
        }

        reachableCells = reachableTileService.GetReachableTiles(board, selectedUnit.GridPosition, selectedUnit.Stats.MoveRange);

        commandMode = CommandMode.Move;
        hoveredCoord = null;

        board.ClearHighlights();
        board.ShowCells(reachableCells);

        Debug.Log($"Move mode for {selectedUnit.name}");
    }

    private void CommitMove(GridCoord targetCoord)
    {
        if (selectedUnit == null)
            return;

        if (!selectedUnit.CanMove)
            return;

        List<GridCoord> path = pathService.FindPath(board, selectedUnit.GridPosition, targetCoord);
        if (path.Count <= 1)
        {
            return;
        }

        StartCoroutine(ResolveUnitMove(selectedUnit, path));
    }

    private IEnumerator ResolveUnitMove(CombatUnit unit, List<GridCoord> path)
    {
        isResolvingAction = true;

        board.ShowPath(path);

        GridCoord destination = path[path.Count - 1];

        bool moved = board.TryMoveUnit(unit, destination);
        if (!moved)
        {
            Debug.LogWarning($"Failed to reserve move for {unit.name} to {destination}");
        }

        UnitMover mover = selectedUnit.GetComponent<UnitMover>();
        yield return mover.MoveAlongPath(board, path);

        Debug.Log("MarkMoved for this unit.");
        unit.MarkMoved();

        board.ClearHighlights();
        board.ClearPath();
        reachableCells.Clear();
        hoveredCoord = null;

        commandMode = CommandMode.None;
        isResolvingAction = false;

        selectedUnit = unit;
    }

    // --------------------
    // Attack flow
    // --------------------
    private void EnterMeleeAttackMode()
    {
        EnterAttackMode(CommandMode.MeleeAttack);
    }

    private void EnterRangedAttackMode()
    {
        EnterAttackMode(CommandMode.RangedAttack);
    }

    private void EnterAttackMode(CommandMode attackMode)
    {
        if (selectedUnit == null)
            return;

        if (!selectedUnit.CanAttack)
        {
            Debug.Log("Selected unit cannot attack.");
            return;
        }

        commandMode = attackMode;

        reachableCells = attackRangeService.GetAttackRangeCells(board, selectedUnit, commandMode);
        //Debug.Log(reachableCells.Count);

        hoveredCoord = null;

        board.ClearHighlights();
        board.ShowAttackCells(reachableCells);
        board.ShowSelected(selectedUnit.GridPosition);

        Debug.Log($"{attackMode} mode for {selectedUnit.name}");
    }

    private bool TryCommitAttackAt(GridCoord targetCoord)
    {
        if (selectedUnit == null)
            return false;

        if (!selectedUnit.CanAttack)
            return false;

        if (!reachableCells.Contains(targetCoord))
        {
            Debug.Log("Clicked cell is outside attack range.");
            return false;
        }

        CombatUnit target = board.GetUnitAt(targetCoord);

        if (target == null || !target.IsAlive)
        {
            Debug.Log("No valid target on clicked attack cell.");
            return false;
        }

        if (target.OwnerFaction == selectedUnit.OwnerFaction)
        {
            Debug.Log("Cannot attack friendly unit.");
            return false;
        }

        CommitAttack(target);
        return true;
    }

    private void CommitAttack(CombatUnit target)
    {
        if (selectedUnit == null || target == null)
            return;

        if (!selectedUnit.CanAttack)
            return;

        StartCoroutine(ResolveUnitAttack(selectedUnit, target));
    }

    private IEnumerator ResolveUnitAttack(CombatUnit attacker, CombatUnit target)
    {
        isResolvingAction = true;

        board.ClearPath();

        int damage = attacker.Stats.GetDamageForCommand(commandMode);

        Debug.Log($"{attacker.name} uses {commandMode} on {target.name} for {damage} damage.");

        // Later:
        // - attacker animation
        // - particle FX
        // - camera shake
        // - projectile coroutine for ranged units
        yield return null;

        if (target.TakeDamage(damage))
        {
            turnManager.RemoveUnitFromBattle(target);
        }

        attacker.MarkAttacked();

        board.ClearHighlights();
        board.ClearPath();

        reachableCells.Clear();
        hoveredCoord = null;

        commandMode = CommandMode.None;
        isResolvingAction = false;

        selectedUnit = attacker;

        turnManager.RefreshBattleEndState();
    }

    // --------------------
    // End Turn 
    // --------------------
    private void TryEndPlayerTurn()
    {
        if (!turnManager.IsPlayerTurn())
            return;

        if (isResolvingAction)
            return;

        ClearSelection();
        commandMode = CommandMode.None;

        turnManager.EndCurrentTurn();
    }

    // --------------------
    // Tile overlay preview
    // --------------------
    private void HandleHoverPreview()
    {
        if (commandMode != CommandMode.Move || selectedUnit == null)
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
        if (isResolvingAction)
            return;

        commandMode = CommandMode.None;
        reachableCells.Clear();
        hoveredCoord = null;

        board.ClearHighlights();

        Debug.Log("Command cancelled.");
    }

    private void ClearSelection()
    {
        if (isResolvingAction)
            return;

        selectedUnit = null;

        commandMode = CommandMode.None;
        reachableCells.Clear();
        hoveredCoord = null;

        board.ClearHighlights();

        Debug.Log("Selection cleared.");
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -mainCamera.transform.position.z;
        Vector3 world = mainCamera.ScreenToWorldPoint(mouse);
        world.z = 0f;
        return world;
    }

    private bool IsAttackMode(CommandMode mode)
    {
        return mode == CommandMode.MeleeAttack || mode == CommandMode.RangedAttack;
    }

    // { }
}