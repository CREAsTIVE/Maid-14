using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Shared._Maid.Mood;
using Content.Shared.Alert;
using Content.Shared.Atmos;

using Content.Shared.Chat;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Maid.Mood;

public sealed class MoodSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoodComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MoodComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MoodComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MoodComponent, MoodEffectEvent>(OnMoodEffect);
        SubscribeLocalEvent<MoodComponent, MoodRemoveEffectEvent>(OnRemoveEffect);
        SubscribeLocalEvent<MoodComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MoodComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<MoodComponent, ShowMoodEffectsAlertEvent>(OnShowMoodEffects);
        SubscribeLocalEvent<MoodComponent, CuffedStateChangeEvent>(OnCuffedStateChanged);
        SubscribeLocalEvent<MoodComponent, IgnitedEvent>(OnIgnited);
        SubscribeLocalEvent<MoodComponent, ExtinguishedEvent>(OnExtinguished);

        SubscribeLocalEvent<AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<MoodComponent>();
        while (query.MoveNext(out var uid, out var mood))
        {
            if (mood.EffectsEndTime.Count == 0)
                continue;

            var expired = mood.EffectsEndTime
                .Where(pair => pair.Value <= _timing.CurTime)
                .Select(pair => pair.Key)
                .ToList();

            if (expired.Count == 0)
                continue;

            foreach (var effect in expired)
            {
                RemoveEffectInternal(mood, effect);
            }

            RefreshMood((uid, mood));
        }
    }

    #region Public API

    /// <summary>
    /// Applies a mood effect to the entity. Reapplying an active timed effect refreshes its duration.
    /// </summary>
    public void ApplyEffect(Entity<MoodComponent?> ent, ProtoId<MoodEffectPrototype> effectId)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !_prototype.TryIndex(effectId, out var effect))
            return;

        if (effect.Category is { } category)
        {
            if (ent.Comp.CategorisedEffects.TryGetValue(category, out var oldEffect) && oldEffect != effectId)
                ent.Comp.EffectsEndTime.Remove(oldEffect);

            ent.Comp.CategorisedEffects[category] = effectId;
        }
        else
        {
            ent.Comp.UncategorisedEffects.Add(effectId);
        }

        if (effect.Timeout > TimeSpan.Zero)
            ent.Comp.EffectsEndTime[effectId] = _timing.CurTime + effect.Timeout;

        RefreshMood((ent, ent.Comp));
    }

    public void RemoveEffect(Entity<MoodComponent?> ent, ProtoId<MoodEffectPrototype> effectId)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (RemoveEffectInternal(ent.Comp, effectId))
            RefreshMood((ent, ent.Comp));
    }

    #endregion

    private void OnMapInit(Entity<MoodComponent> ent, ref MapInitEvent args)
    {
        RefreshMood(ent);
    }

    private void OnShutdown(Entity<MoodComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _alerts.ClearAlertCategory(ent, ent.Comp.MoodCategory);

        RemComp<SaturationScaleComponent>(ent);
    }

    private void OnMobStateChanged(Entity<MoodComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead || args.OldMobState == MobState.Dead)
            RefreshMood(ent);
    }

    private void OnMoodEffect(Entity<MoodComponent> ent, ref MoodEffectEvent args)
    {
        ApplyEffect((ent, ent.Comp), args.EffectId);
    }

    private void OnRemoveEffect(Entity<MoodComponent> ent, ref MoodRemoveEffectEvent args)
    {
        RemoveEffect((ent, ent.Comp), args.EffectId);
    }

    private void OnCuffedStateChanged(Entity<MoodComponent> ent, ref CuffedStateChangeEvent args)
    {
        if (!TryComp<CuffableComponent>(ent, out var cuffable))
            return;

        if (cuffable.CanStillInteract)
            RemoveEffect((ent, ent.Comp), "Handcuffed");
        else
            ApplyEffect((ent, ent.Comp), "Handcuffed");
    }

    private void OnIgnited(Entity<MoodComponent> ent, ref IgnitedEvent args)
    {
        ApplyEffect((ent, ent.Comp), "OnFire");
    }

    private void OnExtinguished(Entity<MoodComponent> ent, ref ExtinguishedEvent args)
    {
        RemoveEffect((ent, ent.Comp), "OnFire");
    }

    private void OnAntagSelected(ref AfterAntagEntitySelectedEvent args)
    {
        if (args.Def.MoodEffect is { } effect)
            ApplyEffect(args.EntityUid, effect);
    }

    private void OnDamageChanged(Entity<MoodComponent> ent, ref DamageChangedEvent args)
    {
        var damage = args.Damageable.TotalDamage.Float();
        ProtoId<MoodEffectPrototype>? healthEffect = null;
        var highest = float.MinValue;

        foreach (var (effect, threshold) in ent.Comp.HealthMoodEffectsThresholds)
        {
            if (damage < threshold || threshold < highest)
                continue;

            healthEffect = effect;
            highest = threshold;
        }

        if (healthEffect == null)
            return;

        if (_prototype.TryIndex(healthEffect.Value, out var proto)
            && proto.Category is { } category
            && ent.Comp.CategorisedEffects.TryGetValue(category, out var current)
            && current == healthEffect.Value)
            return;

        ApplyEffect((ent, ent.Comp), healthEffect.Value);
    }

    private void OnRefreshMoveSpeed(Entity<MoodComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.CurrentMoodThreshold == MoodThreshold.Dead || _jetpack.IsUserFlying(ent))
            return;

        var modifier = GetModifierLevel(ent.Comp.CurrentMoodThreshold) switch
        {
            -1 => ent.Comp.SlowdownSpeedModifier,
            1 => ent.Comp.IncreaseSpeedModifier,
            _ => 1f,
        };

        args.ModifySpeed(modifier, modifier);
    }

    private void OnShowMoodEffects(Entity<MoodComponent> ent, ref ShowMoodEffectsAlertEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.CurrentMoodThreshold == MoodThreshold.Dead || !TryComp<ActorComponent>(ent, out var actor))
            return;

        SendToChat(actor.PlayerSession, Loc.GetString("mood-show-effects-start"));

        var effects = ent.Comp.CategorisedEffects.Values.Concat(ent.Comp.UncategorisedEffects);
        foreach (var effectId in effects)
        {
            if (!_prototype.TryIndex(effectId, out var effect) || effect.Hidden)
                continue;

            SendToChat(actor.PlayerSession, Loc.GetString(effect.Positive ? "mood-effect-positive" : "mood-effect-negative",
                ("text", Loc.GetString(effect.Description))));
        }
    }

    private void SendToChat(ICommonSession session, string message)
    {
        _chat.ChatMessageToOne(ChatChannel.Emotes, message, message, EntityUid.Invalid, false, session.Channel);
    }

    private bool RemoveEffectInternal(MoodComponent comp, ProtoId<MoodEffectPrototype> effectId)
    {
        comp.EffectsEndTime.Remove(effectId);

        if (comp.UncategorisedEffects.Remove(effectId))
            return true;

        foreach (var (category, effect) in comp.CategorisedEffects)
        {
            if (effect != effectId)
                continue;

            comp.CategorisedEffects.Remove(category);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Recalculates the mood level from all active effects and applies threshold effects.
    /// </summary>
    private void RefreshMood(Entity<MoodComponent> ent)
    {
        var comp = ent.Comp;

        if (_mobState.IsDead(ent))
        {
            comp.CurrentMoodLevel = comp.MoodThresholds[MoodThreshold.Dead];
            comp.CurrentMoodThreshold = MoodThreshold.Dead;
            DoThresholdEffects(ent);
            return;
        }

        var level = comp.MoodThresholds[MoodThreshold.Neutral];
        foreach (var effectId in comp.CategorisedEffects.Values.Concat(comp.UncategorisedEffects))
        {
            if (!_prototype.TryIndex(effectId, out var effect)
                || !comp.MoodChangeValues.TryGetValue(effect.MoodChange, out var value))
                continue;

            level += effect.Positive ? value : -value;
        }

        comp.CurrentMoodLevel = Math.Clamp(level,
            comp.MoodThresholds[MoodThreshold.Dead] + 0.1f,
            comp.MoodThresholds[MoodThreshold.VeryVeryGood]);

        comp.CurrentMoodThreshold = GetMoodThreshold(comp);
        DoThresholdEffects(ent);
    }

    private void DoThresholdEffects(Entity<MoodComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.MoodThresholdsAlerts.TryGetValue(comp.CurrentMoodThreshold, out var alert))
            _alerts.ShowAlert(ent, alert);
        else
            _alerts.ClearAlertCategory(ent, comp.MoodCategory);

        if (comp.CurrentMoodThreshold == comp.LastThreshold)
            return;

        var modifier = comp.CurrentMoodThreshold == MoodThreshold.Dead
            ? 0
            : GetModifierLevel(comp.CurrentMoodThreshold);

        var lastModifier = comp.LastThreshold == MoodThreshold.Dead
            ? 0
            : GetModifierLevel(comp.LastThreshold);

        comp.LastThreshold = comp.CurrentMoodThreshold;

        if (modifier == lastModifier)
            return;

        _movementSpeed.RefreshMovementSpeedModifiers(ent);
        SetCritThreshold(ent, modifier);

        if (modifier == -1)
            EnsureComp<SaturationScaleComponent>(ent);
        else
            RemComp<SaturationScaleComponent>(ent);
    }

    private void SetCritThreshold(Entity<MoodComponent> ent, int modifier)
    {
        if (!TryComp<MobThresholdsComponent>(ent, out var thresholds))
            return;

        if (ent.Comp.BaseCritThreshold == null)
        {
            if (modifier == 0
                || !_mobThreshold.TryGetThresholdForState(ent, MobState.Critical, out var current, thresholds))
                return;

            ent.Comp.BaseCritThreshold = current.Value;
        }

        var baseThreshold = ent.Comp.BaseCritThreshold.Value;
        var newThreshold = modifier switch
        {
            1 => baseThreshold * ent.Comp.IncreaseCritThreshold,
            -1 => baseThreshold * ent.Comp.DecreaseCritThreshold,
            _ => baseThreshold,
        };

        if (modifier == 0)
            ent.Comp.BaseCritThreshold = null;

        _mobThreshold.SetMobStateThreshold(ent, newThreshold, MobState.Critical, thresholds);
    }

    private static MoodThreshold GetMoodThreshold(MoodComponent comp)
    {
        var result = MoodThreshold.Dead;
        var value = comp.MoodThresholds[MoodThreshold.VeryVeryGood];

        foreach (var (threshold, thresholdValue) in comp.MoodThresholds)
        {
            if (thresholdValue > value || thresholdValue < comp.CurrentMoodLevel)
                continue;

            result = threshold;
            value = thresholdValue;
        }

        return result;
    }

    /// <summary>
    /// -1 for bad mood that slows the mob down, 1 for good mood that speeds it up, 0 otherwise.
    /// </summary>
    private static int GetModifierLevel(MoodThreshold threshold)
    {
        return threshold switch
        {
            >= MoodThreshold.VeryGood => 1,
            <= MoodThreshold.VeryBad => -1,
            _ => 0,
        };
    }
}
