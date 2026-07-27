using System;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server._Maid.Toolshed.Commands;

[ToolshedCommand(Name = "&&"), AdminCommand(AdminFlags.VarEdit)]
public sealed class LogicalAndCommand : ToolshedCommand
{
    public static bool ToBool(object? obj)
    {
        if (obj is bool b)
            return b;

        return obj is null;
    }

    [CommandImplementation]
    public bool And([PipedArgument] bool x, bool y) => x && y;

    [CommandImplementation]
    public bool And(IInvocationContext ctx, [PipedArgument] bool x, Block<bool> y)
    {
        return x && (bool)y.Invoke(null, ctx)!;
    }

    [CommandImplementation]
    public bool And([PipedArgument] object? x, object? y)
    {
        return ToBool(x) && ToBool(y);
    }

    [CommandImplementation]
    public bool And(IInvocationContext ctx, [PipedArgument] object? x, Block y)
    {
        return ToBool(x) && ToBool(y.Invoke(null, ctx));
    }
}

// We can't do "||" cause of pipe operator
[ToolshedCommand(Name = "or"), AdminCommand(AdminFlags.VarEdit)]
public sealed class LogicalOrCommand : ToolshedCommand
{
    [CommandImplementation]
    public bool Or([PipedArgument] bool x, bool y) => x || y;

    [CommandImplementation]
    public bool Or(IInvocationContext ctx, [PipedArgument] bool x, Block<bool> y)
    {
        return x || (bool)y.Invoke(null, ctx)!;
    }

    [CommandImplementation]
    public bool Or([PipedArgument] object? x, object? y)
    {
        return LogicalAndCommand.ToBool(x) || LogicalAndCommand.ToBool(y);
    }

    [CommandImplementation]
    public bool Or(IInvocationContext ctx, [PipedArgument] object? x, Block y)
    {
        return LogicalAndCommand.ToBool(x) || LogicalAndCommand.ToBool(y.Invoke(null, ctx));
    }
}
