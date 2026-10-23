using System.Numerics;
using Content.Shared.Actions;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Maid.Extraction;

public sealed partial class RequestExtractionActionEvent : InstantActionEvent;

public sealed partial class CallDropPodActionEvent : InstantActionEvent;

public sealed partial class LaunchDropPodActionEvent : InstantActionEvent;

/// <summary>
/// Tells clients to play the lift-off animation of a drop pod that is being deleted.
/// </summary>
[Serializable, NetSerializable]
public sealed class DropPodLaunchAnimationEvent(NetCoordinates coordinates, float duration, List<SpriteSpecifier> layers, Vector2 offset) : EntityEventArgs
{
    public readonly NetCoordinates Coordinates = coordinates;
    public readonly float Duration = duration;
    public readonly List<SpriteSpecifier> Layers = layers;

    /// <summary>
    /// Sprite offset of the pod, the lift-off starts from it.
    /// </summary>
    public readonly Vector2 Offset = offset;
}

[Serializable, NetSerializable]
public enum DropPodVisuals : byte
{
    Glow,
}

[Serializable, NetSerializable]
public enum DropPodVisualLayers : byte
{
    Glow,
}

/// <summary>
/// Glow of the pod: red while locked, green once the code is entered, blue when sealed for launch, yellow on lift-off.
/// </summary>
[Serializable, NetSerializable]
public enum DropPodGlow : byte
{
    Locked,
    Unlocked,
    Sealed,
    Launching,
}

[Serializable, NetSerializable]
public enum DropPodUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum DropPodStatus : byte
{
    Locked,
    WrongCode,
    Unlocked,
    Launching,
}

[Serializable, NetSerializable]
public sealed class DropPodUiState(DropPodStatus status, int enteredLength, int codeLength) : BoundUserInterfaceState
{
    public readonly DropPodStatus Status = status;
    public readonly int EnteredLength = enteredLength;
    public readonly int CodeLength = codeLength;
}

[Serializable, NetSerializable]
public sealed class DropPodKeypadMessage(int value) : BoundUserInterfaceMessage
{
    public readonly int Value = value;
}

[Serializable, NetSerializable]
public sealed class DropPodKeypadClearMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DropPodKeypadEnterMessage : BoundUserInterfaceMessage;
