using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Maid.Extraction;

/// <summary>
/// Lets an antagonist leave the round through a personal extraction zone once enough objectives are done.
/// Station items they carry are left at the zone, their own gear leaves with them.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AntagExtractableComponent : Component
{
    public override bool SendOnlyToOwner => true;

    /// <summary>
    /// Whether <see cref="Threshold"/> is a percent of counted objectives or a flat number of them.
    /// </summary>
    [DataField]
    public ExtractionThresholdMode ThresholdMode = ExtractionThresholdMode.Percent;

    /// <summary>
    /// Percent (0-100) or count of counted objectives that must be completed.
    /// </summary>
    [DataField]
    public float Threshold = 100f;

    /// <summary>
    /// Objectives that must all be completed. They also count toward <see cref="Threshold"/>.
    /// </summary>
    [DataField]
    public List<EntProtoId> RequiredObjectives = new();

    /// <summary>
    /// Objectives that count toward <see cref="Threshold"/>. Objectives in neither list are ignored.
    /// </summary>
    [DataField]
    public List<EntProtoId> OptionalObjectives = new();

    /// <summary>
    /// How many times a zone can be requested: the first request plus re-rolls.
    /// </summary>
    [DataField]
    public int MaxRequests = 2;

    [DataField, AutoNetworkedField]
    public int RequestsUsed;

    [DataField, AutoNetworkedField]
    public bool Unlocked;

    /// <summary>
    /// Unlocked by an admin: objectives are not re-checked when extraction starts.
    /// </summary>
    [DataField]
    public bool ForceUnlocked;

    [DataField, AutoNetworkedField]
    public EntityUid? Zone;

    [DataField]
    public TimeSpan ExtractionDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public EntProtoId ZonePrototype = "ExtractionZoneMarker";

    /// <summary>
    /// Holopads the zone never spawns at, e.g. inside the AI core.
    /// </summary>
    [DataField]
    public List<EntProtoId> HolopadBlacklist = new() { "HolopadAiCore" };

    [DataField]
    public EntProtoId Action = "ActionRequestExtraction";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public LocId Announcement = "extraction-announcement";

    [DataField]
    public SoundSpecifier ExtractSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");

    [ViewVariables]
    public DoAfterId? DoAfter;

    [ViewVariables]
    public TimeSpan NextCheck;
}

public enum ExtractionThresholdMode : byte
{
    Percent,
    Count,
}
