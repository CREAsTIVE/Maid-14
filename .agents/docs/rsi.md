# RSI (Robust State Image)

An **RSI** is the engine's format for packaging the visuals of one *thing* — an item, a machine, a mob, an effect — into a single self-describing bundle. It is the basic unit of 2D art in the game.

## Concept

Instead of loading loose image files, the game loads an RSI that contains:

- one or more **images** (the pixel data), and
- a **metadata description** saying how to interpret them.

An RSI is thus "a sprite sheet with a built-in table of contents". On disk it appears as a folder whose name marks it as an RSI, holding the images plus one metadata file.

## States

The core idea is the **state**: a named visual variant of the thing. A single RSI describes many states, for example:

- an item icon,
- the same item as held in the left or right hand,
- the same item as worn on a body part,
- open/closed, on/off, damaged, or charging variants of a machine,
- a mob's walking, lying down, or dead appearance.

Code and prototypes pick visuals by asking the RSI for a **state by name**. The states are therefore a small vocabulary shared between art and logic; renaming a state breaks whoever refers to it.

## What each state describes

- **Directions** — whether the state has just one view, or distinct views for several facing directions (typically four, sometimes eight). Directional states let a thing look correct whichever way it faces; the image data holds one picture per direction.
- **Frames and timing** — whether the state is a still image or an animation. Animated states list how long each frame lasts. They may loop or play once.
- **Flags and options** — optional extras on top of the basics, such as marking a state as having alternative behaviors.

## Metadata (the table of contents)

The metadata file is a small structured document containing:

- **Format version.**
- **Frame size** — the pixel dimensions of one picture; all states in the RSI share it, which is why large things get their own RSI.
- **License and copyright** — who made the art and under what terms. This is mandatory in this project: every RSI must carry attribution, and the source of borrowed art must be traceable.
- **The list of states** with their direction counts and timing information.

Tools and the game both read this one file to know how many images to expect per state and how to slice the sheet.

## How sprites use RSIs

A sprite on an entity is built from **layers**. Each layer pairs an RSI with a state name and can be independently shown, hidden, recolored, offset, rotated, or swapped. A simple object has one layer; complex objects stack several (a base body, an overlay light, a panel, a damage overlay).

Layers are usually given **keys** so that logic can address them by role ("the door panel layer", "the light layer") without caring about their order.

## Driving visuals from game state

Game logic does not poke sprites. The pattern is:

1. Logic publishes abstract facts about the entity (open, powered, charge, mode).
2. A client-side visualizer subscribes to those facts and sets a layer's state or visibility accordingly.
3. Simple cases need no code: a generic visualizer in the prototype maps appearance values to RSI states directly.

This keeps simulation (Shared/Server) independent of rendering (Client), and makes sprites purely a function of replicated state.

## Equipment visuals

Wearable and holdable items follow naming conventions inside their RSI so the game can find the right image automatically:

- a base icon state used in inventory and on the floor,
- per-hand states for being held,
- per-slot states for being worn (the slot is part of the state name),
- optional variants per species or body-shape, stored as separate states or sub-bundles.

Clothing and held-item systems construct the layers on the character from these states, so adding art with the expected names often makes an item appear correctly with no further data.

## Authoring and maintenance notes

- Art is edited as images, but the metadata must be kept in sync: a state present in the metadata with missing images (or vice versa) is an error that surfaces at load time.
- Direction count and frame count must match the actual image content.
- Because RSIs are loaded as units, big mixed bundles load more than needed, while many tiny ones add overhead; grouping by thing, not by category, is the convention.
- Attribution fields are not optional bookkeeping; legal compliance depends on them.

## Related

- `ECS.md` — sprite and appearance data are components attached to entities.
- `ECS.md` — visualizer systems and the equipment systems that consume RSI states.
