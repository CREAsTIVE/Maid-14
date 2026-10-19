# DoAfter

Timed, cancellable actions with a progress bar.

- Start one by building DoAfter args (user, delay, an event instance, event target, optional target and used item) and calling the start method of the shared system.
- Args carry the break conditions: break on move (with threshold), on damage (with threshold), on hand change or dropped item, distance limit, need a free hand, require can-interact, and duplicate handling (block or cancel duplicates).
- When it finishes or is cancelled the engine raises your event class on the event target (optionally also broadcast). The event is a networked subclass of the DoAfter event base (or the simple stateless variant). Convenience members give user, target, used and whether it was cancelled.
- Handler pattern: if cancelled or already handled, return; do the work; mark handled. Setting `Repeat` on the event restarts it.
- The system also exposes cancel, is-running and status queries by id.
