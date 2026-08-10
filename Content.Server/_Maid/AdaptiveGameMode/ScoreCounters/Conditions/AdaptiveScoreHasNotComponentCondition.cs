using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScoreHasNotComponentCondition : IAdaptiveScoreCondition
{
    [DataField(required: true)]
    public List<string> Components { get; set; } = new();

    public bool ConditionMet(EntityUid uid, IEntityManager entMan)
    {
        var compFactory = IoCManager.Resolve<IComponentFactory>();
        foreach (var compName in Components)
        {
            if (!compFactory.TryGetRegistration(compName, out var registration))
                continue;

            if (entMan.HasComponent(uid, registration.Type))
                return false;
        }
        return true;
    }
}
