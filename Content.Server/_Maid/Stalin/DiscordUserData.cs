using System.Text.Json.Serialization;

namespace Content.Server._Maid.Stalin;

/// <summary>
///     Linking service answer for one player.
/// </summary>
public sealed class DiscordUserData
{
    [JsonPropertyName("registered")]
    public bool Registered { get; set; }

    /// <summary>
    ///     Discord account creation time, unix seconds.
    /// </summary>
    [JsonPropertyName("created_at")]
    public double UnixTimestamp { get; set; }

    public DateTime DiscordCreatedAt => DateTime.UnixEpoch.AddSeconds(UnixTimestamp);
}

public sealed class DiscordUsersDataRequest
{
    [JsonPropertyName("uuids")]
    public List<string> Uids { get; set; } = new();
}
