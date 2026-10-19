# Entities, Components and Systems (ECS)

Game objects in Maid-14 follow the Entity–Component–System (ECS) model. This document covers all three parts: **entities** and **components** (data) and **systems** (behavior).

In one sentence: an *entity* is an identity, *components* are the data hanging off it, and *systems* are the rules that act on entities according to the components they carry.

## Entities

An **entity** is nothing more than a unique identifier plus the set of components attached to it. It has no type, no class, no behavior, and no fields of its own beyond a few engine-provided basics (name, description, lifecycle state, a parent in the world hierarchy).

### What an entity is
- A door, a crowbar, a player character, a puddle, a spawn marker and a light bulb are all the same kind of thing — entities — and differ only in which components they have and what values those hold.
- "What is this?" is answered by looking at components, never by asking for a type. Code that needs "all doors" asks for "all entities that have the door component".

### Identity and references
- Every entity has a unique id for the lifetime of the round. Ids are not reused for different entities within a round.
- Other data refers to an entity by its id. Such a reference can become **stale** when the entity is deleted, so holders of references must tolerate "target no longer exists".
- Entities that exist on both server and client share the id mapping for networking; entities that exist on only one side (client-only visuals) are invisible to the other.
- A reference is not ownership. Deleting an entity does not delete whatever merely references it; cleanup is the responsibility of systems that care.

### Creation: prototypes
- Entities are created from **prototypes**: YAML templates naming the components an entity starts with and their field values. Spawning is "instantiate prototype P here".
- Prototypes **inherit** from parents. Abstract parents define reusable families (a base item, a base wall); concrete children override only what differs. Children can add, change, or remove inherited components.
- Entities can also be created with no prototype and have components added at runtime, but this is the exception.
- The prototype id an entity came from is remembered, which lets systems and tools say "what is this a copy of".

### Lifecycle
1. **Allocation** — the id exists, components are being attached.
2. **Initialization** — all components are attached and cross-component setup runs.
3. **Map initialization** — a one-time "turn it on" step when the surrounding world begins play. Entities loaded from a saved map that has already started skip it.
4. **Alive** — systems react to events and updates.
5. **Terminating / deleted** — components are shut down, children and contents are handled, and the id becomes invalid.

Entities in a **paused** world are initialized but do not tick.

### World hierarchy
- Every entity (except maps) sits under a parent: a map, a grid, or another entity.
- Position and rotation are stored **relative to the parent**. Moving a parent moves everything beneath it. A held item is a child of its holder, a crate's content is a child of the crate, a shuttle's contents are children of the shuttle grid.
- **Maps** are the roots; **grids** are movable tile structures (stations, shuttles) within a map; everything else is placed on a grid or directly on the map.
- **Containers** are a special parenting arrangement: an entity inside a container is not in the open world (not visible to spatial queries, not collidable), but is still a live entity. Inventory slots, hands, storage and machine internals all use containers.

### Entity categories
- **Real world entities**: part of the simulation, saved with maps and replicated.
- **Client-only entities**: spawned locally for visuals or UI; never seen by the server.
- **Pure data/virtual entities**: used as abstract holders (e.g. map/global data) without physical presence.

### Deletion and queueing
- Deleting is usually **queued** to the end of the current update so that systems currently processing it are not surprised.
- Systems that modify or delete entities while iterating must be careful: iteration is over a snapshot-like view and mutation of the set being walked is a classic source of bugs.
- Anything the deleted entity set up elsewhere (effects on others, subscriptions, temporary components) is cleaned in the shutdown stage of its components.

## Components

A **component** is a unit of data attached to an entity. Taken together, the components on an entity define what it is and what it can do.
A **component** is a unit of data attached to an entity. Entities have no behavior and no identity beyond what their components say. Taken together, the components on an entity define what it is and what it can do.

### What a component is (and is not)

- **Is:** a small, focused bundle of state describing one aspect — "can be worn", "has health", "is lockable", "emits light".
- **Is not:** a place for logic. Behavior belongs to systems (see the Systems section below). Components may carry trivial helpers, but the rule of thumb is "data here, rules there".
- At most **one component of each type** can exist on an entity. Behavior variations come from different component types, not from stacking duplicates.
- A component is *keyed by its type*; systems ask "does this entity have X?" rather than "what kind of entity is this?".

### Anatomy

A component declares a set of **fields**. Each field falls into one of a few roles:

