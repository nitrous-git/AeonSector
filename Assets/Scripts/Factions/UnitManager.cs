using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitManager 
{
    private readonly List<CombatUnit> units = new(); 
    public IReadOnlyList<CombatUnit> Units => units;

    internal void Register(CombatUnit unit)
    {
        if (unit == null || units.Contains(unit)) { return; }
        units.Add(unit);
    }

    public void Remove(CombatUnit unit)
    {
        if (unit == null){ return; }
        units.Remove(unit);
    }

    public IEnumerable<CombatUnit> GetLivingUnits()
    {
        List<CombatUnit> livingUnits = new();
        foreach (CombatUnit unit in units)
        {
            if (unit != null && unit.IsAlive)
            {
                livingUnits.Add(unit);
            }
        }
        return livingUnits;
    }

    internal bool HasLivingUnits()
    {
        return GetLivingUnits().Any();
    }

    // All units turn are exhausted, go to next faction turn
    public bool AllUnitsExhausted()
    {
        foreach (CombatUnit unit in units) {
            if (unit == null || !unit.IsAlive){ continue; }
            if (!unit.IsTurnExhausted){ return false; }
        }
        return true;
    }

    internal void BeginTurnForAllUnits()
    {
        foreach (CombatUnit unit in GetLivingUnits()) {
            unit.BeginTurn();
        }
    }
}
