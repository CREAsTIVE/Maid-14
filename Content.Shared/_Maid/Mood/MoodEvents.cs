using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Maid.Mood;

[Serializable, NetSerializable]
public enum MoodChangeLevel : byte
{
    None,
    Small,
    Medium,
    Big,
    Huge,
    Large,
}

[Serializable, NetSerializable]
public enum MoodThreshold : byte
{
    Dead = 0,
    Insane = 1,
    VeryVeryBad = 2,
    VeryBad = 3,
    Bad = 4,
    NotGreat = 5,
    Neutral = 6,
    Great = 7,
    Good = 8,
    VeryGood = 9,
    VeryVeryGood = 10,
}

/// <summary>
/// Raised on an entity to apply a mood effect to it.
/// </summary>
public sealed class MoodEffectEvent(ProtoId<MoodEffectPrototype> effectId) : EntityEventArgs
{
    public readonly ProtoId<MoodEffectPrototype> EffectId = effectId;
}

/// <summary>
/// Raised on an entity to remove a mood effect from it.
/// </summary>
public sealed class MoodRemoveEffectEvent(ProtoId<MoodEffectPrototype> effectId) : EntityEventArgs
{
    public readonly ProtoId<MoodEffectPrototype> EffectId = effectId;
}

public sealed partial class ShowMoodEffectsAlertEvent : BaseAlertEvent;
