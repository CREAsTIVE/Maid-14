using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared._Maid.CVars;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server._Maid.Administration.AhelpHistory;

/// <summary>
/// Stores ahelp messages in the database so admins can browse a player's ahelp history across rounds.
/// </summary>
public sealed class AhelpHistorySystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private const int MaxSenderNameLength = 256;

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, MaidCVars.AhelpHistoryEnabled, v => _enabled = v, true);
    }

    /// <summary>
    /// Saves an ahelp message to the database. Never throws; database failures are only logged.
    /// </summary>
    /// <param name="player">The player whose ahelp thread the message belongs to.</param>
    /// <param name="sender">Who sent the message. Default for Discord relays.</param>
    /// <param name="senderName">Sender name at the time of sending.</param>
    /// <param name="text">Raw message text, without markup.</param>
    /// <param name="senderIsAdmin">Whether the message was sent by an admin (or through the webhook).</param>
    /// <param name="adminOnly">Whether the message was only visible to admins.</param>
    /// <param name="playSound">Whether the message played a sound.</param>
    /// <param name="fromWebhook">Whether the message came from the Discord webhook.</param>
    public void Record(
        NetUserId player,
        NetUserId sender,
        string senderName,
        string text,
        bool senderIsAdmin,
        bool adminOnly,
        bool playSound,
        bool fromWebhook)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(text))
            return;

        if (senderName.Length > MaxSenderNameLength)
            senderName = senderName[..MaxSenderNameLength];

        var roundId = _gameTicker.RoundId;

        var message = new AhelpMessage
        {
            RoundId = roundId > 0 ? roundId : null,
            PlayerUserId = player.UserId,
            SenderUserId = sender.UserId,
            SenderName = senderName,
            SentAt = DateTime.UtcNow,
            Message = text,
            SenderIsAdmin = senderIsAdmin,
            AdminOnly = adminOnly,
            PlaySound = playSound,
            FromWebhook = fromWebhook,
        };

        Save(message);
    }

    private async void Save(AhelpMessage message)
    {
        try
        {
            await _db.AddAhelpMessageAsync(message);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save ahelp message for {message.PlayerUserId}: {e}");
        }
    }
}
