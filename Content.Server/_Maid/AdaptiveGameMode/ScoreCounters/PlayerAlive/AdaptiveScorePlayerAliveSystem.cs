using Content.Server._Maid.AdaptiveGameMode;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Station;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.PlayerAlive;

/// <summary>
/// System that handles calculating score contributions for alive player-controlled entities.
/// </summary>
public sealed class AdaptiveScorePlayerAliveSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdaptiveScorePlayerAliveComponent, ComponentInit>(OnPlayerAliveComponentInit);
        SubscribeLocalEvent<GetAdaptiveScoreEvent>(OnGetAdaptiveScore);
    }

    private void OnPlayerAliveComponentInit(EntityUid uid, AdaptiveScorePlayerAliveComponent component, ref ComponentInit args)
    {
        component.ComponentCreated = _timing.CurTime;
    }


    private void OnGetAdaptiveScore(ref GetAdaptiveScoreEvent ev)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<AdaptiveScorePlayerAliveComponent, ActorComponent>();
        while (query.MoveNext(out var uid, out var component, out var actor))
        {
            if (component.OnStation && _station.GetOwningStation(uid) == null)
                continue;

            if (actor.PlayerSession.Status != SessionStatus.InGame)
                continue;

            if (HasComp<GhostComponent>(uid))
                continue;

            float multiplier = 1f;
            if (TryComp<MobStateComponent>(uid, out var mobState))
            {
                if (_mobState.IsAlive(uid, mobState))
                {
                    multiplier = 1f;
                }
                else if (_mobState.IsCritical(uid, mobState))
                {
                    multiplier = component.CriticalMultiplier;
                }
                else
                {
                    // Dead or other state
                    continue;
                }
            }

            var elapsed = curTime - component.ComponentCreated;

            var chaos = component.ChaosScore.GetScore(elapsed) * multiplier;
            var combat = component.CombatScore.GetScore(elapsed) * multiplier;

            ev.ChaosScore += chaos;
            ev.CombatScore += combat;
        }
    }
}
