using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Maid.Extraction;

/// <summary>
/// Lets an antagonist leave the round once enough objectives are done: they get an extraction zone,
/// call a drop pod into it, unlock it with their code and launch it from inside.
/// Station items they carry are left behind, their own gear leaves with them.
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
    /// With both lists empty every objective except survival ones counts.
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
    /// Unlocked by an admin: objectives are not re-checked later.
    /// </summary>
    [DataField]
    public bool ForceUnlocked;

    [DataField, AutoNetworkedField]
    public EntityUid? Zone;

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
    public EntProtoId CallPodAction = "ActionCallDropPod";

    [DataField]
    public EntityUid? CallPodActionEntity;

    /// <summary>
    /// Code that unlocks the drop pod, given to the player on unlock. Server only.
    /// </summary>
    [DataField]
    public string Code = string.Empty;

    [DataField]
    public int CodeLength = 6;

    [DataField]
    public EntProtoId PodPrototype = "ExtractionDropPod";

    /// <summary>
    /// Pod that already landed for this antagonist.
    /// </summary>
    [DataField]
    public EntityUid? Pod;

    /// <summary>
    /// Where the called pod is going to land, while it is on its way.
    /// </summary>
    [DataField]
    public EntityCoordinates? PodTarget;

    [DataField]
    public TimeSpan PodLandAt;

    [DataField]
    public TimeSpan? PodAnnounceAt;

    [DataField]
    public TimeSpan PodFlightTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan PodAnnounceDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public LocId PodAnnouncement = "extraction-pod-announcement";

    [DataField]
    public SoundSpecifier PodImpactSound = new SoundCollectionSpecifier("Explosion");

    /// <summary>
    /// When the antagonist arrived, for the round end summary.
    /// </summary>
    [DataField]
    public TimeSpan ArrivalTime;

    /// <summary>
    /// Already left the round, the body waits on the extraction map.
    /// </summary>
    [DataField]
    public bool Extracted;

    [ViewVariables]
    public TimeSpan NextCheck;
}

public enum ExtractionThresholdMode : byte
{
    Percent,
    Count,
}
