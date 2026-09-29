using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Collector;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class ObjectiveOwner : AdaptiveScoreCondition
{
    [DataField]
    public List<AdaptiveScoreCondition> Conditions { get; set; } = [];


    public Entity<MindComponent>? FindOwner(Entity<ObjectiveComponent> objectiveOwner, IEntityManager entMan)
    {
        var query = entMan.EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var uid, out var mind))
        {
            if (mind.Objectives.Contains(objectiveOwner))
                return new Entity<MindComponent>(uid, mind);
        }

        return null;
    }

    public override bool ConditionMet(EntityUid objectiveUid, EntityUid? controlledMob, Entity<MindComponent>? mind, IEntityManager entMan)
    {
        var adaptiveScoreCollectorSystem = entMan.System<AdaptiveScoreCollectorSystem>();

        if (!entMan.TryGetComponent(objectiveUid, out ObjectiveComponent? objectiveComp))
            return false;

        if (FindOwner((objectiveUid, objectiveComp), entMan) is not { } trueMind)
            return false;

        return adaptiveScoreCollectorSystem.IsConditionsMet(Conditions, trueMind);
    }
}
