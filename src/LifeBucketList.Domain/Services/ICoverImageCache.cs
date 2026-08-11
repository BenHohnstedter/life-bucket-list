namespace LifeBucketList.Domain.Services;

/// <summary>Downloads and locally caches cover images by URL, so previously-seen covers keep
/// displaying even without a network connection. Returns null (never throws) if a cover cannot
/// be obtained.</summary>
public interface ICoverImageCache
{
    Task<byte[]?> GetOrDownloadAsync(string imageUrl, CancellationToken cancellationToken = default);
}
