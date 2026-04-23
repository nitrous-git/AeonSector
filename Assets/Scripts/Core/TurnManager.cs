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
        Debug.Log($"Player Units: {PlayerFaction.UnitManager.GetLivingUnits().Count()}");
        Debug.Log($"Enemy Units: {EnemyFaction.UnitManager.GetLivingUnits().Count()}");
    }

    private void RegisterSceneUnits(List<CombatUnit> sceneUnits, Faction faction)
    {
        foreach (CombatUnit unit in sceneUnits)
        {
            if (unit == null){ return; }

            unit.Initialize(faction, unit.StartingGridPosition);
            faction.RegisterUnit(unit);
            Debug.Log(unit.ToString());

            bool placed = board.TryPlaceUnit(unit, unit.StartingGridPosition);

            if (!placed)
            {
                Debug.LogError($"Failed to place unit '{unit.name}' at {unit.StartingGridPosition}");
            }
            else
            {
                Vector3 worldPos = board.ConvertGridToWorld(unit.StartingGridPosition);
                unit.transform.position = worldPos;
            }
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

    // Debug ContextMenu for Pass 1
     
    [ContextMenu("Debug End Turn")]
    public void DebugEndTurn()
    {
        EndCurrentTurn();
    }

    [ContextMenu("Debug Damage First Enemy")]
    public void DebugDamageFirstEnemy()
    {
        CombatUnit firstEnemy = EnemyFaction?.UnitManager?.GetLivingUnits()?.FirstOrDefault();
        if (firstEnemy != null) {
            firstEnemy.TakeDamage(999);
            CheckBattleEnd();
        }
    }

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
