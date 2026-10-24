using Content.Shared.Popups;
using Content.Shared.UserInterface;

namespace Content.Shared._Maid.Extraction;

/// <summary>
/// Keeps the extraction device closed for anyone but its owner.
/// </summary>
public sealed class SharedExtractionDeviceSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtractionDeviceComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
    }

    private void OnOpenAttempt(Entity<ExtractionDeviceComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || ent.Comp.Owner == args.User)
            return;

        args.Cancel();
        _popup.PopupClient(Loc.GetString(ent.Comp.UnrecognizedMessage), ent, args.User);
    }
}
