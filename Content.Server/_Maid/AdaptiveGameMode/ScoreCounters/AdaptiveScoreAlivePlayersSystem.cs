using Content.Server._Maid.AdaptiveGameMode;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Station;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters;

/// <summary>
/// System that calculates negative chaos/combat score contribution based on the number of active, alive on-station crew members.
/// </summary>
public sealed class AdaptiveScoreAlivePlayersSystem : AdaptiveScoreByAmountSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    protected override float? ChaosContribution => -2f;
    protected override string EntityPrototype => "MobHuman";
    protected override string ConditionName => "AlivePlayersSystem";

    protected override int GetAmount()
    {
        var query = EntityQueryEnumerator<ActorComponent, MobStateComponent, TransformComponent>();
        var count = 0;
        while (query.MoveNext(out var uid, out var actor, out var mobState, out var xform))
        {
            if (actor.PlayerSession.Status != Robust.Shared.Enums.SessionStatus.InGame)
                continue;

            if (!_mobState.IsAlive(uid, mobState))
                continue;

            if (xform.GridUid == null || _station.GetOwningStation(uid) == null)
                continue;

            // TODO: add check if player is there and controlling entity

            count++;
        }

        return count;
    }
}
