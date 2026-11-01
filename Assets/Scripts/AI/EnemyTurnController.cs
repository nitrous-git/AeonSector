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

    [SerializeField] private GameObject rangedProjectilePrefab;

    [Header("Area Attack FX")]
    [SerializeField] private GameObject areaProjectilePrefab;
    [SerializeField] private Vector3 areaProjectileFallOffset = new Vector3(0.0f, 8.0f, 0f);
    [SerializeField] private Vector3 areaProjectileHitOffset = Vector3.zero;
    [SerializeField] private float areaProjectileCellStagger = 0.045f;
    [SerializeField] private float areaAttackPostDelay = 0.2f;

    private readonly ReachableTileService reachableTileService = new();

    private readonly AttackRangeService attackRangeService = new();

    private EnemyTacticalPlanner Planner => new EnemyTacticalPlanner(board, turnManager);

    public bool IsRunning { get; private set; }

    public void BeginEnemyTurn()
    {
        if (IsRunning || turnManager == null || turnManager.IsBattleOver || !turnManager.IsEnemyTurn())
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

        if (turnManager.IsBattleOver || !turnManager.IsEnemyTurn())
        {
            IsRunning = false;
            yield break;
        }

        List<CombatUnit> enemies = turnManager.EnemyFaction.UnitManager.GetLivingUnits().ToList();

        foreach (CombatUnit enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            yield return ResolveEnemyUnit(enemy);

            if (turnManager.IsBattleOver || turnManager.RefreshBattleEndState())
            {
                board.ClearHighlights();
                board.ClearPath();
                IsRunning = false;
                yield break;
            }

            yield return new WaitForSeconds(unitDelay);
        }

        enemyCard?.Hide();
        playerCard?.Hide();

        board.ClearHighlights();
        board.ClearPath();

        IsRunning = false;

        if (!turnManager.IsBattleOver)
            turnManager.EndCurrentTurn();
    }

    private IEnumerator ResolveEnemyUnit(CombatUnit enemy)
    {
        if (turnManager.IsBattleOver || enemy == null || !enemy.IsAlive)
            yield break;

        Debug.Log($"Enemy AI resolving unit: {enemy.name}");

        enemyCard?.Show(enemy);

        if (enemy.Stats == null) yield break;
        CommandMode attack = EnemyTacticalPlanner.CommandFor(enemy.Stats.EnemyRole);
        if (!enemy.Stats.Supports(attack))
        {
            Debug.LogWarning($"{enemy.name}: missing/incompatible enemy role configuration.");
            yield break;
        }
        bool isAreaEnemy = attack == CommandMode.AreaAttack;
        if (attack == CommandMode.RangedAttack && enemy.CanAttack &&
            Planner.FindBestMeleeTarget(enemy, attackRangeService.GetAttackRangeCells(board, enemy, attack)) != null)
        {
            yield return TryRangedAttack(enemy);
            yield break;
        }

        SFXManager.UnitSelect();

        // Artillery should not walk forward if it already has a valid AoE shot.
        if (isAreaEnemy && enemy.CanAttack && HasAreaAttackTarget(enemy))
        {
            yield return TryAreaAttack(enemy);

            enemyCard?.Refresh();
            playerCard?.Refresh();
            yield break;
        }

        if (enemy.CanMove)
        {
            yield return TryMoveTowardPlayer(enemy);
            enemyCard?.Refresh();
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
            else if (attack == CommandMode.RangedAttack) yield return TryRangedAttack(enemy);
            else yield return TryMeleeAttack(enemy);

            enemyCard?.Refresh();
            playerCard?.Refresh();
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

        if (turnManager.IsBattleOver || enemy == null || !enemy.IsAlive)
        {
            board.ClearHighlights();
            yield break;
        }

        List<GridCoord> path = new EnemyTacticalPlanner(board, turnManager).FindPath(enemy, reachableCells);

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

        if (turnManager.IsBattleOver || !turnManager.IsEnemyTurn() || enemy == null || !enemy.CanMove) yield break;
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

    // --------------------------------------------------
    // Attack
    // --------------------------------------------------

    private IEnumerator TryMeleeAttack(CombatUnit enemy)
    {
        if (!CanExecute(enemy, CommandMode.MeleeAttack)) yield break;
        HashSet<GridCoord> attackCells = attackRangeService.GetAttackRangeCells(board, enemy, CommandMode.MeleeAttack);

        board.ClearHighlights();
        board.ClearPath();
        board.ShowAttackCells(attackCells);
        board.ShowSelected(enemy.GridPosition);

        yield return new WaitForSeconds(previewDelay);

        if (turnManager.IsBattleOver || enemy == null || !enemy.IsAlive)
        {
            board.ClearHighlights();
            yield break;
        }

        CombatUnit target = Planner.FindBestMeleeTarget(enemy, attackCells);

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
        playerCard?.Show(target);

        enemy.FaceTowards(target.GridPosition);
        enemy.PlayAttack();

        var meleeProfile = CombatPresentation.Instance != null ? CombatPresentation.Profile(enemy) : null;
        float contactDelay = meleeProfile != null ? meleeProfile.meleeContact : attackDelay;
        CombatPresentation.Begin(enemy, CommandMode.MeleeAttack, contactDelay);
        yield return new WaitForSeconds(contactDelay);

        if (turnManager.IsBattleOver || enemy == null || !enemy.IsAlive || target == null || !target.IsAlive)
        {
            if (enemy != null && enemy.IsAlive && !turnManager.IsBattleOver)
                enemy.MarkAttacked();
            enemy?.PlayIdle();
            board.ClearHighlights();
            board.ClearPath();
            yield break;
        }

        int damage = enemy.Stats.GetDamageForCommand(CommandMode.MeleeAttack);

        Debug.Log($"{enemy.name} melee attacks {target.name} for {damage} damage.");

        if (CanExecute(enemy, CommandMode.MeleeAttack))
        {
            CombatPresentation.MeleeContact(enemy, target);
            SFXManager.EnemyMelee();
            ApplyEnemyDamage(enemy, target, damage, CommandMode.MeleeAttack);
        }
        if (meleeProfile != null) yield return new WaitForSeconds(meleeProfile.meleeRecovery);

        if (!turnManager.IsBattleOver && enemy != null && enemy.IsAlive)
        {
            enemyCard?.Refresh();
            playerCard?.Refresh();
            enemy.MarkAttacked();
            enemy.PlayIdle();
        }

        board.ClearHighlights();
        board.ClearPath();

        turnManager.RefreshBattleEndState();
    }



    // --------------------------------------------------
    // Area Attack FX Helpers
    // --------------------------------------------------

    private bool HasAreaAttackTarget(CombatUnit enemy)
    {
        HashSet<GridCoord> attackCells = attackRangeService.GetAttackRangeCells(board, enemy, CommandMode.AreaAttack);

        List<CombatUnit> targets = Planner.FindAreaAttackTargets(enemy, attackCells);

        return targets.Count > 0;
    }

    private IEnumerator TryAreaAttack(CombatUnit enemy)
    {
        if (!CanExecute(enemy, CommandMode.AreaAttack)) yield break;
        HashSet<GridCoord> attackCells = attackRangeService.GetAttackRangeCells(board, enemy, CommandMode.AreaAttack);

        board.ClearHighlights();
        board.ClearPath();
        board.ShowAttackCells(attackCells);
        board.ShowSelected(enemy.GridPosition);

        yield return new WaitForSeconds(previewDelay);

        if (turnManager.IsBattleOver || enemy == null || !enemy.IsAlive)
        {
            board.ClearHighlights();
            yield break;
        }

        List<CombatUnit> targets = Planner.FindAreaAttackTargets(enemy, attackCells);

        if (targets.Count == 0)
        {
            Debug.Log($"{enemy.name} has no area attack target.");
            board.ClearHighlights();
            yield break;
        }

        CombatUnit focusTarget = Planner.FindLowestHpTarget(targets);

        if (focusTarget != null)
        {
            playerCard?.Show(focusTarget);
            enemy.FaceTowards(focusTarget.GridPosition);
        }

        enemy.PlayAttack();

        int damage = enemy.Stats.GetDamageForCommand(CommandMode.AreaAttack);
        CombatPresentation.Begin(enemy, CommandMode.AreaAttack, 0.22f);
        if (CombatPresentation.Instance != null) yield return new WaitForSeconds(0.22f);
        CombatPresentation.Launch(enemy, CombatPresentation.HitPosition(enemy), CombatPresentation.HitPosition(enemy) + Vector3.up);

        List<GridCoord> orderedCells = attackCells
            .OrderByDescending(cell => cell.y)
            .ThenBy(cell => cell.x)
            .ToList();

        yield return FireAreaProjectilesAndApplyDamage(enemy, orderedCells, damage);

        if (turnManager.IsBattleOver)
        {
            board.ClearHighlights();
            board.ClearPath();
            yield break;
        }

        if (enemy == null || !enemy.IsAlive)
            yield break;

        enemy.MarkAttacked();

        enemyCard?.Refresh();
        playerCard?.Refresh();

        enemy.PlayIdle();

        board.ClearHighlights();
        board.ClearPath();

        turnManager.RefreshBattleEndState();

        yield return new WaitForSeconds(areaAttackPostDelay);
    }





    // projectile rain logic
    private IEnumerator FireAreaProjectilesAndApplyDamage(CombatUnit attacker, IEnumerable<GridCoord> cells, int damage)
    {
        List<Coroutine> activeProjectiles = new List<Coroutine>();

        foreach (GridCoord cell in cells)
        {
            if (turnManager.IsBattleOver || attacker == null || !attacker.IsAlive)
                break;

            Coroutine projectileRoutine = StartCoroutine(FireSingleAreaProjectile(attacker, cell, damage));

            activeProjectiles.Add(projectileRoutine);

            if (areaProjectileCellStagger > 0f)
            {
                yield return new WaitForSeconds(areaProjectileCellStagger);

                if (turnManager.IsBattleOver)
                    break;
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
        CombatPresentation.AreaWarning(attacker, targetWorldPosition, 0.7f);
        GameObject presentationProjectile = CombatPresentation.Projectile(attacker, areaProjectilePrefab);

        if (presentationProjectile == null)
        {
            yield return new WaitForSeconds(0.05f);

            if (turnManager.IsBattleOver)
                yield break;

            ApplyAreaDamageAtCell(attacker, targetCell, damage);
            yield break;
        }

        GameObject projectileObject = Instantiate(presentationProjectile, startWorldPosition, Quaternion.identity);

        ProjectileMover projectileMover = projectileObject.GetComponent<ProjectileMover>();

        if (projectileMover == null)
        {
            Debug.LogWarning("Area projectile prefab is missing ProjectileMover.");
            Destroy(projectileObject);

            ApplyAreaDamageAtCell(attacker, targetCell, damage);
            yield break;
        }

        yield return projectileMover.FlyAndHit(startWorldPosition, targetWorldPosition,
            () => ApplyAreaDamageAtCell(attacker, targetCell, damage));
    }

    private void ApplyAreaDamageAtCell(CombatUnit attacker, GridCoord targetCell, int damage)
    {
        if (turnManager.IsBattleOver || attacker == null || !attacker.IsAlive)
            return;

        CombatPresentation.AreaImpact(attacker, board.ConvertGridToWorld(targetCell) + areaProjectileHitOffset);
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

        playerCard?.Show(unitAtCell);

        if (CanExecute(attacker, CommandMode.AreaAttack)) ApplyEnemyDamage(attacker, unitAtCell, damage, CommandMode.AreaAttack);

        if (!turnManager.IsBattleOver)
        {
            playerCard?.Refresh();
            turnManager.RefreshBattleEndState();
        }
    }

    private bool CanExecute(CombatUnit unit, CommandMode mode)
        => turnManager != null && !turnManager.IsBattleOver && turnManager.IsEnemyTurn()
        && unit != null && unit.CanAttack && unit.Stats != null && unit.Stats.Supports(mode);

    private void ApplyEnemyDamage(CombatUnit attacker, CombatUnit target, int damage, CommandMode mode)
    {
        if (attacker == null || !attacker.IsAlive || target == null || !target.IsAlive
            || target.OwnerFaction == attacker.OwnerFaction || turnManager.IsBattleOver) return;
        BattleDamage.Apply(turnManager, target, damage, attacker, mode);
        turnManager.RefreshBattleEndState();
    }

    private IEnumerator TryRangedAttack(CombatUnit enemy)
    {
        if (!CanExecute(enemy, CommandMode.RangedAttack)) yield break;
        var cells = attackRangeService.GetAttackRangeCells(board, enemy, CommandMode.RangedAttack);
        board.ShowAttackCells(cells);
        yield return new WaitForSeconds(previewDelay);
        if (!CanExecute(enemy, CommandMode.RangedAttack)) yield break;
        // Requery after anticipation; do not shoot through a newly occupied/blocking cell.
        var target = Planner.FindBestMeleeTarget(enemy, attackRangeService.GetAttackRangeCells(board, enemy, CommandMode.RangedAttack));
        if (target == null) { board.ClearHighlights(); yield break; }
        enemy.FaceTowards(target.GridPosition);
        enemy.PlayAttack(); // Enemy animation; never waits for player-only clip events.
        float animationStarted = Time.time;
        CombatPresentation.Begin(enemy, CommandMode.RangedAttack, enemy.GetRangedReleaseDelay(attackDelay));
        playerCard?.Show(target);
        yield return new WaitForSeconds(enemy.GetRangedReleaseDelay(attackDelay));
        if (!CanExecute(enemy, CommandMode.RangedAttack)) yield break;
        if (target != null && target.IsAlive && attackRangeService.GetAttackRangeCells(board, enemy, CommandMode.RangedAttack).Contains(target.GridPosition))
        {
            var projectilePrefab = CombatPresentation.Projectile(enemy,
                enemy.Stats.RangedProjectilePrefab != null ? enemy.Stats.RangedProjectilePrefab : rangedProjectilePrefab);
            bool damageApplied = false;
            System.Action contact = () =>
            {
                if (damageApplied || !CanExecute(enemy, CommandMode.RangedAttack)) return;
                damageApplied = true;
                SFXManager.ProjectileImpact();
                ApplyEnemyDamage(enemy, target, enemy.Stats.GetDamageForCommand(CommandMode.RangedAttack), CommandMode.RangedAttack);
            };
            if (projectilePrefab != null)
            {
                Vector3 start = enemy.GetProjectileOrigin(board.ConvertGridToWorld(enemy.GridPosition) + Vector3.up * 0.8f);
                Vector3 hit = board.ConvertGridToWorld(target.GridPosition) + Vector3.up * 0.15f;
                if (CombatPresentation.Instance != null) hit = CombatPresentation.HitPosition(target);
                CombatPresentation.Launch(enemy, start, hit);
                var projectile = Instantiate(projectilePrefab, start, Quaternion.identity);
                var mover = projectile.GetComponent<ProjectileMover>();
                if (mover != null) yield return mover.FlyAndHit(start, hit, contact);
                else Destroy(projectile);
            }
            contact(); // Missing presentation fallback, guarded against a second hit.
        }
        if (enemy != null && enemy.IsAlive && !turnManager.IsBattleOver)
        {
            float recovery = enemy.RangedAnimationDuration - (Time.time - animationStarted);
            if (recovery > 0f) yield return new WaitForSeconds(recovery);
            if (enemy != null && enemy.IsAlive && !turnManager.IsBattleOver) { enemy.MarkAttacked(); enemy.PlayIdle(); }
        }
        playerCard?.Refresh(); enemyCard?.Refresh();
        board.ClearHighlights(); board.ClearPath();
    }
}
