using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScorePlayerUncuffedCondition : IAdaptiveScoreCondition
{
    public bool ConditionMet(EntityUid uid, IEntityManager entMan)
    {
        if (entMan.TryGetComponent<CuffableComponent>(uid, out var cuffable))
        {
            var cuffs = entMan.System<SharedCuffableSystem>();
            if (cuffs.IsCuffed((uid, cuffable)))
                return false;
        }

        return true;
    }
}
