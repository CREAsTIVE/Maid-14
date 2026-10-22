extraction-notify-popup = New evacuation message. Check the chat.
extraction-unlocked = Your objectives are done. Use "Request early evacuation" to get an extraction point, then call a drop pod there. Pod code: { $code }.
extraction-unlocked-forced = Evacuation was allowed early. Use "Request early evacuation" to get an extraction point, then call a drop pod there. Pod code: { $code }.
extraction-zone-designated = Extraction point designated: { $location }. Stand inside the zone and call the drop pod. Re-requests left: { $left }.
extraction-no-requests-zone = The extraction point cannot be moved again. It is still here: { $location }.
extraction-no-requests = No evacuation requests left.
extraction-no-zone = No suitable extraction point was found on the station.
extraction-zone-lost = Your extraction point was destroyed. A new one is designated: { $location }.
extraction-zone-lost-none = Your extraction point was destroyed and no new one could be found. Contact the admins.
extraction-zone-reset = You can request an extraction point again.
extraction-reminder = Evacuation is allowed. Use "Request early evacuation" to get an extraction point. Pod code: { $code }.
extraction-reminder-zone = Extraction point: { $location }. Pod code: { $code }.
extraction-not-in-zone = You must stand inside the extraction zone.
extraction-zone-marker-desc = Extraction marker of "{ $name }".
extraction-announcement-sender = Station sensors
extraction-round-end = [color=gold]{ $name }[/color]: evacuated { $minutes } { $minutes ->
    [one] minute
   *[other] minutes
} after arriving at the station.

extraction-pod-announcement = An unknown small object is moving towards the station at high speed, impact is imminent.
extraction-pod-called = The pod is called and lands in { $seconds } { $seconds ->
    [one] second
   *[other] seconds
} on the tile you are standing on. Everything on that tile will be destroyed, step aside!
extraction-pod-landed = The pod has landed. Open it with code { $code }, get inside, close it and press "START".
extraction-pod-lost = The pod could not find its landing point. You can call it again in { $seconds } { $seconds ->
    [one] second
   *[other] seconds
}.
extraction-pod-destroyed = The pod was destroyed. There will be no other.
extraction-pod-inside = Press "START" to lift off.
extraction-pod-not-inside = You must be inside your pod.
extraction-pod-launching = The pod is sealed. Lift-off in { $seconds } { $seconds ->
    [one] second
   *[other] seconds
}.
extraction-pod-launch-cancelled = You left the pod, the launch is cancelled.
extraction-pod-sealed = The pod is sealed.
extraction-pod-ui-title = Drop pod
extraction-pod-ui-locked = Enter code
extraction-pod-ui-wrong-code = Wrong code
extraction-pod-ui-unlocked = Pod open
extraction-pod-ui-launching = Lifting off

command-description-extraction-reset = Removes the extraction zone of the given antagonists and lets them request it again.
command-description-extraction-unlock = Unlocks extraction for the given antagonists regardless of their objectives.

extraction-command-no-entity = Entity not found.
extraction-command-no-component = { $target } has no AntagExtractable component.
extraction-command-already-unlocked = { $target }: extraction is already unlocked.
extraction-command-unlocked = { $target }: extraction unlocked.
extraction-command-unlocked-added = { $target }: AntagExtractable added (everything carried now counts as own gear), extraction unlocked.
extraction-command-reset = { $target }: extraction zone reset.
