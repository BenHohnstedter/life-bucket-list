using System.Net;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;
using Xunit;

namespace LifeBucketList.Data.Tests;

public class TmdbIgdbCoverSearchServiceTests
{
    private const string TmdbMovieResponse =
        """
        {
          "results": [
            { "title": "Inception", "release_date": "2010-07-15", "poster_path": "/abc123.jpg" },
            { "title": "Untitled Project", "release_date": "", "poster_path": null }
          ]
        }
        """;

    private const string TmdbSeriesResponse =
        """
        {
          "results": [
            { "name": "Breaking Bad", "first_air_date": "2008-01-20", "poster_path": "/xyz.jpg" }
          ]
        }
        """;

    // first_release_date is a Unix timestamp (seconds); 509328000 = 1986-02-21T00:00:00Z.
    private const string IgdbGamesResponse =
        """
        [
          { "name": "The Legend of Zelda", "first_release_date": 509328000, "cover": { "id": 1, "image_id": "abc123" } },
          { "name": "Unreleased Game" }
        ]
        """;

    private static TmdbIgdbCoverSearchService CreateService(
        FakeHttpMessageHandler handler,
        FakeApiKeyProvider? apiKeyProvider = null,
        FakeTwitchTokenProvider? tokenProvider = null) =>
        new(new HttpClient(handler), apiKeyProvider ?? new FakeApiKeyProvider(), tokenProvider ?? new FakeTwitchTokenProvider());

    [Fact]
    public async Task SearchAsync_Movie_ParsesTitleYearAndPosterUrl()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("search/movie", HttpStatusCode.OK, TmdbMovieResponse);
        var service = CreateService(handler, new FakeApiKeyProvider { TmdbApiKey = "key" });

        var outcome = await service.SearchAsync(MediaKind.Movie, "Inception");

