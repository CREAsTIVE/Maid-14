using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Server.Pinpointer;
using Content.Server.Station.Systems;
using Content.Shared._Maid.Extraction;
using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Holopad;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mind;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Objectives.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Maid.Extraction;

/// <summary>
/// Unlocks extraction once objectives are done, places the zone at a station holopad on request
/// and removes the antagonist from the round, leaving station items behind.
/// </summary>
public sealed class ExtractionSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ExtractionZoneSystem _zone = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterAntagEntitySelectedEvent>(OnAntagSelected);

        SubscribeLocalEvent<ExtractableComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ExtractableComponent, RequestExtractionActionEvent>(OnRequest);
        SubscribeLocalEvent<ExtractableComponent, ExtractionDoAfterEvent>(OnDoAfter);

        // anything done by or to the extractee interrupts the extraction
        SubscribeLocalEvent<ExtractableComponent, InteractHandEvent>(OnInteractedHand);
        SubscribeLocalEvent<ExtractableComponent, InteractUsingEvent>(OnInteractedUsing);
        SubscribeLocalEvent<ExtractableComponent, PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<ExtractableComponent, AttackAttemptEvent>(OnAttackAttempt);

        SubscribeLocalEvent<ExtractionZoneComponent, InteractHandEvent>(OnZoneInteractHand);
        SubscribeLocalEvent<ExtractionZoneComponent, InteractUsingEvent>(OnZoneInteractUsing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ExtractableComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Unlocked || now < comp.NextCheck)
                continue;

            comp.NextCheck = now + CheckInterval;

            if (_mind.TryGetMind(uid, out var mindId, out var mind) && ObjectivesMet((uid, comp), (mindId, mind)))
                Unlock((uid, comp));
        }
    }

    #region Gear

    private void OnAntagSelected(ref AfterAntagEntitySelectedEvent args)
    {
        if (!HasComp<ExtractableComponent>(args.EntityUid))
            return;

        foreach (var item in _inventory.GetHandOrInventoryEntities(args.EntityUid))
        {
            MarkGear(item);
        }
    }

    private void MarkGear(EntityUid item)
    {
        if (HasComp<VirtualItemComponent>(item))
            return;

        EnsureComp<ExtractionGearComponent>(item);

        foreach (var contained in GetContained(item))
        {
            MarkGear(contained);
        }
    }

    /// <summary>
    /// Drops every station item found on or inside <paramref name="item"/> at the given coordinates.
    /// Gear stays where it is and leaves together with the extractee.
    /// </summary>
    private void ReleaseStationItems(EntityUid item, EntityCoordinates coords)
    {
        if (HasComp<VirtualItemComponent>(item))
            return;

        if (HasComp<ExtractionGearComponent>(item))
        {
            foreach (var contained in GetContained(item))
            {
                ReleaseStationItems(contained, coords);
            }

            return;
        }

        if (_container.TryGetContainingContainer(item, out var container))
            _container.Remove(item, container, force: true, destination: coords);

        DeleteNestedGear(item);
    }

    private void DeleteNestedGear(EntityUid item)
    {
        foreach (var contained in GetContained(item))
        {
            if (HasComp<ExtractionGearComponent>(contained))
                QueueDel(contained);
            else
                DeleteNestedGear(contained);
        }
    }

    private List<EntityUid> GetContained(EntityUid uid)
    {
        var result = new List<EntityUid>();
        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            return result;

        foreach (var container in _container.GetAllContainers(uid, manager))
        {
            result.AddRange(container.ContainedEntities);
        }

        return result;
    }

    #endregion

    #region Objectives

    /// <summary>
    /// Checks required objectives and the threshold over required and optional ones.
    /// Objectives in neither list are not counted.
    /// </summary>
    public bool ObjectivesMet(Entity<ExtractableComponent> ent, Entity<MindComponent> mind)
    {
        var total = 0;
        var completed = 0;

        foreach (var objective in mind.Comp.Objectives)
        {
            if (MetaData(objective).EntityPrototype?.ID is not { } id)
                continue;

            var required = ent.Comp.RequiredObjectives.Contains(id);
            if (!required && !ent.Comp.OptionalObjectives.Contains(id))
                continue;

            total++;
            if (_objectives.IsCompleted(objective, mind))
                completed++;
            else if (required)
                return false;
        }

        if (total == 0)
            return true;

        return ent.Comp.ThresholdMode switch
        {
            ExtractionThresholdMode.Count => completed >= Math.Min(ent.Comp.Threshold, total),
            _ => completed * 100f / total >= ent.Comp.Threshold - 0.01f,
        };
    }

    private void Unlock(Entity<ExtractableComponent> ent)
    {
        ent.Comp.Unlocked = true;
        Dirty(ent);

        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        Notify(ent, Loc.GetString("extraction-unlocked"));
    }

    /// <summary>
    /// Admin shortcut: unlocks extraction regardless of objectives.
    /// </summary>
    public bool TryUnlock(EntityUid uid)
    {
        if (!TryComp<ExtractableComponent>(uid, out var comp) || comp.Unlocked)
            return false;

        Unlock((uid, comp));
        return true;
    }

    #endregion

    #region Zone

    private void OnRequest(Entity<ExtractableComponent> ent, ref RequestExtractionActionEvent args)
    {
        if (args.Handled || !ent.Comp.Unlocked)
            return;

        args.Handled = true;

        var current = ent.Comp.Zone is { } zone && Exists(zone) ? zone : (EntityUid?) null;
        if (ent.Comp.RequestsUsed >= ent.Comp.MaxRequests)
        {
            Notify(ent, current is { } existing
                ? Loc.GetString("extraction-no-requests-zone", ("location", _navMap.GetNearestBeaconString((existing, null))))
                : Loc.GetString("extraction-no-requests"));
            return;
        }

        var exclude = CompOrNull<ExtractionZoneComponent>(current)?.Holopad;
        if (SpawnZone(ent, exclude) is not { } newZone)
        {
            Notify(ent, Loc.GetString("extraction-no-zone"));
            return;
        }

        if (current != null)
            QueueDel(current);

        ent.Comp.Zone = newZone;
        ent.Comp.RequestsUsed++;
        Dirty(ent);

        var location = _navMap.GetNearestBeaconString((newZone, null));
        var left = ent.Comp.MaxRequests - ent.Comp.RequestsUsed;
        Notify(ent, Loc.GetString("extraction-zone-designated", ("location", location), ("left", left)));
        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(ent.Owner):player} requested an extraction zone {ToPrettyString(newZone)}");
    }

    /// <summary>
    /// Places a zone marker at a random holopad on a station's main grid.
    /// </summary>
    private EntityUid? SpawnZone(Entity<ExtractableComponent> ent, EntityUid? exclude)
    {
        var stationGrids = new HashSet<EntityUid>();
        foreach (var station in _station.GetStations())
        {
            if (_station.GetLargestGrid(station) is { } grid)
                stationGrids.Add(grid);
        }

        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<HolopadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (uid == exclude || !xform.Anchored || xform.GridUid is not { } grid || !stationGrids.Contains(grid))
                continue;

            candidates.Add(uid);
        }

        // only one holopad on the station, keep it rather than failing the re-roll
        if (candidates.Count == 0 && exclude is { } previous && Exists(previous))
            candidates.Add(previous);

        if (candidates.Count == 0)
            return null;

        var holopad = _random.Pick(candidates);
        var zone = Spawn(ent.Comp.ZonePrototype, Transform(holopad).Coordinates);
        var zoneComp = EnsureComp<ExtractionZoneComponent>(zone);
        zoneComp.Extractee = ent.Owner;
        zoneComp.Holopad = holopad;
        Dirty(zone, zoneComp);

        return zone;
    }

    /// <summary>
    /// Admin command: removes the zone and lets the extractee request it again.
    /// </summary>
    public bool ResetZone(EntityUid uid)
    {
        if (!TryComp<ExtractableComponent>(uid, out var comp))
            return false;

        CancelExtraction((uid, comp), false);

        if (comp.Zone is { } zone)
            QueueDel(zone);

        comp.Zone = null;
        comp.RequestsUsed = 0;
        Dirty(uid, comp);

        if (comp.Unlocked)
            Notify((uid, comp), Loc.GetString("extraction-zone-reset"));

        return true;
    }

    private void OnRemove(Entity<ExtractableComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.Zone is { } zone)
            QueueDel(zone);
    }

    #endregion

    #region Extraction

    private void OnZoneInteractHand(Entity<ExtractionZoneComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled
            || args.User != ent.Comp.Extractee
            || !TryComp<ExtractableComponent>(args.User, out var extractable))
            return;

        args.Handled = true;
        var user = args.User;

        if (!_zone.IsInZone(ent, user))
        {
            _popup.PopupEntity(Loc.GetString("extraction-not-in-zone"), user, user, PopupType.SmallCaution);
            return;
        }

        if (!_mind.TryGetMind(user, out var mindId, out var mind) || !ObjectivesMet((user, extractable), (mindId, mind)))
        {
            _popup.PopupEntity(Loc.GetString("extraction-not-ready"), user, user, PopupType.SmallCaution);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, user, extractable.ExtractionDelay, new ExtractionDoAfterEvent(), user, target: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnDropItem = true,
            NeedHand = true,
            DistanceThreshold = null,
        };

        if (!_doAfter.TryStartDoAfter(doAfter, out var id))
            return;

        extractable.DoAfter = id;
        _popup.PopupEntity(Loc.GetString("extraction-started"), user, user);
    }

    private void OnZoneInteractUsing(Entity<ExtractionZoneComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.User != ent.Comp.Extractee)
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("extraction-need-empty-hand"), args.User, args.User, PopupType.SmallCaution);
    }

    private void OnDoAfter(Entity<ExtractableComponent> ent, ref ExtractionDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;

        if (args.Cancelled || args.Handled || args.Target is not { } zone || !TryComp<ExtractionZoneComponent>(zone, out var zoneComp))
            return;

        args.Handled = true;
        Extract(ent, (zone, zoneComp));
    }

    private void Extract(Entity<ExtractableComponent> ent, Entity<ExtractionZoneComponent> zone)
    {
        var user = ent.Owner;
        var coords = Transform(zone).Coordinates;
        var station = _station.GetOwningStation(zone.Owner);

        foreach (var item in _inventory.GetHandOrInventoryEntities(user).ToList())
        {
            ReleaseStationItems(item, coords);
        }

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} was extracted from the round at {coords}");

        if (_mind.TryGetMind(user, out var mindId, out var mind))
        {
            EnsureComp<ExtractedMindComponent>(mindId);
            _ghost.SpawnGhost((mindId, mind), coords, canReturn: false);
        }

        _audio.PlayPvs(ent.Comp.ExtractSound, coords);

        if (station != null)
        {
            _chat.DispatchStationAnnouncement(station.Value,
                Loc.GetString(ent.Comp.Announcement),
                Loc.GetString("extraction-announcement-sender"),
                colorOverride: Color.Gold);
        }

        QueueDel(zone.Owner);
        QueueDel(user);
    }

    private void CancelExtraction(Entity<ExtractableComponent> ent, bool popup = true)
    {
        if (ent.Comp.DoAfter is not { } id)
            return;

        ent.Comp.DoAfter = null;
        _doAfter.Cancel(id);

        if (popup)
            _popup.PopupEntity(Loc.GetString("extraction-interrupted"), ent.Owner, ent.Owner, PopupType.MediumCaution);
    }

    private void OnInteractedHand(Entity<ExtractableComponent> ent, ref InteractHandEvent args)
    {
        CancelExtraction(ent);
    }

    private void OnInteractedUsing(Entity<ExtractableComponent> ent, ref InteractUsingEvent args)
    {
        CancelExtraction(ent);
    }

    private void OnPullStarted(Entity<ExtractableComponent> ent, ref PullStartedMessage args)
    {
        CancelExtraction(ent);
    }

    private void OnAttackAttempt(Entity<ExtractableComponent> ent, ref AttackAttemptEvent args)
    {
        CancelExtraction(ent);
    }

    #endregion

    private void Notify(Entity<ExtractableComponent> ent, string message)
    {
        _popup.PopupEntity(Loc.GetString("extraction-notify-popup"), ent.Owner, ent.Owner, PopupType.Medium);

        if (TryComp<ActorComponent>(ent, out var actor))
            _chatManager.DispatchServerMessage(actor.PlayerSession, message);
    }
}
