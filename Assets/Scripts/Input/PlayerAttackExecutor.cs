using System.Collections;
using UnityEngine;

// Owns committed melee/ranged sequences and the at-most-once ranged damage context.
internal sealed class PlayerAttackExecutor
{
    private readonly MonoBehaviour host;
    private readonly TurnManager turnManager;
    private readonly TilemapBoardAdapter board;
    private readonly PlayerCommandState state;
    private readonly PlayerBattleUI ui;
    private readonly GameObject projectilePrefab;
    private readonly Vector3 projectileSpawnOffset;
    private readonly Vector3 projectileHitOffset;
    private readonly GameObject swordSlashPrefab;

    public PlayerAttackExecutor(MonoBehaviour host, TurnManager turnManager,
        TilemapBoardAdapter board, PlayerCommandState state, PlayerBattleUI ui,
        GameObject projectilePrefab, Vector3 projectileSpawnOffset,
        Vector3 projectileHitOffset, GameObject swordSlashPrefab)
    {
        this.host = host;
        this.turnManager = turnManager;
        this.board = board;
        this.state = state;
        this.ui = ui;
        this.projectilePrefab = projectilePrefab;
        this.projectileSpawnOffset = projectileSpawnOffset;
        this.projectileHitOffset = projectileHitOffset;
        this.swordSlashPrefab = swordSlashPrefab;
    }

    private class RangedAttackContext
    {
        public CombatUnit attacker;
        public CombatUnit target;
        public int damage;
        public bool projectileStarted;
        public bool projectileResolved;
        public bool damageApplied;
        public bool animationFinished;

        public RangedAttackContext(CombatUnit attacker, CombatUnit target, int damage)
        { 
            this.attacker = attacker;
            this.target = target;
            this.damage = damage;
        }
    }

    private RangedAttackContext rangedAttackContext;

    private const float RangedProjectileFallbackDelay = 0.25f;
    private const float RangedAttackResolutionTimeout = 5f;

    public bool TryCommitAttackAt(GridCoord targetCoord)
    {
        if (state.IsResolving
            || state.SelectedUnit == null
            || turnManager == null
            || board == null
            || !board.IsReady
            || turnManager.IsBattleOver
            || !turnManager.IsPlayerTurn())
            return false;

        if (state.SelectedUnit.OwnerFaction != turnManager.PlayerFaction
            || state.SelectedUnit.Stats == null
            || !state.SelectedUnit.CanAttack
            || !PlayerCommandState.IsAttackMode(state.Mode)
            || !state.SelectedUnit.Stats.Supports(state.Mode)
            || !board.IsInside(targetCoord))
            return false;

        if (!new AttackRangeService().GetAttackRangeCells(board, state.SelectedUnit, state.Mode).Contains(targetCoord))
        {
            SFXManager.InvalidAction();
            Debug.Log("Clicked cell is outside attack range.");
            return false;
        }

        CombatUnit target = board.GetUnitAt(targetCoord);

        if (target == null || !target.IsAlive)
        {
            SFXManager.InvalidAction();
            Debug.Log("No valid target on clicked attack cell.");
            return false;
        }

        if (target.OwnerFaction == state.SelectedUnit.OwnerFaction)
        {
            SFXManager.InvalidAction();
            Debug.Log("Cannot attack friendly unit.");
            return false;
        }

        ui.ShowEnemy(target);

        CommitAttack(target);
        return true;
    }

    private void CommitAttack(CombatUnit target)
    {
        if (state.IsResolving
            || state.SelectedUnit == null
            || target == null
            || turnManager == null
            || board == null
            || !board.IsReady
            || turnManager.IsBattleOver
            || !turnManager.IsPlayerTurn())
            return;

        if (state.SelectedUnit.OwnerFaction != turnManager.PlayerFaction
            || state.SelectedUnit.Stats == null
            || !state.SelectedUnit.CanAttack
            || !PlayerCommandState.IsAttackMode(state.Mode)
            || !state.SelectedUnit.Stats.Supports(state.Mode)
            || !target.IsAlive
            || target.OwnerFaction == state.SelectedUnit.OwnerFaction)
            return;

        state.IsResolving = true;
        host.StartCoroutine(ResolveUnitAttack(state.SelectedUnit, target));
    }

