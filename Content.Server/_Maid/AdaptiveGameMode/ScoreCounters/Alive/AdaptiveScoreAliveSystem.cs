using Content.Server._Maid.AdaptiveGameMode;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Station;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Alive;

/// <summary>
/// System that handles calculating score contributions for alive/critical entities.
/// </summary>
public sealed class AdaptiveScoreAliveSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdaptiveScoreAliveComponent, ComponentInit>(OnAliveComponentInit);
        SubscribeLocalEvent<GetAdaptiveScoreEvent>(OnGetAdaptiveScore);
    }

    private void OnAliveComponentInit(EntityUid uid, AdaptiveScoreAliveComponent component, ref ComponentInit args)
    {
        component.ComponentCreated = _timing.CurTime;
    }

    private void OnGetAdaptiveScore(ref GetAdaptiveScoreEvent ev)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<AdaptiveScoreAliveComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var component, out var mobState))
        {
            if (component.OnStation && _station.GetOwningStation(uid) == null)
                continue;

            float multiplier;
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

            var elapsed = curTime - component.ComponentCreated;

            var chaos = component.ChaosScore.GetScore(elapsed) * multiplier;
            var combat = component.CombatScore.GetScore(elapsed) * multiplier;

            ev.ChaosScore += chaos;
            ev.CombatScore += combat;
        }
    }
}
