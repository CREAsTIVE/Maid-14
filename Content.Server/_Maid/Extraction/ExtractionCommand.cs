using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._Maid.Extraction;

[ToolshedCommand(Name = "extraction"), AdminCommand(AdminFlags.Admin)]
public sealed class ExtractionCommand : ToolshedCommand
{
    private ExtractionSystem? _extractionField;
    private ExtractionSystem Extraction => _extractionField ??= GetSys<ExtractionSystem>();

    [CommandImplementation("reset")]
    public IEnumerable<EntityUid> Reset([PipedArgument] IEnumerable<EntityUid> input)
    {
        foreach (var uid in input)
        {
            if (Extraction.ResetZone(uid))
                yield return uid;
        }
    }

    [CommandImplementation("unlock")]
    public IEnumerable<EntityUid> Unlock([PipedArgument] IEnumerable<EntityUid> input)
    {
        foreach (var uid in input)
        {
            if (Extraction.TryUnlock(uid))
                yield return uid;
        }
    }
}
