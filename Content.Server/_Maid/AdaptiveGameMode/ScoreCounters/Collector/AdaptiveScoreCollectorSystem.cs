using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using Content.Server._Maid.AdaptiveGameMode.MetaInfo;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Static;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Collector;

public sealed class AdaptiveScoreCollectorSystem : EntitySystem, IAdaptiveBalanceInfoProvider
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

            var conditions = GetConditions(collector).ToArray();
            var count = entities.Count(ent =>
            {
                EntityUid? mob = null;
                Entity<MindComponent>? mind = null;

                if (TryComp<MindRoleComponent>(ent, out var mindRole))
                {
                    var mindId = mindRole.Mind.Owner;
                    if (TryComp<MindComponent>(mindId, out var mindComp))
                    {
                        mob = mindRole.Mind.Comp.OwnedEntity;
                        mind = new Entity<MindComponent>(mindId, mindComp);
                    }
                }
                else if (TryComp<MindComponent>(ent, out var mindComp))
                {
                    mob = mindComp.OwnedEntity;
                    mind = new Entity<MindComponent>(ent, mindComp);
                }
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

                return conditions.All(condition => condition.ConditionMet(mob, mind, _entityManager));
            });

            ev.ChaosScore += count * collector.ChaosScore;
            ev.CombatScore += count * collector.CombatScore;
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
            if (proto.TryGetComponent(out AdaptiveScoreCollectorComponent? component, _componentFactory))
            {
                if (!HasComponentDefinedOrOverridden(proto.ID, "AdaptiveScoreCollector"))
                    continue;

                yield return new AdaptiveBalanceInfo
                {
                    Entity = proto.ID,
                    Condition = string.Join(
                        " + ",
                        new[] { component.EnumerateComponent ?? "" }
                            .Concat(
                                GetConditions(component)
                                    .Select(cond => cond.GetType().Name)
                                    .Select(FixName)
                            )
                            .Where(s => !string.IsNullOrEmpty(s))
                    ),
                    CombatFrom = component.CombatScore,
                    ChaosFrom = component.ChaosScore
                };
            }
        }
    }

    private MappingDataNode? GetRawMapping(string protoId)
    {
        var prototypeManager = _protoManager as PrototypeManager;
        if (prototypeManager == null)
            return null;

        var kindsField = typeof(PrototypeManager).GetField("_kinds", BindingFlags.Instance | BindingFlags.NonPublic);
        if (kindsField == null)
            return null;

        var kinds = kindsField.GetValue(prototypeManager);
        if (kinds == null)
            return null;

        var dict = kinds as System.Collections.IDictionary;
        if (dict == null)
            return null;

        if (!dict.Contains(typeof(EntityPrototype)))
            return null;

        var kindData = dict[typeof(EntityPrototype)];
        if (kindData == null)
            return null;

        var rawResultsField = kindData.GetType().GetField("RawResults", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (rawResultsField == null)
            return null;

        var rawResults = rawResultsField.GetValue(kindData) as Dictionary<string, MappingDataNode>;
        if (rawResults == null)
            return null;

        if (rawResults.TryGetValue(protoId, out var mapping))
            return mapping;

        return null;
    }

    private bool HasComponentDefinedOrOverridden(string protoId, string componentName)
    {
        var mapping = GetRawMapping(protoId);
        if (mapping == null)
            return false;

        if (!mapping.TryGetValue("components", out var componentsNode) || componentsNode is not SequenceDataNode sequenceNode)
            return false;

        foreach (var node in sequenceNode)
        {
            if (node is ValueDataNode valueNode && valueNode.Value == componentName)
            {
                return true;
            }

            if (node is MappingDataNode compMapping)
            {
                if (compMapping.TryGetValue("type", out var typeNode) && typeNode is ValueDataNode valNode)
                {
                    if (valNode.Value == componentName)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
