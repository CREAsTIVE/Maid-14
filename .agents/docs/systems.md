# Systems index

Index of content systems: what each owns and the facts needed to talk to it. Engine-level ECS mechanics (events, components, networking) are in `ECS.md`.

**Layout:** this file is the index and quick reference. Each system gets (or will get) a detailed doc at `systems/<system>.md`; when one exists, prefer it and keep the entry here short with a pointer. Content is large and forked (Goob + others); fork-specific edits show up as `// Goobstation`, `// WD EDIT`, `// Shitmed`, `<Trauma>`, `// MAID` markers. Systems live in `Content.Shared/<Area>` unless noted.

Content is large and forked (Goob + others); here are the load-bearing ones with the facts you'd otherwise grep for. Each lives in `Content.Shared/<Area>` unless noted. Fork-specific edits show up as `// Goobstation`, `// WD EDIT`, `// Shitmed`, `<Trauma>`, `// MAID` markers.

## Interaction (`SharedInteractionSystem`)
The funnel for clicks. Input binding `Use` → `HandleUseInteraction`, alt-use → `HandleAltUseInteraction`, both → `UserInteraction(user, coords, target, altInteract, ...)`.
`UserInteraction` flow: relay redirect (`InteractionRelayComponent`, `TargetInteractionRelayComponent`) → combat-mode check → face the click → if alt: run the first `AlternativeVerb` → `ActionBlocker.CanInteract` → `IsAccessible` (same container/adjacent storage) → range/obstruction (`InRangeUnobstructed`) → decide:
- empty hand: `BeforeInteractHandEvent` (on user) → `InteractHandEvent` (on target, broadcast).
- clicked the held item: `UseInHand` → `UseInHandAttemptEvent` then `UseInHandEvent` (on item).
- item on target in reach: `BeforeRangedInteractEvent` (on item) → `InteractUsingEvent` (on target, broadcast, `Handled` stops) → `AfterInteractEvent` (on item) → `AfterInteractUsingEvent` (on target).
- out of reach: ranged variants of before/after only (`canReach: false`) — used by guns/tools that act at range.
- `ActivateInWorldEvent` (on used/target) and `UserActivateInWorldEvent` (on user) for "activate" key/`InteractionActivate`.
Handlers set `args.Handled = true` to consume the click. `DoContactInteraction` marks physical contact (forensics). Range/accessibility can be overridden with `InRangeOverrideEvent`/`AccessibleOverrideEvent`.

## ActionBlocker (`ActionBlockerSystem`)
Central permission queries: `CanMove`, `CanInteract(user, target)`, `CanUseHeldEntity`, `CanSpeak`, `CanEmote`, `CanAttack`, `CanDrop`, `CanPickup`, `CanThrow`, `CanChangeDirection`, `CanConsciouslyPerformAction`... Each is implemented by raising an `*AttemptEvent` that components cancel (stuns, mob state, restraints, pacifism...). To forbid an action for an entity, subscribe to the matching attempt event and `Cancel()`.

