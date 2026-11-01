// Shared HP/death gateway. CombatUnit and TurnManager retain state/lifecycle ownership.
public static class BattleDamage
{
    public static void Apply(TurnManager turns, CombatUnit target, int damage,
        CombatUnit attacker = null, CommandMode mode = CommandMode.MeleeAttack)
    {
        if (turns == null || turns.IsBattleOver || target == null || !target.IsAlive) return;
        int before = target.CurrentHP;
        bool killed = target.TakeDamage(damage);
        // Capture the visible unit before the existing immediate board/roster cleanup.
        CombatPresentation.Damage(target, attacker, before - target.CurrentHP, killed, mode);
        if (killed) turns.RemoveUnitFromBattle(target);
    }
}
