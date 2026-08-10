using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScoreMindHasComponentCondition : IAdaptiveScoreCondition
{
    [DataField(required: true)]
    public List<string> Components { get; set; } = new();

    public bool ConditionMet(EntityUid uid, IEntityManager entMan)
    {
        var mindSystem = entMan.System<SharedMindSystem>();
        if (!mindSystem.TryGetMind(uid, out var mindId, out _))
            return false;

        var compFactory = IoCManager.Resolve<IComponentFactory>();
        foreach (var compName in Components)
        {
            if (!compFactory.TryGetRegistration(compName, out var registration))
                return false;

            if (!entMan.HasComponent(mindId, registration.Type))
                return false;
        }
        return true;
    }
}
