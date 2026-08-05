using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Server.Player;

namespace Content.Server._Maid.DeathGasps;

public sealed class OnDeath : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OnDeathSoundsComponent, MobStateChangedEvent>(HandleDeathEvent);
        SubscribeLocalEvent<OnDeathSoundsComponent, PlayerDetachedEvent>(OnDetach);
    }

    private readonly Dictionary<EntityUid, EntityUid> _playingStreams = new();


    private void HandleDeathEvent(EntityUid uid, OnDeathSoundsComponent component, MobStateChangedEvent args)
    {
        //^.^
        switch (args.NewMobState)
        {
            case MobState.Invalid:
                StopPlayingStream(uid);
                break;
            case MobState.Alive:
                StopPlayingStream(uid);
                break;
            case MobState.Critical:
                PlayPlayingStream(uid, component);
                break;
            case MobState.Dead:
                StopPlayingStream(uid);
                PlayDeathSound(uid, component);
                break;
        }
    }

    private void PlayPlayingStream(EntityUid uid, OnDeathSoundsComponent component)
    {
        if (_playingStreams.TryGetValue(uid, out var currentStream))
        {
            _audio.Stop(currentStream);
        }

        var newStream = _audio.PlayEntity(component.HeartSounds, uid, uid, component.HeartSounds.Params.WithLoop(true));

        if (newStream.HasValue)
        {
            _playingStreams[uid] = newStream.Value.Entity;
        }
    }

    private void StopPlayingStream(EntityUid uid)
    {
        if (!_playingStreams.TryGetValue(uid, out var currentStream))
            return;

        _audio.Stop(currentStream);
        _playingStreams.Remove(uid);
    }

    private void PlayDeathSound(EntityUid uid, OnDeathSoundsComponent component)
    {
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
        StopPlayingStream(args.Entity);
    }
}
