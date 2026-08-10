using Content.Shared.Mind;
using Content.Shared.Station;
using Robust.Shared.GameObjects;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScoreOnStationGridCondition : IAdaptiveScoreCondition
{
    public bool ConditionMet(EntityUid? mob, Entity<MindComponent>? mind, IEntityManager entMan)
    {
        if (mob == null)
            return false;

        var station = entMan.System<SharedStationSystem>();
        return station.GetOwningStation(mob.Value) != null;
    }
}
