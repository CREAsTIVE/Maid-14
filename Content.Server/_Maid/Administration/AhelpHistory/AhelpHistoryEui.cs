using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared._Maid.Administration.AhelpHistory;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server._Maid.Administration.AhelpHistory;

/// <summary>
/// Lets an admin browse a player's ahelp history stored in the database.
/// History is shown as dialogs, one per round. Long dialogs only send their beginning
/// and can be requested whole with <see cref="AhelpHistoryEuiMsg.FullDialogRequest"/>.
/// </summary>
public sealed class AhelpHistoryEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    /// <summary>
    /// Dialogs loaded per page.
    /// </summary>
    private const int DialogsPerPage = 10;

    /// <summary>
    /// Dialogs with more characters than this only get a preview.
    /// </summary>
    private const int PreviewMaxChars = 2000;

    /// <summary>
    /// Maximum messages in a preview.
    /// </summary>
    private const int PreviewMaxMessages = 20;

    private Guid _playerId;
    private string _playerName = string.Empty;
    private readonly List<AhelpHistoryDialog> _dialogs = new();
    private int? _oldestLastId;
    private bool _hasMore;
    private bool _loading;

    public AhelpHistoryEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();

        _admins.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();

        _admins.OnPermsChanged -= OnPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        return new AhelpHistoryEuiState
        {
            PlayerId = _playerId,
            PlayerName = _playerName,
            Dialogs = new List<AhelpHistoryDialog>(_dialogs),
            HasMore = _hasMore,
            Loading = _loading,
        };
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!CanView())
            return;

        try
        {
            switch (msg)
            {
                case AhelpHistoryEuiMsg.LoadMoreRequest:
                    if (!_loading && _hasMore)
                        await LoadPage();
                    break;
                case AhelpHistoryEuiMsg.FullDialogRequest request:
                    await SendFullDialog(request.RoundId);
                    break;
            }
        }
        catch (Exception e)
        {
            _log.GetSawmill("ahelp.history").Error($"Failed to load ahelp history for {_playerId}: {e}");
        }
    }

    /// <summary>
    /// Sets the player whose history is shown and loads the newest dialogs.
    /// </summary>
    public async Task Load(Guid player)
    {
        _playerId = player;
        _dialogs.Clear();
        _oldestLastId = null;
        _hasMore = false;
        _loading = true;
        StateDirty();

        try
        {
            var located = await _locator.LookupIdAsync(new NetUserId(player));
            _playerName = located?.Username
                          ?? (_players.TryGetSessionById(new NetUserId(player), out var session) ? session.Name : null)
                          ?? player.ToString();

            if (IsShutDown || _playerId != player)
                return;

            await LoadPage();
        }
        catch (Exception e)
        {
            _log.GetSawmill("ahelp.history").Error($"Failed to load ahelp history for {player}: {e}");
            _loading = false;

            if (!IsShutDown)
                StateDirty();
        }
    }

    private async Task LoadPage()
    {
        var player = _playerId;
        _loading = true;
        StateDirty();

        var loaded = new List<AhelpHistoryDialog>();
        List<AhelpDialogSummary> page;
        try
        {
            page = await _db.GetAhelpDialogsAsync(player, DialogsPerPage, _oldestLastId);

            foreach (var summary in page)
            {
                var truncated = summary.TotalChars > PreviewMaxChars || summary.MessageCount > PreviewMaxMessages;
                var messages = await _db.GetAhelpDialogMessagesAsync(
                    player,
                    summary.RoundId,
                    truncated ? PreviewMaxMessages : null);

                loaded.Add(new AhelpHistoryDialog
                {
                    RoundId = summary.RoundId,
                    ServerName = summary.ServerName,
                    StartedAt = summary.StartedAt,
                    MessageCount = summary.MessageCount,
                    Preview = truncated ? CutPreview(messages) : messages.Select(ToEntry).ToList(),
                    Truncated = truncated,
                });
            }
        }
        finally
        {
            _loading = false;
        }

        if (IsShutDown || _playerId != player)
            return;

        if (page.Count > 0)
            _oldestLastId = page[^1].LastId;

        _hasMore = page.Count == DialogsPerPage;
        _dialogs.AddRange(loaded);

        StateDirty();
    }

    private async Task SendFullDialog(int? roundId)
    {
        var player = _playerId;
        var messages = await _db.GetAhelpDialogMessagesAsync(player, roundId, null);

        if (IsShutDown || _playerId != player)
            return;

        SendMessage(new AhelpHistoryEuiMsg.FullDialogResponse
        {
            PlayerName = _playerName,
            RoundId = roundId,
            ServerName = _dialogs.FirstOrDefault(d => d.RoundId == roundId)?.ServerName,
            Entries = messages.Select(ToEntry).ToList(),
        });
    }

    /// <summary>
    /// Keeps whole messages from the start of the dialog until the character budget runs out.
    /// </summary>
    private static List<AhelpHistoryEntry> CutPreview(List<AhelpMessage> messages)
    {
        var preview = new List<AhelpHistoryEntry>();
        var chars = 0;

        foreach (var message in messages)
        {
            if (preview.Count > 0 && chars + message.Message.Length > PreviewMaxChars)
                break;

            chars += message.Message.Length;
            preview.Add(ToEntry(message));
        }

        return preview;
    }

    private static AhelpHistoryEntry ToEntry(AhelpMessage message)
    {
        return new AhelpHistoryEntry
        {
            Id = message.Id,
            SentAt = message.SentAt,
            SenderName = message.SenderName,
            SenderIsAdmin = message.SenderIsAdmin,
            FromWebhook = message.FromWebhook,
            AdminOnly = message.AdminOnly,
            Message = message.Message,
        };
    }

    private bool CanView()
    {
        return _admins.HasAdminFlag(Player, AdminFlags.Adminhelp);
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (!CanView())
            Close();
        else
            StateDirty();
    }
}
