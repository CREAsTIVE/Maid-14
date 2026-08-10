using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScoreAliveCondition : IAdaptiveScoreCondition
{
    [DataField]
    public bool AllowCritical = false;

    public bool ConditionMet(EntityUid? mob, Entity<MindComponent>? mind, IEntityManager entMan)
    {
        if (mob == null || !entMan.TryGetComponent<MobStateComponent>(mob.Value, out var mobState))
            return false;

        var mobStateSystem = entMan.System<MobStateSystem>();

        if (AllowCritical && mobStateSystem.IsCritical(mob.Value, mobState))
            return true;

        return mobStateSystem.IsAlive(mob.Value, mobState);
    }
}
