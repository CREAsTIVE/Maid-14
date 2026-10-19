# Components

A **component** is a unit of data attached to an entity. Entities have no behavior and no identity beyond what their components say. Taken together, the components on an entity define what it is and what it can do.

## What a component is (and is not)

- **Is:** a small, focused bundle of state describing one aspect — "can be worn", "has health", "is lockable", "emits light".
- **Is not:** a place for logic. Behavior belongs to systems (see `systems.md`). Components may carry trivial helpers, but the rule of thumb is "data here, rules there".
- At most **one component of each type** can exist on an entity. Behavior variations come from different component types, not from stacking duplicates.
- A component is *keyed by its type*; systems ask "does this entity have X?" rather than "what kind of entity is this?".

## Anatomy

A component declares a set of **fields**. Each field falls into one of a few roles:

- **Configuration** — values set once from the prototype and usually left alone (a cooldown length, a sound to play, a whitelist of allowed things).
- **Runtime state** — values that change as the game proceeds (is it locked, how much charge is left, when does the next action become available).
- **References** — links to other entities or to prototypes by id. Entity references must be treated as possibly stale: the target may have been deleted.

Fields that should be filled from YAML are explicitly declared as data fields. Fields that are only runtime scratch space are not.

## Lifecycle

Components move through well-defined stages, and systems can hook each one:

1. **Creation / add** — the component is attached, either at spawn from a prototype or later at runtime.
2. **Initialization / startup** — fields are populated and other components on the entity can be relied upon.
3. **Map initialization** — a one-time stage when an entity in a newly created world is "turned on" (for example, timers start, random rolls happen). Entities saved on a map skip this on reload.
4. **Active life** — systems read and write the data in response to events and updates.
5. **Shutdown / removal** — the component is detached; systems clean up anything they set up elsewhere (other entities, subscriptions, ongoing effects).

Entities may also be **paused** (e.g. on frozen maps), during which their components are not updated.

## Networking and prediction

Each component declares how it relates to the network:

- **Networked** components have their data replicated from server to client. Only the fields marked for replication are sent; the rest remain local to whichever side owns them.
- **Not networked** components exist on one side only. Server-only components hold hidden or authoritative bookkeeping; client-only components hold presentation state.
- Components that matter for **prediction** must live in Shared and be networked, so the client sees the same data it needs to simulate the player's action.
- After changing replicated data, the owning system must explicitly flag the component as changed, otherwise clients will not receive the update.
- Some components ship per-player variants of their state, so that different observers see different data.

The practical split is: *gameplay-relevant and predictable → shared and networked; secrets → server only; looks → client only.*

## Access discipline

Components can restrict which systems are allowed to modify their data. Typically a component is mutated only by "its" system, and other systems interact by raising events or calling that system's API. This keeps invariants (for example, "locked and open cannot both be true") in one place.

## Composition patterns

- **Marker components** carry no or almost no data; their presence alone is a flag ("this thing is a bureaucratic item", "this mob is a ghost"). Systems use them to include or exclude entities.
- **Capability components** add one ability to otherwise unrelated entities (can be pulled, can be buckled to, can hold a reagent).
- **Relay components** forward events from one entity to another (e.g. from worn clothing to the wearer) so effects can be attached to equipment.
- **Required-component relationships**: some components depend on others and ensure they are present. Designers can then add one component and get a consistent set.
- **Component-as-state**: temporarily adding a component can represent a transient condition (e.g. a status effect), with a system that removes it when its time is up.

## Components and prototypes

Prototypes are where components get concrete values. A prototype lists the components, with per-field overrides, that an entity of that kind should start with. Inheritance allows abstract bases ("a generic pill") and specific entities that only override what differs. Components can also be removed or replaced in a child prototype.

Because content is largely authored this way, a change in a component's fields is a change in the *data contract* between code and YAML: renaming or removing a field breaks every prototype that uses it, and adding a field with a sensible default is safe.

## Where components live

Components are organized by feature area and split by side (Shared / Server / Client) just like systems. A feature's data type is usually in Shared, with extra server-only or client-only components added only when needed. Maid-specific components live in Maid-owned areas; upstream components are left alone unless a change is unavoidable.
