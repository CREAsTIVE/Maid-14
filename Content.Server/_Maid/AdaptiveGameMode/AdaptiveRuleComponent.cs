using System.Collections.Generic;
using Content.Shared._Maid.Utils;
using Robust.Shared.Prototypes;

namespace Content.Server._Maid.AdaptiveGameMode;

/// <summary>
/// Gamerule component for the Adaptive game mode.
/// </summary>
[RegisterComponent]
public sealed partial class AdaptiveRuleComponent : Component
{
    /// <summary>
    /// Gamerules that get added at round start.
    /// </summary>
    [DataField("roundstartRules")]
    public List<AdaptiveRuleParam> RoundstartRules = new();

    /// <summary>
    /// How often we spawn midround rules.
    /// </summary>
    [DataField("midroundSpawnTimer", required: true)]
    public RangedNumber MidroundSpawnTimer = new();

    /// <summary>
    /// The probability that we skip spawning a rule.
    /// </summary>
    [DataField("midroundSpawnSkipProb")]
    public float MidroundSpawnSkipProb = 0.6f;

    /// <summary>
    /// Gamerules that can spawn midround.
    /// </summary>
    [DataField("midroundRules")]
    public List<AdaptiveRuleParam> MidroundRules = new();
}
