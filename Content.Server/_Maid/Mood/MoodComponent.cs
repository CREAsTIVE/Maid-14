using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Maid.Mood;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Server._Maid.Mood;

[RegisterComponent, Access(typeof(MoodSystem))]
public sealed partial class MoodComponent : Component
{
    [ViewVariables]
    public float CurrentMoodLevel;

    [ViewVariables]
    public MoodThreshold CurrentMoodThreshold = MoodThreshold.Neutral;

    [ViewVariables]
    public MoodThreshold LastThreshold = MoodThreshold.Neutral;

    /// <summary>
    /// Active effects that replace each other within a category. Category -> effect.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, ProtoId<MoodEffectPrototype>> CategorisedEffects = new();

    /// <summary>
    /// Active effects without a category.
    /// </summary>
    [ViewVariables]
    public HashSet<ProtoId<MoodEffectPrototype>> UncategorisedEffects = new();

    /// <summary>
    /// When timed effects run out.
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<MoodEffectPrototype>, TimeSpan> EffectsEndTime = new();

    [DataField]
    public float SlowdownSpeedModifier = 0.75f;

    [DataField]
    public float IncreaseSpeedModifier = 1.15f;

    [DataField]
    public float IncreaseCritThreshold = 1.2f;

    [DataField]
    public float DecreaseCritThreshold = 0.9f;

    /// <summary>
    /// Critical threshold before the mood started modifying it.
    /// </summary>
    [ViewVariables]
    public FixedPoint2? BaseCritThreshold;

    [DataField]
    public Dictionary<MoodThreshold, float> MoodThresholds = new()
    {
        { MoodThreshold.VeryVeryGood, 10.0f },
        { MoodThreshold.VeryGood, 8.0f },
        { MoodThreshold.Good, 7.0f },
        { MoodThreshold.Great, 6.0f },
        { MoodThreshold.Neutral, 5.0f },
        { MoodThreshold.NotGreat, 4.0f },
        { MoodThreshold.Bad, 3.0f },
        { MoodThreshold.VeryBad, 2.0f },
        { MoodThreshold.VeryVeryBad, 1.0f },
        { MoodThreshold.Dead, 0.0f },
    };

    [DataField]
    public Dictionary<MoodThreshold, ProtoId<AlertPrototype>> MoodThresholdsAlerts = new()
    {
        { MoodThreshold.Dead, "MoodDead" },
        { MoodThreshold.Insane, "MoodInsane" },
        { MoodThreshold.VeryVeryBad, "MoodVeryVeryBad" },
        { MoodThreshold.VeryBad, "MoodVeryBad" },
        { MoodThreshold.Bad, "MoodBad" },
        { MoodThreshold.NotGreat, "MoodNotGreat" },
        { MoodThreshold.Neutral, "MoodNeutral" },
        { MoodThreshold.Great, "MoodGreat" },
        { MoodThreshold.Good, "MoodGood" },
        { MoodThreshold.VeryGood, "MoodVeryGood" },
        { MoodThreshold.VeryVeryGood, "MoodVeryVeryGood" },
    };

    [DataField]
    public ProtoId<AlertCategoryPrototype> MoodCategory = "Mood";

    [DataField]
    public Dictionary<MoodChangeLevel, float> MoodChangeValues = new()
    {
        { MoodChangeLevel.None, 0.0f },
        { MoodChangeLevel.Small, 0.3f },
        { MoodChangeLevel.Medium, 0.7f },
        { MoodChangeLevel.Big, 1.0f },
        { MoodChangeLevel.Huge, 1.3f },
        { MoodChangeLevel.Large, 2f },
    };

    /// <summary>
    /// Total damage required for each health mood effect.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MoodEffectPrototype>, float> HealthMoodEffectsThresholds = new()
    {
        { "HealthHeavyDamage", 80f },
        { "HealthSevereDamage", 50f },
        { "HealthLightDamage", 10f },
        { "HealthNoDamage", 0f },
    };
}
