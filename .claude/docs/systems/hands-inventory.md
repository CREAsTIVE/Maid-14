# Hands and Inventory

## Hands
- A hands component holds a dictionary of hand id to hand, the active hand id, and a sorted order. Each hand is a container slot named by its id.
- Pick up, drop, swap, set active hand, enumerate held items, check free hands: all go through the shared hands system.
- Events: drop attempt and pickup attempt (cancellable); got-equipped-hand / got-unequipped-hand on the item; did-equip-hand / did-unequip-hand on the holder; hand count changed.
- Virtual items occupy a hand to represent two-handed holds or pulls.

## Inventory
- An inventory component plus a template prototype that lists slot definitions: name, slot flags, UI position, strip time, whitelist/blacklist, optional dependency on another slot.
- Each slot is a container named after the slot. Maid adds socks, upper underwear and lower underwear slots (and matching flags).
- Slot flags are a flag enum; clothing declares which flags it fits.
- Equip events come in layers: attempts on the item, on the actor and on the wearer (and unequip mirrors), then done events on the item (got) and on the wearer (did).
- **Relay:** selected events raised on the wearer are re-raised on each equipped item whose slot matches the event's target slots, wrapped in a relayed-event type. This is how armor, shoes, eyewear, speed modifiers and similar items affect the wearer. Held items have an analogous relay.
