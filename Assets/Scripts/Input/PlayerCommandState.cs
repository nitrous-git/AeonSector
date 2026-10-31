using System.Collections.Generic;
using UnityEngine;

// The sole live selection, command mode and player-action lock. No unit/board state is duplicated.
internal sealed class PlayerCommandState
{
    private readonly TurnManager turnManager;
    private readonly TilemapBoardAdapter board;
    private readonly PlayerBattleUI ui;
    private readonly ReachableTileService movementRange = new();
    private readonly AttackRangeService attackRange = new();

    public CombatUnit SelectedUnit { get; private set; }
    public CommandMode Mode { get; private set; }
    public bool IsResolving { get; set; }
    public HashSet<GridCoord> ReachableCells { get; private set; } = new();
    public GridCoord? HoveredCoord { get; set; }

    public PlayerCommandState(TurnManager turnManager, TilemapBoardAdapter board, PlayerBattleUI ui)
    {
        this.turnManager = turnManager;
        this.board = board;
        this.ui = ui;
    }

    public bool IsValidSelection(CombatUnit unit)
    {
        return unit != null && unit.IsAlive && unit.Stats != null
            && turnManager != null && !turnManager.IsBattleOver
            && unit.OwnerFaction == turnManager.PlayerFaction
            && board != null && board.IsReady && board.IsInside(unit.GridPosition)
            && board.GetUnitAt(unit.GridPosition) == unit;
    }

    public void ValidateSelection()
    {
        // ReferenceEquals also catches a destroyed Unity object still held in the state.
        if (!IsResolving && !ReferenceEquals(SelectedUnit, null) && !IsValidSelection(SelectedUnit))
            ClearSelection();
    }

    public void SelectUnit(CombatUnit unit)
    {
        if (IsResolving || unit == null || !unit.IsAlive || turnManager == null
            || !turnManager.IsPlayerTurn() || board == null || !board.IsReady)
            return;

        if (unit.OwnerFaction != turnManager.PlayerFaction)
        {
            ClearSelection();
            ui.ShowInspectedEnemy(unit);
            return;
        }
        if (!IsValidSelection(unit)) return;

        SelectedUnit = unit;
        ResetCommand();
        ui.ShowPlayerSelection(unit);
        Debug.Log($"Selected {unit.name}");
    }

    public void RequestCommand(CommandMode mode)
    {
        if (IsResolving || turnManager == null || !turnManager.IsPlayerTurn()
            || !IsValidSelection(SelectedUnit))
            return;
        if (mode == CommandMode.Move ? !SelectedUnit.CanMove
            : !IsAttackMode(mode) || !SelectedUnit.CanAttack)
            return;

        SFXManager.ButtonClick();
        Mode = mode;
        HoveredCoord = null;
        ReachableCells = mode == CommandMode.Move
            ? movementRange.GetReachableTiles(board, SelectedUnit.GridPosition, SelectedUnit.Stats.MoveRange)
            : attackRange.GetAttackRangeCells(board, SelectedUnit, mode);
        ui.ShowCommandPreview(this);
        Debug.Log($"{mode} mode for {SelectedUnit.name}");
    }

    public void CancelCurrentMode()
    {
        if (IsResolving) return;
        ResetCommand();
        ui.HideMenu();
        ui.ClearOverlays();
        Debug.Log("Command cancelled.");
    }

    public void ClearSelection()
    {
        if (IsResolving) return;
        ClearSelectedUnit();
        ResetCommand();
        ui.ClearSelection();
        Debug.Log("Selection cleared.");
    }

    public void EndPlayerTurn()
    {
        if (IsResolving || turnManager == null || turnManager.IsBattleOver || !turnManager.IsPlayerTurn())
            return;
        SFXManager.ButtonClick();
        ClearSelection();
        turnManager.EndCurrentTurn();
    }

    // Executors release the lock and restore/clear selection only after resolving their action.
    public void RestoreSelectedUnit(CombatUnit unit) => SelectedUnit = unit;
    public void ClearSelectedUnit() => SelectedUnit = null;

    public void ResetCommand()
    {
        Mode = CommandMode.None;
        ReachableCells.Clear();
        HoveredCoord = null;
    }

    public static bool IsAttackMode(CommandMode mode)
        => mode == CommandMode.MeleeAttack || mode == CommandMode.RangedAttack;
}
