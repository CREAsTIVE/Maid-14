using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Maid.Extraction;

/// <summary>
/// Extraction zone marker. Only <see cref="Extractee"/> can see and use it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExtractionZoneComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Extractee;

    /// <summary>
    /// Half-size of the square zone in tiles, 1 makes a 3x3 square.
    /// </summary>
    [DataField]
    public int Range = 1;

    /// <summary>
    /// Holopad the zone was placed at, skipped when the zone is re-rolled.
    /// </summary>
    [DataField]
    public EntityUid? Holopad;

    /// <summary>
    /// Non-clickable visual of the zone square, attached to the marker.
    /// </summary>
    [DataField]
    public EntProtoId AreaPrototype = "ExtractionZoneArea";

    [DataField]
    public EntityUid? Area;
}
