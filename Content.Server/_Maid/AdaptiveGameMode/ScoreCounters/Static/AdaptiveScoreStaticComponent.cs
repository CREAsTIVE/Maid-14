using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Static;

/// <summary>
/// Component that provides a static score contribution to the Adaptive game mode while the entity exists.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AdaptiveScoreStaticComponent : Component
{
    [DataField(required: true)]
    public ScoreSlope ChaosScore = new();

    [DataField(required: true)]
    public ScoreSlope CombatScore = new();

    [DataField]
    public bool OnStation = true;

    [DataField, ViewVariables]
    [AutoPausedField]
    public TimeSpan ComponentCreated;
}
