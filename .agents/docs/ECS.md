# Entities, Components, Systems (ECS) — how it actually works here

Facts about the RobustToolbox ECS as used by Maid-14. Engine symbols are named because the engine barely changes. Content (Content.*) is described more loosely. Read `ARCHITECTURE.md` first for layers/sides.

Mental rule: **entity = id, component = data, system = behaviour, event = the only way systems talk to each other.**

---

## 1. Entities

### Identifiers
- `EntityUid` — a struct wrapping an `int Id`. Local to one process (server and client numbers for the same entity differ). `EntityUid.Invalid` is `0`; `Valid`/`IsValid()` means `Id > 0`. `EntityUid?` is used for "maybe no entity".
- `NetEntity` — the id that is identical on server and client. Anything that crosses the wire (events, component states, UI messages) carries `NetEntity`, never `EntityUid`. Convert with `GetNetEntity(uid)` / `GetEntity(netEntity)` (also `TryGetEntity`, and list/set/dictionary/`EntityCoordinates` variants: `GetNetCoordinates`, `GetCoordinates`, `GetEntityList`...). `NetEntity.ClientEntity` marks the range reserved for client-only entities. `IsClientSide(uid)` tells whether an entity is client-only.
- `Entity<T>` — a record struct `(EntityUid Owner, T Comp)`; also `Entity<T1,T2>` ... up to 4 components. Implicitly converts to `EntityUid` and to the component. `Entity<T?>` can be built from a bare `EntityUid` (comp resolved later). This is the preferred modern parameter shape for system methods and event handlers.
- Human readable form for logs: `ToPrettyString(uid)`; `Name(uid)` gives the display name; `Prototype(uid)` the `EntityPrototype`.

### Existence / life stage
`MetaDataComponent` exists on every entity and holds name, description, prototype, `NetEntity`, pause state, `EntityLifeStage`.
`EntityLifeStage`: `PreInit → Initializing → Initialized → MapInitialized → Terminating → Deleted`.
Checks: `Exists(uid)`, `Deleted(uid)`, `Terminating(uid)`, `TerminatingOrDeleted(uid)`. Always use `TerminatingOrDeleted` before touching an entity you got from a stored reference/event that may be stale. Null-safe overloads take `EntityUid?`.

### Creation
- `Spawn(protoId, EntityCoordinates | MapCoordinates)`, `Spawn(protoId)` (nullspace), `SpawnAtPosition`, `SpawnAttachedTo`, `SpawnNextToOrDrop(proto, target)`, `SpawnInContainerOrDrop(proto, containerOwner, containerId)`, `TrySpawnNextTo`, `TrySpawnInContainer`. `ComponentRegistry? overrides` lets the caller override component data at spawn.
- `PredictedSpawn*` variants exist for client prediction (client spawns a predicted entity that the server later reconciles).
- Proto ids are strings; typed wrappers are `EntProtoId` (an entity prototype id) and `ProtoId<T>` (any prototype kind). Use them in `[DataField]`s so YAML ids are validated.
- Creation sequence (`InitializeAndStartEntity`): components are added → `ComponentAdd` for each → `ComponentInit` for each → entity stage `Initialized` → `ComponentStartup` for each → if the world (map) is already map-initialised, `MapInitEvent` is raised → stage `MapInitialized`. Maps that have not been map-initialised (e.g. while mapping) keep entities at `Initialized`; `MapInitEvent` fires later when the map starts.

### Deletion
- `Del(uid)` deletes immediately. `QueueDel(uid)` defers to the end of the current tick (after all systems and queued events have run) — **use `QueueDel` by default** inside event handlers/updates; immediate deletion invalidates iterators and other handlers still holding the entity. `TryQueueDel` returns whether it queued.
- On the client-predicted side use `PredictedDel` / `PredictedQueueDel`.
- Order on delete: `EntityTerminatingEvent` raised on the entity and **recursively on all transform children first** (entities become `Terminating`), then the entity is detached from its parent, children are deleted recursively, then each component gets `ComponentShutdown` then `ComponentRemove`, stage `Deleted`. Deleting a parent deletes everything parented to it (contents of containers, held items, worn items, attached parts).
- Components removed with `RemComp` go through `ComponentShutdown` + `ComponentRemove` immediately; `RemCompDeferred` delays until end of tick.

