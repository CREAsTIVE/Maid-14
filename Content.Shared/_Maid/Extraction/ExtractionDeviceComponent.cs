using Robust.Shared.GameStates;

namespace Content.Shared._Maid.Extraction;

/// <summary>
/// Handheld device of an extractable antagonist: shows the extraction status and holds its controls.
/// Only its owner can open it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExtractionDeviceComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Owner;

    /// <summary>
    /// Popup for anyone else trying to open it.
    /// </summary>
    [DataField]
    public LocId UnrecognizedMessage = "extraction-device-unrecognized";
}
