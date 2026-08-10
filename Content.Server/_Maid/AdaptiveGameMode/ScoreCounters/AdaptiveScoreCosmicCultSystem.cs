using Content.Server._Maid.AdaptiveGameMode;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

public sealed class AdaptiveScoreCosmicCultSystem : AdaptiveScoreByAmountSystem
{
    protected override string EntityPrototype => "CosmicCult";
    protected override string ConditionName => "CosmicCultRuleSystem";

    protected override int GetAmount()
    {
        return 0; // TODO
    }
}
