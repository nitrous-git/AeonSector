using System.Collections.Generic;
using UnityEngine;

// Presentation only: cards, menu and board overlays never decide or commit gameplay actions.
internal sealed class PlayerBattleUI
{
    private readonly TilemapBoardAdapter board;
    private readonly ActionMenuUI menu;
    private readonly UnitCardUI playerCard;
    private readonly UnitCardUI enemyCard;
    private readonly AStarPathService paths = new();

    public PlayerBattleUI(TilemapBoardAdapter board, ActionMenuUI menu,
        UnitCardUI playerCard, UnitCardUI enemyCard)
    {
        this.board = board;
        this.menu = menu;
        this.playerCard = playerCard;
        this.enemyCard = enemyCard;
    }

    public void HideMenu() => menu?.Hide();
    public void ShowEnemy(CombatUnit unit) => enemyCard?.Show(unit);
    public void RefreshEnemy() => enemyCard?.Refresh();
    public void ShowSelected(CombatUnit unit) => board?.ShowSelected(unit.GridPosition);
    public void ShowPath(List<GridCoord> path) => board?.ShowPath(path);
    public void ClearPath() => board?.ClearPath();

    public void ClearOverlays()
    {
        board?.ClearHighlights();
        board?.ClearPath();
    }

    public void ClearSelection()
    {
        HideMenu();
        enemyCard?.Hide();
        ClearOverlays();
    }

    public void ShowPlayerSelection(CombatUnit unit)
    {
        menu?.Show();
        menu?.Refresh(unit);
        playerCard?.Show(unit);
        enemyCard?.Hide();
        ClearOverlays();
        ShowSelected(unit);
    }

    public void ShowInspectedEnemy(CombatUnit unit)
    {
        board?.ShowAttackCells(new List<GridCoord> { unit.GridPosition });
        ShowEnemy(unit);
    }

    public void RefreshPlayer(CombatUnit unit)
    {
        menu?.Refresh(unit);
        playerCard?.Refresh();
    }

    public void RefreshAfterDamage(CombatUnit target)
    {
        playerCard?.Refresh();
        if (target != null && target.IsAlive) ShowEnemy(target);
        else enemyCard?.Hide();
    }

    public void ShowCommandPreview(PlayerCommandState state)
    {
        ClearOverlays();
        if (state.Mode == CommandMode.Move) board?.ShowCells(state.ReachableCells);
        else
        {
            board?.ShowAttackCells(state.ReachableCells);
            ShowSelected(state.SelectedUnit);
        }
    }

    public void PreviewHover(PlayerCommandState state, Vector3 worldPosition)
    {
        if (state.Mode != CommandMode.Move || state.SelectedUnit == null || board == null)
            return;
        GridCoord coord = board.ConvertWorldToGrid(worldPosition);
        if (state.HoveredCoord.HasValue && state.HoveredCoord.Value.x == coord.x
            && state.HoveredCoord.Value.y == coord.y)
            return;
        state.HoveredCoord = coord;
        ClearPath();
        if (!state.ReachableCells.Contains(coord)) return;
        GridCoord origin = state.SelectedUnit.GridPosition;
        if (coord.x == origin.x && coord.y == origin.y) return;
        List<GridCoord> path = paths.FindPath(board, origin, coord);
        if (path.Count > 0) ShowPath(path);
    }
}
