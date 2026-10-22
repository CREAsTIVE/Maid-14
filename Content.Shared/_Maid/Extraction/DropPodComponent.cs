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
    [DataField, AutoNetworkedField]
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
    /// Next countdown popup shown to the occupant.
    /// </summary>
    [DataField]
    public int NextCountdownPopup;

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
        new SpriteSpecifier.Rsi(new ResPath("_Maid/Structures/Storage/closet.rsi"), "generic"),
        new SpriteSpecifier.Rsi(new ResPath("_Maid/Structures/Storage/closet.rsi"), "generic_door"),
    };

    [DataField]
    public EntProtoId LaunchAction = "ActionLaunchDropPod";

    [DataField]
    public EntityUid? LaunchActionEntity;

    /// <summary>
    /// Scorch mark left where the pod stood.
    /// </summary>
    [DataField]
    public string LandingDecal = "burnt1";

    [DataField]
    public SoundSpecifier LaunchSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");

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
}
