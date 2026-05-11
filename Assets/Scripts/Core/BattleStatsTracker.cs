using System;
using UnityEngine;

public class BattleStatsTracker : MonoBehaviour
{
    private float battleStartTime;
    private int playerTurnCount;

    public void BeginBattle()
    {
        battleStartTime = Time.time;
        playerTurnCount = 0;
    }

    public void RegisterPlayerTurnStarted()
    {
        playerTurnCount++;
    }

    public BattleEndStats BuildEndStats(BattleState result, Faction playerFaction)
    {
        float battleTime = Time.time - battleStartTime;
        float averagePowerUsedPercent = CalculateAvgPowerPercent(result, playerFaction);

        return new BattleEndStats(result, battleTime, playerTurnCount,averagePowerUsedPercent);
    }

    private float CalculateAvgPowerPercent(BattleState result, Faction playerFaction)
    {
        float totalUsedPercent = 0f;
        int unitCount = 0;

        foreach (CombatUnit unit in playerFaction.UnitManager.Units)
        {
            if (unit == null)
            {
                continue;
            }

            totalUsedPercent += unit.GetEnergyUsedPercent();
            unitCount++;
        }


        if (unitCount == 0)
        {
            return result == BattleState.Defeat ? 100f : 0f;
        }

        return Mathf.Clamp01(totalUsedPercent / unitCount) * 100f;

    }
}
