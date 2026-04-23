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
    public int AttackDamage = 3;

    [Header("Attack Range")]
    public int MinAttackRange = 1;
    public int MaxAttackRange = 1;
}
