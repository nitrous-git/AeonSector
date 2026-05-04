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

        public RangedAttackContext(CombatUnit attacker, CombatUnit target, int damage)
        { 
            this.attacker = attacker;
            this.target = target;
            this.damage = damage;
        }
    }

    private RangedAttackContext rangedAttackContext;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        actionMenuUI.Hide();
    }

    private void Update()
    {
        if (turnManager == null || board == null || mainCamera == null)
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

        if (clickedUnit.OwnerFaction != turnManager.PlayerFaction)
            return;

        if (!clickedUnit.IsAlive)
            return;

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

        if (actionMenuUI != null)
        {
            actionMenuUI.Show();
            actionMenuUI.Refresh(selectedUnit);
        }

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
        if (selectedUnit == null)
            return;

        if (!selectedUnit.CanMove)
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
        if (selectedUnit == null)
            return;

        if (!selectedUnit.CanMove)
            return;

        List<GridCoord> path = pathService.FindPath(board, selectedUnit.GridPosition, targetCoord);
        if (path.Count <= 1)
        {
            return;
        }

        StartCoroutine(ResolveUnitMove(selectedUnit, path));
    }

    private IEnumerator ResolveUnitMove(CombatUnit unit, List<GridCoord> path)
    {
        isResolvingAction = true;

        board.ShowPath(path);

        GridCoord destination = path[path.Count - 1];

        bool moved = board.TryMoveUnit(unit, destination);
        if (!moved)
        {
            Debug.LogWarning($"Failed to reserve move for {unit.name} to {destination}");
        }

        UnitMover mover = selectedUnit.GetComponent<UnitMover>();
        yield return mover.MoveAlongPath(board, path);

        Debug.Log("MarkMoved for this unit.");
        unit.MarkMoved();

        board.ClearHighlights();
        board.ClearPath();
        reachableCells.Clear();
        hoveredCoord = null;

        commandMode = CommandMode.None;
        isResolvingAction = false;

        selectedUnit = unit;
        board.ShowSelected(selectedUnit.GridPosition);

        actionMenuUI.Refresh(selectedUnit);
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
        if (selectedUnit == null)
            return;

        if (!selectedUnit.CanAttack)
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
        if (selectedUnit == null)
            return false;

        if (!selectedUnit.CanAttack)
            return false;

        if (!reachableCells.Contains(targetCoord))
        {
            Debug.Log("Clicked cell is outside attack range.");
            return false;
        }

        CombatUnit target = board.GetUnitAt(targetCoord);

        if (target == null || !target.IsAlive)
        {
            Debug.Log("No valid target on clicked attack cell.");
            return false;
        }

        if (target.OwnerFaction == selectedUnit.OwnerFaction)
        {
            Debug.Log("Cannot attack friendly unit.");
            return false;
        }

        CommitAttack(target);
        return true;
    }

    private void CommitAttack(CombatUnit target)
    {
        if (selectedUnit == null || target == null)
            return;

        if (!selectedUnit.CanAttack)
            return;

        StartCoroutine(ResolveUnitAttack(selectedUnit, target));
    }

    private IEnumerator ResolveUnitAttack(CombatUnit attacker, CombatUnit target)
    {
        isResolvingAction = true;

        board.ClearPath();

        int damage = attacker.Stats.GetDamageForCommand(commandMode);

        Debug.Log($"{attacker.name} uses {commandMode} on {target.name} for {damage} damage.");

        attacker.FaceFromTo(attacker.GridPosition, target.GridPosition);
        attacker.PlayAttack(commandMode);

        // Later:
        // - particle FX
        if (commandMode == CommandMode.RangedAttack)
        {

            rangedAttackContext = new RangedAttackContext(attacker, target, damage);
            yield break;
            //yield return ResolveRangedAttackVisual(attacker, target);
        }
        else if (commandMode == CommandMode.MeleeAttack)
        {
            yield return ResolveMeleeAttackVisual(attacker, target);
        }

        attacker.PlayIdle();

        if (target.TakeDamage(damage))
        {
            turnManager.RemoveUnitFromBattle(target);
        }

        FinishAttackResolution(attacker);
    }

    public void AnimEvent_FireRangedProjectile(CombatUnit eventOwner)
    {
        if (rangedAttackContext == null)
            return;

        if (rangedAttackContext.attacker != eventOwner)
            return;

        if (rangedAttackContext.target == null || !rangedAttackContext.target.IsAlive)
            return;

        StartCoroutine(FireProjectileAndApplyDamage(rangedAttackContext.attacker, rangedAttackContext.target, rangedAttackContext.damage));
    }

    private IEnumerator FireProjectileAndApplyDamage(CombatUnit attacker, CombatUnit target, int damage)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned.");
            ApplyRangedDamage(target, damage);
            yield break;
        }

        Vector3 startWorld = board.ConvertGridToWorld(attacker.GridPosition) + projectileSpawnOffset;
        Vector3 targetWorld = board.ConvertGridToWorld(target.GridPosition) + projectileHitOffset;

        GameObject projectileObject = Instantiate(projectilePrefab, startWorld, Quaternion.identity);

        ProjectileMover projectileMover = projectileObject.GetComponent<ProjectileMover>();

        if (projectileMover == null)
        {
            Debug.LogWarning("Projectile prefab has no ProjectileMover.");
            Destroy(projectileObject);
            ApplyRangedDamage(target, damage);
            yield break;
        }

        yield return projectileMover.FlyAndHit(startWorld, targetWorld);

        ApplyRangedDamage(target, damage);
    }

    public void AnimEvent_RangedAttackFinished(CombatUnit eventOwner)
    {
        if (rangedAttackContext == null)
            return;

        if (rangedAttackContext.attacker != eventOwner)
            return;

        CombatUnit attacker = rangedAttackContext.attacker;

        rangedAttackContext = null;

        FinishAttackResolution(attacker);
    }

    private void ApplyRangedDamage(CombatUnit target, int damage)
    {
        if (target == null || !target.IsAlive)
            return;

        if (target.TakeDamage(damage))
        {
            turnManager.RemoveUnitFromBattle(target);
        }

        turnManager.RefreshBattleEndState();
    }

    private IEnumerator ResolveMeleeAttackVisual(CombatUnit attacker, CombatUnit target)
    {
        
        // Later we can add Animator.Play("Melee_Attack") here.

        Vector3 spawnWorld = board.ConvertGridToWorld(attacker.GridPosition) + projectileSpawnOffset;
        GameObject slashObject = Instantiate(swordSlashPrefab, spawnWorld, Quaternion.identity);
        slashObject.GetComponent<Animator>().Play("SwordSlash");

        CameraShakeImpulse.PlayLongMediumHit();

        yield return new WaitForSeconds(0.45f);
        Destroy(slashObject);

        yield return null;
    }

    // Shared finish helper (melee and ranged both reuse)
    private void FinishAttackResolution(CombatUnit attacker)
    {
        attacker.PlayIdle();
        attacker.MarkAttacked();

        board.ClearHighlights();
        board.ClearPath();

        reachableCells.Clear();
        hoveredCoord = null;

        commandMode = CommandMode.None;
        isResolvingAction = false;

        selectedUnit = attacker;

        turnManager.RefreshBattleEndState();
        actionMenuUI.Refresh(selectedUnit);
    }

    // --------------------
    // End Turn 
    // --------------------
    private void TryEndPlayerTurn()
    {
        if (!turnManager.IsPlayerTurn())
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

        Debug.Log("Move Command selected");
        SetCommandMode(CommandMode.Move);
    }

    public void SelectRangedAttackCommand()
    {
        if (selectedUnit == null || !selectedUnit.CanAttack)
            return;

        SetCommandMode(CommandMode.RangedAttack);
    }

    public void SelectMeleeAttackCommand()
    {
        if (selectedUnit == null || !selectedUnit.CanAttack)
            return;

        SetCommandMode(CommandMode.MeleeAttack);
    }

    public void EndSelectedUnitTurn()
    {
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