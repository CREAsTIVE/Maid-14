using Content.Server._Maid.AdaptiveGameMode;
using Content.Server.Nuke;
using Content.Shared.Nuke;
using Content.Shared.Station;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Nuke;

public sealed class AdaptiveScoreNukeArmedSystem : AdaptiveScoreByAmountSystem
{
    [Dependency] private readonly SharedStationSystem _station = default!;

    protected override float? ChaosContribution => 50f;
    protected override float? PvpContribution => 0f;
    protected override string EntityPrototype => "NuclearBomb";
    protected override string ConditionName => "NukeSystem";

    protected override int GetAmount()
    {
        var query = EntityQueryEnumerator<NukeComponent, TransformComponent>();
        var count = 0;
        while (query.MoveNext(out var uid, out var nuke, out var xform))
        {
            if (nuke.Status != NukeStatus.ARMED)
                continue;

            // Nuke must be on a station grid
            if (xform.GridUid == null || _station.GetOwningStation(uid) == null)
                continue;

            count++;
        }

        return count;
    }
}
