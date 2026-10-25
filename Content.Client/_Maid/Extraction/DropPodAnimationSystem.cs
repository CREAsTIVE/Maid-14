using System.Numerics;
using Content.Client.Storage.Visualizers;
using Content.Shared._Maid.Extraction;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Timing;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Client._Maid.Extraction;

/// <summary>
/// Plays the drop pod flying up and away, gaining speed. The real pod is already gone on the server,
/// so a local copy of its sprite is animated, like the fulton balloon.
/// </summary>
public sealed class DropPodAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string AnimationKey = "drop-pod-launch";
    private const string LandingKey = "drop-pod-landing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DropPodLaunchAnimationEvent>(OnLaunchAnimation);
        SubscribeLocalEvent<DropPodComponent, ComponentStartup>(OnPodStartup);
        SubscribeLocalEvent<DropPodComponent, AnimationCompletedEvent>(OnPodAnimationCompleted);
    }

    /// <summary>
    /// A pod that just hit the station plays its landing once. Pods seen later (e.g. coming into view) stay still.
    /// </summary>
    private void OnPodStartup(Entity<DropPodComponent> ent, ref ComponentStartup args)
    {
        var length = ent.Comp.LandingAnimationLength;
        if (_timing.CurTime - ent.Comp.LandedAt > length || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        // the door is drawn by the storage visualizer on top, hide it until the pod has risen
        if (_sprite.LayerMapTryGet((ent.Owner, sprite), StorageVisualLayers.Door, out var door, false))
            _sprite.LayerSetVisible((ent.Owner, sprite), door, false);

        var animation = new Animation
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = StorageVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId(ent.Comp.LandingState), 0f),
                        new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId(ent.Comp.BaseState), (float) length.TotalSeconds),
                    },
                },
            },
        };

        _player.Play(ent.Owner, animation, LandingKey);
    }

    private void OnPodAnimationCompleted(Entity<DropPodComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != LandingKey || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((ent.Owner, sprite), StorageVisualLayers.Door, out var door, false))
            _sprite.LayerSetVisible((ent.Owner, sprite), door, true);
    }

    private void OnLaunchAnimation(DropPodLaunchAnimationEvent ev)
    {
        var coordinates = GetCoordinates(ev.Coordinates);
        if (!coordinates.IsValid(EntityManager))
            return;

        var animationEnt = Spawn(null, coordinates);
        var sprite = AddComp<SpriteComponent>(animationEnt);
        sprite.NoRotation = true;

        foreach (var layer in ev.Layers)
        {
            _sprite.AddLayer((animationEnt, sprite), layer);
        }

        var despawn = AddComp<TimedDespawnComponent>(animationEnt);
        despawn.Lifetime = ev.Duration + 0.1f;

        _player.Play(animationEnt, CreateAnimation(ev.Duration, ev.Offset), AnimationKey);
    }

    /// <summary>
    /// Slow start, then quickly gone upwards. Key frame times are deltas from the previous frame.
    /// </summary>
    private static Animation CreateAnimation(float duration, Vector2 offset)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(duration),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(offset, 0f),
                        new AnimationTrackProperty.KeyFrame(offset + new Vector2(0f, 0.2f), duration * 0.4f),
                        new AnimationTrackProperty.KeyFrame(offset + new Vector2(0f, 1.2f), duration * 0.25f),
                        new AnimationTrackProperty.KeyFrame(offset + new Vector2(0f, 4f), duration * 0.2f),
                        new AnimationTrackProperty.KeyFrame(offset + new Vector2(0f, 16f), duration * 0.15f),
                    },
                },
            },
        };
    }
}
