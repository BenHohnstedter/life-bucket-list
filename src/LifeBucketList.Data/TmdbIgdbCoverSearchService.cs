using System.Net.Http.Headers;
using System.Text.Json;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;

namespace LifeBucketList.Data;

/// <summary>Cover search backed by two free, publicly documented APIs: TMDb (themoviedb.org) for
/// movies/series, and IGDB (igdb.com, Twitch-owned) for video games. Both require free credentials
/// (see README). IGDB additionally requires a Twitch OAuth2 access token (client-credentials flow),
/// handled by <see cref="ITwitchTokenProvider"/>.</summary>
public sealed class TmdbIgdbCoverSearchService : ICoverSearchService
{
    private const string TmdbPosterBaseUrl = "https://image.tmdb.org/t/p/w342";
    private const string IgdbCoverBaseUrl = "https://images.igdb.com/igdb/image/upload/t_cover_big/";

    private readonly HttpClient _httpClient;
    private readonly IApiKeyProvider _apiKeyProvider;
    private readonly ITwitchTokenProvider _twitchTokenProvider;

    public TmdbIgdbCoverSearchService(HttpClient httpClient, IApiKeyProvider apiKeyProvider, ITwitchTokenProvider twitchTokenProvider)
    {
        _httpClient = httpClient;
        _apiKeyProvider = apiKeyProvider;
        _twitchTokenProvider = twitchTokenProvider;
    }

    public Task<CoverSearchOutcome> SearchAsync(MediaKind kind, string query, CancellationToken cancellationToken = default) => kind switch
    {
        MediaKind.Movie => SearchTmdbAsync("movie", query, cancellationToken),
        MediaKind.Series => SearchTmdbAsync("tv", query, cancellationToken),
        MediaKind.VideoGame => SearchIgdbAsync(query, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private async Task<CoverSearchOutcome> SearchTmdbAsync(string mediaType, string query, CancellationToken cancellationToken)
    {
        var apiKey = _apiKeyProvider.GetTmdbApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return CoverSearchOutcome.NoApiKey;
        }

        var url = $"https://api.themoviedb.org/3/search/{mediaType}" +
                   $"?api_key={Uri.EscapeDataString(apiKey)}&language=de-DE&query={Uri.EscapeDataString(query)}";

        var titleField = mediaType == "movie" ? "title" : "name";
        var dateField = mediaType == "movie" ? "release_date" : "first_air_date";

        return await FetchAsync(url, cancellationToken, root =>
        {
            var results = new List<MediaSearchResult>();
            foreach (var item in root.GetProperty("results").EnumerateArray())
            {
                var title = GetStringOrNull(item, titleField);
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                var year = ExtractYear(GetStringOrNull(item, dateField));
                var posterPath = GetStringOrNull(item, "poster_path");
                var thumbnailUrl = posterPath is null ? null : TmdbPosterBaseUrl + posterPath;
                results.Add(new MediaSearchResult(title, year, thumbnailUrl));
            }

            return results;
        });
    }

    private async Task<CoverSearchOutcome> SearchIgdbAsync(string query, CancellationToken cancellationToken)
    {
        var clientId = _apiKeyProvider.GetIgdbClientId();
        var clientSecret = _apiKeyProvider.GetIgdbClientSecret();
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return CoverSearchOutcome.NoApiKey;
        }

        var accessToken = await _twitchTokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return CoverSearchOutcome.Failed;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games")
            {
                Content = new StringContent($"search \"{EscapeApicalypseString(query)}\"; fields name,first_release_date,cover.image_id; limit 8;"),
            };
            request.Headers.Add("Client-ID", clientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CoverSearchOutcome.Failed;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var results = new List<MediaSearchResult>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var name = GetStringOrNull(item, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var year = item.TryGetProperty("first_release_date", out var dateElement) && dateElement.ValueKind == JsonValueKind.Number
                    ? DateTimeOffset.FromUnixTimeSeconds(dateElement.GetInt64()).Year.ToString()
                    : null;

                string? thumbnailUrl = null;
                if (item.TryGetProperty("cover", out var coverElement) && coverElement.ValueKind == JsonValueKind.Object)
                {
                    var imageId = GetStringOrNull(coverElement, "image_id");
                    thumbnailUrl = imageId is null ? null : IgdbCoverBaseUrl + imageId + ".jpg";
                }

                results.Add(new MediaSearchResult(name, year, thumbnailUrl));
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

    private async Task<CoverSearchOutcome> FetchAsync(string url, CancellationToken cancellationToken, Func<JsonElement, List<MediaSearchResult>> parseResults)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CoverSearchOutcome.Failed;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return CoverSearchOutcome.Success(parseResults(document.RootElement));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return CoverSearchOutcome.Failed;
        }
    }

    private static string EscapeApicalypseString(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string? GetStringOrNull(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? ExtractYear(string? isoDate) =>
        !string.IsNullOrWhiteSpace(isoDate) && isoDate.Length >= 4 ? isoDate[..4] : null;
}
