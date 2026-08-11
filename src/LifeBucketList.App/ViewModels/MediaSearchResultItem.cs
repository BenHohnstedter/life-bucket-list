using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;

namespace LifeBucketList.App.ViewModels;

/// <summary>One selectable cover-search result in the entry editor. The thumbnail is loaded
/// asynchronously after construction.</summary>
public sealed partial class MediaSearchResultItem : ObservableObject
{
    public MediaSearchResult Source { get; }

    public string DisplayTitle => Source.Year is null ? Source.Title : $"{Source.Title} ({Source.Year})";

    [ObservableProperty]
    private Bitmap? _thumbnailBitmap;

    public MediaSearchResultItem(MediaSearchResult source)
    {
        Source = source;
    }

    public async Task LoadThumbnailAsync(ICoverImageCache coverImageCache)
    {
        if (Source.ThumbnailUrl is not { } url)
        {
            return;
        }

        var bytes = await coverImageCache.GetOrDownloadAsync(url);
        if (bytes is null)
        {
            return;
        }

        using var stream = new MemoryStream(bytes);
        ThumbnailBitmap = new Bitmap(stream);
    }
}
