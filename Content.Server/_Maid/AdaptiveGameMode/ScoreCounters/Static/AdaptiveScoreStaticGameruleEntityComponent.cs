using Robust.Shared.Prototypes;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Static;

/// <summary>
/// How much EXPECTED score it providers (counts only when choosing game rule)
/// </summary>
[RegisterComponent]
public sealed partial class AdaptiveScoreStaticGameruleEntityComponent : Component
{
    [DataField("prototype", required: true)]
    public EntProtoId Prototype { get; set; }

    [DataField("count")]
    public int Count { get; set; } = 1;
}
