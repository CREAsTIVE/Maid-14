using Content.Shared._Maid.DynamicGameMode.Rules;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Maid.DynamicGameMode.EntityTableConditions;

/// <summary>
/// Condition that only succeeds if a table supplies a sufficient "cost" to a given
/// </summary>
public sealed partial class HasBudgetCondition : EntityTableCondition
{
    public const string BudgetContextKey = nameof(DynamicRuleComponent.Budget);

    /// <summary>
    /// Used for determining the cost for the budget.
    /// If null, attempts to fetch the cost from the attached selector.
    /// </summary>
    [DataField]
    public int? CostOverride;

    protected override bool EvaluateImplementation(EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        if (!ctx.TryGetData<float>(BudgetContextKey, out var budget))
            return false;

        int cost;
        if (CostOverride != null)
        {
            cost = CostOverride.Value;
        }
        else
        {
            if (root is not EntSelector entSelector)
                return false;

            if (!proto.Index(entSelector.Id).TryGetComponent(out DynamicRuleCostComponent? costComponent, entMan.ComponentFactory))
            {
                var log = Logger.GetSawmill("HasBudgetCondition");
                log.Error($"Rule {entSelector.Id} does not have a DynamicRuleCostComponent.");
                return false;
            }

            cost = costComponent.Cost;
        }

        return budget >= cost;
    }
}
