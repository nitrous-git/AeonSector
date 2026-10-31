// Shared HP/death gateway. CombatUnit and TurnManager retain state/lifecycle ownership.
public static class BattleDamage
{
    public static void Apply(TurnManager turns, CombatUnit target, int damage)
    {
        if (turns == null || turns.IsBattleOver || target == null || !target.IsAlive) return;
        if (target.TakeDamage(damage)) turns.RemoveUnitFromBattle(target);
    }
}
