using Unity.VisualScripting;
using UnityEngine;

public class CombatUnit : MonoBehaviour
{
    [SerializeField] private UnitStats stats;

    public UnitStats Stats => stats;
    public Faction OwnerFaction { get; private set; }
    public GridCoord GridPosition { get; private set; }  
    
    public int CurrentHP { get; private set; }
    public bool HasMoved { get; private set; }
    public bool HasAttacked { get; private set; }

    public bool IsAlive => CurrentHP > 0;
    public bool CanMove => IsAlive && !HasMoved;
    public bool CanAttack => IsAlive && !HasAttacked;
    public bool IsTurnExhausted => !CanMove && !CanAttack;

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

    public override string ToString()
    {
        string factionName = OwnerFaction != null ? OwnerFaction.Type.ToString() : "None";
        string unitName = stats != null ? stats.UnitName : "NoStats";

        return $"{unitName} [{factionName}] HP:{CurrentHP} Pos:{GridPosition} Moved:{HasMoved} Attacked:{HasAttacked}";
    }

}
