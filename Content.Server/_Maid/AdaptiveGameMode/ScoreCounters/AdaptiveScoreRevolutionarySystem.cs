using Content.Server._Maid.AdaptiveGameMode;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Revolutionary.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

/// <summary>
/// System that calculates chaos/combat score contribution for regular revolutionaries.
/// </summary>
public sealed class AdaptiveScoreRevolutionarySystem : AdaptiveScoreByAmountSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    protected override float? ChaosContribution => 5f;
    protected override float? PvpContribution => 5f;
    protected override string EntityPrototype => "Revolutionary";
    protected override string ConditionName => "RevolutionaryRuleSystem";

    protected override int GetAmount()
    {
        var query = EntityQueryEnumerator<RevolutionaryComponent, MobStateComponent>();
        var count = 0;
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            // Head revolutionaries are handled separately (by PlayerFreeComponent)
            if (HasComp<HeadRevolutionaryComponent>(uid))
                continue;

            if (!_mobState.IsAlive(uid, mobState))
                continue;

            count++;
        }

        return count;
    }
}
