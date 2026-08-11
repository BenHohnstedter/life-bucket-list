using System.Net;
using Xunit;

namespace LifeBucketList.Data.Tests;

public class TwitchTokenProviderTests
{
    private const string TokenResponse = """{ "access_token": "abc123", "expires_in": 3600, "token_type": "bearer" }""";

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsNull_WhenCredentialsAreMissing()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("id.twitch.tv", HttpStatusCode.OK, TokenResponse);
        var provider = new TwitchTokenProvider(new HttpClient(handler), new FakeApiKeyProvider());

        var token = await provider.GetAccessTokenAsync();

        Assert.Null(token);
        Assert.Empty(handler.RequestedUrls);
    }

    [Fact]
    public async Task GetAccessTokenAsync_FetchesAndReturnsToken_WhenCredentialsPresent()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("id.twitch.tv", HttpStatusCode.OK, TokenResponse);
        var apiKeys = new FakeApiKeyProvider { IgdbClientId = "id", IgdbClientSecret = "secret" };
        var provider = new TwitchTokenProvider(new HttpClient(handler), apiKeys);

        var token = await provider.GetAccessTokenAsync();

        Assert.Equal("abc123", token);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("POST", request.Method);
        Assert.Contains("client_id=id", request.Url);
        Assert.Contains("client_secret=secret", request.Url);
        Assert.Contains("grant_type=client_credentials", request.Url);
    }

    [Fact]
    public async Task GetAccessTokenAsync_SecondCallBeforeExpiry_ReusesCachedToken()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("id.twitch.tv", HttpStatusCode.OK, TokenResponse);
        var apiKeys = new FakeApiKeyProvider { IgdbClientId = "id", IgdbClientSecret = "secret" };
        var provider = new TwitchTokenProvider(new HttpClient(handler), apiKeys);

        await provider.GetAccessTokenAsync();
        await provider.GetAccessTokenAsync();

        Assert.Single(handler.RequestedUrls);
    }

    [Fact]
    public async Task GetAccessTokenAsync_AfterExpiry_RequestsANewToken()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("id.twitch.tv", HttpStatusCode.OK, TokenResponse);
        var apiKeys = new FakeApiKeyProvider { IgdbClientId = "id", IgdbClientSecret = "secret" };
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new TwitchTokenProvider(new HttpClient(handler), apiKeys, () => now);

        await provider.GetAccessTokenAsync();
        now = now.AddHours(2); // well past the 1-hour expiry (minus safety margin)
        await provider.GetAccessTokenAsync();

        Assert.Equal(2, handler.RequestedUrls.Count);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsNull_OnServerError()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("id.twitch.tv", HttpStatusCode.Unauthorized, "invalid client");
        var apiKeys = new FakeApiKeyProvider { IgdbClientId = "id", IgdbClientSecret = "wrong-secret" };
        var provider = new TwitchTokenProvider(new HttpClient(handler), apiKeys);

        var token = await provider.GetAccessTokenAsync();

        Assert.Null(token);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsNull_OnNetworkFailure()
    {
        var handler = new FakeHttpMessageHandler().ThrowWhenUrlContains("id.twitch.tv");
        var apiKeys = new FakeApiKeyProvider { IgdbClientId = "id", IgdbClientSecret = "secret" };
        var provider = new TwitchTokenProvider(new HttpClient(handler), apiKeys);

        var token = await provider.GetAccessTokenAsync();

        Assert.Null(token);
    }
}
