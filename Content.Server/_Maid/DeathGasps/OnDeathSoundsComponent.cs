using Robust.Shared.Audio;

namespace Content.Server._Maid.DeathGasps;

[RegisterComponent]
public sealed partial class OnDeathSoundsComponent : Component
{
    [DataField]
    public SoundSpecifier? DeathSounds = new SoundCollectionSpecifier("deathSounds");

    [DataField]
    public SoundSpecifier? HeartSounds = new SoundCollectionSpecifier(
        "heartSounds",
        new AudioParams { Volume = -3, }
    );

    [DataField]
    public bool CanOtherHearDeathSound;

    [ViewVariables]
    public EntityUid? Stream;
}
