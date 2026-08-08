using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.PlayerAlive;

/// <summary>
/// Component that provides a score contribution to the Adaptive game mode while a player is attached, connected, and not ghosted.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AdaptiveScorePlayerAliveComponent : Component
{
    [DataField(required: true)]
    public ScoreSlope ChaosScore = new();

    [DataField(required: true)]
    public ScoreSlope CombatScore = new();

    [DataField]
    public float CriticalMultiplier = 1f;

    [DataField]
    public bool OnStation = true;

    [DataField, ViewVariables]
    [AutoPausedField]
    public TimeSpan ComponentCreated;
}
