using UnityEngine;

[CreateAssetMenu(menuName = "AeonSector/Unit Stats", fileName = "UnitStats_")]

public class UnitStats : ScriptableObject
{
    [Header("Identity")]
    public string UnitName;
    public UnitType UnitType;

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

            default:
                return 0;
        }
    }
}
