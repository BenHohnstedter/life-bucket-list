using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LifeBucketList.Data;

/// <summary>Obtains a Spotify access token via the OAuth2 client-credentials flow, and caches it
/// until shortly before it expires. Same cache/lock shape as <see cref="TwitchTokenProvider"/>, but
/// Spotify's token endpoint takes Basic-auth'd, form-encoded POST instead of Twitch's query-string
/// POST.</summary>
public sealed class SpotifyTokenProvider : ISpotifyTokenProvider
{
    private const string TokenEndpoint = "https://accounts.spotify.com/api/token";

    // Refresh a bit early so a request never fires with a token that expires mid-flight.
    private static readonly TimeSpan RefreshSafetyMargin = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly IApiKeyProvider _apiKeyProvider;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public SpotifyTokenProvider(HttpClient httpClient, IApiKeyProvider apiKeyProvider, Func<DateTimeOffset>? utcNowProvider = null)
    {
        _httpClient = httpClient;
        _apiKeyProvider = apiKeyProvider;
        _utcNow = utcNowProvider ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken is not null && _utcNow() < _expiresAt)
        {
            return _cachedToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Another caller may have refreshed it while we were waiting for the lock.
            if (_cachedToken is not null && _utcNow() < _expiresAt)
            {
                return _cachedToken;
            }

            return await RequestNewTokenAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string?> RequestNewTokenAsync(CancellationToken cancellationToken)
    {
        var clientId = _apiKeyProvider.GetSpotifyClientId();
        var clientSecret = _apiKeyProvider.GetSpotifyClientSecret();
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var token = document.RootElement.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var expiresInSeconds = document.RootElement.GetProperty("expires_in").GetInt64();
            _cachedToken = token;
            _expiresAt = _utcNow().AddSeconds(expiresInSeconds) - RefreshSafetyMargin;
            return _cachedToken;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }
}
