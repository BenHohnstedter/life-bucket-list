using Xunit;

namespace LifeBucketList.Data.Tests;

public class CoverImageCacheTests : IDisposable
{
    private readonly string _cacheDirectory = Path.Combine(Path.GetTempPath(), $"lbl-covercache-{Guid.NewGuid()}");

    public void Dispose()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task GetOrDownloadAsync_DownloadsAndReturnsBytes()
    {
        var expectedBytes = new byte[] { 1, 2, 3, 4 };
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("cover.jpg", expectedBytes);
        var cache = new CoverImageCache(new HttpClient(handler), _cacheDirectory);

        var bytes = await cache.GetOrDownloadAsync("https://example.com/cover.jpg");

        Assert.Equal(expectedBytes, bytes);
    }

    [Fact]
    public async Task GetOrDownloadAsync_SecondCall_ServesFromDiskCacheWithoutAnotherRequest()
    {
        var handler = new FakeHttpMessageHandler().RespondWhenUrlContains("cover.jpg", new byte[] { 9, 9, 9 });
        var cache = new CoverImageCache(new HttpClient(handler), _cacheDirectory);

        await cache.GetOrDownloadAsync("https://example.com/cover.jpg");
        await cache.GetOrDownloadAsync("https://example.com/cover.jpg");

        Assert.Single(handler.RequestedUrls);
    }

    [Fact]
    public async Task GetOrDownloadAsync_ReturnsNull_WhenDownloadFails()
    {
        var handler = new FakeHttpMessageHandler().ThrowWhenUrlContains("cover.jpg");
        var cache = new CoverImageCache(new HttpClient(handler), _cacheDirectory);

        var bytes = await cache.GetOrDownloadAsync("https://example.com/cover.jpg");

        Assert.Null(bytes);
    }
}
