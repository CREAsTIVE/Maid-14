using Content.Client.Eui;
using Content.Shared._Maid.Administration.AhelpHistory;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Maid.Administration.AhelpHistory;

[UsedImplicitly]
public sealed class AhelpHistoryEui : BaseEui
{
    private AhelpHistoryWindow Window { get; }

    private readonly List<AhelpDialogWindow> _dialogWindows = new();

    public AhelpHistoryEui()
    {
        Window = new AhelpHistoryWindow();
        Window.OnLoadMore += () => SendMessage(new AhelpHistoryEuiMsg.LoadMoreRequest());
        Window.OnOpenFullDialog += roundId => SendMessage(new AhelpHistoryEuiMsg.FullDialogRequest { RoundId = roundId });
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

        foreach (var dialogWindow in _dialogWindows)
        {
            dialogWindow.Close();
        }

        _dialogWindows.Clear();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not AhelpHistoryEuiState s)
            return;

        Window.SetState(s);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AhelpHistoryEuiMsg.FullDialogResponse response)
            return;

        var dialogWindow = new AhelpDialogWindow();
        dialogWindow.SetDialog(response);
        dialogWindow.OnClose += () => _dialogWindows.Remove(dialogWindow);
        _dialogWindows.Add(dialogWindow);
        dialogWindow.OpenCentered();
    }
}
