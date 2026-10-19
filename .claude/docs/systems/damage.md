# Damage

- The damageable component stores current damage as a mapping of damage type to amount, which damage container (the set of valid types/groups) applies, which modifier set (resistances) applies, and the total.
- A single shared entry point changes damage (positive hurts, negative heals) with options such as ignoring resistances and whether it interrupts DoAfters.
- Flow: a cancellable before-event; if the target has a complex body (Shitmed), the damage is split and applied per body part, each part being its own entity with a damageable component; per entity a modify-event is raised so resistances (own modifier set, armor via inventory relay, parent body for parts) can alter the damage; the damage is applied (woundable parts have an integrity cap); finally a damage-changed event is raised. Mob state thresholds, alerts, visuals and DoAfter interruption listen to that last event.
- Stamina damage is a separate pipeline with its own component and events.
