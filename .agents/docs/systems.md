# Systems

A **system** owns the behavior for one concern. Entities supply data through components (see `component.md`); systems supply the rules. There is normally exactly one instance of each system, and it serves every entity that carries its components.

## Role of a system

- Reacts to **events** about entities and updates component data.
- Optionally runs **periodically** (every frame/tick) over all entities with a given set of components, for continuous processes like decay, charging, or timers.
- Exposes a small **public API** for other systems that need to perform an operation consistently (e.g. "lock this", "deal this damage") rather than editing component data themselves.
- Keeps its own invariants: it is the single place allowed to mutate its components in meaningful ways.

## How systems react

- **Subscriptions.** A system declares which event types it cares about and for which component types. The event is delivered only when the target entity has the component. Hence adding a component to an entity "switches on" the system's behavior for it.
- **Ordering.** When several systems handle the same event, they can request to run before or after one another. This matters for attempts, modifiers (damage reductions before damage is applied) and cleanup.
- **Update loops.** Periodic work iterates over entities with the relevant components, not over all entities. Systems usually do timing checks against current game time instead of counting ticks.
- **Queries.** Systems ask the world for entities matching a component set, or for entities near a position/area.

## Sides: Shared, Server, Client

Systems come in the same three flavors as components:

- **Shared systems** hold rules both sides must agree on, enabling client prediction. Where behavior differs by side, the shared system is declared abstractly and each side provides its own concrete part.
- **Server systems** add authority: spawning, persistence, anything with hidden information, anything abusable if the client decided it.
- **Client systems** handle presentation: sprite changes via visualizers, UI, overlays, audio cues, input.

A feature commonly has a shared system for the core rules, plus a thin client part for visuals and a thin server part for authority.

## Cross-system collaboration patterns

- **Attempt events** let systems veto or modify an action without the actor knowing about them.
- **Relays** pass an event from one entity to another (clothing → wearer, held tool → user) so equipment can contribute effects.
- **Containers as plumbing.** Many systems care about "what is inside what"; inventory, storage, machines and bodies all use the same container notion.
- **Interactions pipeline.** Player actions funnel into a common interaction flow that raises a sequence of events (use item on target, activate target, alt/verb actions), and feature systems subscribe to the stage they care about.
- **Do-afters.** Actions that take time are modeled as a timed, cancellable process with start, progress and completion events; the acting system handles completion.
- **Verbs.** Context-menu actions are collected by raising an event that every interested system can add to, then executed on selection.

## Map of the most commonly used systems

These form the backbone that most other features lean on. They are grouped by concern.

### Entity fundamentals (engine)
- **Metadata / naming** — names, descriptions, lifecycle flags, pause state.
- **Transform** — parent/child relationships, position, rotation; moving and re-parenting entities.
- **Map / grid** — worlds, tile storage, anchoring to grids.
- **Lookup** — spatial queries, "what is in range".
- **Physics** — collision, contacts, velocity, triggers.
- **Container** — "inside" relationships used everywhere.
- **Appearance** — the abstract key–value bridge between logic and visuals.
- **Sprite / animation / lights / audio / eye (camera)** — client presentation primitives.
- **User interface** — opening, closing and syncing windows tied to entities.
- **Prototype / localization / resource** — data loading and text.

### Player and interaction
- **Interaction** — the central pipeline for use, activate, ranged and adjacent interactions, with range and obstruction checks.
- **Hands** — held items, active hand, swapping, dropping, throwing.
- **Inventory / clothing / item slots** — equipment slots, equip/unequip rules, size limits, slot-based effects.
- **Item** — what makes an entity pick-up-able; size and shape.
- **Verbs / actions** — context menu entries and ability buttons with cooldowns and targets.
- **Do-after** — timed cancellable actions.
- **Examine** — the information shown when looking at something.
- **Popups** — brief in-world feedback messages.
- **Movement and input** — player input to motion, speed modifiers, stun/slow effects, mob state gating.
- **Pulling, buckling, climbing** — physical relations between entities.

### Storage and objects
- **Storage** — bags, boxes, lockers, and the logic of fitting and ordering items.
- **Stack** — counted merged items (sheets, cables, money).
- **Whitelist / tag** — generic "what is allowed" and classification filters used by many features.
- **Lock / access** — who may use or open something, based on identity credentials.
- **Wires / hacking / construction** — machine panels, assembly and deconstruction graphs, tools-with-qualities model (tools act through the capabilities they provide).
- **Item toggle and charges** — on/off devices, limited-use items.

### Mobs and survival
- **Mob state** — alive, critical, dead and what each allows.
- **Damage** — damage types and groups, modifiers by armor/species, thresholds that trigger effects (death, destruction, gibbing).
- **Body / organs / blood / metabolism** — the physiological model.
- **Status effects** — timed conditions (stun, drowsiness, slow) applied and expired centrally.
- **Combat and weapons** — melee, ranged, projectiles, throwing, combat mode.
- **Health feedback** — alerts and damage overlays driven by state.

### Environment and machines
- **Power** — generation, storage, distribution on cable networks and local batteries; powered/unpowered gating for machines.
- **Atmospherics** — gases, pipes, tile mixtures, temperature, pressure effects on entities.
- **Fluids and chemistry** — reagents, solutions in containers, reactions, spills and puddles.
- **Doors, airlocks, shutters** — state machines tied to power, access and atmos.
- **Light** — bulbs, handheld lights, light-related mechanics.
- **Sound** — playing audio, ambient sound, footsteps.
- **Explosions and fire** — area damage with tile propagation.

### Round and meta
- **Game ticking and game rules** — round lifecycle, modes, antagonist selection, events.
- **Station / job / spawn** — mapping players to roles, spawn points, ID cards and manifests.
- **Chat / radio / speech** — communication channels, languages, emotes, text processing.
- **Administration / ban / permissions** — moderation features.
- **Preferences / localization / TTS** — player settings and presentation.
- **Cargo / economy** — trading and funds (Maid adds its own economy and trade pieces).

## Maid-specific systems

Maid adds its own families in its isolated areas, among them: economy, mood, adaptive/dynamic game modes, ghost recruitment, TTS, radio extensions, trade shuttle console, and weapon extensions. They obey the same patterns above; where they need to alter an upstream system, they hook events rather than rewriting it whenever possible.

## Design guidance when reading or extending

- Find the system that *owns* a behavior; change it there, not in callers.
- Prefer a new event subscription over editing someone else's system.
- Keep rules in Shared when the player should feel instant feedback; keep authority and secrets on the Server.
- Put visuals on the Client, reached only through replicated state (appearance), never by logic directly touching sprites.
- Use prototypes to tune values and compose components before writing new code.
