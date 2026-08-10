using System.Collections.Generic;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

namespace Content.Server._Maid.AdaptiveGameMode.MetaInfo;

// THIS IS ONLY FOR BALANCING PURPOSES! Called manually by admins
public interface IAdaptiveBalanceInfoProvider
{
    IEnumerable<AdaptiveBalanceInfo> GetBalanceInfo();
}

public struct AdaptiveBalanceInfo
{
    public string Entity;
    public string Condition;
    public float? CombatFrom;
    public float? CombatTo;
    public float? CombatDuration;
    public float? ChaosFrom;
    public float? ChaosTo;
    public float? ChaosDuration;

    public override string ToString() =>
        $"{Entity},{Condition},{CombatFrom},{CombatTo},{CombatDuration},{ChaosFrom},{ChaosTo},{ChaosDuration}";

    public static AdaptiveBalanceInfo FromSlope(string entity, string condition, ScoreSlope chaos, ScoreSlope combat)
    {
        return new()
        {
            Entity = entity,
            Condition = condition,
            CombatFrom = combat.Base,
            CombatTo = combat.Target,
            CombatDuration = combat.Target.HasValue ? (float)combat.In.TotalSeconds : null,
            ChaosFrom = chaos.Base,
            ChaosTo = chaos.Target,
            ChaosDuration = chaos.Target.HasValue ? (float)chaos.In.TotalSeconds : null,
        };
    }
}
