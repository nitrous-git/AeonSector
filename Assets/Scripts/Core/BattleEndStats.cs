public readonly struct BattleEndStats
{
    public readonly BattleState result;
    public readonly float battleTimeSeconds;
    public readonly int turnCount;
    public readonly float avgPowerUsedPercent;

    public BattleEndStats(BattleState result, float battleTimeSeconds, int turnCount, float avgPowerUsedPercent)
    {
        this.result = result;
        this.battleTimeSeconds = battleTimeSeconds;
        this.turnCount = turnCount;
        this.avgPowerUsedPercent = avgPowerUsedPercent;
    }
}