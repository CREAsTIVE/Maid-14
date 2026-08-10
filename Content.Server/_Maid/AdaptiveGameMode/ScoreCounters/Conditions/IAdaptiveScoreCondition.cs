using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.Conditions;

[ImplicitDataDefinitionForInheritors]
public partial interface IAdaptiveScoreCondition
{
    /*public struct Result
    {
        public bool Passes;
        public float ChaosMultiplier;
        public float CombatMultiplier;

        public static implicit operator Result(bool passes) =>
            passes ? Pass() : No;

        public static Result No { get; } = new()
        {
            Passes = false,
            CombatMultiplier = 0f,
            ChaosMultiplier = 0f,
        };

        public static Result Pass(float multiplier = 1f) =>
            Pass(1, 1);

        public static Result Pass(float chaos, float combat) => new()
        {
            Passes = true,
            ChaosMultiplier = chaos,
            CombatMultiplier = combat,
        };

        public static Result Pass(float shared, float chaos, float combat) =>
            Pass(shared * chaos, shared * combat);
    }*/

    public bool ConditionMet(EntityUid? mob, Entity<MindComponent>? mind, IEntityManager entMan);
}
