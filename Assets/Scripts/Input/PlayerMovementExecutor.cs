using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Executes validated movement. TilemapBoardAdapter remains the only occupancy authority.
internal sealed class PlayerMovementExecutor
{
    private readonly MonoBehaviour host;
    private readonly TurnManager turnManager;
    private readonly TilemapBoardAdapter board;
    private readonly PlayerCommandState state;
    private readonly PlayerBattleUI ui;
    private readonly ReachableTileService reachableTileService = new();
    private readonly AStarPathService pathService = new();

    public PlayerMovementExecutor(MonoBehaviour host, TurnManager turnManager,
        TilemapBoardAdapter board, PlayerCommandState state, PlayerBattleUI ui)
    {
        this.host = host;
        this.turnManager = turnManager;
        this.board = board;
        this.state = state;
        this.ui = ui;
    }

    public void CommitMove(GridCoord targetCoord)
    {
        if (state.IsResolving || turnManager == null || board == null || state.SelectedUnit == null)
            return;

        CombatUnit unit = state.SelectedUnit;
        if (!turnManager.IsPlayerTurn()
            || unit.OwnerFaction != turnManager.PlayerFaction
            || unit.Stats == null
            || !unit.CanMove)
        {
            ResetMoveState(unit);
            return;
        }

        if (!board.IsReady
            || !board.IsInside(unit.GridPosition)
            || !board.IsInside(targetCoord)
            || !board.HasBaseWalkable(targetCoord)
            || !board.IsWalkable(targetCoord)
            || !state.ReachableCells.Contains(targetCoord))
        {
            ResetMoveState(unit);
            return;
        }

        HashSet<GridCoord> currentReachableCells = reachableTileService.GetReachableTiles(board, unit.GridPosition, unit.Stats.MoveRange);
        if (!currentReachableCells.Contains(targetCoord))
        {
            Debug.LogWarning($"Rejected move for {unit.name}: destination {targetCoord} is no longer reachable.");
            ResetMoveState(unit);
            return;
        }

        List<GridCoord> path = pathService.FindPath(board, unit.GridPosition, targetCoord);
        if (path.Count <= 1)
        {
            ResetMoveState(unit);
            return;
        }

        UnitMover mover = unit.GetComponent<UnitMover>();
        if (mover == null || mover.IsMoving)
        {
            Debug.LogWarning($"Rejected move for {unit.name}: no available UnitMover or movement is already in progress.");
            ResetMoveState(unit);
            return;
        }

        state.IsResolving = true;
        host.StartCoroutine(ResolveUnitMove(unit, path));
    }

    private IEnumerator ResolveUnitMove(CombatUnit unit, List<GridCoord> path)
    {
        if (unit == null || path == null || path.Count <= 1 || board == null || !board.IsReady)
        {
            ResetMoveState(unit);
            yield break;
        }

        if (turnManager == null
            || !turnManager.IsPlayerTurn()
            || unit != state.SelectedUnit
            || unit.OwnerFaction != turnManager.PlayerFaction
            || unit.Stats == null
            || !unit.CanMove
            || !board.IsInside(unit.GridPosition))
        {
            ResetMoveState(unit);
            yield break;
        }

        GridCoord origin = unit.GridPosition;
        GridCoord destination = path[path.Count - 1];
        HashSet<GridCoord> currentReachableCells = reachableTileService.GetReachableTiles(board, origin, unit.Stats.MoveRange);
        if (!board.IsInside(destination) || !currentReachableCells.Contains(destination))
        {
            Debug.LogWarning($"Rejected move for {unit.name}: destination {destination} is no longer valid.");
            ResetMoveState(unit);
            yield break;
        }

        List<GridCoord> currentPath = pathService.FindPath(board, origin, destination);
        if (currentPath.Count <= 1)
        {
            Debug.LogWarning($"Rejected move for {unit.name}: no valid path remains to {destination}.");
            ResetMoveState(unit);
            yield break;
        }

        UnitMover mover = unit.GetComponent<UnitMover>();
        if (mover == null || mover.IsMoving)
        {
            Debug.LogWarning($"Rejected move for {unit.name}: no available UnitMover or movement is already in progress.");
            ResetMoveState(unit);
            yield break;
        }

        bool moved = board.TryMoveUnit(unit, destination);
        if (!moved)
        {
            Debug.LogWarning($"Failed to reserve move for {unit.name} to {destination}");
            ResetMoveState(unit);
            yield break;
        }

        ui.ShowPath(currentPath);
        SFXManager.UnitMove();
        yield return mover.MoveAlongPath(board, currentPath);

        Vector3 destinationWorld = board.ConvertGridToWorld(destination);
        if ((unit.transform.position - destinationWorld).sqrMagnitude > 0.0001f)
        {
            Debug.LogWarning($"Movement animation for {unit.name} did not reach {destination}; restoring the reserved board cell.");
            board.TryMoveUnit(unit, origin);
            unit.transform.position = board.ConvertGridToWorld(origin);
            ResetMoveState(unit);
            yield break;
        }

        Debug.Log("MarkMoved for this unit.");
        unit.MarkMoved();

        ResetMoveState(unit);
    }

    private void ResetMoveState(CombatUnit unit)
    {
        ui.ClearOverlays();
        state.ResetCommand();
        state.IsResolving = false;
        if (!state.IsValidSelection(unit))
        {
            state.ClearSelectedUnit();
            ui.HideMenu();
            return;
        }
        state.RestoreSelectedUnit(unit);
        ui.ShowSelected(unit);
        ui.RefreshPlayer(unit);
    }
}
