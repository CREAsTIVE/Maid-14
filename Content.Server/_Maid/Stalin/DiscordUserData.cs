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
    ///     Discord account creation time, unix seconds. Null if the service didn't send it.
    /// </summary>
    [JsonPropertyName("created_at")]
    public double? UnixTimestamp { get; set; }

    public DateTime? DiscordCreatedAt => UnixTimestamp is { } ts ? DateTime.UnixEpoch.AddSeconds(ts) : null;

    /// <summary>
    ///     A linked account with a known creation time. Anything else must not let the player in:
    ///     a missing timestamp would otherwise read as 1970 and pass any age check.
    /// </summary>
    public bool IsValidRegistration => Registered && UnixTimestamp is > 0;
}

public sealed class DiscordUsersDataRequest
{
    [JsonPropertyName("uuids")]
    public List<string> Uids { get; set; } = new();
}
