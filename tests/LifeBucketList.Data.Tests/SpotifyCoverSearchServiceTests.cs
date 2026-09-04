using System.Net;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;
using Xunit;

namespace LifeBucketList.Data.Tests;

public class SpotifyCoverSearchServiceTests
{
    private const string SpotifyArtistsResponse =
        """
        {
          "artists": {
            "items": [
              {
                "name": "Peter Fox",
                "images": [
                  { "url": "https://i.scdn.co/image/large.jpg", "height": 640, "width": 640 },
                  { "url": "https://i.scdn.co/image/medium.jpg", "height": 300, "width": 300 },
                  { "url": "https://i.scdn.co/image/small.jpg", "height": 64, "width": 64 }
                ]
              },
              {
                "name": "No Image Artist",
                "images": []
              }
            ]
          }
        }
        """;

    private static SpotifyCoverSearchService CreateService(
        FakeHttpMessageHandler handler,
        FakeApiKeyProvider apiKeys,
        FakeSpotifyTokenProvider? tokenProvider = null) =>
        new(new HttpClient(handler), apiKeys, tokenProvider ?? new FakeSpotifyTokenProvider());

    [Fact]
    public async Task SearchAsync_Artist_PrefersMidSizeImage_AndSkipsArtistsWithoutImages()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.spotify.com", HttpStatusCode.OK, SpotifyArtistsResponse);
        var apiKeys = new FakeApiKeyProvider { SpotifyClientId = "client-id", SpotifyClientSecret = "client-secret" };
        var service = CreateService(handler, apiKeys);

        var outcome = await service.SearchAsync(MediaKind.Artist, "Peter Fox");

        Assert.Equal(CoverSearchStatus.Success, outcome.Status);
        Assert.Equal(2, outcome.Results.Count);
        Assert.Equal(new MediaSearchResult("Peter Fox", null, "https://i.scdn.co/image/medium.jpg"), outcome.Results[0]);
        Assert.Equal(new MediaSearchResult("No Image Artist", null, null), outcome.Results[1]);
    }

    [Fact]
    public async Task SearchAsync_Artist_SendsBearerTokenHeader_AndArtistTypeQuery()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.spotify.com", HttpStatusCode.OK, SpotifyArtistsResponse);
        var apiKeys = new FakeApiKeyProvider { SpotifyClientId = "id", SpotifyClientSecret = "secret" };
        var service = CreateService(handler, apiKeys, new FakeSpotifyTokenProvider { Token = "my-token" });

        await service.SearchAsync(MediaKind.Artist, "Peter Fox");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("GET", request.Method);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("my-token", request.Headers.Authorization?.Parameter);
        Assert.Contains("type=artist", request.Url);
        Assert.Contains("q=Peter", request.Url);
    }

    [Fact]
    public async Task SearchAsync_Artist_WithoutCredentials_ReturnsApiKeyMissingWithoutAnyRequest()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.spotify.com", HttpStatusCode.OK, SpotifyArtistsResponse);
        var service = CreateService(handler, new FakeApiKeyProvider());

        var outcome = await service.SearchAsync(MediaKind.Artist, "Peter Fox");

        Assert.Equal(CoverSearchStatus.ApiKeyMissing, outcome.Status);
        Assert.Empty(handler.RequestedUrls);
    }

    [Fact]
    public async Task SearchAsync_Artist_WhenTokenRequestFails_ReturnsRequestFailed()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.spotify.com", HttpStatusCode.OK, SpotifyArtistsResponse);
        var apiKeys = new FakeApiKeyProvider { SpotifyClientId = "id", SpotifyClientSecret = "secret" };
        var service = CreateService(handler, apiKeys, new FakeSpotifyTokenProvider { Token = null });

        var outcome = await service.SearchAsync(MediaKind.Artist, "Peter Fox");

        Assert.Equal(CoverSearchStatus.RequestFailed, outcome.Status);
        Assert.Empty(handler.RequestedUrls);
    }

    [Fact]
    public async Task SearchAsync_Artist_ServerError_ReturnsRequestFailed()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("api.spotify.com", HttpStatusCode.InternalServerError, "oops");
        var apiKeys = new FakeApiKeyProvider { SpotifyClientId = "id", SpotifyClientSecret = "secret" };
        var service = CreateService(handler, apiKeys);

        var outcome = await service.SearchAsync(MediaKind.Artist, "Peter Fox");

        Assert.Equal(CoverSearchStatus.RequestFailed, outcome.Status);
    }

    [Fact]
    public async Task SearchAsync_Artist_WhenCancelledByCaller_PropagatesCancellation()
    {
        var handler = new FakeHttpMessageHandler().HangUntilCancelledWhenUrlContains("api.spotify.com");
        var apiKeys = new FakeApiKeyProvider { SpotifyClientId = "id", SpotifyClientSecret = "secret" };
        var service = CreateService(handler, apiKeys);
        using var cts = new CancellationTokenSource();

        var searchTask = service.SearchAsync(MediaKind.Artist, "Peter Fox", cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => searchTask);
    }
}
