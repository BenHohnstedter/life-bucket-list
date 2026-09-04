using System.Net;
using LifeBucketList.Domain.Models;
using Xunit;

namespace LifeBucketList.Data.Tests;

public class CompositeCoverSearchServiceTests
{
    private const string TmdbResponse = """{ "results": [] }""";
    private const string SpotifyResponse = """{ "artists": { "items": [] } }""";

    private static CompositeCoverSearchService CreateService(FakeHttpMessageHandler handler)
    {
        var apiKeys = new FakeApiKeyProvider
        {
            TmdbApiKey = "tmdb-key",
            SpotifyClientId = "spotify-id",
            SpotifyClientSecret = "spotify-secret",
        };
        var httpClient = new HttpClient(handler);
        var tmdbIgdb = new TmdbIgdbCoverSearchService(httpClient, apiKeys, new FakeTwitchTokenProvider());
        var spotify = new SpotifyCoverSearchService(httpClient, apiKeys, new FakeSpotifyTokenProvider());
        return new CompositeCoverSearchService(tmdbIgdb, spotify);
    }

    [Fact]
    public async Task SearchAsync_Artist_DispatchesToSpotify_NotTmdbIgdb()
    {
        var handler = new FakeHttpMessageHandler()
            .RespondWhenUrlContains("api.spotify.com", HttpStatusCode.OK, SpotifyResponse)
            .RespondWhenUrlContains("api.themoviedb.org", HttpStatusCode.OK, TmdbResponse);
        var service = CreateService(handler);

        await service.SearchAsync(MediaKind.Artist, "Peter Fox");

        Assert.Contains(handler.RequestedUrls, u => u.Contains("api.spotify.com"));
        Assert.DoesNotContain(handler.RequestedUrls, u => u.Contains("api.themoviedb.org"));
    }

    [Theory]
    [InlineData(MediaKind.Movie)]
    [InlineData(MediaKind.Series)]
    public async Task SearchAsync_MovieOrSeries_DispatchesToTmdb_NotSpotify(MediaKind kind)
    {
        var handler = new FakeHttpMessageHandler()
            .RespondWhenUrlContains("api.spotify.com", HttpStatusCode.OK, SpotifyResponse)
            .RespondWhenUrlContains("api.themoviedb.org", HttpStatusCode.OK, TmdbResponse);
        var service = CreateService(handler);

        await service.SearchAsync(kind, "Inception");

        Assert.Contains(handler.RequestedUrls, u => u.Contains("api.themoviedb.org"));
        Assert.DoesNotContain(handler.RequestedUrls, u => u.Contains("api.spotify.com"));
    }
}
