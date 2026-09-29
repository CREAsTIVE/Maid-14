using Robust.Shared.Prototypes;

namespace Content.Shared._Maid.Mood;

/// <summary>
/// A single source of mood change, e.g. "being hugged" or "starving".
/// </summary>
[Prototype]
public sealed partial class MoodEffectPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Text shown to the player when they inspect their mood.
    /// </summary>
    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    public MoodChangeLevel MoodChange;

    [DataField]
    public bool Positive;

    /// <summary>
    /// How long the effect lasts. Zero means it lasts until removed explicitly.
    /// </summary>
    [DataField]
    public TimeSpan Timeout = TimeSpan.Zero;

    /// <summary>
    /// Hidden effects are not listed when the player inspects their mood.
    /// </summary>
    [DataField]
    public bool Hidden;

    /// <summary>
    /// If the mob already has an effect of the same category, the new one replaces it.
    /// </summary>
    [DataField]
    public string? Category;
}
