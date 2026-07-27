using System;
using System.Diagnostics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Content.Server._Maid.Toolshed.Commands;

[ToolshedCommand(Name = "unwrap"), AdminCommand(AdminFlags.VarEdit)]
public sealed class UnwrapCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Unwrap<T>(IInvocationContext ctx, [PipedArgument] T? value)
        where T : struct
    {
        if (value is null)
        {
            ctx.ReportError(new NullValueError());
            return default;
        }
        return value.Value;
    }

    [CommandImplementation("ordefault"), TakesPipedTypeAsGeneric]
    public T OrDefault<T>([PipedArgument] T? value)
        where T : struct
    {
        return value.GetValueOrDefault();
    }
}

public record struct NullValueError : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("The piped value was null.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
