using Robust.Shared.Audio;

namespace Content.Server._Maid.DeathGasps;

[RegisterComponent]
public sealed partial class OnDeathSoundsComponent : Component
{
    [DataField]
    public SoundSpecifier DeathSounds = new SoundCollectionSpecifier("deathSounds");

    [DataField]
    public SoundSpecifier HeartSounds = new SoundCollectionSpecifier("heartSounds");

    [DataField]
    public bool CanOtherHearDeathSound;
}
