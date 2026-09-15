using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

[ImplicitDataDefinitionForInheritors]
public abstract partial class AdaptiveScoreCondition
{
    public virtual string BalanceTableName => GetType().Name;
    public abstract bool ConditionMet(EntityUid owner, EntityUid? controlledMob, Entity<MindComponent>? mind, IEntityManager entMan);

    public static EntityUid? ResolveTarget(
        AdaptiveScoreConditionTarget target,
        EntityUid owner,
        EntityUid? controlledMob,
        Entity<MindComponent>? mind
    ) => target switch
    {
        AdaptiveScoreConditionTarget.Owner => owner,
        AdaptiveScoreConditionTarget.Mind => mind,
        AdaptiveScoreConditionTarget.Mob => controlledMob,
        _ => null,
    };
}

[Serializable]
public enum AdaptiveScoreConditionTarget
{
    Owner = 0,
    Mind = 1,
    Mob = 2, // Controlled mob by mind
}
