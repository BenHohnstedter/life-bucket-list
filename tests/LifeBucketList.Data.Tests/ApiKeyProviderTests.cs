using Xunit;

namespace LifeBucketList.Data.Tests;

public class ApiKeyProviderTests : IDisposable
{
    private const string TmdbEnvVar = "LBL_TMDB_API_KEY";
    private const string IgdbClientIdEnvVar = "LBL_IGDB_CLIENT_ID";
    private const string IgdbClientSecretEnvVar = "LBL_IGDB_CLIENT_SECRET";

    private readonly string _keysFilePath = Path.Combine(Path.GetTempPath(), $"lbl-apikeys-{Guid.NewGuid()}.json");

    // Isolated per-test directory with no ".env" anywhere in its ancestry (it lives directly under
    // the OS temp root), so these tests never pick up a real ".env" file from this repo.
    private readonly string _isolatedDirectory = Path.Combine(Path.GetTempPath(), $"lbl-dotenv-{Guid.NewGuid()}");

    public ApiKeyProviderTests()
    {
        Directory.CreateDirectory(_isolatedDirectory);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(TmdbEnvVar, null);
        Environment.SetEnvironmentVariable(IgdbClientIdEnvVar, null);
        Environment.SetEnvironmentVariable(IgdbClientSecretEnvVar, null);
        if (File.Exists(_keysFilePath))
        {
            File.Delete(_keysFilePath);
        }

        Directory.Delete(_isolatedDirectory, recursive: true);
    }

    private ApiKeyProvider CreateProvider() => new(_keysFilePath, _isolatedDirectory);

    [Fact]
    public void GetTmdbApiKey_ReturnsNull_WhenNothingIsConfigured()
    {
        var provider = CreateProvider();

        Assert.Null(provider.GetTmdbApiKey());
        Assert.Null(provider.GetIgdbClientId());
        Assert.Null(provider.GetIgdbClientSecret());
    }

    [Fact]
    public void GetTmdbApiKey_ReadsFromJsonFile_WhenNothingElseIsSet()
    {
        File.WriteAllText(_keysFilePath, """{ "TmdbApiKey": "file-tmdb-key", "IgdbClientId": "file-id", "IgdbClientSecret": "file-secret" }""");
        var provider = CreateProvider();

        Assert.Equal("file-tmdb-key", provider.GetTmdbApiKey());
        Assert.Equal("file-id", provider.GetIgdbClientId());
        Assert.Equal("file-secret", provider.GetIgdbClientSecret());
    }

    [Fact]
    public void GetTmdbApiKey_ReadsFromDotEnvFile_WhenNoEnvVarOrJsonFile()
    {
        File.WriteAllText(Path.Combine(_isolatedDirectory, ".env"), $"{TmdbEnvVar}=dotenv-tmdb-key\n{IgdbClientIdEnvVar}=dotenv-id");
        var provider = CreateProvider();

        Assert.Equal("dotenv-tmdb-key", provider.GetTmdbApiKey());
        Assert.Equal("dotenv-id", provider.GetIgdbClientId());
    }

    [Fact]
    public void GetTmdbApiKey_FindsDotEnvFile_InAnAncestorDirectory()
    {
        File.WriteAllText(Path.Combine(_isolatedDirectory, ".env"), $"{TmdbEnvVar}=dotenv-tmdb-key");
        var nestedSubdirectory = Path.Combine(_isolatedDirectory, "src", "LifeBucketList.App", "bin", "Debug", "net7.0");
        Directory.CreateDirectory(nestedSubdirectory);
        var provider = new ApiKeyProvider(_keysFilePath, nestedSubdirectory);

        Assert.Equal("dotenv-tmdb-key", provider.GetTmdbApiKey());
    }

    [Fact]
    public void GetTmdbApiKey_PrefersEnvVar_OverDotEnvAndJsonFile()
    {
        File.WriteAllText(_keysFilePath, """{ "TmdbApiKey": "file-tmdb-key" }""");
        File.WriteAllText(Path.Combine(_isolatedDirectory, ".env"), $"{TmdbEnvVar}=dotenv-tmdb-key");
        Environment.SetEnvironmentVariable(TmdbEnvVar, "env-tmdb-key");
        var provider = CreateProvider();

        Assert.Equal("env-tmdb-key", provider.GetTmdbApiKey());
    }

    [Fact]
    public void GetTmdbApiKey_PrefersDotEnv_OverJsonFile()
    {
        File.WriteAllText(_keysFilePath, """{ "TmdbApiKey": "file-tmdb-key" }""");
        File.WriteAllText(Path.Combine(_isolatedDirectory, ".env"), $"{TmdbEnvVar}=dotenv-tmdb-key");
        var provider = CreateProvider();

        Assert.Equal("dotenv-tmdb-key", provider.GetTmdbApiKey());
    }

    [Fact]
    public void GetTmdbApiKey_ReturnsNull_WhenJsonFileIsMalformed()
    {
        File.WriteAllText(_keysFilePath, "not valid json");
        var provider = CreateProvider();

        Assert.Null(provider.GetTmdbApiKey());
    }
}
