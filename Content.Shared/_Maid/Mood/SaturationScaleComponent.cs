using Robust.Shared.GameStates;

namespace Content.Shared._Maid.Mood;

/// <summary>
/// Desaturates the screen of the player controlling this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SaturationScaleComponent : Component;
