using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Mind;
using Content.Shared.Roles;
using System.Collections.Generic;
using Content.Shared.Roles.Jobs;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

[DataDefinition]
public sealed partial class AdaptiveScoreMindHasJobCondition : IAdaptiveScoreCondition
{
    [DataField(required: true)]
    public List<string> Jobs { get; set; } = [];

    public bool ConditionMet(EntityUid? mob, Entity<MindComponent>? mind, IEntityManager entMan)
    {
        if (mind == null)
            return false;

        var jobSystem = entMan.System<SharedJobSystem>();
        if (!jobSystem.MindTryGetJobId(mind.Value.Owner, out var jobId) || jobId is null)
            return false;

        return Jobs.Contains(jobId.Value);
    }
}
