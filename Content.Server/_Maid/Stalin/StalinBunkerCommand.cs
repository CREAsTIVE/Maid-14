using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared._Maid.CVars;
using Content.Shared.Administration;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server._Maid.Stalin;

[AdminCommand(AdminFlags.Admin)]
public sealed class StalinBunkerCommand : IConsoleCommand
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public string Command => "stalinbunker";
    public string Description => Loc.GetString("cmd-stalinbunker-desc");
    public string Help => Loc.GetString("cmd-stalinbunker-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 0), ("upper", 1)));
            return;
        }

        var enabled = !_cfg.GetCVar(MaidCVars.StalinEnabled);

        if (args.Length == 1 && !bool.TryParse(args[0], out enabled))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-boolean"));
            return;
        }

        _cfg.SetCVar(MaidCVars.StalinEnabled, enabled);

        _chat.DispatchServerAnnouncement(Loc.GetString("stalin-panic-bunker", ("enabled", enabled.ToString())), Color.Red);
    }
}
