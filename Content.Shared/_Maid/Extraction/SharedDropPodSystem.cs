using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Robust.Shared.Network;

namespace Content.Shared._Maid.Extraction;

/// <summary>
/// Keeps the drop pod shut while it is locked or launching. Trying to open a locked pod brings up its keypad.
/// </summary>
public sealed class SharedDropPodSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DropPodComponent, StorageOpenAttemptEvent>(OnOpenAttempt);
    }

    private void OnOpenAttempt(Entity<DropPodComponent> ent, ref StorageOpenAttemptEvent args)
    {
        if (!ent.Comp.Locked && !ent.Comp.Launching)
            return;

        args.Cancelled = true;

        if (args.Silent || !_net.IsServer)
            return;

        if (ent.Comp.Launching)
        {
            _popup.PopupEntity(Loc.GetString("extraction-pod-sealed"), ent.Owner, args.User);
            return;
        }

        _ui.OpenUi(ent.Owner, DropPodUiKey.Key, args.User);
    }
}
