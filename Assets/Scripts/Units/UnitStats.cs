using UnityEngine;

[CreateAssetMenu(menuName = "AeonSector/Unit Stats", fileName = "UnitStats_")]

public class UnitStats : ScriptableObject
{
    [Header("Identity")]
    public string UnitName;
    public UnitType UnitType;
    public Sprite UnitIcon;

    [Header("Combat permissions (independent of identity)")]
    public AttackCapabilities SupportedAttacks = AttackCapabilities.None;
    public EnemyCombatRole EnemyRole = EnemyCombatRole.None;
    public GameObject RangedProjectilePrefab;

    public bool Supports(CommandMode mode)
    {
        switch (mode)
        {
            case CommandMode.MeleeAttack: return (SupportedAttacks & AttackCapabilities.Melee) != 0 && MeleeMinAttackRange >= 0 && MeleeMaxAttackRange >= MeleeMinAttackRange && MeleeAttackDamage >= 0;
            case CommandMode.RangedAttack: return (SupportedAttacks & AttackCapabilities.Ranged) != 0 && RangedMinAttackRange >= 1 && RangedMaxAttackRange >= RangedMinAttackRange && RangedAttackDamage >= 0;
            case CommandMode.AreaAttack: return (SupportedAttacks & AttackCapabilities.Area) != 0 && AreaAttackMinRange >= 0 && AreaAttackMaxRange >= AreaAttackMinRange && AreaAttackDamage >= 0;
            default: return false;
        }
    }

    private void OnValidate()
    {
        if (SupportedAttacks == AttackCapabilities.None)
            Debug.LogWarning($"{name}: no supported attacks configured.", this);
        foreach (var mode in new[] { CommandMode.MeleeAttack, CommandMode.RangedAttack, CommandMode.AreaAttack })
        {
            var flag = mode == CommandMode.MeleeAttack ? AttackCapabilities.Melee : mode == CommandMode.RangedAttack ? AttackCapabilities.Ranged : AttackCapabilities.Area;
            if ((SupportedAttacks & flag) != 0 && !Supports(mode))
                Debug.LogWarning($"{name}: invalid damage/range for {mode}; command disabled.", this);
        }
        if (EnemyRole != EnemyCombatRole.None && !Supports(EnemyTacticalPlanner.CommandFor(EnemyRole)))
            Debug.LogWarning($"{name}: enemy role requires an unsupported attack; AI will skip this unit.", this);
    }

    [Header("Core Stats")]
    public int MaxHP = 10;
    public int MoveRange = 4;

    [Header("Melee Attack")]
    public int MeleeAttackDamage = 4;
    public int MeleeMinAttackRange = 1;
    public int MeleeMaxAttackRange = 1;

    [Header("Ranged Attack")]
    public int RangedAttackDamage = 2;
    public int RangedMinAttackRange = 2;
    public int RangedMaxAttackRange = 5;

    [Header("Card Display")]
    public int DisplayMaxHP = 0;
    public int DisplayMaxEN = 0;

    [Header("Area Attack")]
    public int AreaAttackDamage = 2;
    public int AreaAttackMinRange = 2;
    public int AreaAttackMaxRange = 5;

    // ---------------
    // Helpers
    // ---------------
    public int GetDamageForCommand(CommandMode mode)
    {
        switch (mode)
        {
            case CommandMode.MeleeAttack:
                return MeleeAttackDamage;

            case CommandMode.RangedAttack:
                return RangedAttackDamage;

            case CommandMode.AreaAttack:
                return AreaAttackDamage;

            default:
                return 0;
        }
    }
}
