using Content.Client.Eui;
using Content.Shared._Maid.Administration.AhelpHistory;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Maid.Administration.AhelpHistory;

[UsedImplicitly]
public sealed class AhelpHistoryEui : BaseEui
{
    private AhelpHistoryWindow Window { get; }

    public AhelpHistoryEui()
    {
        Window = new AhelpHistoryWindow();
        Window.OnLoadMore += () => SendMessage(new AhelpHistoryEuiMsg.LoadMoreRequest());
        Window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        Window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        Window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not AhelpHistoryEuiState s)
            return;

        Window.SetState(s);
    }
}
