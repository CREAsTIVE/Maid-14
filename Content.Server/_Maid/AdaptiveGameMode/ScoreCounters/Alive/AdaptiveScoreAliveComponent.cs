using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Alive;

/// <summary>
/// Component that provides a score contribution to the Adaptive game mode while the entity is alive.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AdaptiveScoreAliveComponent : Component
{
    [DataField(required: true)]
    public ScoreSlope ChaosScore = new();

    [DataField(required: true)]
    public ScoreSlope CombatScore = new();

    [DataField]
    public float CriticalMultiplier = 1f;

    [DataField]
    public bool OnStation = false;

    [DataField, ViewVariables]
    [AutoPausedField]
    public TimeSpan ComponentCreated;
}
