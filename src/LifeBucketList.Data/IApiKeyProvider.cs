namespace LifeBucketList.Data;

/// <summary>Supplies third-party API credentials (TMDb, IGDB/Twitch) from configuration, without
/// hard-coding them.</summary>
public interface IApiKeyProvider
{
    string? GetTmdbApiKey();
    string? GetIgdbClientId();
    string? GetIgdbClientSecret();
}
