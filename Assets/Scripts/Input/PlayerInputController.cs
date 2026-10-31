using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private TilemapBoardAdapter board;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ActionMenuUI actionMenuUI;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Vector3 projectileSpawnOffset = new Vector3(0f, 0.80f, 0f);
    [SerializeField] private Vector3 projectileHitOffset = new Vector3(0f, 0.15f, 0f);

    [Header("Sword Slash")]
    [SerializeField] private GameObject swordSlashPrefab;

    [Header("Selection")]
    [SerializeField] private LayerMask unitLayerMask;

    [Header("Unit Cards")]
    [SerializeField] private UnitCardUI playerCard;
    [SerializeField] private UnitCardUI enemyCard;

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

    // Internal class (for simplicity)
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

    // Unity Lifecycle
    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        actionMenuUI?.Hide();
    }

    private void Update()
    {
        if (turnManager == null || board == null || mainCamera == null || turnManager.IsBattleOver)
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

            //if (selectedUnit.OwnerFaction.Type != FactionType.Player)
            //{
            //    ClearSelection();
            //}
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
            else
            {
                SFXManager.InvalidAction();
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

        //if (clickedUnit.OwnerFaction != turnManager.PlayerFaction)
        //    return;

        if (!clickedUnit.IsAlive)
            return;

        SFXManager.UnitSelect();

        //Debug.Log($"SelectedUnit : {clickedUnit.name}");
        SelectUnit(clickedUnit);
    }

    private void HandleCommandHotkeys()
    {
        if (selectedUnit == null)
            return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            SelectMoveCommand();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            SelectMeleeAttackCommand();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SelectRangedAttackCommand();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            EndSelectedUnitTurn();
        }
    }

    // ----------------------------
    // Selection
    // ----------------------------
    private void SelectUnit(CombatUnit clickedUnit)
    {
        selectedUnit = clickedUnit;

        if (clickedUnit.OwnerFaction != turnManager.PlayerFaction)
        {
            //Debug.Log($"Selected {clickedUnit.name}");
            ClearSelection();   
            board.ClearHighlights();
            board.ShowAttackCells( new List<GridCoord>(){ clickedUnit.GridPosition });
            enemyCard.Show(clickedUnit);
            return;
        }

        actionMenuUI.Show();
        actionMenuUI.Refresh(selectedUnit);

        playerCard.Show(selectedUnit);
        enemyCard.Hide();

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
        if (selectedUnit == null || turnManager == null || board == null || !board.IsReady)
            return;

        if (!turnManager.IsPlayerTurn() || selectedUnit.Stats == null || !selectedUnit.CanMove)
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
        if (isResolvingAction || turnManager == null || board == null || selectedUnit == null)
            return;

        CombatUnit unit = selectedUnit;
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
            || !reachableCells.Contains(targetCoord))
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

        isResolvingAction = true;
        StartCoroutine(ResolveUnitMove(unit, path));
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
            || unit != selectedUnit
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

        board.ShowPath(currentPath);
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
        board?.ClearHighlights();
        board?.ClearPath();
        reachableCells.Clear();
        hoveredCoord = null;

        commandMode = CommandMode.None;
        isResolvingAction = false;

        if (unit == null || !unit.IsAlive || board == null || !board.IsReady || !board.IsInside(unit.GridPosition))
        {
            selectedUnit = null;
            actionMenuUI?.Hide();
            return;
        }

        selectedUnit = unit;
        board.ShowSelected(unit.GridPosition);

        actionMenuUI?.Refresh(unit);
        playerCard?.Refresh();
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
        if (selectedUnit == null || turnManager == null || board == null || !board.IsReady || turnManager.IsBattleOver)
            return;

        if (!turnManager.IsPlayerTurn()
            || selectedUnit.OwnerFaction != turnManager.PlayerFaction
            || selectedUnit.Stats == null
            || !selectedUnit.CanAttack)
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
        if (isResolvingAction
            || selectedUnit == null
            || turnManager == null
            || board == null
            || !board.IsReady
            || turnManager.IsBattleOver
            || !turnManager.IsPlayerTurn())
            return false;

        if (selectedUnit.OwnerFaction != turnManager.PlayerFaction
            || selectedUnit.Stats == null
            || !selectedUnit.CanAttack
            || !board.IsInside(targetCoord))
            return false;

        if (!reachableCells.Contains(targetCoord))
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

        if (target.OwnerFaction == selectedUnit.OwnerFaction)
        {
            SFXManager.InvalidAction();
            Debug.Log("Cannot attack friendly unit.");
            return false;
        }

        // We should show the card on hovering 
        if (enemyCard != null)
        {
            enemyCard.Show(target);
        }

        CommitAttack(target);
        return true;
    }

    private void CommitAttack(CombatUnit target)
    {
        if (isResolvingAction
            || selectedUnit == null
            || target == null
            || turnManager == null
            || board == null
            || !board.IsReady
            || turnManager.IsBattleOver
            || !turnManager.IsPlayerTurn())
            return;

        if (selectedUnit.OwnerFaction != turnManager.PlayerFaction
            || selectedUnit.Stats == null
            || !selectedUnit.CanAttack
            || !target.IsAlive
            || target.OwnerFaction == selectedUnit.OwnerFaction)
            return;

        isResolvingAction = true;
        StartCoroutine(ResolveUnitAttack(selectedUnit, target));
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
            || attacker != selectedUnit
            || attacker.OwnerFaction != turnManager.PlayerFaction
            || attacker.Stats == null
            || !attacker.IsAlive
            || !attacker.CanAttack
            || !target.IsAlive
            || target.OwnerFaction == attacker.OwnerFaction)
        {
            CancelAttackResolution();
            yield break;
        }

        board.ClearPath();

        int damage = attacker.Stats.GetDamageForCommand(commandMode);

        Debug.Log($"{attacker.name} uses {commandMode} on {target.name} for {damage} damage.");

        if (commandMode == CommandMode.RangedAttack)
        {
            rangedAttackContext = new RangedAttackContext(attacker, target, damage);
            attacker.FaceFromTo(attacker.GridPosition, target.GridPosition);
            attacker.PlayAttack(commandMode);
            yield return ResolveRangedAttackTimeout(rangedAttackContext);
            yield break;
        }

        attacker.FaceFromTo(attacker.GridPosition, target.GridPosition);
        attacker.PlayAttack(commandMode);

        if (commandMode == CommandMode.MeleeAttack)
        {
            yield return ResolveMeleeAttackVisual(attacker, target);
        }

        if (attacker == null || !attacker.IsAlive || turnManager.IsBattleOver)
        {
            FinishAttackResolution(attacker);
            yield break;
        }
      
        ApplyDamage(target, damage);

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

        StartCoroutine(FireProjectileAndApplyDamage(rangedAttackContext));
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

        GameObject projectileObject = Instantiate(projectilePrefab, startWorld, Quaternion.identity);

        ProjectileMover projectileMover = projectileObject.GetComponent<ProjectileMover>();

        SFXManager.PlayerShoot();

        if (projectileMover == null)
        {
            Debug.LogWarning("Projectile prefab has no ProjectileMover.");
            Destroy(projectileObject);
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
        enemyCard?.Refresh();
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
        if (context == null || context.damageApplied)
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

        if (target.TakeDamage(damage))
        {
            turnManager.RemoveUnitFromBattle(target);
        }

        playerCard?.Refresh();
        if (target != null && target.IsAlive)
            enemyCard?.Show(target);
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
        
        // Later we can add Animator.Play("Melee_Attack") here.

        GameObject slashObject = null;
        if (swordSlashPrefab != null && board != null && board.IsReady)
        {
            Vector3 spawnWorld = board.ConvertGridToWorld(attacker.GridPosition) + projectileSpawnOffset;
            slashObject = Instantiate(swordSlashPrefab, spawnWorld, Quaternion.identity);
            slashObject.GetComponent<Animator>()?.Play("SwordSlash");
        }

        CameraShakeImpulse.PlayLongMediumHit();

        SFXManager.PlayerMelee();

        yield return new WaitForSeconds(1.20f);
        if (slashObject != null)
            Destroy(slashObject);

        yield return null;
    }

    // Shared finish helper (melee and ranged both reuse)
    private void FinishAttackResolution(CombatUnit attacker)
    {
        bool battleEnded = turnManager == null || turnManager.IsBattleOver;
        bool attackerValid = attacker != null && attacker.IsAlive && !battleEnded;

        if (attackerValid)
        {
            attacker.PlayIdle();
            attacker.MarkAttacked();
        }

        board?.ClearHighlights();
        board?.ClearPath();

        reachableCells.Clear();
        hoveredCoord = null;

        commandMode = CommandMode.None;
        isResolvingAction = false;

        if (turnManager != null)
            turnManager.RefreshBattleEndState();

        if (attackerValid && turnManager != null && !turnManager.IsBattleOver)
        {
            selectedUnit = attacker;
            playerCard?.Refresh();
            actionMenuUI?.Refresh(selectedUnit);
        }
        else
        {
            selectedUnit = null;
            actionMenuUI?.Hide();
        }
    }

    private void CancelAttackResolution()
    {
        rangedAttackContext = null;
        board?.ClearHighlights();
        board?.ClearPath();
        reachableCells.Clear();
        hoveredCoord = null;
        commandMode = CommandMode.None;
        isResolvingAction = false;
        selectedUnit = null;
        actionMenuUI?.Hide();
    }

    // --------------------
    // End Turn 
    // --------------------
    private void TryEndPlayerTurn()
    {
        if (turnManager == null || turnManager.IsBattleOver || !turnManager.IsPlayerTurn())
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

        actionMenuUI.Hide();

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

        actionMenuUI.Hide();
        //playerCard.Hide();
        enemyCard.Hide();

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

    // --------------------
    // ActionMenuUI Helpers
    // --------------------

    public void SelectMoveCommand()
    {
        if (selectedUnit == null || !selectedUnit.CanMove)
            return;

        SFXManager.ButtonClick();
        Debug.Log("Move Command selected");
        SetCommandMode(CommandMode.Move);
    }

    public void SelectRangedAttackCommand()
    {
        if (selectedUnit == null || !selectedUnit.CanAttack)
            return;

        SFXManager.ButtonClick();
        SetCommandMode(CommandMode.RangedAttack);
    }

    public void SelectMeleeAttackCommand()
    {
        if (selectedUnit == null || !selectedUnit.CanAttack)
            return;

        SFXManager.ButtonClick();
        SetCommandMode(CommandMode.MeleeAttack);
    }

    public void EndSelectedUnitTurn()
    {
        SFXManager.ButtonClick();
        TryEndPlayerTurn();
    }

    private void SetCommandMode(CommandMode commandMode)
    {
        switch (commandMode)
        {
            case CommandMode.Move:
                EnterMoveMode();
                break;
            case CommandMode.RangedAttack:
                EnterRangedAttackMode();
                break;
            case CommandMode.MeleeAttack:
                EnterMeleeAttackMode();
                break;
        }
    }

    // { }
}
