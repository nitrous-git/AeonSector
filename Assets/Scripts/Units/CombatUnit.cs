using Unity.VisualScripting;
using UnityEngine;

public class CombatUnit : MonoBehaviour
{
    [SerializeField] private UnitStats stats;
    [SerializeField] private UnitFacing startingFacing = UnitFacing.SouthWest;
    [SerializeField] private UnitVisualAnimator visualAnimator;

    public UnitStats Stats => stats;
    public Faction OwnerFaction { get; private set; }
    public GridCoord GridPosition { get; private set; }

    public UnitFacing Facing { get; private set; }

    public int CurrentHP { get; private set; }
    public bool HasMoved { get; private set; }
    public bool HasAttacked { get; private set; }

    public bool IsAlive => CurrentHP > 0;
    public bool CanMove => IsAlive && !HasMoved;
    public bool CanAttack => IsAlive && !HasAttacked;
    public bool IsTurnExhausted => !CanMove && !CanAttack;

    private void Awake()
    {
        if (visualAnimator == null)
        {
            visualAnimator = GetComponentInChildren<UnitVisualAnimator>();
        }
    }

    public void Initialize(Faction ownerFaction, GridCoord startCoord)
    {
        OwnerFaction = ownerFaction;
        GridPosition = startCoord;

        //Debug.Log($"{ownerFaction.Type.ToString()} , {startCoord}");

        if (stats == null) {
            Debug.LogError($"CombatUnit '{name}' has no UnitStats assigned");
            return;
        }

        CurrentHP = stats.MaxHP;
        HasMoved = false;
        HasAttacked = false;

        SetFacing(startingFacing);
        PlayIdle();
    }

    public void BeginTurn()
    {
        if (!IsAlive) { return; }
        HasMoved = false;
        HasAttacked = false;
    }

    public void MarkMoved()
    {
        if (!IsAlive) { return; }
        HasMoved = true;
    }

    public void MarkAttacked()
    {
        if (!IsAlive) { return; }
        HasAttacked = true;
    }

    public void SetGridPosition(GridCoord coord)
    {
        GridPosition = coord;
    }

    public bool TakeDamage(int amount)
    {
        if (!IsAlive) 
        { 
            return false; 
        }

        CurrentHP = Mathf.Max(0, CurrentHP - amount);

        if (CurrentHP == 0)
        {
            OnDeath();
            return true;
        }

        return false;
    }

    private void OnDeath()
    {
        HasMoved = true;
        HasAttacked = true;
        Debug.Log($"{name} has been destroyed.");
    }

    // -----------------------
    // Facing helpers 
    // -----------------------

    public void SetFacing(UnitFacing facing)
    {
        Facing = facing;
    }

    public void FaceTowards(GridCoord targetCoord)
    {
        Facing = GetFacingFromDelta(GridPosition, targetCoord);
    }

    public void FaceFromTo(GridCoord fromCoord, GridCoord toCoord)
    {
        Facing = GetFacingFromDelta(fromCoord, toCoord);
    }

    public void PlayIdle()
    {
        if (visualAnimator != null)
            visualAnimator.PlayIdle(Facing);
    }

    public void PlayAttack()
    {
        if (visualAnimator != null)
            visualAnimator.PlayAttack(Facing);
    }

    private UnitFacing GetFacingFromDelta(GridCoord from, GridCoord to)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;

        Debug.Log($"dx : {dx}, dy = {dy}");

        if (dx == 0 && dy == 0)
            return Facing;

        // Fix the mapping here
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            Debug.Log($"dx > dy");
            return dx < 0 ? UnitFacing.SouthWest : UnitFacing.NorthEast;
        }

        Debug.Log($"dx < dy");
        return dy < 0 ? UnitFacing.SouthEast : UnitFacing.NorthWest;
    }

    // -----------------------
    // Pretty printing
    // -----------------------

    public override string ToString()
    {
        string factionName = OwnerFaction != null ? OwnerFaction.Type.ToString() : "None";
        string unitName = stats != null ? stats.UnitName : "NoStats";

        return $"{unitName} [{factionName}] HP:{CurrentHP} Pos:{GridPosition} Facing:{Facing} Moved:{HasMoved} Attacked:{HasAttacked}";
    }

}
