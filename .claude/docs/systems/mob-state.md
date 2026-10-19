# Mob state

- A mob has a current state: alive, critical or dead (invalid is the zero value).
- A thresholds component maps total damage amounts to states. The threshold system listens to the damage-changed event and recomputes; an update event lets other systems override the result.
- State changes are announced by a mob-state-changed event (also broadcast).
- The mob state system subscribes to a large set of attempt events (use, attack, speak, emote, drop, pickup, throw, move, stand, pull, equip and more) and cancels them for incapacitated mobs.
- Visual state is exposed through a mob-state appearance key.
