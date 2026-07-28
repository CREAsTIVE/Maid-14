using System;
using System.Collections.Generic;
using System.Diagnostics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Chemistry.EntitySystems;
using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Content.Server._Maid.Toolshed.Commands;

[ToolshedCommand(Name = "solutionutils"), AdminCommand(AdminFlags.Admin)]
public sealed class SolutionUtilsCommands : ToolshedCommand
{
    private SharedSolutionContainerSystem? _solutionContainerField;
    private SharedSolutionContainerSystem SolutionContainer => _solutionContainerField ??= GetSys<SharedSolutionContainerSystem>();

    [CommandImplementation("addsolution")]
    public EntityUid AddContainer(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] string solutionName,
        [CommandArgument] float maxVol
    )
    {
        if (SolutionContainer.EnsureSolutionEntity(input, solutionName, out var solutionEntity, FixedPoint2.New(maxVol)))
        {
            return input;
        }

        ctx.ReportError(new SolutionCreationFailedError(solutionName, input));
        return EntityUid.Invalid;
    }
}

public record struct SolutionCreationFailedError(string SolutionName, EntityUid Entity) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Failed to ensure solution '{SolutionName}' on entity {Entity}.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
