using Content.Server.GameTicking.Rules;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Static;
using Content.Server.Administration.Logs;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Antag.Components;
using Robust.Server.Player;
using Content.Server.Antag;
using Robust.Shared.Random;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Random.Helpers;
namespace Content.Server._Maid.AdaptiveGameMode;
public sealed class AdaptiveRuleSystem : GameRuleSystem<AdaptiveRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    protected override void Started(EntityUid uid, AdaptiveRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.TimeUntilNextAttempt = component.MidroundSpawnTimer.GetValue(_random);
        // Calculate and log estimated roundstart score
        var estScore = CalculateEstimatedRoundstartScore(component);

        _adminLog.Add(
            LogType.EventStarted,
            LogImpact.High,
            $"Estimated roundstart Adaptive Score - {estScore}"
        );

        SpawnRoundstartRules(uid, component);
    }

    private void SpawnRoundstartRules(EntityUid uid, AdaptiveRuleComponent component)
    {
        foreach (var ruleParam in component.RoundstartRules)
        {
            if (ruleParam.Conditions.All(c => c.Condition(ruleParam, component, EntityManager)))
            {
                SpawnRule(uid, component, ruleParam.Id);
            }
        }
    }

    public AdaptiveScore CalculateEstimatedRoundstartScore(AdaptiveRuleComponent component)
    {
        var readyPlayersCount = _playerManager.Sessions
            .Count(session => GameTicker.PlayerGameStatuses.TryGetValue(session.UserId, out var status) &&
                              (status == PlayerGameStatus.ReadyToPlay || status == PlayerGameStatus.JoinedGame));

        return component.RoundstartChaosPerPlayer * readyPlayersCount;
    }

    protected override void ActiveTick(EntityUid uid, AdaptiveRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        component.TimeUntilNextAttempt -= frameTime;
        if (component.TimeUntilNextAttempt > 0)
            return;

        // Reset timer
        component.TimeUntilNextAttempt = component.MidroundSpawnTimer.GetValue(_random);

        // Try spawning a rule
        TrySpawnRandomRule(uid, component);
    }

    private void TrySpawnRandomRule(EntityUid uid, AdaptiveRuleComponent component)
    {
        if (component.MidroundRules.Count == 0)
            return;

        // Check skip probability
        if (_random.Prob(component.MidroundSpawnSkipProb))
        {
            Log.Info("Skipped spawning midround rule due to skip probability.");
            return;
        }

        // Evaluate conditions for candidate rules
        var candidateRules = new List<AdaptiveRuleParam>();
        foreach (var ruleParam in component.MidroundRules)
        {
            if (ruleParam.Conditions.All(c => c.Condition(ruleParam, component, EntityManager)))
            {
                candidateRules.Add(ruleParam);
            }
        }

        if (candidateRules.Count == 0)
            return;

        var currentChaos = CalculateChaosScore().ChaosScore;
        var scoreBudget = component.TargetChaosValue - currentChaos;

        var chosenRule = ChooseRandomRule(component, candidateRules, scoreBudget);
        if (chosenRule == null)
            return;

        SpawnRule(uid, component, chosenRule.Id);
    }

    public AdaptiveRuleParam? ChooseRandomRule(
        AdaptiveRuleComponent component,
        List<AdaptiveRuleParam> rules,
        float scoreBudget)
    {
        var weightedRules = GetRulesWeighted(component, rules, scoreBudget);
        var weightsDict = new Dictionary<AdaptiveRuleParam, float>();
        foreach (var (rule, weight) in weightedRules)
        {
            if (weight > 0f)
            {
                weightsDict[rule] = weight;
            }
        }

        if (weightsDict.Count == 0)
            return null;

        return _random.Pick(weightsDict);
    }

    public List<(AdaptiveRuleParam Rule, float Weight)> GetRulesWeighted(
        AdaptiveRuleComponent component,
        List<AdaptiveRuleParam> rules,
        float scoreBudget)
    {
        var result = new List<(AdaptiveRuleParam Rule, float Weight)>();
        var decay = component.ScoreDifferenceMultiplierDecay;

        var m = component.ScoreDifferenceMultiplierMin;

        foreach (var rule in rules)
        {
            var expectedBudget = CalculatePossibleScoreForPrototype(rule.Id).Chaos;
            var x = scoreBudget - expectedBudget;
            var exponent = -(x * x) / decay;
            var multiplier = MathF.Exp(exponent) * (1f - m) + m;
            var weight = rule.BaseWeight * multiplier;
            result.Add((rule, weight));
        }

        return result;
    }

    public AdaptiveScore CalculatePossibleScoreForPrototype(string ruleId, int? playerCount = null)
    {
        var visited = new HashSet<string>();
        var totalScore = GetPrototypeStaticScore(ruleId, visited);

        if (!_protoManager.TryIndex<EntityPrototype>(ruleId, out var proto))
            return totalScore;

        if (proto.TryGetComponent(out AntagSelectionComponent? antagComp, _compFactory))
        {
            var poolSize = playerCount ?? _antagSelection.GetTotalPlayerCount(_playerManager.Sessions);

            foreach (var def in antagComp.Definitions)
            {
                var countOffset = 0;
                foreach (var otherDef in antagComp.Definitions)
                {
                    countOffset += System.Math.Clamp((poolSize - countOffset) / otherDef.PlayerRatio, otherDef.Min, otherDef.Max) * otherDef.PlayerRatio;
                }
                countOffset -= System.Math.Clamp(poolSize / def.PlayerRatio, def.Min, def.Max) * def.PlayerRatio;
                var antagCount = System.Math.Clamp((poolSize - countOffset) / def.PlayerRatio, def.Min, def.Max);

                if (antagCount <= 0)
                    continue;

                // MindRoles
                if (def.MindRoles != null)
                {
                    foreach (var role in def.MindRoles)
                    {
                        totalScore += GetPrototypeStaticScore(role, visited) * antagCount;
                    }
                }

                // Spawners
                if (def.SpawnerPrototype != null)
                {
                    totalScore += GetPrototypeStaticScore(def.SpawnerPrototype, visited) * antagCount;
                }

                // Added components
                var staticCompName = _compFactory.GetComponentName<AdaptiveScoreStaticComponent>();
                if (def.Components.TryGetValue(staticCompName, out var staticCompEntry))
                {
                    var staticComp = (AdaptiveScoreStaticComponent) staticCompEntry.Component;
                    totalScore += (AdaptiveScore) staticComp * antagCount;
                }
            }
        }

        return totalScore;
    }

    public EntityUid? SpawnRule(EntityUid uid, AdaptiveRuleComponent component, string ruleId)
    {
        if (GameTicker.StartGameRule(ruleId, out var ruleEnt))
        {
            component.SpawnedRules.Add(new AdaptiveSpawnedRule
            {
                RuleId = ruleId,
                Entity = ruleEnt,
                SpawnTime = Timing.CurTime
            });

            Log.Info($"Successfully started adaptive rule: {ruleId}");
            _adminLog.Add(LogType.EventStarted, $"Adaptive Gamemode spawned rule: {ruleId}");
            ChatManager.SendAdminAnnouncement($"Adaptive Gamemode spawned rule: {ruleId}");

            return ruleEnt;
        }

        return null;
    }
    /// <summary>
    /// Gets the current chaos score by broadcasting a <see cref="GetAdaptiveScoreEvent"/>.
    /// </summary>
    public GetAdaptiveScoreEvent CalculateChaosScore()
    {
        var ev = new GetAdaptiveScoreEvent();
        RaiseLocalEvent(ref ev);
        return ev;
    }

    /// <summary>
    /// Calculates the potential score for a given gamerule entity prototype.
    /// This resolves the prototype's own static score, plus the scores of any mind roles it spawns.
    /// </summary>
    public AdaptiveScore CalculatePossibleScoreForDefinition(Entity<AntagSelectionComponent> antagSelection, int? playerCount = null)
    {
        var visited = new HashSet<string>();
        var totalScore = MetaData(antagSelection).EntityPrototype?.ID is {} id
            ? GetPrototypeStaticScore(id, visited)
            : new();

        var antagComp = antagSelection.Comp;

        foreach (var def in antagComp.Definitions)
        {
            var antagCount = _antagSelection.GetTargetAntagCount(antagSelection, playerCount, def);
            if (antagCount <= 0)
                continue;

            // MindRoles
            if (def.MindRoles != null)
            {
                foreach (var role in def.MindRoles)
                {
                    totalScore += GetPrototypeStaticScore(role, visited) * antagCount;
                }
            }

            // Spawners
            if (def.SpawnerPrototype != null)
            {
                totalScore += GetPrototypeStaticScore(def.SpawnerPrototype, visited) * antagCount;
            }

            // Added components (i will kill you if you will add static score components like that)
            var staticCompName = _compFactory.GetComponentName<AdaptiveScoreStaticComponent>();
            if (def.Components.TryGetValue(staticCompName, out var staticCompEntry))
            {
                var staticComp = (AdaptiveScoreStaticComponent) staticCompEntry.Component;
                totalScore += (AdaptiveScore) staticComp * antagCount;
            }
        }

        return totalScore;
    }

    private AdaptiveScore GetPrototypeStaticScore(string protoId, HashSet<string> visited)
    {
        if (!visited.Add(protoId))
            return new();

        var score = new AdaptiveScore();

        if (!_protoManager.TryIndex<EntityPrototype>(protoId, out var proto))
            return new();

        // Has AdaptiveScoreStaticComponent
        if (proto.TryGetComponent(out AdaptiveScoreStaticComponent? staticScore, _compFactory))
        {
            score += staticScore;
        }

        // Is spawner
        if (proto.TryGetComponent(out GhostRoleComponent? ghostComp, _compFactory))
        {
            foreach (var role in ghostComp.MindRoles)
            {
                score += GetPrototypeStaticScore(role, visited);
            }
        }

        return score;
    }
}
