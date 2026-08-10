using Content.Server._Maid.AdaptiveGameMode;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

public abstract class AdaptiveScoreByAmountSystem : EntitySystem, IAdaptiveBalanceInfoProvider
{
    protected virtual float? ChaosContribution => null;
    protected virtual float? PvpContribution => null;
    protected abstract string EntityPrototype { get; }
    protected abstract string ConditionName { get; }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GetAdaptiveScoreEvent>(OnGetAdaptiveScore);
    }

    private void OnGetAdaptiveScore(ref GetAdaptiveScoreEvent ev)
    {
        var amount = GetAmount();
        if (ChaosContribution.HasValue)
            ev.ChaosScore += amount * ChaosContribution.Value;
        if (PvpContribution.HasValue)
            ev.CombatScore += amount * PvpContribution.Value;
    }

    protected abstract int GetAmount();

    // Balance table generation (yes, its dirty, but that will help a lot)
    public virtual IEnumerable<AdaptiveBalanceInfo> GetBalanceInfo()
    {
        yield return new AdaptiveBalanceInfo(
            entity: EntityPrototype,
            condition: ConditionName,
            pvpFrom: PvpContribution,
            chaosFrom: ChaosContribution
        );
    }
}
