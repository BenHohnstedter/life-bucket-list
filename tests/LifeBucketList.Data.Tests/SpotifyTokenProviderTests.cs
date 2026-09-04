using System.Net;
using System.Text;
using Xunit;

namespace LifeBucketList.Data.Tests;

public class SpotifyTokenProviderTests
{
    private const string TokenResponse = """{ "access_token": "abc123", "expires_in": 3600, "token_type": "Bearer" }""";

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsNull_WhenCredentialsAreMissing()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("accounts.spotify.com", HttpStatusCode.OK, TokenResponse);
        var provider = new SpotifyTokenProvider(new HttpClient(handler), new FakeApiKeyProvider());

        var token = await provider.GetAccessTokenAsync();

        Assert.Null(token);
        Assert.Empty(handler.RequestedUrls);
    }

    [Fact]
    public async Task GetAccessTokenAsync_FetchesAndReturnsToken_WhenCredentialsPresent()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("accounts.spotify.com", HttpStatusCode.OK, TokenResponse);
        var apiKeys = new FakeApiKeyProvider { SpotifyClientId = "id", SpotifyClientSecret = "secret" };
        var provider = new SpotifyTokenProvider(new HttpClient(handler), apiKeys);

        var token = await provider.GetAccessTokenAsync();

        Assert.Equal("abc123", token);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("POST", request.Method);
        Assert.Contains("grant_type=client_credentials", request.Body);

        var expectedAuth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("id:secret"));
        Assert.Equal(expectedAuth, request.Headers.Authorization?.ToString());
    }

    [Fact]
    public async Task GetAccessTokenAsync_SecondCallBeforeExpiry_ReusesCachedToken()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("accounts.spotify.com", HttpStatusCode.OK, TokenResponse);
        var apiKeys = new FakeApiKeyProvider { SpotifyClientId = "id", SpotifyClientSecret = "secret" };
        var provider = new SpotifyTokenProvider(new HttpClient(handler), apiKeys);

        await provider.GetAccessTokenAsync();
        await provider.GetAccessTokenAsync();

        Assert.Single(handler.RequestedUrls);
    }

    [Fact]
    public async Task GetAccessTokenAsync_AfterExpiry_RequestsANewToken()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("accounts.spotify.com", HttpStatusCode.OK, TokenResponse);
        var apiKeys = new FakeApiKeyProvider { SpotifyClientId = "id", SpotifyClientSecret = "secret" };
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new SpotifyTokenProvider(new HttpClient(handler), apiKeys, () => now);

        await provider.GetAccessTokenAsync();
        now = now.AddHours(2); // well past the 1-hour expiry (minus safety margin)
        await provider.GetAccessTokenAsync();

        Assert.Equal(2, handler.RequestedUrls.Count);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsNull_OnServerError()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("accounts.spotify.com", HttpStatusCode.Unauthorized, "invalid client");
        var apiKeys = new FakeApiKeyProvider { SpotifyClientId = "id", SpotifyClientSecret = "wrong-secret" };
        var provider = new SpotifyTokenProvider(new HttpClient(handler), apiKeys);

        var token = await provider.GetAccessTokenAsync();

        Assert.Null(token);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsNull_OnNetworkFailure()
    {
        var handler = new FakeHttpMessageHandler().ThrowWhenUrlContains("accounts.spotify.com");
        var apiKeys = new FakeApiKeyProvider { SpotifyClientId = "id", SpotifyClientSecret = "secret" };
        var provider = new SpotifyTokenProvider(new HttpClient(handler), apiKeys);

        var token = await provider.GetAccessTokenAsync();

        Assert.Null(token);
    }
}
