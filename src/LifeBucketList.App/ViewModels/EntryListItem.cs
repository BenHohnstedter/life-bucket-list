using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;

namespace LifeBucketList.App.ViewModels;

/// <summary>Read-only, display-ready wrapper around an <see cref="Entry"/> for the entry list.
/// The cover thumbnail, if any, is loaded asynchronously after construction.</summary>
public sealed partial class EntryListItem : ObservableObject
{
    public Entry Source { get; }
    public string Title { get; }
    public string DateText { get; }
    public int RatingValue { get; }
    public string? Note { get; }
    public string? Venue { get; }
    public bool HasCover { get; }

    [ObservableProperty]
    private Bitmap? _coverBitmap;

    private EntryListItem(Entry source, string title, string dateText, int ratingValue, string? note, string? venue)
    {
        Source = source;
        Title = title;
        DateText = dateText;
        RatingValue = ratingValue;
        Note = note;
        Venue = venue;
        HasCover = source.CoverImageUrl is not null;
    }

    public static EntryListItem From(Entry entry) => new(
        entry,
        entry.Title,
        entry.OccurredOn is { } date ? date.ToString("dd.MM.yyyy") : "Kein Datum",
        entry.Rating ?? 0,
        entry.Note,
        entry.Venue);

    /// <summary>Fire-and-forget from the caller; never throws (the cache itself swallows failures).</summary>
    public async Task LoadCoverAsync(ICoverImageCache coverImageCache)
    {
        if (Source.CoverImageUrl is not { } url)
        {
            return;
        }

        var bytes = await coverImageCache.GetOrDownloadAsync(url);
        if (bytes is null)
        {
            return;
        }

        using var stream = new MemoryStream(bytes);
        CoverBitmap = new Bitmap(stream);
    }
}
