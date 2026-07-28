using System;
using System.Collections.Generic;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Chemistry.EntitySystems;
using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

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
        SolutionContainer.EnsureSolutionEntity(input, solutionName, out _, FixedPoint2.New(maxVol));
        return input;
    }
}
