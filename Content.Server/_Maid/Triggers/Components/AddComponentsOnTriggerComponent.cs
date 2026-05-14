using Robust.Shared.Prototypes;

namespace Content.Server._Maid.Triggers.Components;

/// <summary>
/// Adds configured components to a target entity when this entity is triggered.
/// </summary>
[RegisterComponent]
public sealed partial class AddComponentsOnTriggerComponent : Component
{
    [DataField]
    public bool RemoveExisting;

    [DataField(serverOnly: true, readOnly: true, required: true)]
    public ComponentRegistry Components = new();
}
