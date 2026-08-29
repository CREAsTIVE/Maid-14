using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Item;
using Content.Shared.Station.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Random;

namespace Content.Server._Maid.RandomItemArtifacts;

public sealed class RandomItemArtifactsSystem : GameRuleSystem<RandomItemArtifactsRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;

    protected override void Started(EntityUid uid, RandomItemArtifactsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var entities = EntityQueryEnumerator<ItemComponent>();
        while (entities.MoveNext(out var ent, out var comp))
        {
            if (!Resolve(ent, ref comp))
                return;

            if (!TryComp(ent, out TransformComponent? xform))
                return;

            if (xform.Anchored)
                return;

            if (_random.Prob(component.ConversionChance))
            {
                EnsureComp<XenoArtifactComponent>(ent);
            }
        }
    }
}
