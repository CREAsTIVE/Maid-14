using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Maid.Administration.AhelpHistory;

[Serializable, NetSerializable]
public sealed class AhelpHistoryEuiState : EuiStateBase
{
    public Guid PlayerId;
    public string PlayerName = string.Empty;

    /// <summary>
    /// Loaded messages, oldest first.
    /// </summary>
    public List<AhelpHistoryEntry> Entries = new();

    /// <summary>
    /// True if older messages exist in the database.
    /// </summary>
    public bool HasMore;

    /// <summary>
    /// True while the server is fetching from the database.
    /// </summary>
    public bool Loading;
}

[Serializable, NetSerializable]
public sealed class AhelpHistoryEntry
{
    public int Id;
    public int? RoundId;
    public string? ServerName;
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
    /// Asks the server for the next page of older messages.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class LoadMoreRequest : EuiMessageBase
    {
    }
}
