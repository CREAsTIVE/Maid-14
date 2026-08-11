using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
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
                if (!HasComponentDefinedOrOverridden(proto.ID, "AdaptiveScoreStatic"))
                    continue;

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

    // WARNING: DIRTY STUFF
    // But its fine its only for debug

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
