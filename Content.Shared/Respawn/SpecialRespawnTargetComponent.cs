namespace Content.Shared.Respawn;

// MAID FILE nuke disk respawn

[RegisterComponent]
public sealed partial class SpecialRespawnTargetComponent : Component
{
    [DataField, ViewVariables]
    public string Tag = "any";

    [DataField, ViewVariables]
    public int Priority = 0;
}
