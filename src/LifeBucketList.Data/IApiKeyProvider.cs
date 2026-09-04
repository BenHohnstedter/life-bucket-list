namespace LifeBucketList.Data;

/// <summary>Supplies third-party API credentials (TMDb, IGDB/Twitch, Spotify) from configuration,
/// without hard-coding them.</summary>
public interface IApiKeyProvider
{
    string? GetTmdbApiKey();
    string? GetIgdbClientId();
    string? GetIgdbClientSecret();
    string? GetSpotifyClientId();
    string? GetSpotifyClientSecret();
}
