using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScoreHasComponentCondition : IAdaptiveScoreCondition
{
    [DataField(required: true)]
    public List<string> Components { get; set; } = [];

    public bool ConditionMet(EntityUid uid, IEntityManager entMan)
    {
        var compFactory = IoCManager.Resolve<IComponentFactory>();
        foreach (var compName in Components)
        {
            if (!compFactory.TryGetRegistration(compName, out var registration))
                return false;

            if (!entMan.HasComponent(uid, registration.Type))
                return false;
        }

        return true;
    }
}
