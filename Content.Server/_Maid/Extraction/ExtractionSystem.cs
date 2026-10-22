using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Decals;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Objectives.Components;
using Content.Server.Pinpointer;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared._DV.Carrying;
using Content.Shared._Maid.Extraction;
using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Eye;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Holopad;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Objectives.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Maid.Extraction;

/// <summary>
/// Unlocks extraction once objectives are done and places the extraction zone at a station holopad on request.
/// The pod part lives in ExtractionSystem.Pod.cs.
/// Extracted bodies are kept alive on a paused map so the character counts as alive, not dead.
/// </summary>
public sealed partial class ExtractionSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly CarryingSystem _carrying = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly FollowerSystem _follower = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ExtractionZoneSystem _zone = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Paused map holding the bodies of extracted antagonists.
    /// </summary>
    private EntityUid? _limboMap;

    private readonly List<(string Name, int Minutes)> _extracted = new();

    /// <summary>
    /// Extracted bodies whose player came back into them, e.g. after a reconnect deleted their ghost.
    /// </summary>
    private readonly HashSet<EntityUid> _reghost = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterAntagEntitySelectedEvent>(OnAntagSelected);
        SubscribeLocalEvent<GetVisMaskEvent>(OnGetVisMask);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnGhostAttached);

        SubscribeLocalEvent<AntagExtractableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AntagExtractableComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<AntagExtractableComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<AntagExtractableComponent, RequestExtractionActionEvent>(OnRequest);

        InitializePod();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        foreach (var body in _reghost)
        {
            Reghost(body);
        }

        _reghost.Clear();

        var query = EntityQueryEnumerator<AntagExtractableComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Extracted)
                continue;

            // the zone got destroyed (singularity, grid deletion...), the clan sends a new one for free
            if (comp.Zone is { } zone && TerminatingOrDeleted(zone))
                ReplaceLostZone((uid, comp));

            UpdatePodCall((uid, comp), now);

            if (comp.Unlocked || now < comp.NextCheck)
                continue;

            comp.NextCheck = now + CheckInterval;

            if (!_mind.TryGetMind(uid, out var mindId, out var mind)
                || !ObjectivesMet((uid, comp), (mindId, mind), out var completed, out var total))
                continue;

            _adminLog.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(uid):player} unlocked extraction with {completed}/{total} counted objectives completed");
            Unlock((uid, comp));
        }

        UpdatePods(now);
    }

    #region Gear

    private void OnMapInit(Entity<AntagExtractableComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ArrivalTime = _timing.CurTime;
    }

    private void OnAntagSelected(ref AfterAntagEntitySelectedEvent args)
    {
        if (!TryComp<AntagExtractableComponent>(args.EntityUid, out var comp))
            return;

        comp.ArrivalTime = _timing.CurTime;
        MarkCarriedGear(args.EntityUid);
    }

    private void MarkCarriedGear(EntityUid uid)
    {
        foreach (var item in _inventory.GetHandOrInventoryEntities(uid))
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
    private void ReleaseStationItems(EntityUid user, EntityUid item, EntityCoordinates coords, List<EntityUid> released)
    {
        // something released earlier may have pulled this one along, e.g. a hardsuit retracting its helmet
        if (HasComp<VirtualItemComponent>(item) || !IsCarriedBy(item, user))
            return;

        if (HasComp<ExtractionGearComponent>(item))
        {
            foreach (var contained in GetContained(item))
            {
                ReleaseStationItems(user, contained, coords, released);
            }

            return;
        }

        // actions, solutions and other non-item children stay where they are
        if (!HasComp<ItemComponent>(item))
            return;

        if (_container.TryGetContainingContainer(item, out var container))
            _container.Remove(item, container, force: true, destination: coords);

        DeleteNestedGear(item);
        released.Add(item);
    }

    /// <summary>
    /// Drops what is attached to the body outside of inventory: carried people, embedded projectiles,
    /// contents of storage implants.
    /// </summary>
    private void ReleaseAttached(EntityUid user, EntityCoordinates coords, List<EntityUid> released)
    {
        if (TryComp<CarryingComponent>(user, out var carrying))
        {
            var carried = carrying.Carried;
            _carrying.DropCarried(user, carried);
            _transform.SetCoordinates(carried, coords);
            released.Add(carried);
        }

        if (TryComp<FollowedComponent>(user, out var followed))
            _follower.StopAllFollowers(user, followed);

        var children = new List<EntityUid>();
        var enumerator = Transform(user).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            children.Add(child);
        }

        foreach (var child in children)
        {
            if (TerminatingOrDeleted(child)
                || _container.IsEntityInContainer(child)
                || HasComp<ExtractionGearComponent>(child))
                continue;

            if (TryComp<EmbeddableProjectileComponent>(child, out var embed))
            {
                _projectile.EmbedDetach(child, embed);
                if (TerminatingOrDeleted(child) || EntityManager.IsQueuedForDeletion(child))
                    continue;
            }
            else if (!HasComp<ItemComponent>(child) && !HasComp<MindContainerComponent>(child))
            {
                continue;
            }

            _transform.SetCoordinates(child, coords);
            released.Add(child);
        }

        if (TryComp<ImplantedComponent>(user, out var implanted))
        {
            foreach (var implant in implanted.ImplantContainer.ContainedEntities.ToList())
            {
                foreach (var contained in GetContained(implant))
                {
                    ReleaseStationItems(user, contained, coords, released);
                }
            }
        }
    }

    private bool IsCarriedBy(EntityUid item, EntityUid user)
    {
        var parent = Transform(item).ParentUid;
        while (parent.IsValid())
        {
            if (parent == user)
                return true;

            parent = Transform(parent).ParentUid;
        }

        return false;
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
    /// Objectives in neither list are not counted. With both lists empty every objective
    /// counts except survival ones. Without any counted objective extraction stays locked.
    /// </summary>
    public bool ObjectivesMet(Entity<AntagExtractableComponent> ent, Entity<MindComponent> mind, out int completed, out int total)
    {
        total = 0;
        completed = 0;
        var requiredMissing = false;
        var countAll = ent.Comp.RequiredObjectives.Count == 0 && ent.Comp.OptionalObjectives.Count == 0;

        foreach (var objective in mind.Comp.Objectives)
        {
            if (MetaData(objective).EntityPrototype?.ID is not { } id)
                continue;

            var required = ent.Comp.RequiredObjectives.Contains(id);
            if (countAll ? HasComp<SurviveConditionComponent>(objective) : !required && !ent.Comp.OptionalObjectives.Contains(id))
                continue;

            total++;
            if (_objectives.IsCompleted(objective, mind))
                completed++;
            else if (required)
                requiredMissing = true;
        }

        if (total == 0 || requiredMissing)
            return false;

        return ent.Comp.ThresholdMode switch
        {
            ExtractionThresholdMode.Count => completed >= Math.Min(ent.Comp.Threshold, total),
            _ => completed * 100f / total >= ent.Comp.Threshold - 0.01f,
        };
    }

    private void Unlock(Entity<AntagExtractableComponent> ent, bool forced = false)
    {
        ent.Comp.Unlocked = true;
        ent.Comp.ForceUnlocked |= forced;

        if (string.IsNullOrEmpty(ent.Comp.Code))
        {
            var max = (int) Math.Pow(10, ent.Comp.CodeLength);
            ent.Comp.Code = _random.Next(max).ToString().PadLeft(ent.Comp.CodeLength, '0');
        }

        Dirty(ent);

        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        _actions.AddAction(ent.Owner, ref ent.Comp.CallPodActionEntity, ent.Comp.CallPodAction);
        Notify(ent, Loc.GetString(forced ? "extraction-unlocked-forced" : "extraction-unlocked", ("code", ent.Comp.Code)));
    }

    /// <summary>
    /// Admin command: unlocks extraction regardless of objectives.
    /// Entities without <see cref="AntagExtractableComponent"/> get it, with what they carry now counted as their gear.
    /// </summary>
    public string UnlockCommand(EntityUid uid)
    {
        if (!Exists(uid))
            return Loc.GetString("extraction-command-no-entity");

        var name = ToPrettyString(uid).ToString();
        var added = false;
        if (!TryComp<AntagExtractableComponent>(uid, out var comp))
        {
            comp = AddComp<AntagExtractableComponent>(uid);
            MarkCarriedGear(uid);
            added = true;
        }

        if (comp.Unlocked)
            return Loc.GetString("extraction-command-already-unlocked", ("target", name));

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"Extraction of {ToPrettyString(uid):player} was unlocked by an admin command");
        Unlock((uid, comp), true);
        return Loc.GetString(added ? "extraction-command-unlocked-added" : "extraction-command-unlocked", ("target", name));
    }

    #endregion

    #region Zone

    private void OnRequest(Entity<AntagExtractableComponent> ent, ref RequestExtractionActionEvent args)
    {
        if (args.Handled || !ent.Comp.Unlocked || ent.Comp.Extracted)
            return;

        args.Handled = true;

        var current = ent.Comp.Zone is { } zone && !TerminatingOrDeleted(zone) ? zone : (EntityUid?) null;
        if (ent.Comp.RequestsUsed >= ent.Comp.MaxRequests)
        {
            Notify(ent, current is { } existing
                ? Loc.GetString("extraction-no-requests-zone", ("location", GetLocation(existing)))
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

        ent.Comp.RequestsUsed++;
        Dirty(ent);

        var left = ent.Comp.MaxRequests - ent.Comp.RequestsUsed;
        Notify(ent, Loc.GetString("extraction-zone-designated", ("location", GetLocation(newZone)), ("left", left)));
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(ent.Owner):player} requested extraction zone {ToPrettyString(newZone)} at {GetLocation(newZone)} ({ent.Comp.RequestsUsed}/{ent.Comp.MaxRequests})");
    }

    private void ReplaceLostZone(Entity<AntagExtractableComponent> ent)
    {
        var lost = ent.Comp.Zone;
        ent.Comp.Zone = null;

        if (SpawnZone(ent, null) is not { } zone)
        {
            Dirty(ent);
            Notify(ent, Loc.GetString("extraction-zone-lost-none"));
            _adminLog.Add(LogType.Action, LogImpact.Low, $"Extraction zone {lost} of {ToPrettyString(ent.Owner):player} was destroyed, no replacement found");
            return;
        }

        Notify(ent, Loc.GetString("extraction-zone-lost", ("location", GetLocation(zone))));
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"Extraction zone {lost} of {ToPrettyString(ent.Owner):player} was destroyed, replaced by {ToPrettyString(zone)} at {GetLocation(zone)}");
    }

    /// <summary>
    /// Places a zone marker at a random holopad on a station's main grid and makes it the current zone.
    /// Only the extractee and admin ghosts receive it.
    /// </summary>
    private EntityUid? SpawnZone(Entity<AntagExtractableComponent> ent, EntityUid? exclude)
    {
        var stationGrids = new HashSet<EntityUid>();
        // only stations events can target, so no CentComm or other off-station bases
        var stations = EntityQueryEnumerator<StationEventEligibleComponent>();
        while (stations.MoveNext(out var station, out _))
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

            if (MetaData(uid).EntityPrototype?.ID is { } protoId && ent.Comp.HolopadBlacklist.Contains(protoId))
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

        _metaData.SetEntityDescription(zone, Loc.GetString("extraction-zone-marker-desc", ("name", Name(ent.Owner))));

        ent.Comp.Zone = zone;
        Dirty(ent);
        SendZoneToOwner(ent);
        return zone;
    }

    private void SendZoneToOwner(Entity<AntagExtractableComponent> ent)
    {
        if (ent.Comp.Zone is { } zone
            && !TerminatingOrDeleted(zone)
            && TryComp<ActorComponent>(ent, out var actor))
            _pvsOverride.AddForceSend(zone, actor.PlayerSession);
    }

    private void OnGetVisMask(ref GetVisMaskEvent args)
    {
        // admin ghosts see every extraction zone, everyone else only gets their own one force-sent
        if (HasComp<GhostComponent>(args.Entity)
            && TryComp<ActorComponent>(args.Entity, out var actor)
            && _admin.IsAdmin(actor.PlayerSession))
            args.VisibilityMask |= (int) VisibilityFlags.MaidExtraction;
    }

    private void OnGhostAttached(Entity<GhostComponent> ent, ref PlayerAttachedEvent args)
    {
        // admin ghosts are spawned before the player is attached, so the mask has to be refreshed now
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnPlayerAttached(Entity<AntagExtractableComponent> ent, ref PlayerAttachedEvent args)
    {
        if (ent.Comp.Extracted)
        {
            _reghost.Add(ent.Owner);
            return;
        }

        if (!ent.Comp.Unlocked)
            return;

        // force-sends are dropped on disconnect, and the player may have missed the messages
        SendZoneToOwner(ent);
        Notify(ent, ent.Comp.Zone is { } zone && !TerminatingOrDeleted(zone)
            ? Loc.GetString("extraction-reminder-zone", ("location", GetLocation(zone)), ("code", ent.Comp.Code))
            : Loc.GetString("extraction-reminder", ("code", ent.Comp.Code)));
    }

    private string GetLocation(EntityUid uid)
    {
        return FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString((uid, null)));
    }

    /// <summary>
    /// Admin command: removes the zone and lets the extractee request it again.
    /// </summary>
    public string ResetCommand(EntityUid uid)
    {
        if (!Exists(uid))
            return Loc.GetString("extraction-command-no-entity");

        var name = ToPrettyString(uid).ToString();
        if (!TryComp<AntagExtractableComponent>(uid, out var comp))
            return Loc.GetString("extraction-command-no-component", ("target", name));

        if (comp.Zone is { } zone)
            QueueDel(zone);

        comp.Zone = null;
        comp.RequestsUsed = 0;
        Dirty(uid, comp);

        if (comp.Unlocked)
            Notify((uid, comp), Loc.GetString("extraction-zone-reset"));

        _adminLog.Add(LogType.Action, LogImpact.Low, $"Extraction zone of {ToPrettyString(uid):player} was reset by an admin command");
        return Loc.GetString("extraction-command-reset", ("target", name));
    }

    private void OnRemove(Entity<AntagExtractableComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.Zone is { } zone)
            QueueDel(zone);

        ent.Comp.Zone = null;
        ent.Comp.PodTarget = null;

        // the whole entity is going away, its actions go with it
        if (TerminatingOrDeleted(ent.Owner))
            return;

        // e.g. the ninja implant was removed: nothing must be left to use
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.CallPodActionEntity);
        ent.Comp.ActionEntity = null;
        ent.Comp.CallPodActionEntity = null;
    }

    #endregion

    #region Leaving

    /// <summary>
    /// Takes the extractee out of the round: station items drop at <paramref name="coords"/>,
    /// the player becomes a ghost and the body moves to the paused map.
    /// </summary>
    private void Extract(Entity<AntagExtractableComponent> ent, EntityCoordinates coords)
    {
        var user = ent.Owner;
        var released = new List<EntityUid>();

        foreach (var item in _inventory.GetHandOrInventoryEntities(user).ToList())
        {
            ReleaseStationItems(user, item, coords, released);
        }

        ReleaseAttached(user, coords, released);

        var minutes = (int) Math.Round((_timing.CurTime - ent.Comp.ArrivalTime).TotalMinutes);
        var characterName = Name(user);

        if (_mind.TryGetMind(user, out var mindId, out var mind))
        {
            characterName = mind.CharacterName ?? characterName;
            EnsureComp<ExtractedMindComponent>(mindId);

            // the mind keeps owning the living body, so the character is alive for kill and survive objectives
            if (_ghost.SpawnGhost((mindId, mind), coords, canReturn: true) is { } ghost)
                _ghost.SetCanReturnToBody(ghost, false);
        }

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} was extracted from the round at {coords} after {minutes} min, left behind: {string.Join(", ", released.Select(e => ToPrettyString(e).ToString()))}");

        _extracted.Add((characterName, minutes));

        if (ent.Comp.Zone is { } zone)
            QueueDel(zone);

        ent.Comp.Extracted = true;
        ent.Comp.Zone = null;
        MoveToLimbo(user);
    }

    /// <summary>
    /// Moves the body to a paused map: nothing can reach or hurt it there, and it stays alive.
    /// </summary>
    private void MoveToLimbo(EntityUid user)
    {
        if (TryComp<PullerComponent>(user, out var puller)
            && puller.Pulling is { } pulled
            && TryComp<PullableComponent>(pulled, out var pulledComp))
            _pulling.TryStopPull(pulled, pulledComp, user);

        if (TryComp<PullableComponent>(user, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(user, pullable);

        if (_container.TryGetContainingContainer(user, out var container))
            _container.Remove(user, container, force: true);

        if (_limboMap == null || TerminatingOrDeleted(_limboMap.Value))
        {
            _limboMap = _map.CreateMap(out _);
            _metaData.SetEntityName(_limboMap.Value, "Extraction limbo");
            _map.SetPaused(_limboMap.Value, true);
        }

        _transform.SetCoordinates(user, new EntityCoordinates(_limboMap.Value, Vector2.Zero));
    }

    /// <summary>
    /// Sends the player of an extracted body back to a ghost, keeping the mind on the body.
    /// </summary>
    private void Reghost(EntityUid body)
    {
        if (TerminatingOrDeleted(body)
            || !_mind.TryGetMind(body, out var mindId, out var mind)
            || mind.IsVisitingEntity)
            return;

        if (_ghost.SpawnGhost((mindId, mind), null, canReturn: true) is { } ghost)
            _ghost.SetCanReturnToBody(ghost, false);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        foreach (var (name, minutes) in _extracted)
        {
            ev.AddLine(Loc.GetString("extraction-round-end", ("name", name), ("minutes", minutes)));
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _limboMap = null;
        _reghost.Clear();
        _extracted.Clear();
    }

    #endregion

    private void Notify(Entity<AntagExtractableComponent> ent, string message)
    {
        _popup.PopupEntity(Loc.GetString("extraction-notify-popup"), ent.Owner, ent.Owner, PopupType.Medium);

        if (TryComp<ActorComponent>(ent, out var actor))
            _chatManager.DispatchServerMessage(actor.PlayerSession, message);
    }
}
