using Content.Shared._Maid.Extraction;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Maid.Extraction;

[UsedImplicitly]
public sealed class ExtractionDeviceBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ExtractionDeviceMenu? _menu;

    public ExtractionDeviceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ExtractionDeviceMenu>();
        _menu.RefreshButton.OnPressed += _ => SendMessage(new ExtractionDeviceRefreshMessage());
        _menu.ZoneButton.OnPressed += _ => SendMessage(new ExtractionDeviceRequestZoneMessage());
        _menu.PodButton.OnPressed += _ => SendMessage(new ExtractionDeviceCallPodMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ExtractionDeviceUiState deviceState)
            _menu?.UpdateState(deviceState);
    }
}
