using System.Collections.Generic;
using System.Linq;
using Content.Server._Maid.AdaptiveGameMode.MetaInfo;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
namespace Content.Server._Maid.AdaptiveGameMode.Commands;

[ToolshedCommand(Name = "adaptivebalance"), AdminCommand(AdminFlags.Round)]
public sealed class AdaptiveBalanceCommand : ToolshedCommand
{
    #if DEBUG
    [CommandImplementation("calculatebalancetable")]
    public string CalculateBalanceTable()
    {
        var cfg = IoCManager.Resolve<IConfigurationManager>();
        if (!cfg.GetCVar(CCVars.ConfigPresetDevelopment))
            return "This command can only be run in a development environment.";

        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Entity,Condition/Component,Chaos From,Chaos To,Chaos Duration,Combat From,Combat To,Combat Duration");

        var providers = new List<IAdaptiveBalanceInfoProvider>();
        foreach (var type in entitySystemManager.GetEntitySystemTypes())
        {
            if (typeof(IAdaptiveBalanceInfoProvider).IsAssignableFrom(type) &&
                entitySystemManager.TryGetEntitySystem(type, out var system) &&
                system is IAdaptiveBalanceInfoProvider provider)
            {
                sb.AppendLine(string.Join(
                    "\n",
                    provider
                        .GetBalanceInfo()
                        .Select(info => info.ToString())
                ));
            }
        }

        return sb.ToString();
    }
    #endif
}