- **Configuration** — values set once from the prototype and usually left alone (a cooldown length, a sound to play, a whitelist of allowed things).
- **Runtime state** — values that change as the game proceeds (is it locked, how much charge is left, when does the next action become available).
- **References** — links to other entities or to prototypes by id. Entity references must be treated as possibly stale: the target may have been deleted.

Fields that should be filled from YAML are explicitly declared as data fields. Fields that are only runtime scratch space are not.

### Lifecycle

Components move through well-defined stages, and systems can hook each one:

1. **Creation / add** — the component is attached, either at spawn from a prototype or later at runtime.
2. **Initialization / startup** — fields are populated and other components on the entity can be relied upon.
3. **Map initialization** — a one-time stage when an entity in a newly created world is "turned on" (for example, timers start, random rolls happen). Entities saved on a map skip this on reload.
4. **Active life** — systems read and write the data in response to events and updates.
5. **Shutdown / removal** — the component is detached; systems clean up anything they set up elsewhere (other entities, subscriptions, ongoing effects).

Entities may also be **paused** (e.g. on frozen maps), during which their components are not updated.

### Networking and prediction

Each component declares how it relates to the network:

- **Networked** components have their data replicated from server to client. Only the fields marked for replication are sent; the rest remain local to whichever side owns them.
- **Not networked** components exist on one side only. Server-only components hold hidden or authoritative bookkeeping; client-only components hold presentation state.
- Components that matter for **prediction** must live in Shared and be networked, so the client sees the same data it needs to simulate the player's action.
- After changing replicated data, the owning system must explicitly flag the component as changed, otherwise clients will not receive the update.
- Some components ship per-player variants of their state, so that different observers see different data.

The practical split is: *gameplay-relevant and predictable → shared and networked; secrets → server only; looks → client only.*

### Access discipline

Components can restrict which systems are allowed to modify their data. Typically a component is mutated only by "its" system, and other systems interact by raising events or calling that system's API. This keeps invariants (for example, "locked and open cannot both be true") in one place.

### Composition patterns

- **Marker components** carry no or almost no data; their presence alone is a flag ("this thing is a bureaucratic item", "this mob is a ghost"). Systems use them to include or exclude entities.
- **Capability components** add one ability to otherwise unrelated entities (can be pulled, can be buckled to, can hold a reagent).
- **Relay components** forward events from one entity to another (e.g. from worn clothing to the wearer) so effects can be attached to equipment.
- **Required-component relationships**: some components depend on others and ensure they are present. Designers can then add one component and get a consistent set.
- **Component-as-state**: temporarily adding a component can represent a transient condition (e.g. a status effect), with a system that removes it when its time is up.

### Components and prototypes

Prototypes are where components get concrete values. A prototype lists the components, with per-field overrides, that an entity of that kind should start with. Inheritance allows abstract bases ("a generic pill") and specific entities that only override what differs. Components can also be removed or replaced in a child prototype.

Because content is largely authored this way, a change in a component's fields is a change in the *data contract* between code and YAML: renaming or removing a field breaks every prototype that uses it, and adding a field with a sensible default is safe.

### Where components live

Components are organized by feature area and split by side (Shared / Server / Client) just like systems. A feature's data type is usually in Shared, with extra server-only or client-only components added only when needed. Maid-specific components live in Maid-owned areas; upstream components are left alone unless a change is unavoidable.

## Systems

A **system** owns the behavior for one concern. Entities supply data through components; systems supply the rules. There is normally exactly one instance of each system, and it serves every entity that carries its components.

A **system** owns the behavior for one concern. Entities supply data through components (see `ECS.md`); systems supply the rules. There is normally exactly one instance of each system, and it serves every entity that carries its components.

### Role of a system

- Reacts to **events** about entities and updates component data.
- Optionally runs **periodically** (every frame/tick) over all entities with a given set of components, for continuous processes like decay, charging, or timers.
- Exposes a small **public API** for other systems that need to perform an operation consistently (e.g. "lock this", "deal this damage") rather than editing component data themselves.
- Keeps its own invariants: it is the single place allowed to mutate its components in meaningful ways.

### How systems react

- **Subscriptions.** A system declares which event types it cares about and for which component types. The event is delivered only when the target entity has the component. Hence adding a component to an entity "switches on" the system's behavior for it.
- **Ordering.** When several systems handle the same event, they can request to run before or after one another. This matters for attempts, modifiers (damage reductions before damage is applied) and cleanup.
- **Update loops.** Periodic work iterates over entities with the relevant components, not over all entities. Systems usually do timing checks against current game time instead of counting ticks.
- **Queries.** Systems ask the world for entities matching a component set, or for entities near a position/area.

