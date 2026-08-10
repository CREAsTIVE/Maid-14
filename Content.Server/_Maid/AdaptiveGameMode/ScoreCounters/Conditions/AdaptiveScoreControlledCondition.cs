using Content.Shared.Ghost;
using Content.Shared.Mind;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScoreControlledCondition : IAdaptiveScoreCondition
{
    public bool ConditionMet(EntityUid? mob, Entity<MindComponent>? mind, IEntityManager entMan)
    {
        if (mob == null || !entMan.TryGetComponent<ActorComponent>(mob.Value, out var actor))
            return false;

        if (actor.PlayerSession.Status != SessionStatus.InGame)
            return false;

        if (entMan.HasComponent<GhostComponent>(mob.Value))
            return false;

        return true;
    }
}
