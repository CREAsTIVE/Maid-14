using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Maid.Extraction;

/// <summary>
/// Extraction drop pod: an entity storage locked with the extractee's code.
/// Once the extractee is inside, they can launch it and leave the round.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DropPodComponent : Component
{
    /// <summary>
    /// Not networked: the client never needs it, and it may point to an entity that is gone.
    /// </summary>
    [DataField]
    public EntityUid? Extractee;

    [DataField, AutoNetworkedField]
    public bool Locked = true;

    [DataField, AutoNetworkedField]
    public bool Launching;

    [DataField]
    public TimeSpan LaunchDelay = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan LaunchAt;

    /// <summary>
    /// Seconds of the countdown shown to the occupant as a popup.
    /// </summary>
    [DataField]
    public List<int> CountdownPopups = new() { 10, 5, 4, 3, 2, 1 };

    /// <summary>
    /// Last countdown second already announced to the occupant.
    /// </summary>
    [DataField]
    public int LastCountdownSecond;

    /// <summary>
    /// Length of the lift-off animation.
    /// </summary>
    [DataField]
    public float LaunchAnimationDuration = 2f;

    /// <summary>
    /// Sprite layers of the pod flying away, the real pod is deleted when the animation starts.
    /// </summary>
    [DataField]
    public List<SpriteSpecifier> LaunchSprite = new()
    {
        new SpriteSpecifier.Rsi(new ResPath("_Maid/Structures/Storage/drop_pod.rsi"), "base"),
        new SpriteSpecifier.Rsi(new ResPath("_Maid/Structures/Storage/drop_pod.rsi"), "door"),
        new SpriteSpecifier.Rsi(new ResPath("_Maid/Structures/Storage/drop_pod.rsi"), "glow_yellow"),
    };

    /// <summary>
    /// Sprite offset of the pod, so the lift-off starts where the pod is drawn.
    /// </summary>
    [DataField]
    public Vector2 LaunchSpriteOffset = new(0f, 0.25f);

    /// <summary>
    /// Point light colour for each glow state.
    /// </summary>
    [DataField]
    public Dictionary<DropPodGlow, Color> GlowColors = new()
    {
        { DropPodGlow.Locked, Color.FromHex("#ff3b3b") },
        { DropPodGlow.Unlocked, Color.FromHex("#3bff6b") },
        { DropPodGlow.Sealed, Color.FromHex("#3b8bff") },
        { DropPodGlow.Launching, Color.FromHex("#ffd23b") },
    };

    [DataField]
    public EntProtoId LaunchAction = "ActionLaunchDropPod";

    [DataField]
    public EntityUid? LaunchActionEntity;

    /// <summary>
    /// Rubble decal left where the pod hit.
    /// </summary>
    [DataField]
    public string LandingDecal = "DropPodRubble";

    /// <summary>
    /// Decal position from the corner of the landing tile. Decals are drawn from their bottom left corner,
    /// so a 64x64 decal is moved half a tile back to sit centered, and up like the pod sprite.
    /// </summary>
    [DataField]
    public Vector2 LandingDecalOffset = new(-0.5f, -0.25f);

    [DataField]
    public SoundSpecifier LaunchSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");

    /// <summary>
    /// Played every second of the launch countdown. None yet, the sound is picked later.
    /// </summary>
    [DataField]
    public SoundSpecifier? CountdownSound;

    /// <summary>
    /// Looped engine sound while the pod counts down. None yet, the sound is picked later.
    /// </summary>
    [DataField]
    public SoundSpecifier? EngineSound;

    [ViewVariables]
    public EntityUid? EngineSoundStream;

    [DataField]
    public SoundSpecifier KeypadSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

    [DataField]
    public SoundSpecifier WrongCodeSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

    [DataField]
    public SoundSpecifier UnlockSound = new SoundPathSpecifier("/Audio/Machines/Nuke/confirm_beep.ogg");

    /// <summary>
    /// Digits typed on the keypad so far. Server only.
    /// </summary>
    [ViewVariables]
    public string EnteredCode = string.Empty;

    [ViewVariables]
    public bool WrongCode;

    [DataField]
    public LocId InsideMessage = "extraction-pod-inside";

    [DataField]
    public LocId NotInsideMessage = "extraction-pod-not-inside";

    /// <summary>
    /// Gets <c>$seconds</c>.
    /// </summary>
    [DataField]
    public LocId LaunchingMessage = "extraction-pod-launching";

    [DataField]
    public LocId LaunchCancelledMessage = "extraction-pod-launch-cancelled";

    [DataField]
    public LocId SealedMessage = "extraction-pod-sealed";
}
