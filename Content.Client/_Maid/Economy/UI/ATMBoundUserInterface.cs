using Content.Shared._Maid.Economy;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Maid.Economy.UI;

[UsedImplicitly]
public sealed class ATMBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ATMWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ATMWindow>();
        _window.OnWithdrawAttempt += (amount, pin) => SendMessage(new ATMRequestWithdrawMessage(amount, pin));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ATMBuiState atmState)
            _window?.UpdateState(atmState);
    }
}
