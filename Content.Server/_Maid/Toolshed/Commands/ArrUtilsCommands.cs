using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._Maid.Toolshed.Commands;

[ToolshedCommand(Name = "arrutils"), AdminCommand(AdminFlags.VarEdit)]
public sealed class ArrUtilsCommands : ToolshedCommand
{
    [CommandImplementation("push"), TakesPipedTypeAsGeneric]
    public T Push<T>(
        IInvocationContext ctx,
        [PipedArgument] ICollection<T> input,
        T value
    )
    {
        if (input.IsReadOnly)
        {
            ctx.ReportError(new NotMutableCollectionError());
        }
        else
        {
            input.Add(value);
        }
        return value;
    }

    [CommandImplementation("pushfirst"), TakesPipedTypeAsGeneric]
    public T PushFirst<T>(
        IInvocationContext ctx,
        [PipedArgument] IList<T> input,
        T value
    )
    {
        if (input.IsReadOnly)
        {
            ctx.ReportError(new NotMutableCollectionError());
        }
        else
        {
            input.Insert(0, value);
        }
        return value;
    }

    [CommandImplementation("insertat"), TakesPipedTypeAsGeneric]
    public T InsertAt<T>(
        IInvocationContext ctx,
        [PipedArgument] IList<T> input,
        int index,
        T value
    )
    {
        if (input.IsReadOnly)
        {
            ctx.ReportError(new NotMutableCollectionError());
            return value;
        }

        if (index < 0 || index > input.Count)
        {
            ctx.ReportError(new IndexOutOfBoundsError(index, input.Count));
            return value;
        }

        input.Insert(index, value);
        return value;
    }

    [CommandImplementation("addafter"), TakesPipedTypeAsGeneric]
    public T AddAfter<T>(
        IInvocationContext ctx,
        [PipedArgument] IList<T> input,
        int index,
        T value
    )
    {
        if (input.IsReadOnly)
        {
            ctx.ReportError(new NotMutableCollectionError());
            return value;
        }

        var targetIndex = index + 1;
        if (targetIndex < 0 || targetIndex > input.Count)
        {
            ctx.ReportError(new IndexOutOfBoundsError(targetIndex, input.Count));
            return value;
        }

        input.Insert(targetIndex, value);
        return value;
    }

    [CommandImplementation("pop"), TakesPipedTypeAsGeneric]
    public T Pop<T>(
        IInvocationContext ctx,
        [PipedArgument] IList<T> input
    )
    {
        if (input.IsReadOnly)
        {
            ctx.ReportError(new NotMutableCollectionError());
            return default!;
        }

        if (input.Count == 0)
        {
            ctx.ReportError(new IndexOutOfBoundsError(-1, 0));
            return default!;
        }

        var index = input.Count - 1;
        var item = input[index];
        input.RemoveAt(index);
        return item;
    }

    [CommandImplementation("popfirst"), TakesPipedTypeAsGeneric]
    public T PopFirst<T>(
        IInvocationContext ctx,
        [PipedArgument] IList<T> input
    )
    {
        if (input.IsReadOnly)
        {
            ctx.ReportError(new NotMutableCollectionError());
            return default!;
        }

        if (input.Count == 0)
        {
            ctx.ReportError(new IndexOutOfBoundsError(0, 0));
            return default!;
        }

        var item = input[0];
        input.RemoveAt(0);
        return item;
    }

    [CommandImplementation("removeat"), TakesPipedTypeAsGeneric]
    public T RemoveAt<T>(
        IInvocationContext ctx,
        [PipedArgument] IList<T> input,
        int index
    )
    {
        if (input.IsReadOnly)
        {
            ctx.ReportError(new NotMutableCollectionError());
            return default!;
        }

        if (index < 0 || index >= input.Count)
        {
            ctx.ReportError(new IndexOutOfBoundsError(index, input.Count));
            return default!;
        }

        var item = input[index];
        input.RemoveAt(index);
        return item;
    }

    [CommandImplementation("clear"), TakesPipedTypeAsGeneric]
    public ICollection<T> Clear<T>(
        IInvocationContext ctx,
        [PipedArgument] ICollection<T> input
    )
    {
        if (input.IsReadOnly)
        {
            ctx.ReportError(new NotMutableCollectionError());
        }
        else
        {
            input.Clear();
        }
        return input;
    }
}
