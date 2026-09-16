using System.Linq;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

public sealed partial class MetTable : AdaptiveScoreCondition
{
    [DataField(required: true)]
    public List<ProtoId<AdaptiveScoreConditionsTablePrototype>> Tables { get; set; } = [];

    public override string BalanceTableName => $"{string.Join(" + ", Tables)}";

    public override bool ConditionMet(EntityUid owner, EntityUid? mob, Entity<MindComponent>? mind, IEntityManager entMan)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        foreach (var tableId in Tables)
        {
            if (!protoMan.TryIndex(tableId, out var table))
                return false;

            if (!table.Conditions.All(cond => cond.ConditionMet(owner, mob, mind, entMan)))
                return false;
        }

        return true;
    }
}
