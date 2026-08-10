using Content.Server._Maid.AdaptiveGameMode;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;
using Content.Shared.Access.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Contraband;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Station;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.PlayerFree;

/// <summary>
/// System that handles calculating score contributions for alive, player-controlled entities who are uncuffed and unbuckled.
/// </summary>
public sealed class AdaptiveScorePlayerFreeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdaptiveScorePlayerFreeComponent, ComponentInit>(OnPlayerFreeComponentInit);
        SubscribeLocalEvent<GetAdaptiveScoreEvent>(OnGetAdaptiveScore);
    }

    private void OnPlayerFreeComponentInit(EntityUid uid, AdaptiveScorePlayerFreeComponent component, ref ComponentInit args)
    {
        component.ComponentCreated = _timing.CurTime;
    }

    private void OnGetAdaptiveScore(ref GetAdaptiveScoreEvent ev)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<AdaptiveScorePlayerFreeComponent, ActorComponent>();
        while (query.MoveNext(out var uid, out var component, out var actor))
        {
            if (component.OnStation && _station.GetOwningStation(uid) == null)
                continue;

            if (actor.PlayerSession.Status != SessionStatus.InGame)
                continue;

            // Not ghost
            if (HasComp<GhostComponent>(uid))
                continue;

            // Must be alive
            if (TryComp<MobStateComponent>(uid, out var mobState) && !_mobState.IsAlive(uid, mobState))
                continue;

            // Must not be handcuffed
            if (TryComp<CuffableComponent>(uid, out var cuffable) && _cuffs.IsCuffed((uid, cuffable)))
                continue;

            // Must have ID (not prisoner ID) or ANY contraband
            var isFree = false;
            if (_idCard.TryFindIdCard(uid, out var idCard))
            {
                if (idCard.Comp.JobPrototype != "Prisoner")
                {
                    isFree = true;
                }
            }

            if (!isFree && !HasContraband(uid))
                continue;

            var elapsed = curTime - component.ComponentCreated;

            var chaos = component.ChaosScore.GetScore(elapsed);
            var combat = component.CombatScore.GetScore(elapsed);

            ev.ChaosScore += chaos;
            ev.CombatScore += combat;
        }
    }
    private bool HasContraband(EntityUid uid)
    {
        return HasContrabandRecursive(uid);
    }

    private bool HasContrabandRecursive(EntityUid parent)
    {
        var xform = Transform(parent);
        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (HasComp<ContrabandComponent>(child))
                return true;

            if (HasContrabandRecursive(child))
                return true;
        }

        return false;
    }
}
