using System.Linq;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Collector;
using Content.Shared.Mind;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AnyOf : AdaptiveScoreCondition
{
    [DataField]
    public List<List<AdaptiveScoreCondition>> Conditions = [];

    public override bool ConditionMet(EntityUid owner, EntityUid? controlledMob, Entity<MindComponent>? mind, IEntityManager entMan)
    {
        var sys = entMan.System<AdaptiveScoreCollectorSystem>();

        return Conditions.Any(conditions => sys.IsConditionsMet(conditions, owner));
    }
}
