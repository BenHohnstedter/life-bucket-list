namespace LifeBucketList.Data.Tests;

public sealed class FakeApiKeyProvider : IApiKeyProvider
{
    public string? TmdbApiKey { get; set; }
    public string? IgdbClientId { get; set; }
    public string? IgdbClientSecret { get; set; }

    public string? GetTmdbApiKey() => TmdbApiKey;

    public string? GetIgdbClientId() => IgdbClientId;

    public string? GetIgdbClientSecret() => IgdbClientSecret;
}
