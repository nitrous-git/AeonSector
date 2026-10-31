using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Pure deterministic decisions; never reserves cells, animates or applies damage.
public sealed class EnemyTacticalPlanner
{
    private readonly TilemapBoardAdapter board;
    private readonly TurnManager turnManager;
    private readonly AStarPathService pathService = new();
    private readonly AttackRangeService ranges = new();
    public EnemyTacticalPlanner(TilemapBoardAdapter board, TurnManager turns) { this.board = board; turnManager = turns; }
    public static CommandMode CommandFor(EnemyCombatRole role)
        => role == EnemyCombatRole.Assault ? CommandMode.MeleeAttack : role == EnemyCombatRole.Skirmisher ? CommandMode.RangedAttack : role == EnemyCombatRole.Bombardment ? CommandMode.AreaAttack : CommandMode.None;

    public List<GridCoord> FindPath(CombatUnit enemy, HashSet<GridCoord> reachable)
    {
        if (enemy.Stats.EnemyRole != EnemyCombatRole.Skirmisher) return FindApproachPath(enemy, reachable);
        var players = turnManager.PlayerFaction.UnitManager.GetLivingUnits().ToList();
        var choices = new List<(List<GridCoord> path, bool shot, int distance, GridCoord cell)>();
        foreach (var cell in reachable.OrderBy(c => c.y).ThenBy(c => c.x))
        {
            bool current = cell.x == enemy.GridPosition.x && cell.y == enemy.GridPosition.y;
            if (!current && !board.IsWalkable(cell)) continue;
            var path = current ? new List<GridCoord> { cell } : pathService.FindPath(board, enemy.GridPosition, cell);
            if (path == null || path.Count == 0) continue;
            var cells = ranges.GetAttackRangeCellsFrom(board, enemy.Stats, cell, CommandMode.RangedAttack, enemy);
            choices.Add((path, players.Any(p => cells.Contains(p.GridPosition)), GetDistanceToClosestPlayer(cell, players), cell));
        }
        // Firing solutions first, avoid adjacency, then prefer shorter movement and stable cell order.
        var shot = choices.Where(c => c.shot).OrderBy(c => c.distance <= 1).ThenBy(c => c.path.Count).ThenBy(c => c.cell.y).ThenBy(c => c.cell.x).ToList();
        if (shot.Count > 0) return shot[0].path;
        return FindApproachPath(enemy, reachable);
    }
    public List<GridCoord> FindApproachPath(CombatUnit enemy, HashSet<GridCoord> reachableCells)
    {
        List<CombatUnit> players = turnManager.PlayerFaction.UnitManager.GetLivingUnits().ToList();

        if (players.Count == 0)
        {
            return null;
        }

        GridCoord start = enemy.GridPosition;

        List<GridCoord> bestPath = null;
        int bestDistanceToPlayer = int.MaxValue;
        int bestPathLength = int.MaxValue;

        foreach (GridCoord candidate in reachableCells.OrderBy(c => c.y).ThenBy(c => c.x))
        {
            bool isCurrentCell = candidate.x == start.x && candidate.y == start.y;

            int distanceToClosestPlayer = GetDistanceToClosestPlayer(candidate, players);

            // If already adjacent, do not move away.
            if (isCurrentCell && distanceToClosestPlayer <= 1)
            {
                return new List<GridCoord> { start };
            }

            if (isCurrentCell)
            {
                continue;
            }

            List<GridCoord> path = pathService.FindPath(board, start, candidate);

            if (path == null || path.Count <= 1)
            {
                continue;
            }

            bool betterDistance = distanceToClosestPlayer < bestDistanceToPlayer;

            bool sameDistanceButShorterPath = distanceToClosestPlayer == bestDistanceToPlayer && path.Count < bestPathLength;

            if (betterDistance || sameDistanceButShorterPath)
            {
                bestDistanceToPlayer = distanceToClosestPlayer;
                bestPathLength = path.Count;
                bestPath = path;
            }
        }

        return bestPath;
    }

    private int GetDistanceToClosestPlayer(GridCoord from, List<CombatUnit> players)
    {
        int best = int.MaxValue;

        foreach (CombatUnit player in players)
        {
            if (player == null || !player.IsAlive)
            {
                continue;
            }

            int distance = GridUtils.ManhattanDistance(from, player.GridPosition);

            if (distance < best)
            {
                best = distance;
            }
        }

        return best;
    }

    public CombatUnit FindBestMeleeTarget(CombatUnit enemy, HashSet<GridCoord> attackCells)
    {
        CombatUnit bestTarget = null;
        int lowestHP = int.MaxValue;

        foreach (GridCoord cell in attackCells.OrderBy(c => c.y).ThenBy(c => c.x))
        {
            CombatUnit unitAtCell = board.GetUnitAt(cell);

            if (unitAtCell == null)
            {
                continue;
            }

            if (!unitAtCell.IsAlive)
            {
                continue;
            }

            if (unitAtCell.OwnerFaction == enemy.OwnerFaction)
            {
                continue;
            }

            if (unitAtCell.CurrentHP < lowestHP)
            {
                lowestHP = unitAtCell.CurrentHP;
                bestTarget = unitAtCell;
            }
        }

        return bestTarget;
    }

    public List<CombatUnit> FindAreaAttackTargets(CombatUnit attacker, HashSet<GridCoord> attackCells)
    {
        List<CombatUnit> targets = new List<CombatUnit>();

        foreach (GridCoord cell in attackCells.OrderBy(c => c.y).ThenBy(c => c.x))
        {
            CombatUnit unitAtCell = board.GetUnitAt(cell);

            if (unitAtCell == null)
            {
                continue;
            }

            if (!unitAtCell.IsAlive)
            {
                continue;
            }

            if (unitAtCell.OwnerFaction == attacker.OwnerFaction)
            {
                continue;
            }

            targets.Add(unitAtCell);
        }

        return targets;
    }

    public CombatUnit FindLowestHpTarget(List<CombatUnit> targets)
    {
        CombatUnit bestTarget = null;
        int bestHp = int.MaxValue;

        foreach (CombatUnit target in targets)
        {
            if (target.CurrentHP < bestHp)
            {
                bestHp = target.CurrentHP;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

}
