extraction-notify-popup = Your clan is on the line. Check the chat.
extraction-unlocked = Your objectives are done. Use "Request early evacuation" to get an extraction point on the station.
extraction-unlocked-forced = The clan is on the line early. Use "Request early evacuation" to get an extraction point on the station.
extraction-zone-designated = Extraction point designated at a holopad: { $location }. Stand inside the zone and touch the marker with an empty hand. Re-requests left: { $left }.
extraction-no-requests-zone = The clan will not move the point again. It is still at a holopad: { $location }.
extraction-no-requests = The clan does not answer anymore.
extraction-no-zone = No suitable extraction point was found on the station.
extraction-zone-lost = Your extraction point was destroyed. The clan designated a new one at a holopad: { $location }.
extraction-zone-lost-none = Your extraction point was destroyed and no new one could be found. Contact the admins.
extraction-zone-reset = The clan is back on the line: you can request a new extraction point.
extraction-reminder = The clan is waiting. Use "Request early evacuation" to get an extraction point.
extraction-reminder-zone = The clan is waiting at a holopad: { $location }.
extraction-not-in-zone = You must stand inside the extraction zone.
extraction-not-ready = Your objectives are not done yet.
extraction-need-empty-hand = You need an empty hand.
extraction-started = You begin the extraction...
extraction-interrupted = Extraction interrupted!
extraction-announcement = Sensors registered an unknown quantum jump within the station.
extraction-announcement-sender = Station sensors
extraction-zone-marker-desc = Extraction marker of "{ $name }".
extraction-round-end = [color=gold]{ $name }[/color] was evacuated { $minutes } { $minutes ->
    [one] minute
   *[other] minutes
} after arriving at the station.

command-description-extraction-reset = Removes the extraction zone of the given antagonists and lets them request it again.
command-description-extraction-unlock = Unlocks extraction for the given antagonists regardless of their objectives.

extraction-command-no-entity = Entity not found.
extraction-command-no-component = { $target } has no AntagExtractable component.
extraction-command-already-unlocked = { $target }: extraction is already unlocked.
extraction-command-unlocked = { $target }: extraction unlocked.
extraction-command-unlocked-added = { $target }: AntagExtractable added (everything carried now counts as own gear), extraction unlocked.
extraction-command-reset = { $target }: extraction zone reset.
