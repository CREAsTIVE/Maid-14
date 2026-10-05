using Content.Client.UserInterface.Fragments;
using Content.Shared._Maid.Economy;
using Content.Shared.CartridgeLoader;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Maid.Economy.UI;

[UsedImplicitly]
public sealed partial class BankUi : UIFragment
{
    private BankUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new BankUiFragment();
        _fragment.OnLinkAttempt += (accountId, pin) =>
            userInterface.SendMessage(new CartridgeUiMessage(new BankAccountLinkMessage(accountId, pin)));
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is BankCartridgeUiState bankState)
            _fragment?.UpdateState(bankState);
    }
}
