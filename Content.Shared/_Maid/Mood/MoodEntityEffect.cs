using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Maid.Mood;

/// <summary>
/// Applies a mood effect to the target, e.g. when a narcotic is metabolized.
/// </summary>
public sealed partial class AddMoodEffect : EntityEffect
{
    [DataField(required: true)]
    public ProtoId<MoodEffectPrototype> MoodEffect;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.EventBus.RaiseLocalEvent(args.TargetEntity, new MoodEffectEvent(MoodEffect));
    }
}
