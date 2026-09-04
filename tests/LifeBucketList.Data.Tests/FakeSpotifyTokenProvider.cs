namespace LifeBucketList.Data.Tests;

public sealed class FakeSpotifyTokenProvider : ISpotifyTokenProvider
{
    public string? Token { get; set; } = "fake-access-token";

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult(Token);
}
