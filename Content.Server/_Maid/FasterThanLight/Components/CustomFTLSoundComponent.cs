using Robust.Shared.Audio;

namespace Content.Server._Maid.FasterThanLight.Components;

[RegisterComponent]
public sealed partial class CustomFTLSoundComponent : Component
{
    [DataField]
    public SoundSpecifier? StartupSound { get; set; } = null;

    [DataField]
    public SoundSpecifier? ArrivalSound { get; set; } = null;

    [DataField]
    public SoundSpecifier? TravelSound { get; set; } = null;
}
