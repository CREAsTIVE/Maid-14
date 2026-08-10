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

    public bool ConditionMet(EntityUid? mob, Entity<MindComponent>? mind, IEntityManager entMan)
    {
        if (mind == null)
            return false;

        var compFactory = IoCManager.Resolve<IComponentFactory>();
        foreach (var compName in Components)
        {
            if (!compFactory.TryGetRegistration(compName, out var registration))
                return false;

            if (!entMan.HasComponent(mind.Value.Owner, registration.Type))
                return false;
        }
        return true;
    }
}
