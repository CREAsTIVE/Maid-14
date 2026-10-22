using Content.Shared._Maid.Extraction;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server._Maid.Extraction;

/// <summary>
/// Drop pod part of extraction: calling the pod into the zone, its landing, the code keypad,
/// the launch countdown and the lift-off.
/// </summary>
public sealed partial class ExtractionSystem
{
    private static readonly int[] CountdownPopups = { 10, 5, 4, 3, 2, 1 };

    private void InitializePod()
    {
        SubscribeLocalEvent<AntagExtractableComponent, CallDropPodActionEvent>(OnCallPod);
        SubscribeLocalEvent<AntagExtractableComponent, LaunchDropPodActionEvent>(OnLaunchPod);

        SubscribeLocalEvent<DropPodComponent, EntInsertedIntoContainerMessage>(OnPodInserted);
        SubscribeLocalEvent<DropPodComponent, EntRemovedFromContainerMessage>(OnPodRemoved);
        SubscribeLocalEvent<DropPodComponent, ComponentShutdown>(OnPodShutdown);
        SubscribeLocalEvent<DropPodComponent, BoundUIOpenedEvent>(OnPodUiOpened);
        SubscribeLocalEvent<DropPodComponent, DropPodKeypadMessage>(OnKeypad);
        SubscribeLocalEvent<DropPodComponent, DropPodKeypadClearMessage>(OnKeypadClear);
        SubscribeLocalEvent<DropPodComponent, DropPodKeypadEnterMessage>(OnKeypadEnter);
    }

    #region Calling and landing

    /// <summary>
    /// The pod can be called once, and only from inside the extraction zone.
    /// </summary>
    private bool CanCallPod(Entity<AntagExtractableComponent> ent, TimeSpan now)
    {
        return ent.Comp.Unlocked
               && !ent.Comp.Extracted
               && !ent.Comp.PodCalled
               && now >= ent.Comp.PodRecallAt
               && ent.Comp.Zone is { } zone
               && TryComp<ExtractionZoneComponent>(zone, out var zoneComp)
               && _zone.IsInZone((zone, zoneComp), ent.Owner);
    }

    private void UpdateCallPodAction(Entity<AntagExtractableComponent> ent, TimeSpan now)
    {
        if (ent.Comp.CallPodActionEntity is not { } action)
            return;

        var enabled = CanCallPod(ent, now);
        if (enabled == ent.Comp.CallPodEnabled)
            return;

        ent.Comp.CallPodEnabled = enabled;
        _actions.SetEnabled(action, enabled);
    }

