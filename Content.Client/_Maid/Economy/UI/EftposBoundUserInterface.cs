using Content.Shared._Maid.Economy;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Maid.Economy.UI;

[UsedImplicitly]
public sealed class EftposBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private EftposWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<EftposWindow>();
        _window.OnCardButtonPressed += amount => SendMessage(new EftposLockMessage(amount));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is EftposBuiState eftposState)
            _window?.UpdateState(eftposState);
    }
}
