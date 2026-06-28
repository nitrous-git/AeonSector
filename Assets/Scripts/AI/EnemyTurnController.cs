using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Area Attack FX")]
    [SerializeField] private GameObject areaProjectilePrefab;
    [SerializeField] private Vector3 areaProjectileFallOffset = new Vector3(0.0f, 8.0f, 0f);
    [SerializeField] private Vector3 areaProjectileHitOffset = Vector3.zero;
    [SerializeField] private float areaProjectileCellStagger = 0.045f;
    [SerializeField] private float areaAttackPostDelay = 0.2f;

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

        bool isAreaEnemy = enemy.Stats != null && enemy.Stats.UnitType == UnitType.InsectArtillery;

        SFXManager.UnitSelect();

        // Artillery should not walk forward if it already has a valid AoE shot.
        if (isAreaEnemy && enemy.CanAttack && HasAreaAttackTarget(enemy))
        {
            yield return TryAreaAttack(enemy);

            enemyCard.Refresh();
            playerCard.Refresh();
            yield break;
        }

        if (enemy.CanMove)
        {
            yield return TryMoveTowardPlayer(enemy);
            enemyCard.Refresh();
            //playerCard.Refresh();

            if (turnManager.RefreshBattleEndState())
            {
                yield break;
            }
        }

        if (enemy == null || !enemy.IsAlive)
        {
            yield break;
        }

        if (enemy.CanAttack)
        {
            if (isAreaEnemy)
            {
                yield return TryAreaAttack(enemy);
            }
            else
            {
                yield return TryMeleeAttack(enemy);
            }

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

    // --------------------------------------------------
    // Area Attack FX Helpers
    // --------------------------------------------------

    private bool HasAreaAttackTarget(CombatUnit enemy)
    {
        HashSet<GridCoord> attackCells = attackRangeService.GetAttackRangeCells(board, enemy, CommandMode.AreaAttack);

        List<CombatUnit> targets = FindAreaAttackTargets(enemy, attackCells);

        return targets.Count > 0;
    }

    private IEnumerator TryAreaAttack(CombatUnit enemy)
    {
        HashSet<GridCoord> attackCells = attackRangeService.GetAttackRangeCells(board, enemy, CommandMode.AreaAttack);

        board.ClearHighlights();
        board.ClearPath();
        board.ShowAttackCells(attackCells);
        board.ShowSelected(enemy.GridPosition);

        yield return new WaitForSeconds(previewDelay);

        List<CombatUnit> targets = FindAreaAttackTargets(enemy, attackCells);

        if (targets.Count == 0)
        {
            Debug.Log($"{enemy.name} has no area attack target.");
            board.ClearHighlights();
            yield break;
        }

        CombatUnit focusTarget = FindLowestHpTarget(targets);

        if (focusTarget != null)
        {
            playerCard.Show(focusTarget);
            enemy.FaceTowards(focusTarget.GridPosition);
        }

        enemy.PlayAttack();

        int damage = enemy.Stats.GetDamageForCommand(CommandMode.AreaAttack);

        List<GridCoord> orderedCells = attackCells
            .OrderByDescending(cell => cell.y)
            .ThenBy(cell => cell.x)
            .ToList();

        yield return FireAreaProjectilesAndApplyDamage(enemy, orderedCells, damage);

        enemy.MarkAttacked();

        enemyCard.Refresh();
        playerCard.Refresh();

        enemy.PlayIdle();

        board.ClearHighlights();
        board.ClearPath();

        turnManager.RefreshBattleEndState();

        yield return new WaitForSeconds(areaAttackPostDelay);
    }

    private List<CombatUnit> FindAreaAttackTargets(CombatUnit attacker, HashSet<GridCoord> attackCells)
    {
        List<CombatUnit> targets = new List<CombatUnit>();

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

            if (unitAtCell.OwnerFaction == attacker.OwnerFaction)
            {
                continue;
            }

            targets.Add(unitAtCell);
        }

        return targets;
    }

    private CombatUnit FindLowestHpTarget(List<CombatUnit> targets)
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

    // projectile rain logic
    private IEnumerator FireAreaProjectilesAndApplyDamage(CombatUnit attacker, IEnumerable<GridCoord> cells, int damage)
    {
        List<Coroutine> activeProjectiles = new List<Coroutine>();

        foreach (GridCoord cell in cells)
        {
            Coroutine projectileRoutine = StartCoroutine(FireSingleAreaProjectile(attacker, cell, damage));

            activeProjectiles.Add(projectileRoutine);

            if (areaProjectileCellStagger > 0f)
            {
                yield return new WaitForSeconds(areaProjectileCellStagger);
            }
        }

        foreach (Coroutine projectileRoutine in activeProjectiles)
        {
            yield return projectileRoutine;
        }
    }

    private IEnumerator FireSingleAreaProjectile(CombatUnit attacker, GridCoord targetCell, int damage)
    {
        Vector3 targetWorldPosition = board.ConvertGridToWorld(targetCell) + areaProjectileHitOffset;

        Vector3 startWorldPosition = targetWorldPosition + areaProjectileFallOffset;

        if (areaProjectilePrefab == null)
        {
            yield return new WaitForSeconds(0.05f);
            ApplyAreaDamageAtCell(attacker, targetCell, damage);
            yield break;
        }

        GameObject projectileObject = Instantiate(areaProjectilePrefab, startWorldPosition, Quaternion.identity);

        ProjectileMover projectileMover = projectileObject.GetComponent<ProjectileMover>();

        if (projectileMover == null)
        {
            Debug.LogWarning("Area projectile prefab is missing ProjectileMover.");
            Destroy(projectileObject);

            ApplyAreaDamageAtCell(attacker, targetCell, damage);
            yield break;
        }

        yield return projectileMover.FlyAndHit(startWorldPosition,targetWorldPosition);

        ApplyAreaDamageAtCell(attacker, targetCell, damage);
    }

    private void ApplyAreaDamageAtCell(CombatUnit attacker, GridCoord targetCell, int damage)
    {
        CombatUnit unitAtCell = board.GetUnitAt(targetCell);

        if (unitAtCell == null)
        {
            return;
        }

        if (!unitAtCell.IsAlive)
        {
            return;
        }

        if (unitAtCell.OwnerFaction == attacker.OwnerFaction)
        {
            return;
        }

        playerCard.Show(unitAtCell);

        bool died = unitAtCell.TakeDamage(damage);

        playerCard.Refresh();

        if (died)
        {
            turnManager.RemoveUnitFromBattle(unitAtCell);
        }

        turnManager.RefreshBattleEndState();
    }
}
