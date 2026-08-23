using System.Collections;
using System.Diagnostics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Content.Server._Maid.Toolshed.Commands;

public record struct KeyNotFoundError(string Key) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Key '{Key}' not found in the dictionary");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

[ToolshedCommand(Name = "dictutils"), AdminCommand(AdminFlags.VarEdit)]
public sealed class DictUtilsCommands : ToolshedCommand
{
    // We can't do IDictionary<TKey, TValue> since TakesPipedAsArgument works only with one generic argument
    [CommandImplementation("add")]
    public object? Add(
        IInvocationContext ctx,
        [PipedArgument] IDictionary input,
        [CommandArgument(unparseable: true)] object key,
        [CommandArgument(unparseable: true)] object val
    )
    {
        if (!input.IsReadOnly)
        {
            input[key] = val;
        }
        else
        {
            ctx.ReportError(new NotMutableCollectionError());
        }
        return val;
    }

    [CommandImplementation("tryadd")]
    public bool TryAdd(
        IInvocationContext ctx,
        [PipedArgument] IDictionary input,
        [CommandArgument(unparseable: true)] object key,
        [CommandArgument(unparseable: true)] object val
    )
    {
        if (input.IsReadOnly)
        {
            ctx.ReportError(new NotMutableCollectionError());
            return false;
        }

        if (input.Contains(key))
            return false;

        input[key] = val;
        return true;
    }

    [CommandImplementation("update")]
    public object? Update(
        IInvocationContext ctx,
        [PipedArgument] IDictionary input,
        [CommandArgument(unparseable: true)] object key,
        [CommandArgument(unparseable: true)] object val
    )
    {
        if (input.IsReadOnly)
        {
            ctx.ReportError(new NotMutableCollectionError());
            return null;
        }

        if (!input.Contains(key))
        {
            ctx.ReportError(new KeyNotFoundError(key?.ToString() ?? "null"));
            return null;
        }

        input[key] = val;
        return val;
    }

    [CommandImplementation("remove")]
    public object? Remove(
        IInvocationContext ctx,
        [PipedArgument] IDictionary input,
        [CommandArgument(unparseable: true)] object key
    )
    {
        if (!input.IsReadOnly)
        {
            if (input.Contains(key))
            {
                var val = input[key];
                input.Remove(key);
                return val;
            }

            ctx.ReportError(new KeyNotFoundError(key?.ToString() ?? "null"));
            return null;
        }

        ctx.ReportError(new NotMutableCollectionError());
        return null;
    }

    [CommandImplementation("clear")]
    public IDictionary Clear(
        IInvocationContext ctx,
        [PipedArgument] IDictionary input
    )
    {
        if (!input.IsReadOnly)
        {
            input.Clear();
        }
        else
        {
            ctx.ReportError(new NotMutableCollectionError());
        }
        return input;
    }
}
