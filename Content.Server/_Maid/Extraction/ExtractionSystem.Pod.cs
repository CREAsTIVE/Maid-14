using Content.Shared._Maid.Extraction;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
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
        SubscribeLocalEvent<DropPodComponent, BoundUIOpenedEvent>(OnPodUiOpened);
        SubscribeLocalEvent<DropPodComponent, DropPodKeypadMessage>(OnKeypad);
        SubscribeLocalEvent<DropPodComponent, DropPodKeypadClearMessage>(OnKeypadClear);
        SubscribeLocalEvent<DropPodComponent, DropPodKeypadEnterMessage>(OnKeypadEnter);
    }

    #region Calling and landing

    private void OnCallPod(Entity<AntagExtractableComponent> ent, ref CallDropPodActionEvent args)
    {
        if (args.Handled || !ent.Comp.Unlocked || ent.Comp.Extracted)
            return;

        args.Handled = true;
        var user = ent.Owner;

        if (ent.Comp.PodTarget != null)
        {
            _popup.PopupEntity(Loc.GetString("extraction-pod-already-called"), user, user, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Pod is { } pod && !TerminatingOrDeleted(pod))
        {
            _popup.PopupEntity(Loc.GetString("extraction-pod-already-landed"), user, user, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Zone is not { } zone
            || !TryComp<ExtractionZoneComponent>(zone, out var zoneComp)
            || !_zone.IsInZone((zone, zoneComp), user))
        {
            _popup.PopupEntity(Loc.GetString("extraction-not-in-zone"), user, user, PopupType.SmallCaution);
            return;
        }

        var xform = Transform(user);
        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        // the pod lands on the tile the call was made from
        var tile = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        var now = _timing.CurTime;
        ent.Comp.PodTarget = _map.GridTileToLocal(grid, gridComp, tile);
        ent.Comp.PodLandAt = now + ent.Comp.PodFlightTime;
        ent.Comp.PodAnnounceAt = now + ent.Comp.PodAnnounceDelay;

        Notify(ent, Loc.GetString("extraction-pod-called", ("seconds", (int) ent.Comp.PodFlightTime.TotalSeconds)));
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} called an extraction drop pod to {ent.Comp.PodTarget} ({GetLocation(user)})");
    }

    private void UpdatePodCall(Entity<AntagExtractableComponent> ent, TimeSpan now)
    {
        // the pod was destroyed before launching, another one can be called
        if (ent.Comp.Pod is { } pod && TerminatingOrDeleted(pod))
        {
            ent.Comp.Pod = null;
            Notify(ent, Loc.GetString("extraction-pod-destroyed"));
            _adminLog.Add(LogType.Action, LogImpact.Low, $"Extraction drop pod of {ToPrettyString(ent.Owner):player} was destroyed");
        }

        if (ent.Comp.PodTarget is not { } target)
            return;

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

        if (now >= ent.Comp.PodLandAt)
            LandPod(ent, target);
    }

    private void LandPod(Entity<AntagExtractableComponent> ent, EntityCoordinates target)
    {
        ent.Comp.PodTarget = null;
        ent.Comp.PodAnnounceAt = null;

        if (!target.IsValid(EntityManager)
            || TerminatingOrDeleted(target.EntityId)
            || !TryComp<MapGridComponent>(target.EntityId, out var gridComp))
        {
            Notify(ent, Loc.GetString("extraction-pod-lost"));
            return;
        }

        var grid = target.EntityId;
        var tile = _map.TileIndicesFor(grid, gridComp, target);

        // whatever is alive on the landing tile gets crushed
        foreach (var victim in _lookup.GetLocalEntitiesIntersecting(grid, tile, gridComp: gridComp))
        {
            if (!HasComp<MobStateComponent>(victim) || _mobState.IsDead(victim) || TerminatingOrDeleted(victim))
                continue;

            _adminLog.Add(LogType.Gib, LogImpact.High, $"{ToPrettyString(victim):player} was crushed by the extraction drop pod of {ToPrettyString(ent.Owner):player}");

            if (HasComp<BodyComponent>(victim))
                _body.GibBody(victim);
            else
                _destructible.DestroyEntity(victim);
        }

        var pod = Spawn(ent.Comp.PodPrototype, target);
        var podComp = EnsureComp<DropPodComponent>(pod);
        podComp.Extractee = ent.Owner;
        Dirty(pod, podComp);
        ent.Comp.Pod = pod;

        // heard by everyone on the map, from the direction of the impact
        var mapId = _transform.GetMapId(target);
        var audioParams = ent.Comp.PodImpactSound.Params.WithMaxDistance(150f).WithVolume(5f);
        _audio.PlayStatic(ent.Comp.PodImpactSound, Filter.BroadcastMap(mapId), target, true, audioParams);

        Notify(ent, Loc.GetString("extraction-pod-landed", ("code", ent.Comp.Code)));
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"Extraction drop pod {ToPrettyString(pod)} of {ToPrettyString(ent.Owner):player} landed at {target}");
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

        _actions.AddAction(args.Entity, ref ent.Comp.LaunchActionEntity, ent.Comp.LaunchAction);
        _popup.PopupEntity(Loc.GetString("extraction-pod-inside"), args.Entity, args.Entity);
    }

    private void OnPodRemoved(Entity<DropPodComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != SharedEntityStorageSystem.ContainerName || args.Entity != ent.Comp.Extractee)
            return;

        _actions.RemoveAction(args.Entity, ent.Comp.LaunchActionEntity);
        ent.Comp.LaunchActionEntity = null;
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
        pod.NextCountdownPopup = 0;
        Dirty(container.Owner, pod);
        UpdatePodUi((container.Owner, pod));

        _popup.PopupEntity(Loc.GetString("extraction-pod-launching", ("seconds", (int) pod.LaunchDelay.TotalSeconds)), user, user, PopupType.Medium);
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} started the launch of extraction drop pod {ToPrettyString(container.Owner)}");
    }

    private void UpdatePods(TimeSpan now)
    {
        var query = EntityQueryEnumerator<DropPodComponent>();
        while (query.MoveNext(out var uid, out var pod))
        {
            if (!pod.Launching)
                continue;

            var remaining = (int) Math.Ceiling((pod.LaunchAt - now).TotalSeconds);
            if (remaining > 0)
            {
                // countdown for whoever is inside
                if (pod.NextCountdownPopup != remaining && Array.IndexOf(CountdownPopups, remaining) >= 0)
                {
                    pod.NextCountdownPopup = remaining;
                    if (pod.Extractee is { } occupant)
                        _popup.PopupEntity(remaining.ToString(), occupant, occupant, PopupType.Large);
                }

                continue;
            }

            LaunchPod((uid, pod));
        }
    }

    private void LaunchPod(Entity<DropPodComponent> pod)
    {
        var coords = Transform(pod).Coordinates;
        pod.Comp.Launching = false;

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

        // scorch mark where the pod stood
        if (Transform(pod).GridUid is { } grid && TryComp<MapGridComponent>(grid, out var gridComp))
        {
            var tile = _map.TileIndicesFor(grid, gridComp, coords);
            _decals.TryAddDecal(pod.Comp.LandingDecal, new EntityCoordinates(grid, tile.X, tile.Y), out _, cleanable: true);
        }

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"Extraction drop pod {ToPrettyString(pod.Owner)} launched from {coords}");
        QueueDel(pod.Owner);
    }

    #endregion
}
