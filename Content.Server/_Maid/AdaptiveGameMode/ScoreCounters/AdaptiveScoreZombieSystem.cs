using Content.Server._Maid.AdaptiveGameMode;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Zombies;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

/// <summary>
/// System that calculates chaos/combat score contribution for Zombie Patient Zero (Initial Infected).
/// </summary>
public sealed class AdaptiveScoreZombieSystem : AdaptiveScoreByAmountSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    protected override float? ChaosContribution => 15f;
    protected override float? PvpContribution => 0f;
    protected override string EntityPrototype => "ZombieInitialInfected";
    protected override string ConditionName => "ZombieRuleSystem";

    protected override int GetAmount()
    {
        var query = EntityQueryEnumerator<InitialInfectedComponent, MobStateComponent>();
        var count = 0;
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            if (!_mobState.IsAlive(uid, mobState))
                continue;

            count++;
        }

        return count;
    }
}
