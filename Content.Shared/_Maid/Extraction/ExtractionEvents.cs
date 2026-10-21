using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Maid.Extraction;

public sealed partial class RequestExtractionActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class ExtractionDoAfterEvent : SimpleDoAfterEvent;
