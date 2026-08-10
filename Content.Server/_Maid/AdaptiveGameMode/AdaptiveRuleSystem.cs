using Content.Server.GameTicking.Rules;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

namespace Content.Server._Maid.AdaptiveGameMode;

/// <summary>
/// Gamerule system for the Adaptive game mode.
/// </summary>
public sealed class AdaptiveRuleSystem : GameRuleSystem<AdaptiveRuleComponent>
{
    /// <summary>
    /// Gets the current chaos score by broadcasting a <see cref="GetAdaptiveScoreEvent"/>.
    /// </summary>
    public GetAdaptiveScoreEvent CalculateChaosScore()
    {
        var ev = new GetAdaptiveScoreEvent();
        RaiseLocalEvent(ref ev);
        return ev;
    }
}
