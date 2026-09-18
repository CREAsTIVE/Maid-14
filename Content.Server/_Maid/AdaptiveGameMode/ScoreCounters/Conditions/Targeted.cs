using Content.Shared.Mind;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public abstract partial class Targeted : AdaptiveScoreCondition
{
    [DataField]
    public AdaptiveScoreConditionTarget Target = AdaptiveScoreConditionTarget.Owner;

    public override bool ConditionMet(EntityUid owner, EntityUid? controlledMob, Entity<MindComponent>? mind, IEntityManager entMan)
    {
        return ConditionMetOnTarget(AdaptiveScoreCondition.ResolveTarget(Target, owner, controlledMob, mind), entMan);
    }

    protected abstract bool ConditionMetOnTarget(EntityUid? ent, IEntityManager entityManager);
}
