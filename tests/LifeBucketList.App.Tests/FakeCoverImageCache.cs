using LifeBucketList.Domain.Services;

namespace LifeBucketList.App.Tests;

public sealed class FakeCoverImageCache : ICoverImageCache
{
    public Task<byte[]?> GetOrDownloadAsync(string imageUrl, CancellationToken cancellationToken = default) =>
        Task.FromResult<byte[]?>(null);
}
