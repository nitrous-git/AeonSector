public class Faction
{
    public FactionType Type { get; set; }
    public UnitManager UnitManager { get; set; }

    public Faction(FactionType type)
    {
        this.Type = type;
        this.UnitManager = new UnitManager();
    }

    public void RegisterUnit(CombatUnit unit)
    {
        UnitManager.Register(unit);
    }

    public bool HasLivingUnits()
    {
        return UnitManager.HasLivingUnits();
    }

    public void BeginTurn()
    {
        UnitManager.BeginTurnForAllUnits();
    }
}