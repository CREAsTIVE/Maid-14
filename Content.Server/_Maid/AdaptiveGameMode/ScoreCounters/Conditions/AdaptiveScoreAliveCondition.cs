using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScoreAliveCondition : IAdaptiveScoreCondition
{
    [DataField]
    public bool AllowCritical = false;

    public bool ConditionMet(EntityUid uid, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<MobStateComponent>(uid, out var mobState))
            return false;

        var mobStateSystem = entMan.System<MobStateSystem>();

        if (AllowCritical && mobStateSystem.IsCritical(uid, mobState))
            return true;

        return mobStateSystem.IsAlive(uid, mobState);
    }
}