    private IEnumerator ResolveUnitAttack(CombatUnit attacker, CombatUnit target)
    {
        if (attacker == null
            || target == null
            || turnManager == null
            || board == null
            || !board.IsReady
            || turnManager.IsBattleOver
            || !turnManager.IsPlayerTurn()
            || attacker != state.SelectedUnit
            || attacker.OwnerFaction != turnManager.PlayerFaction
            || attacker.Stats == null
            || !attacker.IsAlive
            || !attacker.CanAttack
            || !attacker.Stats.Supports(state.Mode)
            || !target.IsAlive
            || target.OwnerFaction == attacker.OwnerFaction)
        {
            CancelAttackResolution();
            yield break;
        }

        ui.ClearPath();

        int damage = attacker.Stats.GetDamageForCommand(state.Mode);

        Debug.Log($"{attacker.name} uses {state.Mode} on {target.name} for {damage} damage.");

        if (state.Mode == CommandMode.RangedAttack)
        {
            rangedAttackContext = new RangedAttackContext(attacker, target, damage);
            attacker.FaceFromTo(attacker.GridPosition, target.GridPosition);
            attacker.PlayAttack(state.Mode);
            yield return ResolveRangedAttackTimeout(rangedAttackContext);
            yield break;
        }

        attacker.FaceFromTo(attacker.GridPosition, target.GridPosition);
        attacker.PlayAttack(state.Mode);

        if (state.Mode == CommandMode.MeleeAttack)
        {
            yield return ResolveMeleeAttackVisual(attacker, target);
        }

        if (attacker == null || !attacker.IsAlive || turnManager.IsBattleOver)
        {
            FinishAttackResolution(attacker);
            yield break;
        }
      
        if (attacker.Stats.Supports(state.Mode)) ApplyDamage(target, damage);

        FinishAttackResolution(attacker);
    }

    public void AnimEvent_FireRangedProjectile(CombatUnit eventOwner)
    {
        if (rangedAttackContext == null
            || rangedAttackContext.projectileStarted
            || turnManager == null
            || turnManager.IsBattleOver)
            return;

        if (rangedAttackContext.attacker != eventOwner)
            return;

        rangedAttackContext.projectileStarted = true;

        host.StartCoroutine(FireProjectileAndApplyDamage(rangedAttackContext));
    }

    private IEnumerator FireProjectileAndApplyDamage(RangedAttackContext context)
    {
        if (context == null || rangedAttackContext != context)
            yield break;

        CombatUnit attacker = context.attacker;
        CombatUnit target = context.target;

        if (attacker == null || !attacker.IsAlive || target == null || !target.IsAlive || board == null || !board.IsReady)
        {
            context.projectileResolved = true;
            TryCompleteRangedAttack(context);
            yield break;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned.");
            ApplyRangedDamage(context);
            context.projectileResolved = true;
            TryCompleteRangedAttack(context);
            yield break;
        }

        Vector3 startWorld = board.ConvertGridToWorld(attacker.GridPosition) + projectileSpawnOffset;
        Vector3 targetWorld = board.ConvertGridToWorld(target.GridPosition) + projectileHitOffset;

        GameObject projectileObject = Object.Instantiate(projectilePrefab, startWorld, Quaternion.identity);

        ProjectileMover projectileMover = projectileObject.GetComponent<ProjectileMover>();

        SFXManager.PlayerShoot();

        if (projectileMover == null)
        {
            Debug.LogWarning("Projectile prefab has no ProjectileMover.");
            Object.Destroy(projectileObject);
            ApplyRangedDamage(context);
            context.projectileResolved = true;
            TryCompleteRangedAttack(context);
            yield break;
        }

        yield return projectileMover.FlyAndHit(startWorld, targetWorld);

        if (rangedAttackContext != context)
            yield break;

        ApplyRangedDamage(context);
        context.projectileResolved = true;
        ui.RefreshEnemy();
        TryCompleteRangedAttack(context);
    }

