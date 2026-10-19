# Entities and Components (ECS)

Game objects in Maid-14 follow the Entity–Component–System (ECS) model. This document covers the two data halves: **entities** and **components**. The behavior half, **systems**, is in `systems.md`.

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
- **Is not:** a place for logic. Behavior belongs to systems (see `systems.md`). Components may carry trivial helpers, but the rule of thumb is "data here, rules there".
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

## Entities, components and systems together

- The entity says *who*, the components say *what*, the systems say *how*.
- Adding or removing a component at runtime changes an entity's behavior instantly, because systems pick entities by component presence. This is the main extensibility mechanism of the codebase.
- Systems never assume an entity "is" something; they check for the components they need and skip if absent.
