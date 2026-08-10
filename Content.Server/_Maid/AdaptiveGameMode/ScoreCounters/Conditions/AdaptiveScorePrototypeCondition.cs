using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class AdaptiveScorePrototypeCondition : IAdaptiveScoreCondition
{
    [DataField(required: true)]
    public List<string> Prototypes { get; set; } = new();

    public bool ConditionMet(EntityUid uid, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<MetaDataComponent>(uid, out var meta) || meta.EntityPrototype == null)
            return false;

        return Prototypes.Contains(meta.EntityPrototype.ID);
    }
}
