using Robust.Shared.GameObjects;
using System.Linq;
using Content.Server._Maid.AdaptiveGameMode.MetaInfo;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Collector;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Static;

public sealed class AdaptiveScoreStaticSystem : EntitySystem, IAdaptiveBalanceInfoProvider
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetAdaptiveScoreEvent>(CollectScores);
        SubscribeLocalEvent<AdaptiveScoreStaticComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, AdaptiveScoreStaticComponent component, ref ComponentInit args)
    {
        component.CreationTime = _gameTiming.CurTime;
    }

    private IEnumerable<IAdaptiveScoreCondition> GetConditions(AdaptiveScoreStaticComponent comp)
    {
        return comp.ConditionTables
            .SelectMany(table =>
                _protoManager.TryIndex(table, out var proto)
                    ? proto.Conditions
                    : []
            )
            .Concat(comp.Conditions);
    }

    private void CollectScores(ref GetAdaptiveScoreEvent ev)
    {
        var enumerator = EntityQueryEnumerator<AdaptiveScoreStaticComponent>();
        while (enumerator.MoveNext(out var ent, out var comp))
        {
            if (GetConditions(comp).All(cond => cond.ConditionMet(ent, _entityManager)))
            {
                var age = _gameTiming.CurTime - comp.CreationTime;
                ev.ChaosScore += comp.ChaosScore.GetScore(age);
                ev.CombatScore += comp.CombatScore.GetScore(age);
            }
        }
    }

    public IEnumerable<AdaptiveBalanceInfo> GetBalanceInfo()
    {
        static string FixName(string name)
        {
            if (name.StartsWith("AdaptiveScore"))
                name = name.Substring("AdaptiveScore".Length);

            if (name.EndsWith("Condition"))
                name = name.Substring(0, name.Length - "Condition".Length);

            return name;
        }

        var protos = _protoManager.EnumeratePrototypes<EntityPrototype>();
        foreach (var proto in protos)
        {
            if (proto.TryGetComponent(out AdaptiveScoreStaticComponent? component, _compFactory))
            {
                yield return AdaptiveBalanceInfo.FromSlope(
                    proto.ID,
                    string.Join(
                        " + ",
                        GetConditions(component)
                            .Select(cond => cond.GetType().Name)
                            .Select(FixName) // Make names COOLER
                    ),
                    component.CombatScore,
                    component.ChaosScore
                );
            }
        }
    }
}
