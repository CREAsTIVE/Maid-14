# Architecture Overview

Maid-14 is a fork of Goob Station, which itself descends from Space Station 14 (SS14). It is a multiplayer 2D game built on the RobustToolbox engine. This document describes the big picture; the detailed concepts live in the sibling documents:

- `ECS.md` — entities and components: identity, lifecycle, data, networking, composition (`entity.md` is a pointer to it)
- `systems.md` — what systems are, how they talk to each other, and which ones are used most
- `rsi.md` — how sprites and their animation state are packaged

This documentation is deliberately abstract. It describes ideas and relationships, not file locations or signatures, so that refactors which keep the architecture intact do not invalidate it.

## Layers

The project is three layers stacked on each other:

1. **Engine (RobustToolbox).** Generic game-engine services: networking, entity/component runtime, physics, rendering, audio, resource loading, prototypes, UI toolkit. It knows nothing about "a station" or "a crowbar". It is vendored as a submodule and treated as read-only.
2. **Content.** The actual game: items, mobs, machines, jobs, chemistry, atmospherics, game modes. It is written against the engine's APIs.
3. **Resources.** Pure data: entity definitions (prototypes), sprites, sounds, maps, localization strings, UI layouts. Most gameplay variation is made here, without code.

Content code is itself split by *who runs it*:

- **Shared** — runs on both server and client. Holds the data definitions and the rules that must agree on both sides (so the client can predict).
- **Server** — authoritative logic, secrets, persistence, anything the client must not decide.
- **Client** — presentation: rendering, UI, input, sounds, visual feedback.

The fork-specific layers (upstream Goob content and Maid's own content) follow the same Shared/Server/Client split. Maid-owned material is isolated in dedicated `_Maid` areas; upstream material is only touched minimally and with markers.

## The Entity–Component–System model

All game objects are **entities**. An entity is nothing but an identifier plus a bag of components; it has no behavior and no type of its own.

- **Components** are pure data attached to an entity. The set of components on an entity *is* what that entity is. See `ECS.md`.
- **Systems** are the behavior. Each system owns the logic for one concern and operates on every entity that has the relevant components. See `systems.md`.
- **Prototypes** are YAML templates that say "an entity of this kind starts with these components, with these values". Spawning an entity means instantiating a prototype. Prototypes can inherit from parents, so large families of items share a base and override details.

This inversion matters for how to think about the codebase: a "door" is not a class, it is an entity that happens to carry door, power, access, and sprite components. Behaviors compose by combining components, not by subclassing.

## Communication: events, not calls

Systems rarely call each other directly on a specific entity. Instead they raise **events** about an entity, and any system interested in that entity's components reacts.

- **Directed events** are addressed to one entity and delivered only to systems subscribed for components that entity has. This is the dominant pattern: "something is trying to be picked up", "this entity was damaged", "someone is examining this".
- **Broadcast events** go to every subscriber regardless of entity, for global occurrences.
- Many events come in **attempt / action pairs**: an *attempt* event lets any interested system veto or modify, then the real event announces the outcome. This is how unrelated features (restraints, stuns, locks, blockers) can cancel an action without the acting system knowing about them.

The result is loose coupling: adding a feature usually means adding a component and a system that listens to existing events, rather than editing the system that fires them.

## Client–server model and prediction

The server is authoritative. The world state that matters is replicated to clients.

- Components marked as networked have their data sent to clients, which receive it as state snapshots.
- Because Shared code runs on the client too, the client can **predict** the result of the player's own actions immediately, then reconcile when the authoritative state arrives. Prediction is why most gameplay logic sits in Shared rather than Server.
- Things the client cannot or should not know (hidden roles, server-only bookkeeping) stay in Server-only systems and components.
- Purely cosmetic reactions run on the client only, driven by replicated state.

A recurring bridge between logic and visuals is the **appearance** mechanism: server/shared logic writes abstract key–value facts ("open", "powered", "charge level") and client-side visualizers translate them into sprite changes. Logic never touches sprites directly. See `rsi.md` for the sprite side.

## Data-driven content

A large share of gameplay is configured, not coded:

- Prototypes (entities, jobs, reagents, recipes, damage types, tiles, and many more) are loaded at startup and can be hot-reloaded.
- Component fields are filled from prototype YAML, so a tuning change is a data change.
- Localization strings are separate from code and from prototypes, with one set per language.
- Maps are authored data referencing prototypes.

Practical consequence: before writing code for a new variation, check whether existing components plus a new prototype already express it.

## World structure

- **Maps** contain **grids** (stations, shuttles, asteroids), each a movable tile-based structure, plus empty space.
- Entities live in a hierarchy: an entity is parented to a grid, map, or another entity (a held item is a child of the holder, an item in a locker is a child of the locker via a container).
- **Containers** are the mechanism for "inside" relationships: inventory slots, hands, storage, machine parts. An entity inside a container is removed from the world's spatial view but still exists.
- **Physics** handles collision, movement and triggers; **lookup** answers "what is near here".

## Lifecycle of a typical feature

A feature such as "a locker that can be locked" decomposes naturally:

1. Components hold its data (lock state, access requirements, storage contents).
2. A system reacts to interaction events and attempt events, mutates component data, and marks it as changed for replication.
3. Appearance data is updated; the client visualizer picks the matching sprite state from the RSI.
4. A prototype wires the components together with concrete values; localization provides the text.

## Fork conventions

- `_Maid` areas are Maid-owned and freely editable.
- Upstream (Goob) areas are changed minimally and annotated so merges stay tractable.
- The engine is never modified.
