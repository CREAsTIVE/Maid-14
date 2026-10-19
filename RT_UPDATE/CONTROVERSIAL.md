# Controversial / needs-decision items (RT 266 -> 293.0.1)

Status legend: OPEN = needs your decision, NOTE = done but please be aware.

## Build / tooling
- NOTE **C# 14 everywhere** (`MSBuild/Content.props`, copied from Goob). Was `LangVersion 12`. Needed because RT generated code
  needs it (first-class span `Contains` etc). Possible subtle overload-resolution changes in hand-written code.
- NOTE **Package bumps** (mirrors Goob): EF Core Design/Sqlite.Core 9.0.1 -> 10.0.0, Npgsql.EFCore 9.0.3 -> 10.0.0, OpenTK 4.7.2 -> 4.9.4, CsvHelper 33.1.0.
  EF/Npgsql 10 touches the production DB layer (no migration changes made). Needs a DB smoke test on Postgres + SQLite.
- NOTE NuGet audit warnings (NU1902/NU1903) on `Content.Server.Database` transitive System.Security.Cryptography.* 9.0.0.
- NOTE `RA0049`/`RA0051` (`[Dependency]` fields should be `partial`/non-readonly) are suppressed in `Content.props` — RT says
  they will become errors later. Hundreds of sites.
- NOTE Pow3r restore shows OpenTK NU1608 warnings (tool project, not shipped).

## Silent behaviour changes in RT (please review / test)
- OPEN **XAML `StyleClasses="..."` now *replaces* all classes** (RT 285). Stock `Button` is compat-patched, but custom controls that add
  classes in their constructors and are then given `StyleClasses=` in XAML lose them. Needs a UI visual pass.
- OPEN **Transform lerping** was reworked in 292 and reverted in 293 — net effect none, but any Maid/Goob code touching
  `RenderTransform`/`GetRender*` (none found) would have been affected.
- OPEN **Partial prototypes / `!CombineIndex`** (289): YAML that modifies existing list elements in prototypes using the old
  partial syntax must be rewritten. Not scanned in YAML yet.
- OPEN **`PredictedQueueDel` / spawn changes**, `StartCollide/EndCollide` buffered until end of substeps (268): physics-event
  timing changed; gameplay relying on same-tick collide events should be playtested.
- OPEN **ReallyBeIdle tick count 25 -> 5**, Box2/UIBox2i input validation (negative sizes now throw) — could surface as runtime exceptions.
- OPEN **Server now serializes PVS asynchronously / EntityPrototype component interning** — memory/perf only, but test under load.
- OPEN **`SpawnAtPosition(EntityCoordinates)` uses the attached entity's rotation** (279) — spawned entities may be rotated differently.
- OPEN Replay system changed (288, lazy loading) — replays recorded on old versions are not guaranteed to load.

## Code decisions made by me that may deserve a second look
- NOTE **`string` prototype-id fields became `ProtoId<T>`/`EntProtoId`** (dictionary keys, hash sets, lists too). Matches Goob.
  Consumers that did `Dictionary<string,...>` lookups were adapted via implicit conversions / `.Id`.
- NOTE `[ValidatePrototypeId<T>] const string X` -> `static readonly ProtoId<T> X` (no longer `const`). Matches Goob. Anything needing a compile-time const had to change.
- NOTE `IWeightedRandomPrototype<T>` generic interface + `Pick<T>` added (copied from Goob); `WeightedRandom{Ore,Species,Entity}Prototype` now use `ProtoId<T>` keys.
- NOTE `MergeComponentsExtensions` (Maid) used `ComponentRegistryEntry.Mapping`, which RT removed. Now re-serialises the registry component
  (`WriteValue`), so fields equal to the type default are **not** applied over the existing component (old: only YAML-specified fields). OPEN: acceptable?
- NOTE `HitscanBatteryAmmoProviderComponent.Prototype` name lookup in `SelectableAmmoSystem` keeps old behaviour (looks the *hitscan id* up as an *entity prototype*). Probably a pre-existing bug.
- NOTE `OccluderComponent.BoundingBox` removed -> `LocalBounds` (occluders can be convex polygons now; examine-occlusion check uses bounds only).
- NOTE Primary-constructor DoAfterEvents (`CarveRune`, `SmHalberdExecute`, `TeslaBlast`) rewritten with normal ctors (serialization generator requirement).
- NOTE Prototype/`IdDataField` properties `{ get; }`/`private init` got `private set` (new serializer generator needs a setter).
- NOTE Event classes with primary constructors: `[DataField]` removed from them (Goob does the same); `[DataDefinition]`/`partial` added where RT analyzers require.