        Assert.Equal(CoverSearchStatus.Success, outcome.Status);
        Assert.Equal(2, outcome.Results.Count);
        Assert.Equal(new MediaSearchResult("Inception", "2010", "https://image.tmdb.org/t/p/w342/abc123.jpg"), outcome.Results[0]);
        Assert.Equal(new MediaSearchResult("Untitled Project", null, null), outcome.Results[1]);
    }

    [Fact]
    public async Task SearchAsync_Series_UsesTvEndpointAndNameField()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("search/tv", HttpStatusCode.OK, TmdbSeriesResponse);
        var service = CreateService(handler, new FakeApiKeyProvider { TmdbApiKey = "key" });

        var outcome = await service.SearchAsync(MediaKind.Series, "Breaking Bad");

        var result = Assert.Single(outcome.Results);
        Assert.Equal("Breaking Bad", result.Title);
        Assert.Equal("2008", result.Year);
        Assert.Equal("https://image.tmdb.org/t/p/w342/xyz.jpg", result.ThumbnailUrl);
    }

    [Fact]
    public async Task SearchAsync_Movie_WithoutApiKey_ReturnsApiKeyMissingWithoutMakingRequest()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("search/movie", HttpStatusCode.OK, TmdbMovieResponse);
        var service = CreateService(handler);

        var outcome = await service.SearchAsync(MediaKind.Movie, "Inception");

        Assert.Equal(CoverSearchStatus.ApiKeyMissing, outcome.Status);
        Assert.Empty(outcome.Results);
        Assert.Empty(handler.RequestedUrls);
    }

    [Fact]
    public async Task SearchAsync_ServerError_ReturnsRequestFailed()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("search/movie", HttpStatusCode.InternalServerError, "oops");
        var service = CreateService(handler, new FakeApiKeyProvider { TmdbApiKey = "key" });

        var outcome = await service.SearchAsync(MediaKind.Movie, "Inception");

        Assert.Equal(CoverSearchStatus.RequestFailed, outcome.Status);
        Assert.Empty(outcome.Results);
    }

    [Fact]
    public async Task SearchAsync_NetworkException_ReturnsRequestFailed()
    {
        var handler = new FakeHttpMessageHandler().ThrowWhenUrlContains("search/movie");
        var service = CreateService(handler, new FakeApiKeyProvider { TmdbApiKey = "key" });

        var outcome = await service.SearchAsync(MediaKind.Movie, "Inception");

        Assert.Equal(CoverSearchStatus.RequestFailed, outcome.Status);
    }

    [Fact]
    public async Task SearchAsync_Movie_CallerCancellation_PropagatesAsCancellation_NotAsRequestFailed()
    {
        // A live/debounced search that gets superseded by newer input must be distinguishable from
        // a genuine failure, so the UI doesn't flash a misleading error message.
        var handler = new FakeHttpMessageHandler().HangUntilCancelledWhenUrlContains("search/movie");
        var service = CreateService(handler, new FakeApiKeyProvider { TmdbApiKey = "key" });
        using var cts = new CancellationTokenSource();

        var searchTask = service.SearchAsync(MediaKind.Movie, "Inception", cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => searchTask);
    }

    [Fact]
    public async Task SearchAsync_VideoGame_UsesIgdbAndBuildsCoverUrlFromImageId()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.igdb.com", HttpStatusCode.OK, IgdbGamesResponse);
        var apiKeys = new FakeApiKeyProvider { IgdbClientId = "client-id", IgdbClientSecret = "client-secret" };
        var service = CreateService(handler, apiKeys, new FakeTwitchTokenProvider { Token = "twitch-token" });

        var outcome = await service.SearchAsync(MediaKind.VideoGame, "Zelda");

        Assert.Equal(CoverSearchStatus.Success, outcome.Status);
        Assert.Equal(2, outcome.Results.Count);
        Assert.Equal(new MediaSearchResult("The Legend of Zelda", "1986", "https://images.igdb.com/igdb/image/upload/t_cover_big/abc123.jpg"), outcome.Results[0]);
        Assert.Equal(new MediaSearchResult("Unreleased Game", null, null), outcome.Results[1]);
    }

    [Fact]
    public async Task SearchAsync_VideoGame_SendsClientIdAndBearerTokenHeaders_AndApicalypseQueryBody()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.igdb.com", HttpStatusCode.OK, IgdbGamesResponse);
        var apiKeys = new FakeApiKeyProvider { IgdbClientId = "my-client-id", IgdbClientSecret = "client-secret" };
        var service = CreateService(handler, apiKeys, new FakeTwitchTokenProvider { Token = "my-token" });

        await service.SearchAsync(MediaKind.VideoGame, "Zelda");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("POST", request.Method);
        Assert.Equal("my-client-id", request.Headers.GetValues("Client-ID").Single());
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("my-token", request.Headers.Authorization?.Parameter);
        Assert.Contains("search \"Zelda\"", request.Body);
    }

    [Fact]
    public async Task SearchAsync_VideoGame_EscapesQuotesInQuery()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.igdb.com", HttpStatusCode.OK, "[]");
        var apiKeys = new FakeApiKeyProvider { IgdbClientId = "id", IgdbClientSecret = "secret" };
        var service = CreateService(handler, apiKeys);

        await service.SearchAsync(MediaKind.VideoGame, "Ori and the \"Blind\" Forest");

        var request = Assert.Single(handler.Requests);
        Assert.Contains("""Ori and the \"Blind\" Forest""", request.Body);
    }

    [Fact]
    public async Task SearchAsync_VideoGame_WithoutCredentials_ReturnsApiKeyMissingWithoutAnyRequest()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.igdb.com", HttpStatusCode.OK, IgdbGamesResponse);
        var service = CreateService(handler, new FakeApiKeyProvider());

        var outcome = await service.SearchAsync(MediaKind.VideoGame, "Zelda");

        Assert.Equal(CoverSearchStatus.ApiKeyMissing, outcome.Status);
        Assert.Empty(handler.RequestedUrls);
    }

    [Fact]
    public async Task SearchAsync_VideoGame_WhenTokenRequestFails_ReturnsRequestFailed()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.igdb.com", HttpStatusCode.OK, IgdbGamesResponse);
        var apiKeys = new FakeApiKeyProvider { IgdbClientId = "id", IgdbClientSecret = "secret" };
        var service = CreateService(handler, apiKeys, new FakeTwitchTokenProvider { Token = null });

        var outcome = await service.SearchAsync(MediaKind.VideoGame, "Zelda");

        Assert.Equal(CoverSearchStatus.RequestFailed, outcome.Status);
        Assert.Empty(handler.RequestedUrls);
    }

    [Fact]
    public async Task SearchAsync_VideoGame_ServerError_ReturnsRequestFailed()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.igdb.com", HttpStatusCode.InternalServerError, "oops");
        var apiKeys = new FakeApiKeyProvider { IgdbClientId = "id", IgdbClientSecret = "secret" };
        var service = CreateService(handler, apiKeys);

        var outcome = await service.SearchAsync(MediaKind.VideoGame, "Zelda");

        Assert.Equal(CoverSearchStatus.RequestFailed, outcome.Status);
    }
}