    private void OnCallPod(Entity<AntagExtractableComponent> ent, ref CallDropPodActionEvent args)
    {
        if (args.Handled)
            return;

        var now = _timing.CurTime;
        var user = ent.Owner;

        if (!CanCallPod(ent, now))
        {
            _popup.PopupEntity(Loc.GetString("extraction-not-in-zone"), user, user, PopupType.SmallCaution);
            return;
        }

        var xform = Transform(user);
        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        args.Handled = true;

        // the pod lands on the tile the call was made from
        var tile = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        ent.Comp.PodCalled = true;
        ent.Comp.PodTarget = _map.GridTileToLocal(grid, gridComp, tile);
        ent.Comp.PodLandAt = now + ent.Comp.PodFlightTime;
        ent.Comp.PodAnnounceAt = now + ent.Comp.PodAnnounceDelay;
        UpdateCallPodAction(ent, now);

        Notify(ent, Loc.GetString("extraction-pod-called", ("seconds", (int) ent.Comp.PodFlightTime.TotalSeconds)));
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} called an extraction drop pod to {ent.Comp.PodTarget} ({GetLocation(user)})");
    }

    /// <summary>
    /// Handles the destroyed pod and the announcement, returns where to land once the pod arrives.
    /// </summary>
    private EntityCoordinates? UpdatePodCall(Entity<AntagExtractableComponent> ent, TimeSpan now)
    {
        UpdateCallPodAction(ent, now);

        if (ent.Comp.Pod is { } pod && TerminatingOrDeleted(pod))
        {
            ent.Comp.Pod = null;
            Notify(ent, Loc.GetString("extraction-pod-destroyed"));
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"Extraction drop pod of {ToPrettyString(ent.Owner):player} was destroyed");
        }

        if (ent.Comp.PodTarget is not { } target)
            return null;

        // the target grid is gone while the pod is on its way
        if (TerminatingOrDeleted(target.EntityId))
        {
            FailPodLanding(ent, now);
            return null;
        }

        if (ent.Comp.PodAnnounceAt is { } announceAt && now >= announceAt)
        {
            ent.Comp.PodAnnounceAt = null;
            if (_station.GetOwningStation(target.EntityId) is { } station)
            {
                _chat.DispatchStationAnnouncement(station,
                    Loc.GetString(ent.Comp.PodAnnouncement),
                    Loc.GetString("extraction-announcement-sender"),
                    colorOverride: Color.Gold);
            }
        }

        if (now < ent.Comp.PodLandAt)
            return null;

        ent.Comp.PodTarget = null;
        return target;
    }

    /// <summary>
    /// The call did not go through, it can be made again after a delay.
    /// </summary>
    private void FailPodLanding(Entity<AntagExtractableComponent> ent, TimeSpan now)
    {
        ent.Comp.PodTarget = null;
        ent.Comp.PodAnnounceAt = null;
        ent.Comp.PodCalled = false;
        ent.Comp.PodRecallAt = now + ent.Comp.PodRecallDelay;

        Notify(ent, Loc.GetString("extraction-pod-lost", ("seconds", (int) ent.Comp.PodRecallDelay.TotalSeconds)));
        _adminLog.Add(LogType.Action, LogImpact.Low, $"Extraction drop pod of {ToPrettyString(ent.Owner):player} lost its landing point");
    }

    private void LandPod(Entity<AntagExtractableComponent> ent, EntityCoordinates target)
    {
        if (!target.IsValid(EntityManager)
            || TerminatingOrDeleted(target.EntityId)
            || !TryComp<MapGridComponent>(target.EntityId, out var gridComp))
        {
            FailPodLanding(ent, _timing.CurTime);
            return;
        }

        var grid = target.EntityId;
        var tile = _map.TileIndicesFor(grid, gridComp, target);

        ClearLandingTile(ent, grid, gridComp, tile);

        var pod = Spawn(ent.Comp.PodPrototype, target);
        var podComp = EnsureComp<DropPodComponent>(pod);
        podComp.Extractee = ent.Owner;
        ent.Comp.Pod = pod;

        // scorch mark where the pod hit, it stays even if the pod is destroyed
        _decals.TryAddDecal(podComp.LandingDecal, new EntityCoordinates(grid, tile.X, tile.Y), out _, cleanable: true);

        PlayImpactSound(ent, target);

        // the pod comes only once
        _actions.RemoveAction(ent.Owner, ent.Comp.CallPodActionEntity);
        ent.Comp.CallPodActionEntity = null;

        Notify(ent, Loc.GetString("extraction-pod-landed", ("code", ent.Comp.Code)));
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"Extraction drop pod {ToPrettyString(pod)} of {ToPrettyString(ent.Owner):player} landed at {target}");
    }

    /// <summary>
    /// Everything on the landing tile is destroyed, walls included, the way an arriving shuttle flattens what is in its way.
    /// Bodies are gibbed.
    /// </summary>
    private void ClearLandingTile(Entity<AntagExtractableComponent> ent, EntityUid grid, MapGridComponent gridComp, Vector2i tile)
    {
        var immune = new HashSet<EntityUid>();
        foreach (var victim in _lookup.GetLocalEntitiesIntersecting(grid, tile, flags: LookupFlags.Uncontained, gridComp: gridComp))
        {
            if (immune.Contains(victim)
                || TerminatingOrDeleted(victim)
                || HasComp<GhostComponent>(victim)
                || HasComp<ExtractionZoneComponent>(victim)
                || HasComp<FTLSmashImmuneComponent>(victim))
                continue;

            if (TryComp<BodyComponent>(victim, out var body))
            {
                _adminLog.Add(LogType.Gib, LogImpact.High,
                    $"{ToPrettyString(victim):player} was crushed by the extraction drop pod of {ToPrettyString(ent.Owner):player}");
                immune.UnionWith(_body.GibBody(victim, body: body));
                continue;
            }

            // body-less mobs like borgs break down properly instead of vanishing
            if (HasComp<MobStateComponent>(victim))
            {
                _adminLog.Add(LogType.Gib, LogImpact.High,
                    $"{ToPrettyString(victim):player} was crushed by the extraction drop pod of {ToPrettyString(ent.Owner):player}");
                _destructible.DestroyEntity(victim);
                continue;
            }

            QueueDel(victim);
        }
    }

    /// <summary>
    /// Near players hear the positional impact, everyone else on the map the distant one, like explosions and meteors.
    /// </summary>
    private void PlayImpactSound(Entity<AntagExtractableComponent> ent, EntityCoordinates target)
    {
        var mapCoords = _transform.ToMapCoordinates(target);
        var nearRange = ent.Comp.PodImpactNearRange;

        var nearFilter = Filter.Pvs(target).AddInRange(mapCoords, nearRange);
        _audio.PlayStatic(ent.Comp.PodImpactSound, nearFilter, target, true, ent.Comp.PodImpactSound.Params);

        var farFilter = Filter.Empty().AddInMap(mapCoords.MapId).RemoveInRange(mapCoords, nearRange);
        _audio.PlayGlobal(ent.Comp.PodImpactSoundFar, farFilter, true, ent.Comp.PodImpactSoundFar.Params);
    }

    #endregion

    #region Keypad

    private void OnPodUiOpened(Entity<DropPodComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdatePodUi(ent);
    }

    private void OnKeypad(Entity<DropPodComponent> ent, ref DropPodKeypadMessage args)
    {
        if (!ent.Comp.Locked || ent.Comp.EnteredCode.Length >= GetCodeLength(ent))
            return;

        ent.Comp.EnteredCode += Math.Clamp(args.Value, 0, 9).ToString();
        ent.Comp.WrongCode = false;
        _audio.PlayPvs(ent.Comp.KeypadSound, ent.Owner);
        UpdatePodUi(ent);
    }

    private void OnKeypadClear(Entity<DropPodComponent> ent, ref DropPodKeypadClearMessage args)
    {
        ent.Comp.EnteredCode = string.Empty;
        ent.Comp.WrongCode = false;
        _audio.PlayPvs(ent.Comp.KeypadSound, ent.Owner);
        UpdatePodUi(ent);
    }

    private void OnKeypadEnter(Entity<DropPodComponent> ent, ref DropPodKeypadEnterMessage args)
    {
        if (!ent.Comp.Locked)
            return;

        var code = CompOrNull<AntagExtractableComponent>(ent.Comp.Extractee)?.Code;
        var correct = !string.IsNullOrEmpty(code) && ent.Comp.EnteredCode == code;
        ent.Comp.EnteredCode = string.Empty;

        if (correct)
        {
            ent.Comp.Locked = false;
            ent.Comp.WrongCode = false;
            Dirty(ent);
            _audio.PlayPvs(ent.Comp.UnlockSound, ent.Owner);
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Actor):player} unlocked extraction drop pod {ToPrettyString(ent.Owner)}");
        }
        else
        {
            ent.Comp.WrongCode = true;
            _audio.PlayPvs(ent.Comp.WrongCodeSound, ent.Owner);
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Actor):player} entered a wrong code on extraction drop pod {ToPrettyString(ent.Owner)}");
        }

        UpdatePodUi(ent);
    }

    private int GetCodeLength(Entity<DropPodComponent> ent)
    {
        return CompOrNull<AntagExtractableComponent>(ent.Comp.Extractee)?.CodeLength ?? 6;
    }

    private void UpdatePodUi(Entity<DropPodComponent> ent)
    {
        var status = ent.Comp.Launching ? DropPodStatus.Launching
            : !ent.Comp.Locked ? DropPodStatus.Unlocked
            : ent.Comp.WrongCode ? DropPodStatus.WrongCode
            : DropPodStatus.Locked;

        _ui.SetUiState(ent.Owner, DropPodUiKey.Key, new DropPodUiState(status, ent.Comp.EnteredCode.Length, GetCodeLength(ent)));
    }

    #endregion

    #region Launch

    private void OnPodInserted(Entity<DropPodComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != SharedEntityStorageSystem.ContainerName || args.Entity != ent.Comp.Extractee)
            return;

        // the same action entity is reused on every entry
        _actions.AddAction(args.Entity, ref ent.Comp.LaunchActionEntity, ent.Comp.LaunchAction);
        _popup.PopupEntity(Loc.GetString("extraction-pod-inside"), args.Entity, args.Entity);
    }

    private void OnPodRemoved(Entity<DropPodComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != SharedEntityStorageSystem.ContainerName || args.Entity != ent.Comp.Extractee)
            return;

        _actions.RemoveAction(args.Entity, ent.Comp.LaunchActionEntity);

        if (!ent.Comp.Launching)
            return;

        // the extractee left the pod during the countdown: no launch without them
        StopLaunch(ent);
        _popup.PopupEntity(Loc.GetString("extraction-pod-launch-cancelled"), args.Entity, args.Entity, PopupType.MediumCaution);
        _adminLog.Add(LogType.Action, LogImpact.Low, $"Launch of extraction drop pod {ToPrettyString(ent.Owner)} was cancelled, {ToPrettyString(args.Entity):player} left it");
    }

    private void OnPodShutdown(Entity<DropPodComponent> ent, ref ComponentShutdown args)
    {
        _audio.Stop(ent.Comp.EngineSoundStream);
    }

    private void OnLaunchPod(Entity<AntagExtractableComponent> ent, ref LaunchDropPodActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var user = ent.Owner;

        if (!_container.TryGetContainingContainer(user, out var container)
            || !TryComp<DropPodComponent>(container.Owner, out var pod)
            || pod.Extractee != user)
        {
            _popup.PopupEntity(Loc.GetString("extraction-pod-not-inside"), user, user, PopupType.SmallCaution);
            return;
        }

        if (pod.Launching)
            return;

        pod.Launching = true;
        pod.LaunchAt = _timing.CurTime + pod.LaunchDelay;
        pod.LastCountdownSecond = 0;
        Dirty(container.Owner, pod);
        UpdatePodUi((container.Owner, pod));

        if (pod.EngineSound != null)
            pod.EngineSoundStream = _audio.PlayPvs(pod.EngineSound, container.Owner, pod.EngineSound.Params.WithLoop(true))?.Entity;

        _popup.PopupEntity(Loc.GetString("extraction-pod-launching", ("seconds", (int) pod.LaunchDelay.TotalSeconds)), user, user, PopupType.Medium);
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} started the launch of extraction drop pod {ToPrettyString(container.Owner)}");
    }

    private void StopLaunch(Entity<DropPodComponent> pod)
    {
        pod.Comp.Launching = false;
        pod.Comp.EngineSoundStream = _audio.Stop(pod.Comp.EngineSoundStream);
        Dirty(pod);
        UpdatePodUi(pod);
    }

    private void UpdatePods(TimeSpan now)
    {
        var query = EntityQueryEnumerator<DropPodComponent>();
        while (query.MoveNext(out var uid, out var pod))
        {
            if (!pod.Launching)
                continue;

            var remaining = (int) Math.Ceiling((pod.LaunchAt - now).TotalSeconds);
            if (remaining <= 0)
            {
                LaunchPod((uid, pod));
                continue;
            }

            if (pod.LastCountdownSecond == remaining)
                continue;

            pod.LastCountdownSecond = remaining;

            if (pod.CountdownSound != null)
                _audio.PlayPvs(pod.CountdownSound, uid);

            if (Array.IndexOf(CountdownPopups, remaining) >= 0 && pod.Extractee is { } occupant)
                _popup.PopupEntity(remaining.ToString(), occupant, occupant, PopupType.Large);
        }
    }

    private void LaunchPod(Entity<DropPodComponent> pod)
    {
        var coords = Transform(pod).Coordinates;
        StopLaunch(pod);

        if (pod.Comp.Extractee is { } extractee
            && TryComp<AntagExtractableComponent>(extractee, out var extractable)
            && _container.TryGetContainingContainer(extractee, out var container)
            && container.Owner == pod.Owner)
        {
            extractable.Pod = null;
            Extract((extractee, extractable), coords);
        }

        // anyone and anything else stuffed inside stays on the station
        _entityStorage.EmptyContents(pod.Owner);

        RaiseNetworkEvent(new DropPodLaunchAnimationEvent(GetNetCoordinates(coords), pod.Comp.LaunchAnimationDuration, pod.Comp.LaunchSprite),
            Filter.Pvs(pod.Owner));
        _audio.PlayPvs(pod.Comp.LaunchSound, coords);

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"Extraction drop pod {ToPrettyString(pod.Owner)} launched from {coords}");
        QueueDel(pod.Owner);
    }

    #endregion
}
