using System;
using System.Collections.Generic;

public enum AttackShape
{
    Diamond,
    CardinalLine
}

public class AttackRangeService
{
    public HashSet<GridCoord> GetAttackRangeCells(TilemapBoardAdapter board, CombatUnit attacker, CommandMode commandMode)
    {
        HashSet<GridCoord> empty = new();

        if (board == null || attacker == null || attacker.Stats == null)
            return empty;

        switch (commandMode)
        {
            case CommandMode.MeleeAttack:
                return GetDiamondRangeCells(board, attacker.GridPosition, attacker.Stats.MeleeMinAttackRange, attacker.Stats.MeleeMaxAttackRange);

            case CommandMode.RangedAttack:
                return GetCardinalLineRangeCells(board, attacker.GridPosition, attacker.Stats.RangedMinAttackRange, attacker.Stats.RangedMaxAttackRange);

            default:
                return empty;
        }
    }

    private HashSet<GridCoord> GetDiamondRangeCells(
        TilemapBoardAdapter board,
        GridCoord origin,
        int minRange,
        int maxRange)
    {
        HashSet<GridCoord> cells = new();

        int min = Math.Max(0, minRange);
        int max = Math.Max(min, maxRange);

        for (int dx = -max; dx <= max; dx++)
        {
            for (int dy = -max; dy <= max; dy++)
            {
                int distance = Math.Abs(dx) + Math.Abs(dy);

                if (distance < min || distance > max)
                    continue;

                GridCoord coord = new GridCoord(origin.x + dx, origin.y + dy);

                if (!board.IsInside(coord))
                    continue;

                if (board.BlocksAttackLine(coord))
                    continue;

                cells.Add(coord);
            }
        }

        return cells;
    }

    private HashSet<GridCoord> GetCardinalLineRangeCells(
        TilemapBoardAdapter board,
        GridCoord origin,
        int minRange,
        int maxRange)
    {
        HashSet<GridCoord> cells = new();

        GridCoord[] directions =
        {
            new GridCoord(1, 0),
            new GridCoord(-1, 0),
            new GridCoord(0, 1),
            new GridCoord(0, -1)
        };

        foreach (GridCoord direction in directions)
        {
            for (int distance = 1; distance <= maxRange; distance++)
            {
                GridCoord coord = new GridCoord(
                    origin.x + direction.x * distance,
                    origin.y + direction.y * distance
                );

                if (!board.IsInside(coord))
                    break;

                if (board.BlocksAttackLine(coord))
                    break;

                CombatUnit unitAtCell = board.GetUnitAt(coord);

                if (distance >= minRange)
                {
                    cells.Add(coord);
                }

                // Any unit blocks further projectile travel.
                // Enemy can be targeted.
                // Friendly blocks the line.
                if (unitAtCell != null)
                    break;
            }
        }

        return cells;
    }
}