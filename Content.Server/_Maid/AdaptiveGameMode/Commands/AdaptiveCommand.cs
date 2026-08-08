using Content.Server.Administration;
using Content.Server._Maid.AdaptiveGameMode;
using Content.Shared.Administration;
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
}
