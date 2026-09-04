namespace LifeBucketList.Data;

/// <summary>Provides a valid Spotify OAuth2 access token, obtained via the client-credentials flow
/// and cached until shortly before it expires. Returns null (never throws) if credentials are missing
/// or the token request fails.</summary>
public interface ISpotifyTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
