using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server._Maid.Toolshed.Commands;

[ToolshedCommand(Name = "physicsutils"), AdminCommand(AdminFlags.VarEdit)]
public sealed class PhysicsUtilsCommands : ToolshedCommand
{
    [CommandImplementation("parentuntil")]
    public EntityUid ParentUntil(
        IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        Block<EntityUid, bool> predicate
    )
    {
        var current = input;
        var depth = 0; // cycle parenting aren't real they can't hurt you...
        while (current.IsValid() && depth++ < 1000)
        {
            if (predicate.Invoke(current, ctx))
                return current;

            if (!TryComp<TransformComponent>(current, out var transform))
                break;

            current = transform.ParentUid;
        }

        return EntityUid.Invalid;
    }

    [CommandImplementation("parentuntil")]
    public IEnumerable<EntityUid> ParentUntil(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        Block<EntityUid, bool> predicate
    )
    {
        foreach (var item in input)
        {
            yield return ParentUntil(ctx, item, predicate);
        }
    }
}

[ToolshedCommand(Name = "onsamegrid"), AdminCommand(AdminFlags.VarEdit)]
public sealed class OnSameGridCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> OnSameGrid(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> input)
    {
        if (ctx.Session?.AttachedEntity is { } selfEnt &&
            TryComp<TransformComponent>(selfEnt, out var selfTransform) &&
            selfTransform.GridUid is { } selfGrid)
        {
            return input
                .Where(x => TryComp<TransformComponent>(x, out var transform) && transform.GridUid == selfGrid);
        }

        return [];
    }
}

[ToolshedCommand(Name = "onsamemap"), AdminCommand(AdminFlags.VarEdit)]
public sealed class OnSameMapCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> OnSameMap(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> input)
    {
        if (ctx.Session?.AttachedEntity is { } selfEnt &&
            TryComp<TransformComponent>(selfEnt, out var selfTransform) &&
            selfTransform.MapUid is { } selfMap)
        {
            return input.Where(x => TryComp<TransformComponent>(x, out var transform) && transform.MapUid == selfMap);
        }

        return [];
    }
}
