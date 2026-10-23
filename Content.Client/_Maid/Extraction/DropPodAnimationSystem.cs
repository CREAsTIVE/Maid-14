using System.Numerics;
using Content.Shared._Maid.Extraction;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
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

    private const string AnimationKey = "drop-pod-launch";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DropPodLaunchAnimationEvent>(OnLaunchAnimation);
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
