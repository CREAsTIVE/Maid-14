using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Content.Server._Maid.AdaptiveGameMode.MetaInfo;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Collector;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Static;

public sealed class AdaptiveScoreStaticSystem : EntitySystem
#if DEBUG
    , IAdaptiveBalanceInfoProvider
#endif
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

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
            EntityUid? mob = null;
            Entity<MindComponent>? mind = null;

            // If ent controlled by mind
            if (TryComp<MindRoleComponent>(ent, out var mindRole))
            {
                var mindId = mindRole.Mind.Owner;
                if (TryComp<MindComponent>(mindId, out var mindComp))
                {
                    mob = mindRole.Mind.Comp.OwnedEntity;
                    mind = new Entity<MindComponent>(mindId, mindComp);
                }
            }
            // If mind itself
            else if (TryComp<MindComponent>(ent, out var mindComp))
            {
                mob = mindComp.OwnedEntity;
                mind = new Entity<MindComponent>(ent, mindComp);
            }
            // Idk something else
            else
            {
                var mindSystem = _entityManager.System<SharedMindSystem>();
                if (mindSystem.TryGetMind(ent, out var mobMindId, out var mobMindComp))
                {
                    mob = ent;
                    mind = new Entity<MindComponent>(mobMindId, mobMindComp);
                }
                else
                {
                    mob = ent;
                }
            }
            var conditions = GetConditions(comp).ToArray();
            if (conditions.All(cond => cond.ConditionMet(mob, mind, _entityManager)))
            {
                var age = _gameTiming.CurTime - comp.CreationTime;
                ev.ChaosScore += comp.ChaosScore.GetScore(age);
                ev.CombatScore += comp.CombatScore.GetScore(age);
            }
        }
    }

#if DEBUG
    // WARNING: VERY DIRTY REFLECTION STUFF
    // But it only used in debug commands
    // and shouldn't be called outside of development env,
    // So should be fine.

    public IEnumerable<AdaptiveBalanceInfo> GetBalanceInfo()
    {
        var rawResults = GetRawResults(_protoManager);
        if (rawResults == null)
            yield break;

        foreach (var (protoId, mapping) in rawResults)
        {
            var compMapping = GetComponentMapping(mapping, "AdaptiveScoreStatic");
            if (compMapping is null)
                continue;

            var component = _serializationManager.Read<AdaptiveScoreStaticComponent?>(compMapping);
            if (component is null)
                continue;

            yield return AdaptiveBalanceInfo.FromSlope(
                protoId,
                string.Join(
                    " + ",
                    GetConditions(component)
                        .Select(cond => cond.GetType().Name)
                        .Select(FixName) // Make names COOLER
                ),
                component.ChaosScore,
                component.CombatScore
            );
        }

        yield break;

        static string FixName(string name)
        {
            if (name.StartsWith("AdaptiveScore"))
                name = name["AdaptiveScore".Length..];

            if (name.EndsWith("Condition"))
                name = name[..^"Condition".Length];

            return name;
        }
    }

    private static Dictionary<string, MappingDataNode>? GetRawResults(IPrototypeManager protoManager)
    {
        if (protoManager is not PrototypeManager prototypeManager)
            return null;

        // Some reflection nonsense to retrieve private fields. May break on engine update
        var kindsField = typeof(PrototypeManager)
            .GetField("_kinds", BindingFlags.Instance | BindingFlags.NonPublic);

        if (kindsField?.GetValue(prototypeManager) is not IDictionary dict)
            return null;

        if (!dict.Contains(typeof(EntityPrototype)))
            return null;

        var kindData = dict[typeof(EntityPrototype)];

        var rawResultsField = kindData?
            .GetType()
            .GetField("RawResults", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return rawResultsField?.GetValue(kindData) as Dictionary<string, MappingDataNode>;
    }

    private static MappingDataNode? GetComponentMapping(MappingDataNode mapping, string componentName)
    {
        if (!mapping.TryGetValue("components", out var componentsNode) || componentsNode is not SequenceDataNode sequenceNode)
            return null;

        foreach (var node in sequenceNode)
        {
            if (node is not MappingDataNode compMapping)
                continue;

            if (!compMapping.TryGetValue("type", out var typeNode) || typeNode is not ValueDataNode valNode)
                continue;

            if (valNode.Value == componentName)
                return compMapping;
        }

        return null;
    }
#endif
}
