using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class TurnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TilemapBoardAdapter board;

    [Header("Scene Unit References")]
    [SerializeField] private List<CombatUnit> playerUnits = new();
    [SerializeField] private List<CombatUnit> enemyUnits = new();

    public Faction PlayerFaction { get; private set; }
    public Faction EnemyFaction { get; private set; }
    public Faction CurrentFaction { get; private set; }
    public BattleState CurrentBattleState { get; private set; } = BattleState.None;

    private void Start()
    {
        InitializeBattle();
        StartPlayerTurn();
    }
    private void InitializeBattle()
    {
        if (board == null) {
            Debug.LogError("TurnManager requires a TilemapBoardAdapter reference");
            return;
        }

        PlayerFaction = new Faction(FactionType.Player);
        EnemyFaction = new Faction(FactionType.Enemy);

        RegisterSceneUnits(playerUnits, PlayerFaction);
        RegisterSceneUnits(enemyUnits, EnemyFaction);

        Debug.Log("Battle initialized");
        //Debug.Log($"Player Units: {PlayerFaction.UnitManager.GetLivingUnits().Count()}");
        //Debug.Log($"Enemy Units: {EnemyFaction.UnitManager.GetLivingUnits().Count()}");
    }

    private void RegisterSceneUnits(List<CombatUnit> sceneUnits, Faction faction)
    {
        foreach (CombatUnit unit in sceneUnits)
        {
            if (unit == null){ continue; }

            GridCoord sceneCoord = board.ConvertWorldToGrid(unit.transform.position);

            if (!board.IsInside(sceneCoord))
            {
                Debug.LogError( $"Unit '{unit.name}' is outside the board. " + $"World:{unit.transform.position} Grid:{sceneCoord}");
                continue;
            }

            if (!board.HasBaseWalkable(sceneCoord))
            {
                Debug.LogError($"Unit '{unit.name}' was placed on a non-walkable cell {sceneCoord}.");
                continue;
            }

            bool placed = board.TryPlaceUnit(unit, sceneCoord);
            if (!placed)
            {
                Debug.LogError($"Failed to place unit '{unit.name}' at inferred cell {sceneCoord}. " +$"Cell may already be occupied.");
            }

            // Register Unit only if placement succeed 
            unit.Initialize(faction, sceneCoord);
            faction.RegisterUnit(unit);

            Vector3 worldPos = board.ConvertGridToWorld(sceneCoord);
            unit.transform.position = worldPos;
            //Debug.Log($"{unit.name} placed at inferred cell {sceneCoord}");
        }
    }

    public void StartPlayerTurn()
    {
        if (CheckBattleEnd()) { return; }

        CurrentFaction = PlayerFaction;
        CurrentFaction.BeginTurn();
        CurrentBattleState = BattleState.PlayerTurn;

        Debug.Log("--- PLAYER TURN ---");
    }
    public void StartEnemyTurn()
    {
        if (CheckBattleEnd()) { return; }

        CurrentFaction = EnemyFaction;
        CurrentFaction.BeginTurn();
        CurrentBattleState = BattleState.EnemyTurn;

        Debug.Log("--- ENEMY TURN ---");
    }


    public void EndCurrentTurn()
    {
        if (CurrentBattleState == BattleState.Victory || CurrentBattleState == BattleState.Defeat) {
            return;
        }

        if (CurrentFaction == PlayerFaction) {
            StartEnemyTurn();
        }
        else {
            StartPlayerTurn();
        }
    }

    public bool IsPlayerTurn()
    {
        return CurrentFaction == PlayerFaction;
    }

    public bool IsEnemyTurn()
    {
        return CurrentFaction == EnemyFaction;
    }

    private bool CheckBattleEnd()
    {
        bool playerAlive = PlayerFaction.HasLivingUnits();
        bool enemyAlive = EnemyFaction.HasLivingUnits();

        if (!playerAlive) {
            CurrentBattleState = BattleState.Defeat;
            Debug.Log("DEFEAT");
            return true;
        }

        if (!enemyAlive) {
            CurrentBattleState = BattleState.Victory;
            Debug.Log("VICTORY");
            return true;
        }

        return false;
    }

    public bool RefreshBattleEndState()
    {
        return CheckBattleEnd();
    }

    public void RemoveUnitFromBattle(CombatUnit unit)
    {
        if (unit == null)
            return;

        board.RemoveUnit(unit);

        if (unit.OwnerFaction != null)
        {
            unit.OwnerFaction.UnitManager.Remove(unit);
        }

        unit.gameObject.SetActive(false);
        Destroy(unit.gameObject);
    }

    // Debug ContextMenu for Pass 1

    [ContextMenu("Debug End Turn")]
    public void DebugEndTurn()
    {
        EndCurrentTurn();
    }

    //[ContextMenu("Debug Damage First Enemy")]
    //public void DebugDamageFirstEnemy()
    //{
    //    CombatUnit firstEnemy = EnemyFaction?.UnitManager?.GetLivingUnits()?.FirstOrDefault();
    //    if (firstEnemy != null) {
    //        firstEnemy.TakeDamage(999);
    //        CheckBattleEnd();
    //    }
    //}

    [ContextMenu("Debug Print State")]
    public void DebugPrintState()
    {
        Debug.Log($"Current State: {CurrentBattleState}");
        Debug.Log($"Current Faction: {(CurrentFaction != null ? CurrentFaction.Type.ToString() : "None")}");

        foreach (CombatUnit unit in PlayerFaction.UnitManager.Units)
            Debug.Log(unit);

        foreach (CombatUnit unit in EnemyFaction.UnitManager.Units)
            Debug.Log(unit);
    }

    [ContextMenu("Debug Reachable Tiles For First Player")]
    public void DebugReachableTilesForFirstPlayer()
    {
        CombatUnit firstPlayer = PlayerFaction?.UnitManager?.GetLivingUnits()?.FirstOrDefault();
        if (firstPlayer == null || board == null) { return; }

        var service = new ReachableTileService();
        var reachable = service.GetReachableTiles(board, firstPlayer.GridPosition, firstPlayer.Stats.MoveRange);

        foreach (var coord in reachable) {
            Debug.Log($"Reachable: {coord}");
        }
    }

}
