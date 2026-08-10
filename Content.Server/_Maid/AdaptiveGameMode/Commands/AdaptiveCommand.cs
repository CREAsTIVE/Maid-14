using System.Linq;
using Content.Server._Maid.AdaptiveGameMode.MetaInfo;
using Content.Server.Administration;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Collector;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Static;
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

    [CommandImplementation("getbalancetable")]
    public string ShowBalanceTable()
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Entity,Condition/Component,Combat From,Combat To,Combat Duration,Chaos From,Chaos To,Chaos Duration");

        // 1. Scan systems implementing IAdaptiveBalanceInfoProvider
        var providers = new List<IAdaptiveBalanceInfoProvider>();
        foreach (var type in entitySystemManager.GetEntitySystemTypes())
        {
            if (typeof(IAdaptiveBalanceInfoProvider).IsAssignableFrom(type) &&
                entitySystemManager.TryGetEntitySystem(type, out var system) &&
                system is IAdaptiveBalanceInfoProvider provider)
            {
                sb.AppendLine(string.Join("\n", provider.GetBalanceInfo().Select(info => info.ToString())));
            }
        }

        return sb.ToString();
    }
}
