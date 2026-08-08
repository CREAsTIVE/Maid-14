using Content.Server._Maid.AdaptiveGameMode;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;
using Content.Shared.Station;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Static;

/// <summary>
/// System that handles calculating static score contributions.
/// </summary>
public sealed class AdaptiveScoreStaticSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdaptiveScoreStaticComponent, ComponentInit>(OnStaticComponentInit);
        SubscribeLocalEvent<GetAdaptiveScoreEvent>(OnGetAdaptiveScore);
    }

    private void OnStaticComponentInit(EntityUid uid, AdaptiveScoreStaticComponent component, ref ComponentInit args)
    {
        component.ComponentCreated = _timing.CurTime;
    }

    private void OnGetAdaptiveScore(ref GetAdaptiveScoreEvent ev)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<AdaptiveScoreStaticComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.OnStation && _station.GetOwningStation(uid) == null)
                continue;

            var elapsed = curTime - component.ComponentCreated;

            var chaos = component.ChaosScore.GetScore(elapsed);
            var combat = component.CombatScore.GetScore(elapsed);

            ev.ChaosScore += chaos;
            ev.CombatScore += combat;
        }
    }
}