### Hierarchy (TransformComponent)
Every entity has `TransformComponent` (exposed as `Transform(uid)` or `TryComp`). Position/rotation are **relative to the parent**; world values are derived. Key members: `ParentUid`, `Coordinates` (an `EntityCoordinates` = parent + local offset), `MapPosition`/`MapID`, `GridUid`, `MapUid`, `Anchored`, `LocalPosition`, `LocalRotation`, `WorldPosition`, `WorldRotation`, `ChildEnumerator`/`ChildCount`, `NoLocalRotation`.
Everything that moves/reparents goes through `SharedTransformSystem`: `SetCoordinates`, `SetParent`, `SetWorldPosition`, `SetLocalPositionRotation`, `AttachToGridOrMap`, `DropNextTo`, `PlaceNextTo`, `AnchorEntity`/`Unanchor`, `GetWorldPosition`, `GetMapCoordinates`, `InRange`, `ContainsEntity`, `GetGrid`, `GetMap`. Never write the component's fields directly.
Events: `MoveEvent` (position/rotation changed, has old/new), `EntParentChangedMessage` (reparent), `AnchorStateChangedEvent`.
Maps are entities with a map component; grids are entities with a grid component parented to a map; stations/shuttles are grids. Nullspace = no parent.

### Containers (the "inside" relation)
`ContainerManagerComponent` on the owner holds named `BaseContainer`s (`Container` = many, `ContainerSlot` = one). An entity in a container is still a transform child of the owner but has no world presence (no physics, not rendered, not found by spatial lookups). **Hands, inventory slots, storage, item slots (e.g. a gun's magazine), machine parts, bodies' organs, status-effects all are containers.**
`SharedContainerSystem`: `EnsureContainer<T>(owner, id)`, `Insert`, `Remove`, `CanInsert`, `TryGetContainer`, `TryGetContainingContainer`, `IsEntityInContainer`, `IsEntityOrParentInContainer`, `GetAllContainers`, `EmptyContainer`, `CleanContainer`, `TryFindComponentOnEntityContainerOrParent`.
Events: *attempt* (cancellable) `ContainerIsInsertingAttemptEvent` (on container owner), `ContainerGettingInsertedAttemptEvent` (on the item), `ContainerIsRemovingAttemptEvent`, `ContainerGettingRemovedAttemptEvent`; *done* `EntInsertedIntoContainerMessage`/`EntGotInsertedIntoContainerMessage` (owner / item side), `EntRemovedFromContainerMessage`/`EntGotRemovedFromContainerMessage`. "Inserted" vs "GotInserted" = same occurrence, raised on owner vs on the item.

### Lookup / queries on the world
`EntityLookupSystem` answers "which entities are near / intersect this area / are in this range" (with `LookupFlags`); physics has its own queries. For "all entities with component X" use component queries below, not the lookup.

---

## 2. Components

### Declaration (the attribute vocabulary)
```
[RegisterComponent]            // makes the type a real component (required)
[NetworkedComponent]           // state is replicated server -> client
[AutoGenerateComponentState]   // generate GetState/HandleState from [AutoNetworkedField]s
[AutoGenerateComponentPause]   // generate unpause handling for [AutoPausedField] TimeSpans
[Access(typeof(SomeSystem))]   // only listed types may *write* it (analyzer-enforced)
public sealed partial class FooComponent : Component
```
- Class name **must end in `Component`**; the YAML name is the class name minus that suffix (`type: Foo`). `[ComponentProtoName("X")]` overrides.
- Classes must be `partial` when source-generators are used (they nearly always are) and are normally `sealed`.
- Components are **classes**, one instance per type per entity (`AddComp<T>(uid)` on an entity that already has it is an error unless `overwrite: true`; `EnsureComp` is idempotent).
- Fields:
  - `[DataField]` — loaded from YAML/prototype (YAML key = member name in camelCase unless a name is given; `required: true`, `readOnly: true`, `customTypeSerializer:` options exist). Fields **without** `[DataField]` are runtime-only.
  - `[AutoNetworkedField]` — replicated by the generated state (needs `[AutoGenerateComponentState]`). `EntityUid`, `EntityUid?`, `EntityCoordinates`, and `List/HashSet/Dictionary` of them are converted to `NetEntity` automatically. `[AutoGenerateComponentState(fieldDeltas: true)]` sends only changed fields (then dirty with `DirtyField`).
  - `[AutoPausedField]` — a `TimeSpan` (or nullable) that is shifted forward when the entity is unpaused, so "next fire time" fields don't expire while paused.
  - `[ViewVariables(VVAccess.ReadWrite)]` — shows/edits in the in-game VV tool.
- `[Access(...)]` defaults: owner type and friends = read/write, everyone else = read-only. Violations are compile-time analyzer errors; to change a protected component from elsewhere, call the owning system's API.
- Any file with `sealed partial class FooComponent` can add `: Component` plus extra interfaces; `ComponentState` classes (hand-written networking) are `[Serializable, NetSerializable]`.

### Lifecycle events (raised on the entity, subscribed per component)
Per component, in order: `ComponentAdd` → `ComponentInit` → `ComponentStartup` → (`MapInitEvent` if the entity/map is map-initialised) → … → `ComponentShutdown` → `ComponentRemove`.
- `ComponentInit`: component and its siblings exist; set up own state, don't assume other systems' reactions.
- `ComponentStartup`: all components of the entity are initialised; safe to read siblings. Most "setup" logic goes here.
- `MapInitEvent`: once-per-world-start work (rolling random values, starting timers, spawning contents). Not re-run when a saved map reloads.
- `ComponentShutdown`: clean up anything set elsewhere (containers, other entities, UI, effects). Also fires when the entity is deleted.
- Adding a component to an entity that is already running immediately runs add→init→startup(→mapinit) for that component; removing runs shutdown→remove.
- `ComponentLifeStage` values (`Adding … Running … Stopped … Deleted`) are available as `comp.LifeStage`, `comp.Running`, `comp.Deleted`, `comp.Initialized`.
- Entity pause: `EntityPausedEvent` / `EntityUnpausedEvent(PausedTime)`.

### Accessing components (all on `EntitySystem`, mirror on `EntityManager`)
| Need | Call |
|---|---|
| get or fail | `Comp<T>(uid)` (throws if absent), `CompOrNull<T>(uid)` |
| get safely | `TryComp(uid, out T? comp)` (also accepts `EntityUid?`) |
| presence | `HasComp<T>(uid)` |
| add / ensure / remove | `AddComp<T>(uid)`, `EnsureComp<T>(uid)` / `EnsureComp(uid, out comp)`, `RemComp<T>(uid)`, `RemCompDeferred<T>(uid)` |
| enumerate | `AllComps(uid)` |
| copy | `CopyComp`, `CopyComps`, `TryCopyComponent` |
| resolve optional param | `Resolve(uid, ref comp)` — fills `comp` if null; logs an error if missing unless `logMissing: false`. Public system APIs take `T? comp = null` + `Resolve` so callers may pass what they already have. |

`Dirty(uid, comp)` / `Dirty(ent)` — mark changed for replication. `DirtyField(uid, comp, nameof(Comp.Field))`/`DirtyFields` for delta components. `DirtyEntity(uid)` for entity-level data (transform/meta are handled by their systems).

### Iterating many entities
- `EntityQueryEnumerator<A,B,...>()` — `while (q.MoveNext(out var uid, out var a, out var b))`. **Skips paused entities.** Used in `Update`.
- `AllEntityQuery<A,B>()` — same shape but includes paused entities.
- `GetEntityQuery<T>()` → cached `EntityQuery<T>` struct (`TryComp`, `HasComp`, `Comp`, `Resolve`, `Get`). Fetch once in `Initialize` into a `_fooQuery` field for hot paths.
- `EntityQuery<T>()` (IEnumerable) exists but is slower/older.

### Networking and prediction of component data
1. Server mutates data and calls `Dirty` → component gets a new `LastModifiedTick`.
2. The PVS/game-state system sends each client the changed state of components on entities visible to that client (PVS range). Visibility can be limited with visibility masks/eye settings. Only components that are `[NetworkedComponent]` participate. `SessionSpecific` / `SendOnlyToOwner` components send per-player variants.
3. Server builds the state with `ComponentGetState` (raised by ref on the component; handler sets `args.State`; generated automatically with `[AutoGenerateComponentState]`). Client applies it with `ComponentHandleState` (`args.Current`/`args.Next`). If `[AutoGenerateComponentState(true)]` the generated handler also raises `AfterAutoHandleStateEvent` after applying — subscribe to it to refresh derived/cached data or visuals when replicated fields change.
4. The client **predicts**: shared system code runs on the client on the player's own input. Server states overwrite predicted data; the client rewinds, applies the new state, and re-runs unconfirmed inputs.
5. Prediction helpers (`IGameTiming`): `IsFirstTimePredicted` (guard sounds/popups/spawns so they don't repeat on every re-prediction), `InPrediction`, `ApplyingState` (true while applying a server state — don't raise side effects then). Client-only side effects typically check `_timing.IsFirstTimePredicted`; server-only effects check `_net.IsServer` (`INetManager`).
6. Client → server input that must be validated and predicted is sent with `RaisePredictiveEvent(ev)` (raised locally every re-prediction *and* sent to the server).
7. Fields that must not be trusted/leaked: keep them in server-only components (`Content.Server`), never `[AutoNetworkedField]`.

---

## 3. Systems

### Shape
```
public sealed class FooSystem : EntitySystem   // or Shared base + Server/Client subclass
{
    [Dependency] private readonly BarSystem _bar = default!;   // injected before Initialize
    public override void Initialize() { SubscribeLocalEvent<FooComponent, SomeEvent>(OnSome); }
    private void OnSome(Entity<FooComponent> ent, ref SomeEvent args) { ... }
    public override void Update(float frameTime) { ... }   // every tick (server) / predicted tick (client)
}
```
- Exactly one instance of each system per side. Discovered by reflection; no registration needed. `[Dependency]` also injects managers (`IGameTiming`, `IPrototypeManager`, `INetManager`, `IRobustRandom`, `ILocalizationManager`...).
- `Update(frameTime)` runs per simulation tick, and on the client also during prediction. It is **skipped on the client when prediction is disabled** unless the system sets `UpdatesOutsidePrediction = true` (client-only/visual systems that must always tick set it). `FrameUpdate` runs per rendered frame (client-visual only). Ordering between systems: `UpdatesAfter`/`UpdatesBefore` lists, or `before:`/`after:` arguments on `SubscribeLocalEvent` for handler order.
- Convention for sides: `SharedFooSystem` (abstract or full) in `Content.Shared`; `FooSystem` in `Content.Server` and/or `Content.Client` inherit/extend it. A system with no `Shared` prefix in `Content.Shared` runs on both sides unchanged. Shared system code must never reference server-only or client-only types.
- Systems own their component data: other code calls the system's public API. `[Access(typeof(FooSystem))]` on the component enforces it.

### Events
- Subscription forms:
  - `SubscribeLocalEvent<TComp, TEvent>(handler)` — **directed**: runs only when the event is raised on an entity that has `TComp`. Handler `(Entity<TComp> ent, ref TEvent args)` for `[ByRefEvent]` events, or `(EntityUid uid, TComp comp, TEvent args)` for class events. Several systems/components may subscribe to the same event on the same entity; order via `before:/after:` types.
  - `SubscribeLocalEvent<TEvent>(handler)` — **broadcast**: runs for every raise.
  - `SubscribeNetworkEvent<T>((ev, EntitySessionEventArgs args) => ...)` — receive a networked event (`args.SenderSession` = who sent it on the server). `SubscribeAllEvent<T>` = local + network.
- Raising: `RaiseLocalEvent(uid, ev)` (directed only), `RaiseLocalEvent(uid, ev, broadcast: true)` (directed **and** broadcast subscribers), `RaiseLocalEvent(uid, ref ev)` for by-ref, `RaiseLocalEvent(ev)` broadcast only, `QueueLocalEvent` (deferred to end of the system updates), `RaiseNetworkEvent(ev)` / `(ev, session|filter|uid)`.
- Event kinds: classes deriving `EntityEventArgs`; `HandledEntityEventArgs` (`Handled` flag — first handler to set it stops "who acts"); `CancellableEntityEventArgs` (`Cancel()`; the *attempt* pattern); or `[ByRefEvent] record struct` (preferred for new code; mutate fields through `ref`, return data by writing to the struct). Networked events must be `[Serializable, NetSerializable]`.
- **Attempt/query pattern** (used everywhere): raise `XAttemptEvent`; any interested component cancels or modifies; caller checks the result. **Modifier events** (`DamageModifyEvent`, `RefreshMovementSpeedModifiersEvent`...) are raised, handlers contribute, the raiser reads the aggregate. **Done events** announce the result after the fact.
- **Relay**: an entity forwards an event to related entities (worn items, held items, a body's parts). E.g. inventory relays chosen events to every equipped item matching `IInventoryRelayEvent.TargetSlots`, wrapped as `InventoryRelayedEvent<T>`; hands do the same with `HeldRelayedEvent<T>`. `SubscribeWithRelay` registers a handler for the direct event plus the inventory/held relay versions.

### Time in systems
`_timing.CurTime` (a `TimeSpan`, pause-aware, predicted) is the clock for cooldowns/timers; store "next allowed time" as `TimeSpan` fields (with `[AutoPausedField]`) instead of counting ticks. `IGameTiming.CurTick` is the network tick.

### Localization & prototypes from systems
`Loc.GetString("fluent-key", ("var", value))` for text (Fluent `.ftl` files in `Resources/Locale/<lang>/`). `IPrototypeManager`: `Index<T>(id)`, `TryIndex`, `EnumeratePrototypes<T>()`.

---

---

## 4. Content systems

The index of concrete content systems (Interaction, Hands, Inventory, Damage, DoAfter, ...) with their key facts lives in `systems.md`. Detailed per-system docs go in `systems/<system>.md`.
