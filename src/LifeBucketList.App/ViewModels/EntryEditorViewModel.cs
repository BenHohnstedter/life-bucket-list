using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;

namespace LifeBucketList.App.ViewModels;

/// <summary>Backs the add/edit-entry dialog. Holds UI-friendly types; translation to/from the
/// domain <see cref="Entry"/> happens in <see cref="MainWindowViewModel"/>.</summary>
public partial class EntryEditorViewModel : ViewModelBase
{
    private static readonly TimeSpan DefaultSearchDebounceDelay = TimeSpan.FromMilliseconds(500);
    private static readonly StringComparer GermanNameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("de-DE"), ignoreCase: false);

    private readonly ICoverSearchService _coverSearchService;
    private readonly ICoverImageCache _coverImageCache;
    private readonly TimeSpan _searchDebounceDelay;
    private CancellationTokenSource? _activeSearchCts;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitlePlaceholder))]
    [NotifyPropertyChangedFor(nameof(ShowCoverSearch))]
    [NotifyPropertyChangedFor(nameof(ShowCountryPicker))]
    [NotifyPropertyChangedFor(nameof(ShowRegionPicker))]
    private Category? _selectedCategory;

    [ObservableProperty]
    private DateTimeOffset? _occurredOn;

    [ObservableProperty]
    private int _rating;

    [ObservableProperty]
    private string? _note;

    [ObservableProperty]
    private string? _coverImageUrl;

    [ObservableProperty]
    private Bitmap? _coverPreview;

    [ObservableProperty]
    private bool _isSearchingCover;

    [ObservableProperty]
    private string? _coverSearchMessage;

    [ObservableProperty]
    private CountryInfo? _selectedCountry;

    [ObservableProperty]
    private GermanRegionInfo? _selectedRegion;

    [ObservableProperty]
    private string? _venue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCoverSearch))]
    [NotifyPropertyChangedFor(nameof(TitlePlaceholder))]
    private bool _isFestivalMode;

    public ObservableCollection<MediaSearchResultItem> CoverSearchResults { get; } = new();

    public IReadOnlyList<Category> AvailableCategories { get; }

    public IReadOnlyList<CountryInfo> AvailableCountries { get; } =
        WorldCountries.All.OrderBy(c => c.Name, GermanNameComparer).ToList();

    public IReadOnlyList<GermanRegionInfo> AvailableRegions { get; } =
        GermanRegions.All.OrderBy(r => r.Name, GermanNameComparer).ToList();

    public string WindowTitle { get; }

    /// <summary>Example title matching the selected category, shown as the title field's watermark.</summary>
    public string TitlePlaceholder => SelectedCategory?.Name switch
    {
        DefaultCategories.Movies => "z. B. Inception",
        DefaultCategories.Series => "z. B. Breaking Bad",
        DefaultCategories.VideoGames => "z. B. The Legend of Zelda",
        DefaultCategories.Destinations => "z. B. Kyoto, Japan",
        DefaultCategories.Activities => "z. B. Bungee Jumping",
        DefaultCategories.Concerts when IsFestivalMode => "z. B. Rock am Ring",
        DefaultCategories.Concerts => "z. B. Peter Fox",
        _ => "Titel eingeben…",
    };

    /// <summary>Cover search only makes sense for categories backed by a media database — and for
    /// Konzerte, only in artist mode, since a festival has no single performer to search Spotify for.</summary>
    public bool ShowCoverSearch => SelectedCategory is not null &&
        DefaultCategories.CoverSearchEnabledCategories.Contains(SelectedCategory.Name) &&
        !(SelectedCategory.Name == DefaultCategories.Concerts && IsFestivalMode);

    /// <summary>The country picker (for the world map) only applies to Reiseziele.</summary>
    public bool ShowCountryPicker => SelectedCategory?.Name == DefaultCategories.Destinations;

    /// <summary>The Bundesland/DACH region picker (for the Konzerte map) only applies to Konzerte.</summary>
    public bool ShowRegionPicker => SelectedCategory?.Name == DefaultCategories.Concerts;

    public EntryEditorViewModel(
        IReadOnlyList<Category> availableCategories,
        Entry? existingEntry,
        Category preselectedCategory,
        ICoverSearchService coverSearchService,
        ICoverImageCache coverImageCache,
        TimeSpan? searchDebounceDelay = null)
    {
        _coverSearchService = coverSearchService;
        _coverImageCache = coverImageCache;
        _searchDebounceDelay = searchDebounceDelay ?? DefaultSearchDebounceDelay;
        AvailableCategories = availableCategories;
        WindowTitle = existingEntry is null ? "Neuer Eintrag" : "Eintrag bearbeiten";

        if (existingEntry is null)
        {
            SelectedCategory = preselectedCategory;
            return;
        }

        Title = existingEntry.Title;
        SelectedCategory = availableCategories.FirstOrDefault(c => c.Id == existingEntry.CategoryId) ?? preselectedCategory;
        OccurredOn = existingEntry.OccurredOn is { } date ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue)) : null;
        Rating = existingEntry.Rating ?? 0;
        Note = existingEntry.Note;
        CoverImageUrl = existingEntry.CoverImageUrl;
        SelectedCountry = WorldCountries.FindByCode(existingEntry.CountryCode);
        SelectedRegion = GermanRegions.FindByCode(existingEntry.RegionCode);
        Venue = existingEntry.Venue;
        IsFestivalMode = existingEntry.IsFestival ?? false;

        if (CoverImageUrl is not null)
        {
            _ = LoadCoverPreviewAsync(CoverImageUrl);
        }
    }

    partial void OnTitleChanged(string value) => ScheduleDebouncedSearch();

    partial void OnSelectedCategoryChanged(Category? value) => ScheduleDebouncedSearch();

    partial void OnIsFestivalModeChanged(bool value) => ScheduleDebouncedSearch();

    /// <summary>Manual "Cover suchen" button: searches immediately, without waiting for the debounce.</summary>
    [RelayCommand]
    private async Task SearchCoverAsync()
    {
        _activeSearchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _activeSearchCts = cts;

        try
        {
            await ExecuteCoverSearchAsync(cts);
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>Live search: (re)starts a debounce timer whenever the title or category changes, so
    /// results appear automatically a moment after the user stops typing — no click required.</summary>
    private void ScheduleDebouncedSearch()
    {
        _activeSearchCts?.Cancel();

        if (string.IsNullOrWhiteSpace(Title) || !ShowCoverSearch)
        {
            _activeSearchCts = null;
            IsSearchingCover = false;
            CoverSearchResults.Clear();
            CoverSearchMessage = null;
            return;
        }

        var cts = new CancellationTokenSource();
        _activeSearchCts = cts;
        _ = RunDebouncedSearchAsync(cts);
    }

    private async Task RunDebouncedSearchAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_searchDebounceDelay, cts.Token);
            await ExecuteCoverSearchAsync(cts);
        }
        catch (OperationCanceledException)
        {
            // Superseded by newer input before the debounce elapsed, or while the request was
            // in flight — not an error, just nothing to show for this particular attempt.
        }
    }

    private async Task ExecuteCoverSearchAsync(CancellationTokenSource cts)
    {
        if (string.IsNullOrWhiteSpace(Title) || MapToMediaKind(SelectedCategory?.Name) is not { } kind)
        {
            return;
        }

        IsSearchingCover = true;
        CoverSearchResults.Clear();
        CoverSearchMessage = null;

        var outcome = await _coverSearchService.SearchAsync(kind, Title, cts.Token);

        if (_activeSearchCts != cts)
        {
            // A newer search has already taken over; this result is stale, discard it.
            return;
        }

        CoverSearchMessage = outcome.Status switch
        {
            CoverSearchStatus.ApiKeyMissing => "Kein API-Key hinterlegt (siehe README für eine Anleitung).",
            CoverSearchStatus.RequestFailed => "Cover-Suche momentan nicht erreichbar.",
            CoverSearchStatus.Success when outcome.Results.Count == 0 => "Keine Treffer gefunden.",
            _ => null,
        };

        foreach (var result in outcome.Results)
        {
            var item = new MediaSearchResultItem(result);
            CoverSearchResults.Add(item);
            _ = item.LoadThumbnailAsync(_coverImageCache);
        }

        IsSearchingCover = false;
    }

    [RelayCommand]
    private void SelectCover(MediaSearchResultItem result)
    {
        CoverImageUrl = result.Source.ThumbnailUrl;
        CoverPreview = result.ThumbnailBitmap;
    }

    [RelayCommand]
    private void ClearCover()
    {
        CoverImageUrl = null;
        CoverPreview = null;
    }

    private async Task LoadCoverPreviewAsync(string url)
    {
        var bytes = await _coverImageCache.GetOrDownloadAsync(url);
        if (bytes is null)
        {
            return;
        }

        using var stream = new MemoryStream(bytes);
        CoverPreview = new Bitmap(stream);
    }

    private static MediaKind? MapToMediaKind(string? categoryName) => categoryName switch
    {
        DefaultCategories.Movies => MediaKind.Movie,
        DefaultCategories.Series => MediaKind.Series,
        DefaultCategories.VideoGames => MediaKind.VideoGame,
        DefaultCategories.Concerts => MediaKind.Artist,
        _ => null,
    };
}
