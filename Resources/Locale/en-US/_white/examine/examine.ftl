# Poggers examine system

examine-name = It's [bold]{$name}[/bold].
examine-name-selfaware = It's you, [bold]{$name}[/bold].
examine-can-see = Looking at {OBJECT($ent)}, you can see:
examine-can-see-nothing = {CAPITALIZE(SUBJECT($ent))}'s completely naked!
examine-border-line = ═════════════════════
examine-present-tex = This is a [enttex id="{ $id }" size={ $size }] [bold]{$name}[/bold].
examine-present = This is a [bold]{$name}[/bold].
examine-present-line = ═══

id-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
}.
head-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on the head.
eyes-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} over the eyes.
mask-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on the face.
neck-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} around the neck.
ears-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on the ears.
jumpsuit-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
}.
outer-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
}.
suitstorage-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on the shoulder.
back-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on the back.
gloves-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on the hands.
belt-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on the belt.
shoes-examine = { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on the feet.

id-card-examine-full = { CAPITALIZE(POSS-ADJ($wearer)) } ID: [bold]{ $nameAndJob }[/bold].

# Selfaware version

examine-can-see-selfaware = Looking at yourself, you can see:
examine-can-see-nothing-selfaware = You are completely naked!

id-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
}.
head-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on your head.
eyes-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} over your eyes.
mask-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on your face.
neck-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} around your neck.
ears-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on your ears.
jumpsuit-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
}.
outer-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
}.
suitstorage-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on your shoulder.
back-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on your back.
gloves-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on your hands.
belt-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on your belt.
shoes-examine-selfaware = You wear { $id ->
     [empty] [bold]{ $item }[/bold]
    *[other] [enttex id="{ $id }" size={ $size }][bold]{ $item }[/bold]
} on your feet.

# Selfaware examine

comp-hands-examine-empty-selfaware = You are not holding anything.
comp-hands-examine-selfaware = You are holding { $items }.

humanoid-appearance-component-examine-selfaware = You are { INDEFINITE($age) } { $age } { $species }.
