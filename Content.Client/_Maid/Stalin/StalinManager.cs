using Content.Shared._Maid.Stalin;
using Robust.Shared.Network;

namespace Content.Client._Maid.Stalin;

/// <summary>
///     Requests the Discord linking link from the server and shows it in <see cref="StalinLinkWindow"/>.
/// </summary>
public sealed class StalinManager
{
    [Dependency] private readonly IClientNetManager _net = default!;

    private StalinLinkWindow? _window;

    public void Initialize()
    {
        _net.RegisterNetMessage<DiscordAuthRequest>();
        _net.RegisterNetMessage<DiscordAuthResponse>(OnResponse);
    }

    public void RequestUri()
    {
        _net.ClientSendMessage(new DiscordAuthRequest());
    }

    private void OnResponse(DiscordAuthResponse message)
    {
        _window?.Close();
        _window = new StalinLinkWindow();
        _window.SetUri(message.Uri);
        _window.OpenCentered();
    }
}
