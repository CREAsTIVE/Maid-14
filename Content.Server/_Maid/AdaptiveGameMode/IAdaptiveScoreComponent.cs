using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

namespace Content.Server._Maid.AdaptiveGameMode;

/// <summary>
/// Interface implemented by components that provide static or alive adaptive scores.
/// </summary>
public interface IAdaptiveScoreComponent
{
    /// <summary>
    /// The chaos score contribution.
    /// </summary>
    ScoreSlope ChaosScore { get; }

    /// <summary>
    /// The combat score contribution.
    /// </summary>
    ScoreSlope CombatScore { get; }
}
