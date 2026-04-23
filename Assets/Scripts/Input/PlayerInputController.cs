using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private TilemapBoardAdapter board;
    [SerializeField] private Camera mainCamera;

    [Header("Selection")]
    [SerializeField] private LayerMask unitLayerMask;

    private readonly ReachableTileService reachableTileService = new();

    public CombatUnit SelectedUnit { get; private set; }

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (turnManager == null || board == null || mainCamera == null)
            return;

        if (!turnManager.IsPlayerTurn())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        if (Input.GetMouseButtonDown(1))
        {
            ClearSelection();
        }
    }

    private void HandleLeftClick()
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0.0f;

        Collider2D hit = Physics2D.OverlapPoint(worldPos, unitLayerMask);
        if (hit == null)
        {
            ClearSelection();
            return;
        }

        CombatUnit clickedUnit = hit.GetComponent<CombatUnit>();
        if (clickedUnit == null)
        {
            ClearSelection();
            return;
        }

        if (clickedUnit.OwnerFaction != turnManager.PlayerFaction)
        {
            ClearSelection();
            return;
        }

        if (!clickedUnit.IsAlive)
        {
            ClearSelection();
            return;
        }

        Debug.Log($"SelectedUnit : {clickedUnit.name}");
        SelectUnit(clickedUnit);
    }

    private void SelectUnit(CombatUnit clickedUnit)
    {
        SelectedUnit = clickedUnit;

        HashSet<GridCoord> reachable = reachableTileService.GetReachableTiles(board, clickedUnit.GridPosition, clickedUnit.Stats.MoveRange);

        board.ClearHighlights();
        board.ShowCells(reachable);
        Debug.Log($"Selected : {clickedUnit.name} at position : {clickedUnit.GridPosition}");
    }

    private void ClearSelection()
    {
        SelectedUnit = null;
        board.ClearHighlights();
    }

}
