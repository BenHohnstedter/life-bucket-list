using System.Text.Json;

namespace LifeBucketList.Data;

/// <summary>Obtains a Twitch app access token via the OAuth2 client-credentials flow (required by
/// IGDB, which is Twitch-owned), and caches it until shortly before it expires.</summary>
public sealed class TwitchTokenProvider : ITwitchTokenProvider
{
    private const string TokenEndpoint = "https://id.twitch.tv/oauth2/token";

    // Refresh a bit early so a request never fires with a token that expires mid-flight.
    private static readonly TimeSpan RefreshSafetyMargin = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly IApiKeyProvider _apiKeyProvider;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public TwitchTokenProvider(HttpClient httpClient, IApiKeyProvider apiKeyProvider, Func<DateTimeOffset>? utcNowProvider = null)
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
        var clientId = _apiKeyProvider.GetIgdbClientId();
        var clientSecret = _apiKeyProvider.GetIgdbClientSecret();
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        var url = $"{TokenEndpoint}?client_id={Uri.EscapeDataString(clientId)}&client_secret={Uri.EscapeDataString(clientSecret)}&grant_type=client_credentials";

        try
        {
            using var response = await _httpClient.PostAsync(url, content: null, cancellationToken);
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
