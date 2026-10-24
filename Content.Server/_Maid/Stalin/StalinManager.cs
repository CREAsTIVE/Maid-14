using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Shared._Maid.CVars;
using Content.Shared._Maid.Stalin;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Maid.Stalin;

/// <summary>
///     Stalin bunker, ported from the old WD build: when enabled, only players whose SS14 account is linked to a
///     Discord account old enough may join the round. Linking itself is done by an external service
///     (<see cref="MaidCVars.StalinApiUrl"/>, <see cref="MaidCVars.StalinAuthUrl"/>).
/// </summary>
public sealed class StalinManager
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerNetManager _net = default!;

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);

    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly Dictionary<NetUserId, DiscordUserData> _registeredCache = new();
    private readonly Dictionary<NetUserId, DateTime> _nextAllowedCheck = new();

    private ISawmill _sawmill = default!;
    private CancellationTokenSource? _refreshCancel;
    private bool _enabled;
    private string _apiUrl = string.Empty;
    private string _authUrl = string.Empty;
    private float _minimalDiscordAge;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("stalin");

        _net.RegisterNetMessage<DiscordAuthRequest>(OnDiscordAuthRequest);
        _net.RegisterNetMessage<DiscordAuthResponse>();

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        _cfg.OnValueChanged(MaidCVars.StalinApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(MaidCVars.StalinAuthUrl, v => _authUrl = v, true);
        _cfg.OnValueChanged(MaidCVars.StalinDiscordMinimumAge, v => _minimalDiscordAge = v, true);
        _cfg.OnValueChanged(MaidCVars.StalinEnabled, OnEnabledChanged, true);
    }

    private void OnEnabledChanged(bool enabled)
    {
        _enabled = enabled;

        _refreshCancel?.Cancel();
        _refreshCancel = null;

        if (!enabled)
            return;

        // Keep the cache fresh: the round start check can't wait for the service.
        _refreshCancel = new CancellationTokenSource();
        _ = RefreshUsersData();
        Timer.SpawnRepeating(RefreshInterval, () => _ = RefreshUsersData(), _refreshCancel.Token);
    }

    /// <summary>
    ///     Checks a player before letting them into the round, asking the service if they are not cached yet.
    /// </summary>
    public async Task<(bool Allow, string ErrorMessage)> AllowEnter(ICommonSession session)
    {
        var userId = session.UserId;
        if (_nextAllowedCheck.TryGetValue(userId, out var nextCheck) && DateTime.Now < nextCheck)
        {
            var timeoutTime = (int) (nextCheck - DateTime.Now).TotalSeconds;
            return (false, Loc.GetString("stalin-timeout", ("timeoutTime", timeoutTime)));
        }

        _nextAllowedCheck[userId] = DateTime.Now.AddSeconds(_random.NextDouble(3, 8));

        if (!_registeredCache.TryGetValue(userId, out var data))
        {
            data = await RequestDiscordUserDataAsync(session);
            if (data != null)
                _registeredCache[userId] = data;
        }

        return Verify(data);
    }

    /// <summary>
    ///     Checks a player using only cached data, for the synchronous round start.
    /// </summary>
    public (bool Allow, string ErrorMessage) AllowEnterCached(ICommonSession session)
    {
        _registeredCache.TryGetValue(session.UserId, out var data);
        return Verify(data);
    }

    private (bool Allow, string ErrorMessage) Verify(DiscordUserData? data)
    {
        if (data == null)
            return (false, Loc.GetString("stalin-discord-doesnt-link"));

        var age = (DateTime.UtcNow - data.DiscordCreatedAt).TotalSeconds;
        if (age < _minimalDiscordAge)
        {
            var neededMinutes = (long) Math.Ceiling((_minimalDiscordAge - age) / 60);
            return (false, Loc.GetString("stalin-discord-age-check-fail", ("needed", neededMinutes)));
        }

        return (true, string.Empty);
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (!_enabled || e.NewStatus != SessionStatus.Connected)
            return;

        if (string.IsNullOrEmpty(_apiUrl))
        {
            _sawmill.Warning("Stalin API URL is not set, skipping check.");
            return;
        }

        var data = await RequestDiscordUserDataAsync(e.Session);
        if (data != null)
            _registeredCache[e.Session.UserId] = data;
    }

    /// <summary>
    ///     Refreshes the cache for everyone online in one request.
    /// </summary>
    public async Task RefreshUsersData()
    {
        if (string.IsNullOrEmpty(_apiUrl))
            return;

        var sessions = _playerManager.Sessions.ToList();
        if (sessions.Count == 0)
            return;

        Dictionary<string, DiscordUserData>? response;
        try
        {
            var request = new DiscordUsersDataRequest { Uids = sessions.Select(s => s.UserId.ToString()).ToList() };
            var httpResponse = await _httpClient.PostAsJsonAsync(_apiUrl + "isconnected", request);
            if (!httpResponse.IsSuccessStatusCode)
            {
                _sawmill.Warning($"Stalin API returned {httpResponse.StatusCode} on bulk refresh.");
                return;
            }

            response = await httpResponse.Content.ReadFromJsonAsync<Dictionary<string, DiscordUserData>>();
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Stalin API bulk refresh failed: {e.Message}");
            return;
        }

        if (response == null)
            return;

        foreach (var (uid, data) in response)
        {
            if (data.Registered && Guid.TryParse(uid, out var guid))
                _registeredCache[new NetUserId(guid)] = data;
        }
    }

    /// <summary>
    ///     Asks the service whether the player's account is linked. Returns null if it is not or the request failed.
    /// </summary>
    private async Task<DiscordUserData?> RequestDiscordUserDataAsync(ICommonSession session)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"{_apiUrl}isconnected?uuid={session.UserId}");
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Stalin API request failed: {e.Message}");
            _chat.DispatchServerMessage(session, Loc.GetString("stalin-request-failed", ("error", e.Message)));
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _chat.DispatchServerMessage(session, Loc.GetString("stalin-request-failed", ("error", response.StatusCode)));
            return null;
        }

        DiscordUserData? result;
        try
        {
            result = await response.Content.ReadFromJsonAsync<DiscordUserData>();
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Stalin API returned malformed data: {e.Message}");
            return null;
        }

        return result is { Registered: true } ? result : null;
    }

    private void OnDiscordAuthRequest(DiscordAuthRequest message)
    {
        if (!_playerManager.TryGetSessionByChannel(message.MsgChannel, out var session))
            return;

        var response = new DiscordAuthResponse
        {
            Uri = GenerateDiscordAuthUri(session.Name, session.UserId.ToString()),
        };

        _net.ServerSendMessage(response, message.MsgChannel);
    }

    /// <summary>
    ///     Builds the linking link: <c>auth_url + urlencode(ckey@uid@sha1(ckey + uid + salt))</c>.
    ///     The format must match the linking service, so it is kept exactly as in the old build.
    /// </summary>
    private string GenerateDiscordAuthUri(string ckey, string uid)
    {
        var saltedBytes = Encoding.UTF8.GetBytes(ckey + uid + _cfg.GetCVar(MaidCVars.StalinSalt));
        var hash = Convert.ToHexString(SHA1.HashData(saltedBytes)).ToLowerInvariant();

        return $"{_authUrl}{WebUtility.UrlEncode($"{ckey}@{uid}@{hash}")}";
    }
}
