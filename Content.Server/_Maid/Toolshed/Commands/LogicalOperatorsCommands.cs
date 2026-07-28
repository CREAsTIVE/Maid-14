using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._Maid.Toolshed.Commands;
// For "or" consistency
[ToolshedCommand(Name = "and"), AdminCommand(AdminFlags.VarEdit)]
public sealed class WordAndCommand : ToolshedCommand
{
    [CommandImplementation]
    public bool And([PipedArgument] bool x, bool y) => x && y;
}

// We can't do "||" cause of pipe operator
[ToolshedCommand(Name = "or"), AdminCommand(AdminFlags.VarEdit)]
public sealed class LogicalOrCommand : ToolshedCommand
{
    [CommandImplementation]
    public bool Or([PipedArgument] bool x, bool y) => x || y;
}
