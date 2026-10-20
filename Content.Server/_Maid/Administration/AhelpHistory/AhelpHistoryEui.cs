using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared._Maid.Administration.AhelpHistory;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Network;

namespace Content.Server._Maid.Administration.AhelpHistory;

/// <summary>
/// Lets an admin browse a player's ahelp history stored in the database.
/// </summary>
public sealed class AhelpHistoryEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private const int PageSize = 100;

    private Guid _playerId;
    private string _playerName = string.Empty;
    private readonly List<AhelpHistoryEntry> _entries = new();
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
            Entries = new List<AhelpHistoryEntry>(_entries),
            HasMore = _hasMore,
            Loading = _loading,
        };
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AhelpHistoryEuiMsg.LoadMoreRequest)
            return;

        if (!CanView() || _loading || !_hasMore || _entries.Count == 0)
            return;

        try
        {
            await LoadPage(_entries[0].Id);
        }
        catch (Exception e)
        {
            _log.GetSawmill("ahelp.history").Error($"Failed to load ahelp history page for {_playerId}: {e}");
        }
    }

    /// <summary>
    /// Sets the player whose history is shown and loads the newest page.
    /// </summary>
    public async Task Load(Guid player)
    {
        _playerId = player;
        _entries.Clear();
        _hasMore = false;
        _loading = true;
        StateDirty();

        var located = await _locator.LookupIdAsync(new NetUserId(player));
        _playerName = located?.Username ?? player.ToString();

        if (IsShutDown || _playerId != player)
            return;

        await LoadPage(null);
    }

    private async Task LoadPage(int? beforeId)
    {
        var player = _playerId;
        _loading = true;
        StateDirty();

        List<AhelpMessage> page;
        try
        {
            page = await _db.GetAhelpMessagesAsync(player, PageSize, beforeId);
        }
        finally
        {
            _loading = false;
        }

        if (IsShutDown || _playerId != player)
            return;

        _hasMore = page.Count == PageSize;

        // The database returns newest first, we keep oldest first.
        page.Reverse();
        _entries.InsertRange(0, page.Select(ToEntry));

        StateDirty();
    }

    private static AhelpHistoryEntry ToEntry(AhelpMessage message)
    {
        return new AhelpHistoryEntry
        {
            Id = message.Id,
            RoundId = message.RoundId,
            ServerName = message.Round?.Server?.Name,
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
