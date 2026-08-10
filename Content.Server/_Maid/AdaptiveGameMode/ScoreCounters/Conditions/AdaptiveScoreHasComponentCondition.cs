using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScoreHasComponentCondition : IAdaptiveScoreCondition
{
    [DataField(required: true)]
    public List<string> Components = [];

    [DataField]
    public bool CheckOnMind = false;

    public bool ConditionMet(EntityUid? mob, Entity<MindComponent>? mind, IEntityManager entMan)
    {
        if (mob is null)
            return false;

        if (CheckOnMind)
        {
            if (mind is null)
                return false;

            mob = mind;
        }

        var compFactory = entMan.ComponentFactory;
        foreach (var compName in Components)
        {
            if (!compFactory.TryGetRegistration(compName, out var registration))
                return false;

            if (!entMan.HasComponent(mob.Value, registration.Type))
                return false;
        }

        return true;
    }
}
