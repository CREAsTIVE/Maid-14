using Content.Server.Administration;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._Maid.AdaptiveGameMode.Commands;

[ToolshedCommand(Name = "adaptive"), AdminCommand(AdminFlags.Round)]
public sealed class AdaptiveCommand : ToolshedCommand
{
    private AdaptiveRuleSystem? _adaptiveRuleSystem;

    [CommandImplementation("calculatescore")]
    public GetAdaptiveScoreEvent CalculateScore()
    {
        _adaptiveRuleSystem ??= GetSys<AdaptiveRuleSystem>();
        return _adaptiveRuleSystem.CalculateChaosScore();
    }

    [CommandImplementation("getchaos")]
    public float GetChaos([PipedArgument] GetAdaptiveScoreEvent input)
    {
        return input.ChaosScore;
    }

    [CommandImplementation("getcombat")]
    public float GetCombat([PipedArgument] GetAdaptiveScoreEvent input)
    {
        return input.CombatScore;
    }

    [CommandImplementation("getaverage")]
    public float GetAverage([PipedArgument] GetAdaptiveScoreEvent input)
    {
        return input.Average;
    }

    [CommandImplementation("generatebalancetable")]
    public string ShowBalanceTable()
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Entity,Condition/Component,PVP From,PVP To,PVP Duration (min),Chaos From,Chaos To,Chaos Duration (min)");

        // 1. Scan systems implementing IAdaptiveBalanceInfoProvider
        var providers = new List<IAdaptiveBalanceInfoProvider>();
        foreach (var type in entitySystemManager.GetEntitySystemTypes())
        {
            if (typeof(IAdaptiveBalanceInfoProvider).IsAssignableFrom(type) &&
                entitySystemManager.TryGetEntitySystem(type, out var system) &&
                system is IAdaptiveBalanceInfoProvider provider)
            {
                providers.Add(provider);
            }
        }

        var systemRows = new List<AdaptiveBalanceInfo>();
        foreach (var provider in providers)
        {
            systemRows.AddRange(provider.GetBalanceInfo());
        }

        foreach (var row in systemRows)
        {
            AppendRow(sb, row);
        }

        // 2. Scan entity prototypes
        var protoRows = new List<AdaptiveBalanceInfo>();
        foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract)
                continue;

            foreach (var (compName, entry) in proto.Components)
            {
                if (entry.Component is IAdaptiveScoreComponent scoreComp)
                {
                    var displayName = compName;
                    if (displayName.StartsWith("AdaptiveScore"))
                        displayName = displayName.Substring("AdaptiveScore".Length);
                    if (!displayName.EndsWith("Component"))
                        displayName += "Component";

                    protoRows.Add(GetInfoFromSlope(proto.ID, displayName, scoreComp.ChaosScore, scoreComp.CombatScore));
                }
            }
        }

        // Sort proto rows by ID for readability
        protoRows.Sort((a, b) => string.Compare(a.Entity, b.Entity, StringComparison.OrdinalIgnoreCase));

        foreach (var row in protoRows)
        {
            AppendRow(sb, row);
        }

        return sb.ToString();
    }

    private void AppendRow(System.Text.StringBuilder sb, AdaptiveBalanceInfo row)
    {
        sb.AppendLine($"{row.Entity},{row.Condition},{Format(row.PvpFrom)},{Format(row.PvpTo)},{Format(row.PvpDuration)},{Format(row.ChaosFrom)},{Format(row.ChaosTo)},{Format(row.ChaosDuration)}");
    }

    private string Format(float? val)
    {
        return val.HasValue ? val.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
    }

    private AdaptiveBalanceInfo GetInfoFromSlope(string entity, string condition, ScoreSlope chaos, ScoreSlope combat)
    {
        return new AdaptiveBalanceInfo(
            entity: entity,
            condition: condition,
            pvpFrom: combat.Base,
            pvpTo: combat.Target,
            pvpDuration: combat.Target.HasValue ? (float)combat.In.TotalMinutes : null,
            chaosFrom: chaos.Base,
            chaosTo: chaos.Target,
            chaosDuration: chaos.Target.HasValue ? (float)chaos.In.TotalMinutes : null
        );
    }
}
