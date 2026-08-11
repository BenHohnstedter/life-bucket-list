using System.Security.Cryptography;
using System.Text;
using LifeBucketList.Domain.Services;

namespace LifeBucketList.Data;

/// <summary>Downloads cover images and caches them on disk by a hash of their URL, so a cover
/// keeps displaying on later launches even without a network connection.</summary>
public sealed class CoverImageCache : ICoverImageCache
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;

    public CoverImageCache(HttpClient httpClient, string cacheDirectory)
    {
        _httpClient = httpClient;
        _cacheDirectory = cacheDirectory;
    }

    /// <summary>Default cache location: %AppData%\LifeBucketList\covers.</summary>
    public static string GetDefaultCacheDirectory()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataFolder, "LifeBucketList", "covers");
    }

    public async Task<byte[]?> GetOrDownloadAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        var cachePath = Path.Combine(_cacheDirectory, HashUrl(imageUrl) + ".img");
        if (File.Exists(cachePath))
        {
            return await File.ReadAllBytesAsync(cachePath, cancellationToken);
        }

        try
        {
            var bytes = await _httpClient.GetByteArrayAsync(imageUrl, cancellationToken);
            Directory.CreateDirectory(_cacheDirectory);
            await File.WriteAllBytesAsync(cachePath, bytes, cancellationToken);
            return bytes;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return null;
        }
    }

    private static string HashUrl(string url) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
}
