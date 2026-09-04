using System.Net.Http.Headers;
using System.Text.Json;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;

namespace LifeBucketList.Data;

/// <summary>Cover search backed by the Spotify Web API (developer.spotify.com) for Konzerte artists.
/// Requires free credentials (see README) and a Spotify OAuth2 access token (client-credentials flow),
/// handled by <see cref="ISpotifyTokenProvider"/>.</summary>
public sealed class SpotifyCoverSearchService : ICoverSearchService
{
    private readonly HttpClient _httpClient;
    private readonly IApiKeyProvider _apiKeyProvider;
    private readonly ISpotifyTokenProvider _spotifyTokenProvider;

    public SpotifyCoverSearchService(HttpClient httpClient, IApiKeyProvider apiKeyProvider, ISpotifyTokenProvider spotifyTokenProvider)
    {
        _httpClient = httpClient;
        _apiKeyProvider = apiKeyProvider;
        _spotifyTokenProvider = spotifyTokenProvider;
    }

    public async Task<CoverSearchOutcome> SearchAsync(MediaKind kind, string query, CancellationToken cancellationToken = default)
    {
        var clientId = _apiKeyProvider.GetSpotifyClientId();
        var clientSecret = _apiKeyProvider.GetSpotifyClientSecret();
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return CoverSearchOutcome.NoApiKey;
        }

        var accessToken = await _spotifyTokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return CoverSearchOutcome.Failed;
        }

        try
        {
            var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=artist&limit=8";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CoverSearchOutcome.Failed;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var results = new List<MediaSearchResult>();
            foreach (var item in document.RootElement.GetProperty("artists").GetProperty("items").EnumerateArray())
            {
                var name = GetStringOrNull(item, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                results.Add(new MediaSearchResult(name, Year: null, ExtractThumbnailUrl(item)));
            }

            return CoverSearchOutcome.Success(results);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller (not an internal timeout) asked us to stop — e.g. a debounced live search
            // superseded by newer input. Propagate so the caller can treat it as "no result yet",
            // not as a failed search.
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return CoverSearchOutcome.Failed;
        }
    }

    /// <summary>Spotify returns artist images largest-first (typically 640/300/64px). Prefers the
    /// mid-size one if present — big enough for a crisp thumbnail without downloading the full 640px
    /// image for a small picker grid.</summary>
    private static string? ExtractThumbnailUrl(JsonElement artist)
    {
        if (!artist.TryGetProperty("images", out var images) || images.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = images.EnumerateArray().ToList();
        if (list.Count == 0)
        {
            return null;
        }

        var chosen = list.Count > 1 ? list[1] : list[0];
        return GetStringOrNull(chosen, "url");
    }

    private static string? GetStringOrNull(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
