using Content.Shared.Ghost;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScoreControlledCondition : IAdaptiveScoreCondition
{
    public bool ConditionMet(EntityUid uid, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<ActorComponent>(uid, out var actor))
            return false;

        if (actor.PlayerSession.Status != SessionStatus.InGame)
            return false;

        if (entMan.HasComponent<GhostComponent>(uid))
            return false;

        return true;
    }
}