    public void AnimEvent_RangedAttackFinished(CombatUnit eventOwner)
    {
        if (rangedAttackContext == null
            || rangedAttackContext.attacker != eventOwner)
            return;

        rangedAttackContext.animationFinished = true;
        TryCompleteRangedAttack(rangedAttackContext);
    }

    private IEnumerator ResolveRangedAttackTimeout(RangedAttackContext context)
    {
        yield return new WaitForSeconds(RangedProjectileFallbackDelay);

        if (rangedAttackContext != context)
            yield break;

        if (turnManager == null || turnManager.IsBattleOver)
        {
            CancelAttackResolution();
            yield break;
        }

        if (!context.projectileStarted)
        {
            Debug.LogWarning("Ranged attack fire event was not received; starting the projectile fallback.");
            AnimEvent_FireRangedProjectile(context.attacker);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, RangedAttackResolutionTimeout - RangedProjectileFallbackDelay));

        if (rangedAttackContext != context)
            yield break;

        if (turnManager == null || turnManager.IsBattleOver)
        {
            CancelAttackResolution();
            yield break;
        }

        if (!context.animationFinished)
        {
            Debug.LogWarning("Ranged attack finished event was not received; completing the animation fallback.");
            context.animationFinished = true;
        }

        TryCompleteRangedAttack(context);
    }

    private void ApplyRangedDamage(RangedAttackContext context)
    {
        if (context == null || context.damageApplied || context.attacker == null || !context.attacker.IsAlive || !context.attacker.Stats.Supports(CommandMode.RangedAttack))
            return;

        context.damageApplied = true;

        CombatUnit target = context.target;
        if (target == null || !target.IsAlive || turnManager == null || turnManager.IsBattleOver)
            return;

        SFXManager.ProjectileImpact();

        ApplyDamage(target, context.damage);
    }

    private void ApplyDamage(CombatUnit target, int damage)
    {
        if (target == null || !target.IsAlive || turnManager == null || turnManager.IsBattleOver)
            return;

        BattleDamage.Apply(turnManager, target, damage);

        ui.RefreshAfterDamage(target);
        turnManager.RefreshBattleEndState();
    }

    private void TryCompleteRangedAttack(RangedAttackContext context)
    {
        if (context == null
            || rangedAttackContext != context
            || !context.projectileResolved
            || !context.animationFinished)
            return;

        CombatUnit attacker = context.attacker;
        rangedAttackContext = null;
        FinishAttackResolution(attacker);
    }

    private IEnumerator ResolveMeleeAttackVisual(CombatUnit attacker, CombatUnit target)
    {
        GameObject slashObject = null;
        if (swordSlashPrefab != null && board != null && board.IsReady)
        {
            Vector3 spawnWorld = board.ConvertGridToWorld(attacker.GridPosition) + projectileSpawnOffset;
            slashObject = Object.Instantiate(swordSlashPrefab, spawnWorld, Quaternion.identity);
            slashObject.GetComponent<Animator>()?.Play("SwordSlash");
        }

        CameraShakeImpulse.PlayLongMediumHit();

        SFXManager.PlayerMelee();

        yield return new WaitForSeconds(1.20f);
        if (slashObject != null)
            Object.Destroy(slashObject);

        yield return null;
    }

    private void FinishAttackResolution(CombatUnit attacker)
    {
        bool attackerValid = attacker != null && attacker.IsAlive
            && turnManager != null && !turnManager.IsBattleOver;
        if (attackerValid)
        {
            attacker.PlayIdle();
            attacker.MarkAttacked();
        }
        ui.ClearOverlays();
        state.ResetCommand();
        state.IsResolving = false;
        turnManager?.RefreshBattleEndState();
        if (attackerValid && !turnManager.IsBattleOver)
        {
            state.RestoreSelectedUnit(attacker);
            ui.RefreshPlayer(attacker);
        }
        else
        {
            state.ClearSelectedUnit();
            ui.HideMenu();
        }
    }

    private void CancelAttackResolution()
    {
        rangedAttackContext = null;
        ui.ClearOverlays();
        state.ResetCommand();
        state.IsResolving = false;
        state.ClearSelectedUnit();
        ui.HideMenu();
    }
}
