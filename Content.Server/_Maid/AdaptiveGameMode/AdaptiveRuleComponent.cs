using System.Collections.Generic;
using Content.Server._Maid.AdaptiveGameMode.Conditions;
using Content.Shared._Maid.Utils;
using Robust.Shared.Prototypes;

namespace Content.Server._Maid.AdaptiveGameMode;

/// <summary>
/// Gamerule component for the Adaptive game mode.
/// </summary>
[RegisterComponent]
public sealed partial class AdaptiveRuleComponent : Component
{
    [DataField]
    public float TargetChaosValue = 0f;

    /// <summary>
    /// Gamerules that get added at round start.
    /// </summary>
    [DataField]
    public List<AdaptiveRuleParam> RoundstartRules = new();

    /// <summary>
    /// How often we spawn midround rules.
    /// </summary>
    [DataField(required: true)]
    public RangedNumber MidroundSpawnTimer = new();

    /// <summary>
    /// The probability that we skip spawning a rule.
    /// </summary>
    [DataField]
    public float MidroundSpawnSkipProb = 0.6f;

    /// <summary>
    /// Gamerules that can spawn midround.
    /// </summary>
    [DataField]
    public List<AdaptiveRuleParam> MidroundRules = new();

    [DataField]
    public List<AdaptiveSpawnedRule> SpawnedRules = new();
}

[DataDefinition]
public sealed partial class AdaptiveSpawnedRule
{
    [DataField(required: true)]
    public string RuleId = string.Empty;

    [DataField]
    public EntityUid Entity;

    [DataField]
    public TimeSpan SpawnTime;
}

[DataDefinition]
public sealed partial class AdaptiveRuleParam
{
    [DataField(required: true)]
    public EntProtoId Id;

    [DataField]
    public float BaseWeight = 10f;

    [DataField]
    public List<AdaptiveRuleCondition> Conditions = new();
}