### Sides: Shared, Server, Client

Systems come in the same three flavors as components:

- **Shared systems** hold rules both sides must agree on, enabling client prediction. Where behavior differs by side, the shared system is declared abstractly and each side provides its own concrete part.
- **Server systems** add authority: spawning, persistence, anything with hidden information, anything abusable if the client decided it.
- **Client systems** handle presentation: sprite changes via visualizers, UI, overlays, audio cues, input.

A feature commonly has a shared system for the core rules, plus a thin client part for visuals and a thin server part for authority.

### Cross-system collaboration patterns

- **Attempt events** let systems veto or modify an action without the actor knowing about them.
- **Relays** pass an event from one entity to another (clothing → wearer, held tool → user) so equipment can contribute effects.
- **Containers as plumbing.** Many systems care about "what is inside what"; inventory, storage, machines and bodies all use the same container notion.
- **Interactions pipeline.** Player actions funnel into a common interaction flow that raises a sequence of events (use item on target, activate target, alt/verb actions), and feature systems subscribe to the stage they care about.
- **Do-afters.** Actions that take time are modeled as a timed, cancellable process with start, progress and completion events; the acting system handles completion.
- **Verbs.** Context-menu actions are collected by raising an event that every interested system can add to, then executed on selection.

### Map of the most commonly used systems

These form the backbone that most other features lean on. They are grouped by concern.

#### Entity fundamentals (engine)
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

#### Player and interaction
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

#### Storage and objects
- **Storage** — bags, boxes, lockers, and the logic of fitting and ordering items.
- **Stack** — counted merged items (sheets, cables, money).
- **Whitelist / tag** — generic "what is allowed" and classification filters used by many features.
- **Lock / access** — who may use or open something, based on identity credentials.
- **Wires / hacking / construction** — machine panels, assembly and deconstruction graphs, tools-with-qualities model (tools act through the capabilities they provide).
- **Item toggle and charges** — on/off devices, limited-use items.

#### Mobs and survival
- **Mob state** — alive, critical, dead and what each allows.
- **Damage** — damage types and groups, modifiers by armor/species, thresholds that trigger effects (death, destruction, gibbing).
- **Body / organs / blood / metabolism** — the physiological model.
- **Status effects** — timed conditions (stun, drowsiness, slow) applied and expired centrally.
- **Combat and weapons** — melee, ranged, projectiles, throwing, combat mode.
- **Health feedback** — alerts and damage overlays driven by state.

#### Environment and machines
- **Power** — generation, storage, distribution on cable networks and local batteries; powered/unpowered gating for machines.
- **Atmospherics** — gases, pipes, tile mixtures, temperature, pressure effects on entities.
- **Fluids and chemistry** — reagents, solutions in containers, reactions, spills and puddles.
- **Doors, airlocks, shutters** — state machines tied to power, access and atmos.
- **Light** — bulbs, handheld lights, light-related mechanics.
- **Sound** — playing audio, ambient sound, footsteps.
- **Explosions and fire** — area damage with tile propagation.

#### Round and meta
- **Game ticking and game rules** — round lifecycle, modes, antagonist selection, events.
- **Station / job / spawn** — mapping players to roles, spawn points, ID cards and manifests.
- **Chat / radio / speech** — communication channels, languages, emotes, text processing.
- **Administration / ban / permissions** — moderation features.
- **Preferences / localization / TTS** — player settings and presentation.
- **Cargo / economy** — trading and funds (Maid adds its own economy and trade pieces).

### Maid-specific systems

Maid adds its own families in its isolated areas, among them: economy, mood, adaptive/dynamic game modes, ghost recruitment, TTS, radio extensions, trade shuttle console, and weapon extensions. They obey the same patterns above; where they need to alter an upstream system, they hook events rather than rewriting it whenever possible.

### Design guidance when reading or extending

- Find the system that *owns* a behavior; change it there, not in callers.
- Prefer a new event subscription over editing someone else's system.
- Keep rules in Shared when the player should feel instant feedback; keep authority and secrets on the Server.
- Put visuals on the Client, reached only through replicated state (appearance), never by logic directly touching sprites.
- Use prototypes to tune values and compose components before writing new code.

## Entities, components and systems together

- The entity says *who*, the components say *what*, the systems say *how*.
- Adding or removing a component at runtime changes an entity's behavior instantly, because systems pick entities by component presence. This is the main extensibility mechanism of the codebase.
- Systems never assume an entity "is" something; they check for the components they need and skip if absent.
