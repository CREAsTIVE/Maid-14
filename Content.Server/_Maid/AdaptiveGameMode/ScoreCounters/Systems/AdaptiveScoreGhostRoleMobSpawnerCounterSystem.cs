using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Static;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Systems;

public sealed class AdaptiveScoreGhostRoleMobSpawnerCounterSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetAdaptiveScoreEvent>(GetScore);
    }

    private void GetScore(ref GetAdaptiveScoreEvent ev)
    {
        var enumerator = EntityQueryEnumerator<GhostRoleMobSpawnerComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var ghostRole, out var transform))
        {
            if (!_prototypeManager.TryIndex(ghostRole.Prototype, out var ent))
                continue;

            if (!ent.TryGetComponent(out AdaptiveScoreStaticComponent? score, _componentFactory))
                continue;

            ev.Add(uid, score.ChaosScore.Base, score.CombatScore.Base);
        }
    }
}
