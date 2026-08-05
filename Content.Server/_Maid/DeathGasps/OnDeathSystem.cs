using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Server.Player;

namespace Content.Server._Maid.DeathGasps;

public sealed class OnDeathSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OnDeathSoundsComponent, MobStateChangedEvent>(HandleDeathEvent);
        SubscribeLocalEvent<OnDeathSoundsComponent, PlayerDetachedEvent>(OnDetach);
        SubscribeLocalEvent<OnDeathSoundsComponent, ComponentShutdown>(OnShutdown);
    }


    private void HandleDeathEvent(EntityUid uid, OnDeathSoundsComponent component, MobStateChangedEvent args)
    {
        //^.^
        switch (args.NewMobState)
        {
            case MobState.Invalid:
                StopPlayingStream(uid, component);
                break;
            case MobState.Alive:
                StopPlayingStream(uid, component);
                break;
            case MobState.Critical:
                PlayPlayingStream(uid, component);
                break;
            case MobState.Dead:
                StopPlayingStream(uid, component);
                PlayDeathSound(uid, component);
                break;
        }
    }

    private void PlayPlayingStream(EntityUid uid, OnDeathSoundsComponent component)
    {
        if (component.HeartSounds == null)
            return;

        StopPlayingStream(uid, component);

        var newStream = _audio.PlayEntity(component.HeartSounds, uid, uid, component.HeartSounds.Params.WithLoop(true));

        if (newStream.HasValue)
        {
            component.Stream = newStream.Value.Entity;
        }
    }

    private void StopPlayingStream(EntityUid uid, OnDeathSoundsComponent component)
    {
        if (component.Stream == null)
            return;

        _audio.Stop(component.Stream.Value);
        component.Stream = null;
    }

    private void PlayDeathSound(EntityUid uid, OnDeathSoundsComponent component)
    {
        if (component.DeathSounds == null)
            return;

        if (component.CanOtherHearDeathSound)
        {
            _audio.PlayPvs(component.DeathSounds, uid, component.DeathSounds.Params);
        }
        else if (TryComp<MindContainerComponent>(uid, out var mindContainer) && mindContainer.Mind != null)
        {
            if (TryComp<MindComponent>(mindContainer.Mind, out var mind) && mind.UserId != null)
            {
                if (_playerManager.TryGetSessionById(mind.UserId.Value, out var session))
                {
                    _audio.PlayGlobal(component.DeathSounds, session, component.DeathSounds.Params);
                }
            }
        }
    }

    private void OnDetach(EntityUid uid, OnDeathSoundsComponent component, PlayerDetachedEvent args)
    {
        StopPlayingStream(uid, component);
    }

    private void OnShutdown(EntityUid uid, OnDeathSoundsComponent component, ref ComponentShutdown args)
    {
        StopPlayingStream(uid, component);
    }
}
