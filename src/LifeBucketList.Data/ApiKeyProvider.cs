using System.Text.Json;

namespace LifeBucketList.Data;

/// <summary>Resolves API credentials in order: environment variable, then a ".env" file (found by
/// walking up from <paramref name="dotEnvSearchStartDirectory"/> — convenient for local development),
/// then a local, git-ignored JSON file (convenient for the installed app). None are required: missing
/// credentials simply disable cover search for the affected provider.</summary>
public sealed class ApiKeyProvider : IApiKeyProvider
{
    private const string TmdbEnvironmentVariable = "LBL_TMDB_API_KEY";
    private const string IgdbClientIdEnvironmentVariable = "LBL_IGDB_CLIENT_ID";
    private const string IgdbClientSecretEnvironmentVariable = "LBL_IGDB_CLIENT_SECRET";
    private const string SpotifyClientIdEnvironmentVariable = "LBL_SPOTIFY_CLIENT_ID";
    private const string SpotifyClientSecretEnvironmentVariable = "LBL_SPOTIFY_CLIENT_SECRET";

    private const string TmdbFileKey = "TmdbApiKey";
    private const string IgdbClientIdFileKey = "IgdbClientId";
    private const string IgdbClientSecretFileKey = "IgdbClientSecret";
    private const string SpotifyClientIdFileKey = "SpotifyClientId";
    private const string SpotifyClientSecretFileKey = "SpotifyClientSecret";

    private readonly Lazy<IReadOnlyDictionary<string, string>> _dotEnvValues;
    private readonly Lazy<IReadOnlyDictionary<string, string>> _jsonFileValues;

    public ApiKeyProvider(string apiKeysFilePath, string dotEnvSearchStartDirectory)
    {
        _dotEnvValues = new Lazy<IReadOnlyDictionary<string, string>>(() => DotEnvFile.LoadFromNearestAncestor(dotEnvSearchStartDirectory));
        _jsonFileValues = new Lazy<IReadOnlyDictionary<string, string>>(() => LoadJsonFile(apiKeysFilePath));
    }

    /// <summary>Default location for the optional local key file: %AppData%\LifeBucketList\apikeys.json.</summary>
    public static string GetDefaultFilePath()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataFolder, "LifeBucketList", "apikeys.json");
    }

    public string? GetTmdbApiKey() => Resolve(TmdbEnvironmentVariable, TmdbFileKey);

    public string? GetIgdbClientId() => Resolve(IgdbClientIdEnvironmentVariable, IgdbClientIdFileKey);

    public string? GetIgdbClientSecret() => Resolve(IgdbClientSecretEnvironmentVariable, IgdbClientSecretFileKey);

    public string? GetSpotifyClientId() => Resolve(SpotifyClientIdEnvironmentVariable, SpotifyClientIdFileKey);

    public string? GetSpotifyClientSecret() => Resolve(SpotifyClientSecretEnvironmentVariable, SpotifyClientSecretFileKey);

    private string? Resolve(string environmentVariableName, string fileKeyName)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        if (_dotEnvValues.Value.TryGetValue(environmentVariableName, out var fromDotEnv) && !string.IsNullOrWhiteSpace(fromDotEnv))
        {
            return fromDotEnv;
        }

        return _jsonFileValues.Value.TryGetValue(fileKeyName, out var fromJsonFile) && !string.IsNullOrWhiteSpace(fromJsonFile)
            ? fromJsonFile
            : null;
    }

    private static IReadOnlyDictionary<string, string> LoadJsonFile(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }
}
