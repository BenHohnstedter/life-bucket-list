namespace LifeBucketList.Data;

/// <summary>Provides a valid Twitch/IGDB OAuth2 access token, obtained via the client-credentials
/// flow and cached until shortly before it expires. Returns null (never throws) if credentials are
/// missing or the token request fails.</summary>
public interface ITwitchTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
