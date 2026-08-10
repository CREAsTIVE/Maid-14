
using System.Linq;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Static;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Collector;

public sealed class AdaptiveScoreCollectorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GetAdaptiveScoreEvent>(OnGetAdaptiveScore);
    }

    private IEnumerable<IAdaptiveScoreCondition> GetConditions(AdaptiveScoreCollectorComponent comp)
    {
        return comp.ConditionTables
            .SelectMany(table =>
                _protoManager.TryIndex(table, out var proto)
                    ? proto.Conditions
                    : []
            )
            .Concat(comp.Conditions);
    }

    private IEnumerable<EntityUid> GetEntities()
    {
        return _entityManager.GetEntities();
    }

    private IEnumerable<EntityUid> GetEntities(Type componentType)
    {
        foreach (var (uid, _) in _entityManager.GetAllComponents(componentType))
        {
            yield return uid;
        }
    }

    private void OnGetAdaptiveScore(ref GetAdaptiveScoreEvent ev)
    {
        var query = EntityQueryEnumerator<AdaptiveScoreCollectorComponent>();

        while (query.MoveNext(out var uid, out var collector))
        {
            var entities = collector.EnumerateComponent is not null
                           && _componentFactory.TryGetRegistration(collector.EnumerateComponent, out var reg)
                ? GetEntities(reg.Type)
                : GetEntities();

            var count = entities.Count(ent =>
                GetConditions(collector)
                    .All(condition => condition.ConditionMet(ent, _entityManager))
            );

            ev.ChaosScore += count * collector.ChaosScore;
            ev.CombatScore += count * collector.CombatScore;
        }
    }
}
