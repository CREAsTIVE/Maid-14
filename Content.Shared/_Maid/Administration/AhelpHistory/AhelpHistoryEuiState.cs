using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Maid.Administration.AhelpHistory;

[Serializable, NetSerializable]
public sealed class AhelpHistoryEuiState : EuiStateBase
{
    public Guid PlayerId;
    public string PlayerName = string.Empty;

    /// <summary>
    /// Loaded dialogs, newest first.
    /// </summary>
    public List<AhelpHistoryDialog> Dialogs = new();

    /// <summary>
    /// True if older dialogs exist in the database.
    /// </summary>
    public bool HasMore;

    /// <summary>
    /// True while the server is fetching from the database.
    /// </summary>
    public bool Loading;
}

/// <summary>
/// All ahelp messages of the player within one round.
/// </summary>
[Serializable, NetSerializable]
public sealed class AhelpHistoryDialog
{
    public int? RoundId;
    public string? ServerName;
    public DateTime StartedAt;
    public int MessageCount;

    /// <summary>
    /// The first messages of the dialog, oldest first. Shorter than the full dialog if <see cref="Truncated"/>.
    /// </summary>
    public List<AhelpHistoryEntry> Preview = new();

    /// <summary>
    /// True if the dialog is too long to be sent whole and has to be opened separately.
    /// </summary>
    public bool Truncated;
}

[Serializable, NetSerializable]
public sealed class AhelpHistoryEntry
{
    public int Id;
    public DateTime SentAt;
    public string SenderName = string.Empty;
    public bool SenderIsAdmin;
    public bool FromWebhook;
    public bool AdminOnly;
    public string Message = string.Empty;
}

public static class AhelpHistoryEuiMsg
{
    /// <summary>
    /// Asks the server for the next page of older dialogs.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class LoadMoreRequest : EuiMessageBase
    {
    }

    /// <summary>
    /// Asks the server for every message of one dialog.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class FullDialogRequest : EuiMessageBase
    {
        public int? RoundId;
    }

    /// <summary>
    /// Every message of one dialog, oldest first.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class FullDialogResponse : EuiMessageBase
    {
        public string PlayerName = string.Empty;
        public int? RoundId;
        public string? ServerName;
        public List<AhelpHistoryEntry> Entries = new();
    }
}
