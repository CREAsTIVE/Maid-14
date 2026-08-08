using System.Collections.Generic;
using Content.Server._Maid.AdaptiveGameMode.Conditions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server._Maid.AdaptiveGameMode;

/// <summary>
/// Definition of a midround rule that can be spawned in the Adaptive game mode.
/// </summary>
[DataDefinition]
public sealed partial class AdaptiveRuleParam
{
    /// <summary>
    /// The entity prototype ID of the game rule.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Id;

    /// <summary>
    /// The base weight of the rule when selecting which rule to spawn.
    /// </summary>
    [DataField]
    public float BaseWeight = 10f;

    /// <summary>
    /// The conditions required for this rule to be eligible for spawning.
    /// </summary>
    [DataField]
    public List<AdaptiveRuleCondition> Conditions = new();
}
