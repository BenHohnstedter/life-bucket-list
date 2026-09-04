using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeBucketList.App.Services;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Repositories;
using LifeBucketList.Domain.Services;

namespace LifeBucketList.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IEntryRepository _entryRepository;
    private readonly IBackupService _backupService;
    private readonly IDialogService _dialogService;
    private readonly ICoverSearchService _coverSearchService;
    private readonly ICoverImageCache _coverImageCache;

    private List<Entry> _allEntries = new();

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<EntryListItem> DisplayedEntries { get; } = new();
    public ObservableCollection<CategoryStatRow> DashboardRows { get; } = new();
    public ObservableCollection<string> VisitedCountryCodes { get; } = new();
    public ObservableCollection<string> VisitedRegionCodes { get; } = new();

    public IReadOnlyList<SortOptionItem> SortOptions => SortOptionItem.All;
    public IReadOnlyList<RatingFilterItem> RatingFilters => RatingFilterItem.All;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDestinationsCategorySelected))]
    [NotifyPropertyChangedFor(nameof(IsConcertsCategorySelected))]
    private Category? _selectedCategory;

    /// <summary>Whether the Reiseziele category is showing, so the world map can be displayed.</summary>
    public bool IsDestinationsCategorySelected => SelectedCategory?.Name == DefaultCategories.Destinations;

    /// <summary>Whether the Konzerte category is showing, so the DACH region map can be displayed.</summary>
    public bool IsConcertsCategorySelected => SelectedCategory?.Name == DefaultCategories.Concerts;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private RatingFilterItem _selectedRatingFilter = RatingFilterItem.All[0];

    [ObservableProperty]
    private SortOptionItem _selectedSortOption = SortOptionItem.All[0];

    [ObservableProperty]
    private bool _isDashboardSelected;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private int _dashboardTotalEntries;

    [ObservableProperty]
    private string _dashboardOverallAverageText = "–";

    [ObservableProperty]
    private bool _hasNoDisplayedEntries;

    public MainWindowViewModel(
        ICategoryRepository categoryRepository,
        IEntryRepository entryRepository,
        IBackupService backupService,
        IDialogService dialogService,
        ICoverSearchService coverSearchService,
        ICoverImageCache coverImageCache)
    {
        _categoryRepository = categoryRepository;
        _entryRepository = entryRepository;
        _backupService = backupService;
        _dialogService = dialogService;
        _coverSearchService = coverSearchService;
        _coverImageCache = coverImageCache;
    }

    public async Task InitializeAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        Categories.Clear();
        foreach (var category in categories.OrderBy(c => c.SortOrder))
        {
            Categories.Add(category);
        }

        _allEntries = (await _entryRepository.GetAllAsync()).ToList();
        SelectedCategory = Categories.FirstOrDefault();
        RefreshDisplayedEntries();
        RefreshDashboard();
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (value is not null)
        {
            IsDashboardSelected = false;
        }

        RefreshDisplayedEntries();
    }

    partial void OnSearchTextChanged(string value) => RefreshDisplayedEntries();

    partial void OnSelectedRatingFilterChanged(RatingFilterItem value) => RefreshDisplayedEntries();

    partial void OnSelectedSortOptionChanged(SortOptionItem value) => RefreshDisplayedEntries();

    [RelayCommand]
    private void SelectDashboard()
    {
        SelectedCategory = null;
        IsDashboardSelected = true;
        RefreshDashboard();
    }

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        var category = SelectedCategory ?? Categories.FirstOrDefault();
        if (category is null)
        {
            return;
        }

        var editor = new EntryEditorViewModel(Categories.ToList(), existingEntry: null, preselectedCategory: category, _coverSearchService, _coverImageCache);
        if (!await _dialogService.ShowEntryEditorAsync(editor))
        {
            return;
        }

        try
        {
            var entry = BuildEntryFromEditor(editor, Guid.NewGuid(), DateTimeOffset.UtcNow);
            await _entryRepository.AddAsync(entry);
            _allEntries.Add(entry);
            RefreshDisplayedEntries();
            RefreshDashboard();
        }
        catch (DomainValidationException ex)
        {
            await _dialogService.ShowErrorAsync("Ungültige Eingabe", ex.Message);
        }
    }

    [RelayCommand]
    private async Task EditEntryAsync(Entry entry)
    {
        var editor = new EntryEditorViewModel(Categories.ToList(), entry, SelectedCategory ?? Categories.First(), _coverSearchService, _coverImageCache);
        if (!await _dialogService.ShowEntryEditorAsync(editor))
        {
            return;
        }

        try
        {
            var updated = BuildEntryFromEditor(editor, entry.Id, entry.CreatedAt);
            await _entryRepository.UpdateAsync(updated);

            var index = _allEntries.FindIndex(e => e.Id == entry.Id);
            if (index >= 0)
            {
                _allEntries[index] = updated;
            }

            RefreshDisplayedEntries();
            RefreshDashboard();
        }
        catch (DomainValidationException ex)
        {
            await _dialogService.ShowErrorAsync("Ungültige Eingabe", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(Entry entry)
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Eintrag löschen",
            $"Möchtest du \"{entry.Title}\" wirklich löschen?");
        if (!confirmed)
        {
            return;
        }

        await _entryRepository.DeleteAsync(entry.Id);
        _allEntries.RemoveAll(e => e.Id == entry.Id);
        RefreshDisplayedEntries();
        RefreshDashboard();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var suggestedName = $"life-bucket-list-backup-{DateTime.Now:yyyy-MM-dd}.json";
        var path = await _dialogService.PickExportFilePathAsync(suggestedName);
        if (path is null)
        {
            return;
        }

        try
        {
            var data = new BackupData(DateTimeOffset.UtcNow, Categories.ToList(), _allEntries.ToList());
            await using var stream = File.Create(path);
            await _backupService.ExportAsync(stream, data);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Export fehlgeschlagen", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var path = await _dialogService.PickImportFilePathAsync();
        if (path is null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Backup importieren",
            "Der Import ersetzt alle aktuell gespeicherten Einträge durch den Inhalt der gewählten Datei. Fortfahren?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            BackupData data;
            await using (var stream = File.OpenRead(path))
            {
                data = await _backupService.ImportAsync(stream);
            }

            var (entriesToImport, skippedCount) = RemapEntriesToCurrentCategories(data);

            foreach (var entry in await _entryRepository.GetAllAsync())
            {
                await _entryRepository.DeleteAsync(entry.Id);
            }

            foreach (var entry in entriesToImport)
            {
                await _entryRepository.AddAsync(entry);
            }

            await InitializeAsync();

            if (skippedCount > 0)
            {
                await _dialogService.ShowErrorAsync(
                    "Hinweis",
                    $"{skippedCount} Einträge aus nicht mehr vorhandenen Kategorien wurden beim Import übersprungen.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Import fehlgeschlagen", ex.Message);
        }
    }

    /// <summary>Categories are fixed, but a backup's category Ids are only meaningful within the
    /// database it was exported from. Entries are therefore matched to the current, local
    /// categories by name instead (with "Spiele" understood as the old name for "Videospiele").</summary>
    private (List<Entry> Entries, int SkippedCount) RemapEntriesToCurrentCategories(BackupData data)
    {
        var backupCategoryNameById = data.Categories.ToDictionary(c => c.Id, c => NormalizeCategoryName(c.Name));
        var localCategoryIdByName = Categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        var remapped = new List<Entry>();
        var skipped = 0;

        foreach (var entry in data.Entries)
        {
            if (backupCategoryNameById.TryGetValue(entry.CategoryId, out var categoryName) &&
                localCategoryIdByName.TryGetValue(categoryName, out var localCategoryId))
            {
                entry.CategoryId = localCategoryId;
                remapped.Add(entry);
            }
            else
            {
                skipped++;
            }
        }

        return (remapped, skipped);
    }

    private static string NormalizeCategoryName(string name) => name == "Spiele" ? DefaultCategories.VideoGames : name;

    private static Entry BuildEntryFromEditor(EntryEditorViewModel editor, Guid id, DateTimeOffset createdAt)
    {
        var title = EntryValidator.ValidateTitle(editor.Title);
        var note = EntryValidator.ValidateNote(editor.Note);
        int? rating = editor.Rating == 0 ? null : editor.Rating;
        if (rating.HasValue)
        {
            EntryValidator.ValidateRating(rating);
        }

        var categoryId = editor.SelectedCategory?.Id
            ?? throw new DomainValidationException("Bitte wähle eine Kategorie aus.");
        var isConcerts = editor.SelectedCategory?.Name == DefaultCategories.Concerts;

        return new Entry
        {
            Id = id,
            CategoryId = categoryId,
            Title = title,
            OccurredOn = editor.OccurredOn is { } date ? DateOnly.FromDateTime(date.DateTime) : null,
            Rating = rating,
            Note = note,
            CoverImageUrl = editor.CoverImageUrl,
            CountryCode = editor.SelectedCountry?.Code,
            Venue = isConcerts ? editor.Venue : null,
            RegionCode = isConcerts ? editor.SelectedRegion?.Code : null,
            IsFestival = isConcerts ? editor.IsFestivalMode : null,
            CreatedAt = createdAt,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    private void RefreshDisplayedEntries()
    {
        DisplayedEntries.Clear();
        if (SelectedCategory is null)
        {
            return;
        }

        var filter = new EntryFilter(SelectedCategory.Id, SearchText, SelectedRatingFilter.MinRating);
        var results = EntryQuery.Apply(_allEntries, filter, SelectedSortOption.Value);
        foreach (var entry in results)
        {
            var item = EntryListItem.From(entry);
            DisplayedEntries.Add(item);
            if (item.HasCover)
            {
                _ = item.LoadCoverAsync(_coverImageCache);
            }
        }

        HasNoDisplayedEntries = DisplayedEntries.Count == 0;
    }

    private void RefreshDashboard()
    {
        var stats = DashboardStatsCalculator.Compute(Categories.ToList(), _allEntries);
        DashboardTotalEntries = stats.TotalEntries;
        DashboardOverallAverageText = stats.OverallAverageRating is { } avg ? $"{avg:0.0} ★" : "–";

        var maxCount = stats.PerCategory.Count > 0 ? Math.Max(1, stats.PerCategory.Max(c => c.EntryCount)) : 1;

        DashboardRows.Clear();
        foreach (var stat in stats.PerCategory)
        {
            var ratingText = stat.AverageRating is { } rating ? $"{rating:0.0} ★" : "–";
            DashboardRows.Add(new CategoryStatRow(stat.CategoryName, stat.EntryCount, ratingText, (double)stat.EntryCount / maxCount));
        }

        RefreshVisitedCountries();
        RefreshVisitedRegions();
    }

    /// <summary>Recomputes which countries to highlight on the Reiseziele map, from all entries in
    /// that category regardless of the current search/filter (the map is meant as a full overview).</summary>
    private void RefreshVisitedCountries()
    {
        VisitedCountryCodes.Clear();

        var destinationsCategoryId = Categories.FirstOrDefault(c => c.Name == DefaultCategories.Destinations)?.Id;
        if (destinationsCategoryId is null)
        {
            return;
        }

        var codes = _allEntries
            .Where(e => e.CategoryId == destinationsCategoryId && e.CountryCode is not null)
            .Select(e => e.CountryCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var code in codes)
        {
            VisitedCountryCodes.Add(code);
        }
    }

    /// <summary>Recomputes which German/DACH regions to highlight on the Konzerte map, from all
    /// entries in that category regardless of the current search/filter.</summary>
    private void RefreshVisitedRegions()
    {
        VisitedRegionCodes.Clear();

        var concertsCategoryId = Categories.FirstOrDefault(c => c.Name == DefaultCategories.Concerts)?.Id;
        if (concertsCategoryId is null)
        {
            return;
        }

        var codes = _allEntries
            .Where(e => e.CategoryId == concertsCategoryId && e.RegionCode is not null)
            .Select(e => e.RegionCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var code in codes)
        {
            VisitedRegionCodes.Add(code);
        }
    }
}
