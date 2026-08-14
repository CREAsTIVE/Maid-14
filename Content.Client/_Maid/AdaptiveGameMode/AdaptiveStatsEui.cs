using Content.Client.Eui;
using Content.Shared._Maid.AdaptiveGameMode;
using Content.Shared.Eui;

namespace Content.Client._Maid.AdaptiveGameMode;

public sealed class AdaptiveStatsEui : BaseEui
{
    private AdaptiveStatsWindow? _window;

    public override void Opened()
    {
        base.Opened();
        _window = new AdaptiveStatsWindow(this);
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window?.Close();
        _window = null;
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);
        if (state is not AdaptiveStatsEuiState statsState)
            return;

        _window?.UpdateState(statsState);
    }

    public void SendToggleMessage(bool enabled)
    {
        SendMessage(new AdaptiveStatsToggleMessage(enabled));
    }

    public void SendCalculateMessage()
    {
        SendMessage(new AdaptiveStatsCalculateMessage());
    }
}