## Hands (`SharedHandsSystem`) and Inventory (`InventorySystem`)
- Hands: `HandsComponent.Hands` is a dictionary `handId → Hand` (each hand is a `ContainerSlot` named by its id), `ActiveHandId`, `SortedHands`. API: `TryPickup`/`TryPickupAnyHand`/`TryForcePickup`, `TryDrop`, `TryGetActiveItem`, `GetActiveItem`, `TryGetHeldItem`, `EnumerateHeld`, `GetEmptyHandCount`, `IsHolding`, `SwapHands`, `SetActiveHand`, `PickupOrDrop`. Events: `DropAttemptEvent`, `PickupAttemptEvent`, `GotEquippedHandEvent`/`GotUnequippedHandEvent` (on item), `DidEquipHandEvent`/`DidUnequipHandEvent` (on holder), `HandCountChangedEvent`. Virtual items (`VirtualItem`) occupy a hand to represent two-handed holds/pulls.
- Inventory: `InventoryComponent` + a template (`InventoryTemplatePrototype`) listing `SlotDefinition`s (name, `SlotFlags`, UI position, strip time, whitelist/blacklist, dependency on another slot). Each slot is a container named after the slot (`jumpsuit`, `outerClothing`, `head`, `pocket1`, `back`, `belt`, `id`, `suitstorage`, `socks`/`underweart`/`underwearb` are Maid additions). `SlotFlags` is a flag enum (HEAD, EYES, EARS, MASK, OUTERCLOTHING, INNERCLOTHING, NECK, BACK, BELT, GLOVES, IDCARD, POCKET, LEGS, FEET, SUITSTORAGE + Maid's UNDERWEART/UNDERWEARB/SOCKS).
- Equip events: attempts `BeingEquippedAttemptEvent` (item), `IsEquippingAttemptEvent` (actor), `IsEquippingTargetAttemptEvent` (wearer) + the unequip mirrors; done events `GotEquippedEvent` (item) / `DidEquipEvent` (wearer) and unequip mirrors. `ClothingComponent` declares which `SlotFlags` an item fits.
- Relaying: equipped items get wearer events through `InventoryRelayedEvent<T>`, which is how armor (`DamageModifyEvent`), shoes (`SlipAttemptEvent`), goggles (`CanSeeAttemptEvent`), speed (`RefreshMovementSpeedModifiersEvent`) etc. affect the wearer.

## Item (`SharedItemSystem`) and ItemSlots (`ItemSlotsSystem`)
- `ItemComponent`: `Size` (`ItemSizePrototype` id), optional grid `Shape` for grid storage, `HeldPrefix` (changes in-hand RSI state name), `RsiPath` override. Without `ItemComponent` an entity can't be picked up.
- `ItemSlotsComponent`: named slots on a machine/weapon/etc. (`Slots` dict id→`ItemSlot` with whitelist, swap, locked, sounds). API: `AddItemSlot`, `TryInsert`, `TryInsertFromHand`, `TryEject`, `TryEjectToHands`, `GetItemOrNull`, `SetLock`. Backed by containers.

## Storage (`SharedStorageSystem`, `SharedEntityStorageSystem`)
- Bags/boxes: `StorageComponent` — container id `storagebase`, grid-based (`StoredItems` maps entity → `ItemStorageLocation` (cell + rotation)), `MaxItemSize`, `Whitelist`/`Blacklist`, `QuickInsert`, `AreaInsert`, open-on-activate. API: `CanInsert`, `Insert`, `PlayerInsertHeldEntity`, `TryGetAvailableGridSpace`, `OpenStorageUI`, `UpdateAppearance`.
- Lockers/crates: `EntityStorageComponent` (+ `SharedEntityStorageSystem`) — open/close state; contents are in a container and are marked `InsideEntityStorageComponent`.

## Verbs (`SharedVerbSystem`)
Context-menu actions. Opening the menu raises `GetVerbsEvent<TVerb>` on the target (once per verb kind): `InteractionVerb`, `AlternativeVerb` (alt-click), `ActivationVerb`, `ExamineVerb`, `UtilityVerb`, `InnateVerb`, `EquipmentVerb`. A system adds a verb (`args.Verbs.Add(new AlternativeVerb { Text, Icon, Act = () => ... })`) in a handler of `GetVerbsEvent<...>` using `args.CanAccess`/`args.CanInteract`/`args.User`. `ExecuteVerb` runs `Act` (server-authoritative; client sends `ExecuteVerbEvent`).

## DoAfter (`SharedDoAfterSystem`)
Timed, cancellable actions with a progress bar. Build `new DoAfterArgs(EntityManager, user, delay, ev, eventTarget, target, used)` setting break conditions (`BreakOnMove`, `BreakOnDamage`, `NeedHand`, `BreakOnHandChange`, `DistanceThreshold`, `RequireCanInteract`, `BlockDuplicate`...), then `TryStartDoAfter(args)`. When done or cancelled the engine raises your `DoAfterEvent` subclass (`[Serializable, NetSerializable]` partial class, or `SimpleDoAfterEvent` for stateless ones) on `EventTarget` (set `Broadcast` to also broadcast). In the handler check `args.Cancelled`, `args.Handled`, then do the work and set `args.Handled = true`. `args.User/Target/Used` are available. `args.Repeat` repeats. Manage with `Cancel(id)`, `IsRunning(id)`, `GetStatus(id)`.

## Tools (`SharedToolSystem`)
`ToolComponent.Qualities` (set of `ToolQualityPrototype` ids: Prying, Screwing, Cutting, Welding...). `UseTool(tool, user, target, delay, qualitiesNeeded, doAfterEv, fuel)` checks the tool has the quality (and welder fuel), plays the sound, starts a DoAfter whose event is your own. Machines never check item identity, only qualities.

## Whitelist & Tag
`EntityWhitelist` (YAML-able filter: `components`, `tags`, `sizes`, `requireAll`) evaluated by `EntityWhitelistSystem` (`IsWhitelistPass`, `IsBlacklistFail`, `IsWhitelistPassOrNull`, ...). `TagComponent`/`TagSystem` hold `TagPrototype` ids on an entity (`HasTag`, `AddTag`, `RemoveTag`, `HasAnyTag`, `HasAllTags`). They are the generic "what is allowed / what kind of thing is this" mechanism.

## Damage (`DamageableSystem`)
- `DamageableComponent`: `Damage` (a `DamageSpecifier` = dictionary damage-type id → `FixedPoint2`), `DamageContainerID` (which damage types/groups exist), `DamageModifierSetId` (resistances, a `DamageModifierSetPrototype`), `TotalDamage`.
- Entry point: `TryChangeDamage(uid, DamageSpecifier, ignoreResistances, interruptsDoAfters, origin, targetPart, ...)` — positive = hurt, negative = heal. Flow: `BeforeDamageChangedEvent` (cancellable) → **if the target has a complex body (Shitmed `BodyComponent` with `BodyType.Complex`) damage is split/applied per body part** (each part is itself an entity with `DamageableComponent`, `TargetBodyPart` selects parts) → per entity: `DamageModifyEvent` (raised on target, also relayed through inventory/armor and, for parts, first on the parent body) applies resistances → damage applied (with wound integrity caps for woundables) → `DamageChangedEvent` raised (this is what mob state thresholds, alerts, visuals and `DoAfter` interruption listen to).
- Stamina is a separate pipeline (`SharedStaminaSystem`, `StaminaComponent`, `BeforeStaminaDamageEvent`).

## Mob state (`MobStateSystem`, `MobThresholdSystem`)
`MobStateComponent.CurrentState` ∈ `Alive | Critical | Dead` (`Invalid` = 0). `MobThresholdsComponent.Thresholds` maps total damage → state; `DamageChangedEvent` makes the threshold system recompute and call `UpdateMobState` (`UpdateMobStateEvent` lets others override). Result announced by `MobStateChangedEvent` (broadcast too). `MobStateSystem` subscribes to a large set of *attempt* events and cancels them for incapacitated mobs (use, attack, speak, emote, drop, pickup, throw, move, stand, pull...). Query with `IsAlive/IsCritical/IsDead/IsIncapacitated`. Visual key: `MobStateVisuals.State`.

## Status effects
Two coexisting systems: legacy `StatusEffectsComponent`/`StatusEffectsSystem` (dictionary of timed effect keys) and the newer `StatusEffectNew` (each effect is its own entity stored in a `status-effects` container on the target; API `TryAddStatusEffectDuration`, `TryRemoveStatusEffect`, `HasStatusEffect`, `TryGetTime`, `TryEffectsWithComp<T>`; effect components can relay events to the owner). Prefer the new one when the feature already exists there.

## Popups and Examine
- `SharedPopupSystem`: `PopupEntity(msg, uid, [recipient], type)` — server: shown to everyone in PVS (or one recipient); `PopupClient(msg, uid, recipient)` — client only, for predicted code; `PopupPredicted(msg, uid, recipient)` — client shows to the recipient, the server shows to others; also `PopupCursor`, `PopupCoordinates`. `PopupType`: `Small`, `SmallCaution`, `Medium`, `MediumCaution`, `Large`, `LargeCaution`. In predicted shared code use `PopupClient`/`PopupPredicted` (not `PopupEntity`).
- `ExamineSystemShared`: `ExaminedEvent` is raised on the examined entity; handlers add lines with `args.PushMarkup(text, priority)`/`PushText`, usually gated on `args.IsInDetailsRange`. `ExamineAttemptEvent` can block examination.

## User interfaces (engine `SharedUserInterfaceSystem` + content)
Entity UIs are defined by a `UserInterfaceComponent` in YAML: `interfaces: { enum.SomeUiKey.Key: { type: SomeBoundUserInterface } }` — a key (an enum, networked as a value) → client class name. Server/shared logic: `_ui.OpenUi(uid, key, user)`, `CloseUi`, `SetUiState(uid, key, state)` (state is a `BoundUserInterfaceState`, pushed to all open viewers), `ServerSendUiMessage`. Client: a `BoundUserInterface` subclass builds the window (XAML + `Control`s), receives `UpdateState`, sends `SendMessage(msg)`; server handlers `SubscribeLocalEvent<TComp, SomeMessage>` on `BoundUserInterfaceMessage` types. `ActivatableUIComponent` opens a UI when the entity is activated; requirements (power, lock, anchor) are separate components. UI localisation uses `Loc` keys in `.ftl`.

## Power (mostly Server; shared surface in `Content.Shared/Power`)
Power-net simulation lives in `Content.Server/Power` (cable `Node` groups → `PowerNet`/`ApcNet`, solver `Pow3r`, `BatteryComponent`, `PowerSupplier`/`PowerConsumer`, `ApcPowerReceiverComponent`, `ApcComponent`). A machine opts in with `ApcPowerReceiver` (`powerLoad`); its replicated `Powered` flag is read via `SharedPowerReceiverSystem.IsPowered(ent)`; changes raise `PowerChangedEvent(Powered, ReceivingPower)` on the machine. Gate behaviour by listening to that event or checking `IsPowered`; machines never inspect the net themselves. Other components (`ActivatableUIRequiresPower`, `ItemSlotRequiresPower`, `LitOnPowered`) are thin adapters on `Powered`.

## Doors
`DoorComponent` holds `DoorState` (`Closed, Closing, Open, Opening, Welded, Denying, Emagging`) and timing; `SharedDoorSystem` drives transitions and checks (access via access readers, power, bolts, welded). Appearance: `DoorVisuals.State` (+ helpers) drives the sprite; collision is toggled by state.

## Other recurring systems (names to search when needed)
Movement (`InputMover`, `MovementSpeedModifier`, `RefreshMovementSpeedModifiersEvent`), Standing/Stun/Knockdown, Pulling/Buckle/Climb, Throwing, Combat (`MeleeWeapon`, `Gun`, projectiles, `CombatMode`), Alerts (HUD icons), Actions (hotbar abilities as entities), Access (`AccessReader`, ID cards), Atmos (server-heavy gas simulation, `AtmosphereSystem`), Chemistry (`Solution`/`SolutionContainer`, reagents, reactions), Body/Organs (Shitmed), Construction (graph-based), Wires, Stack, Chat/Radio/Speech, GameTicking/GameRules/Antag, Station/Jobs/Spawning, Cargo/Economy (Maid has its own economy and trade shuttle console).

## Maid-owned systems (in `_Maid` folders across Shared/Server/Client)
Adaptive & dynamic game modes, Economy, Mood, TTS, Radio extensions, Ghost/ERT recruitment, AuthPanel, Chaplain, Trade shuttle console, Weapon extensions, Triggers, Terminator/HateEngine (server), SurveillanceCamera and more. They use the same patterns; hooks into upstream are marked `// MAID ...`.
