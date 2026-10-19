# Interaction (and ActionBlocker)

Everything the player does by clicking funnels through the shared interaction system. Input bindings for use and alt-use call into one entry that takes user, click coordinates, optional target and an alt flag.

Order of checks in that entry:
1. If the user has an interaction relay (or the target has a target-relay), the interaction is redirected to the relay entity.
2. Combat mode may stop hand interactions.
3. The user is turned to face the click.
4. Alt-use: runs the first alternative verb of the target and stops.
5. ActionBlocker "can interact" check.
6. Accessibility check: target must be in the same container chain or reachable through an open storage.
7. Range and obstruction check.

Then it branches on what the user holds:
- **Empty hand:** a before-event on the user, then an interact-hand event on the target (raised with broadcast).
- **Clicked the held item itself:** use-in-hand attempt event, then use-in-hand event, both on the item.
- **Item on a target in reach:** before-ranged-interact on the item, then interact-using on the target (broadcast; `Handled` stops the chain), then after-interact on the item, then after-interact-using on the target.
- **Target out of reach:** only the before/after "ranged" events run, with `canReach` false (guns and ranged tools use this).
- "Activate" is separate: an activate-in-world event on the used entity and a user-side variant on the user.

Handlers consume a click by setting `Handled`. Physical contact is recorded separately (used for forensics). Range and accessibility can be overridden by dedicated override events.

## ActionBlocker
A central set of yes/no permission queries (move, interact, speak, emote, attack, drop, pickup, throw, change direction, conscious action, ...). Each query raises the matching attempt event; components cancel it (mob state, stun, restraints, pacifism and so on). To forbid an action for an entity, subscribe to the matching attempt event and cancel it.
