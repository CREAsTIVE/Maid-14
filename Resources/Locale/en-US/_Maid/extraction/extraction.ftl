extraction-notify-popup = Your clan is on the line. Check the chat.
extraction-unlocked = Your objectives are done. Use "Request early evacuation" to get an extraction point, then call a drop pod there. Pod code: { $code }.
extraction-unlocked-forced = The clan is on the line early. Use "Request early evacuation" to get an extraction point, then call a drop pod there. Pod code: { $code }.
extraction-zone-designated = Extraction point designated at a holopad: { $location }. Stand inside the zone and call the drop pod. Re-requests left: { $left }.
extraction-no-requests-zone = The clan will not move the point again. It is still at a holopad: { $location }.
extraction-no-requests = The clan does not answer anymore.
extraction-no-zone = No suitable extraction point was found on the station.
extraction-zone-lost = Your extraction point was destroyed. The clan designated a new one at a holopad: { $location }.
extraction-zone-lost-none = Your extraction point was destroyed and no new one could be found. Contact the admins.
extraction-zone-reset = The clan is back on the line: you can request a new extraction point.
extraction-reminder = The clan is waiting. Use "Request early evacuation" to get an extraction point. Pod code: { $code }.
extraction-reminder-zone = The clan is waiting at a holopad: { $location }. Pod code: { $code }.
extraction-not-in-zone = You must stand inside the extraction zone.
extraction-announcement-sender = Station sensors
extraction-zone-marker-desc = Extraction marker of "{ $name }".
extraction-round-end = [color=gold]{ $name }[/color] was evacuated { $minutes } { $minutes ->
    [one] minute
   *[other] minutes
} after arriving at the station.

extraction-pod-announcement = An unknown small object is moving towards the station at high speed, impact is imminent.
extraction-pod-called = The pod is called and lands in { $seconds } seconds on the tile you are standing on. Step aside or be crushed!
extraction-pod-already-called = The pod is already on its way.
extraction-pod-already-landed = The pod is already on the station.
extraction-pod-landed = The pod has landed. Open it with code { $code }, get inside, close it and press "START".
extraction-pod-lost = The pod could not find its landing point. Call it again.
extraction-pod-destroyed = The pod was destroyed. You can call a new one.
extraction-pod-inside = Press "START" to lift off.
extraction-pod-not-inside = You must be inside your pod.
extraction-pod-launching = The pod is sealed. Lift-off in { $seconds } seconds.
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
