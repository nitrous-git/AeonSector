using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyTurnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private TilemapBoardAdapter board;

    [Header("Timing")]
    [SerializeField] private float turnStartDelay = 0.35f;
    [SerializeField] private float previewDelay = 0.35f;
    [SerializeField] private float unitDelay = 0.25f;
    [SerializeField] private float attackDelay = 1.5f;

    [Header("Unit Cards")]
    [SerializeField] private UnitCardUI enemyCard;
    [SerializeField] private UnitCardUI playerCard;

    private readonly ReachableTileService reachableTileService = new();
    private readonly AStarPathService pathService = new();
    private readonly AttackRangeService attackRangeService = new();

    public bool IsRunning { get; private set; }

    public void BeginEnemyTurn()
    {
        if (IsRunning)
        {
            return;
        }

        StartCoroutine(RunEnemyTurn());
    }

    private IEnumerator RunEnemyTurn()
    {
        if (turnManager == null || board == null)
        {
            Debug.LogError("EnemyTurnController requires TurnManager and TilemapBoardAdapter references.");
            yield break;
        }

        IsRunning = true;

        yield return new WaitForSeconds(turnStartDelay);

        List<CombatUnit> enemies = turnManager.EnemyFaction.UnitManager.GetLivingUnits().ToList();

        foreach (CombatUnit enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            yield return ResolveEnemyUnit(enemy);

            if (turnManager.RefreshBattleEndState())
            {
                board.ClearHighlights();
                board.ClearPath();
                IsRunning = false;
                yield break;
            }

            yield return new WaitForSeconds(unitDelay);
        }

        enemyCard.Hide();
        playerCard.Hide();

        board.ClearHighlights();
        board.ClearPath();

        IsRunning = false;

        turnManager.EndCurrentTurn();
    }

    private IEnumerator ResolveEnemyUnit(CombatUnit enemy)
    {
        Debug.Log($"Enemy AI resolving unit: {enemy.name}");
        enemyCard.Show(enemy);

        SFXManager.UnitSelect();
        
        if (enemy.CanMove)
        {
            yield return TryMoveTowardPlayer(enemy);
            enemyCard.Refresh();
        }

        if (enemy == null || !enemy.IsAlive)
        {
            yield break;
        }

        if (enemy.CanAttack)
        {
            yield return TryMeleeAttack(enemy);
            enemyCard.Refresh();
            playerCard.Refresh();
        }
    }

    // --------------------------------------------------
    // Movement
    // --------------------------------------------------

    private IEnumerator TryMoveTowardPlayer(CombatUnit enemy)
    {
        HashSet<GridCoord> reachableCells = reachableTileService.GetReachableTiles(board, enemy.GridPosition, enemy.Stats.MoveRange);

        board.ClearHighlights();
        board.ClearPath();
        board.ShowCells(reachableCells);
        board.ShowSelected(enemy.GridPosition);

        yield return new WaitForSeconds(previewDelay);

        List<GridCoord> path = FindBestPathTowardAnyPlayer(enemy, reachableCells);

        if (path == null || path.Count <= 1)
        {
            SFXManager.InvalidAction();
            Debug.Log($"{enemy.name} has no useful move.");
            board.ClearPath();
            yield break;
        }

        SFXManager.UnitMove();

        board.ShowPath(path);

        yield return new WaitForSeconds(previewDelay);

        GridCoord destination = path[path.Count - 1];

        bool moved = board.TryMoveUnit(enemy, destination);

        if (!moved)
        {
            Debug.LogWarning($"Enemy AI failed to reserve move for {enemy.name} to {destination}");
            board.ClearPath();
            yield break;
        }

        UnitMover mover = enemy.GetComponent<UnitMover>();

        if (mover != null)
        {
            yield return mover.MoveAlongPath(board, path);
        }
        else
        {
            enemy.transform.position = board.ConvertGridToWorld(destination);
        }

        enemy.MarkMoved();

        board.ClearHighlights();
        board.ClearPath();
    }

    private List<GridCoord> FindBestPathTowardAnyPlayer(CombatUnit enemy, HashSet<GridCoord> reachableCells)
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

        foreach (GridCoord candidate in reachableCells)
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

    // --------------------------------------------------
    // Attack
    // --------------------------------------------------

    private IEnumerator TryMeleeAttack(CombatUnit enemy)
    {
        HashSet<GridCoord> attackCells = attackRangeService.GetAttackRangeCells(board, enemy, CommandMode.MeleeAttack);

        board.ClearHighlights();
        board.ClearPath();
        board.ShowAttackCells(attackCells);
        board.ShowSelected(enemy.GridPosition);

        yield return new WaitForSeconds(previewDelay);

        CombatUnit target = FindBestMeleeTarget(enemy, attackCells);

        if (target == null)
        {
            Debug.Log($"{enemy.name} has no melee target.");
            board.ClearHighlights();
            yield break;
        }

        //Animator animator = enemy.GetComponentInChildren<Animator>();

        //if (animator != null)
        //{
        //    animator.Play("Attack");
        //}

        Debug.Log("Show player card for : " + target.ToString());
        playerCard.Show(target);

        enemy.FaceTowards(target.GridPosition);
        enemy.PlayAttack();

        CameraShakeImpulse.PlayHeavyHit();

        SFXManager.EnemyMelee();

        yield return new WaitForSeconds(attackDelay);

        int damage = enemy.Stats.GetDamageForCommand(CommandMode.MeleeAttack);

        Debug.Log($"{enemy.name} melee attacks {target.name} for {damage} damage.");

        bool targetDied = target.TakeDamage(damage);

        playerCard.Refresh();
        enemyCard.Refresh();

        enemy.MarkAttacked();

        if (targetDied)
        {
            turnManager.RemoveUnitFromBattle(target);
        }

        enemy.PlayIdle();

        board.ClearHighlights();
        board.ClearPath();

        turnManager.RefreshBattleEndState();
    }

    private CombatUnit FindBestMeleeTarget(CombatUnit enemy, HashSet<GridCoord> attackCells)
    {
        CombatUnit bestTarget = null;
        int lowestHP = int.MaxValue;

        foreach (GridCoord cell in attackCells)
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
}
