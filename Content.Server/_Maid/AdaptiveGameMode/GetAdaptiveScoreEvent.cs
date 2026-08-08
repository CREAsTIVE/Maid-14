namespace Content.Server._Maid.AdaptiveGameMode;

/// <summary>
/// Raised as a broadcast event to calculate the current total adaptive chaos score.
/// </summary>
[ByRefEvent]
public struct GetAdaptiveScoreEvent()
{
    public float ChaosScore = 0f;
    public float CombatScore = 0f;

    public void Add(float chaos, float combat)
    {
        ChaosScore += chaos;
        CombatScore += combat;
    }

    public void Add(float score) => Add(score, score);

    public float Average => (ChaosScore + CombatScore) / 2f;
}
