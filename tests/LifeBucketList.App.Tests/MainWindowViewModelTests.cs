using Avalonia.Headless.XUnit;
using LifeBucketList.App.Services;
using LifeBucketList.App.ViewModels;
using LifeBucketList.Data;
using LifeBucketList.Domain.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace LifeBucketList.App.Tests;

/// <summary>End-to-end tests of the main flows (add/edit/delete/rate/filter/categories/backup),
/// driving the real ViewModel against a real SQLite database with a faked dialog service.</summary>
public class MainWindowViewModelTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"lbl-apptest-{Guid.NewGuid()}.db");
    private FakeDialogService _dialogService = null!;
    private MainWindowViewModel _viewModel = null!;

    public async Task InitializeAsync()
    {
        var connectionFactory = new SqliteConnectionFactory(_dbPath);
        await connectionFactory.InitializeAsync();

        _dialogService = new FakeDialogService();
        _viewModel = new MainWindowViewModel(
            new SqliteCategoryRepository(connectionFactory),
            new SqliteEntryRepository(connectionFactory),
            new JsonBackupService(),
            _dialogService,
            new FakeCoverSearchService(),
            new FakeCoverImageCache());

        await _viewModel.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        return Task.CompletedTask;
    }

    [AvaloniaFact]
    public void InitializeAsync_LoadsPredefinedCategories()
    {
        Assert.Equal(DefaultCategories.Names.Count, _viewModel.Categories.Count);
        Assert.Equal(DefaultCategories.Names[0], _viewModel.SelectedCategory?.Name);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void InitializeTheme_SyncsToggleToWhicheverVariantIsActuallyActive(bool startInDarkMode)
    {
        var app = (LifeBucketList.App.App)Avalonia.Application.Current!;
        app.RequestedThemeVariant = startInDarkMode ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;

        app.InitializeTheme(_viewModel);

        Assert.Equal(startInDarkMode, _viewModel.IsDarkTheme);
    }

    [AvaloniaFact]
    public async Task AddEntry_SimulatedUserInput_AppearsInDisplayedEntries()
    {
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "Inception";
            editor.Rating = 5;
            editor.Note = "Grandios";
            return true;
        };

        await _viewModel.AddEntryCommand.ExecuteAsync(null);

        var item = Assert.Single(_viewModel.DisplayedEntries);
        Assert.Equal("Inception", item.Title);
        Assert.Equal(5, item.RatingValue);
        Assert.Empty(_dialogService.Errors);
    }

    [AvaloniaFact]
    public async Task AddEntry_BlankTitle_ShowsValidationErrorAndDoesNotAdd()
    {
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "   ";
            return true;
        };

        await _viewModel.AddEntryCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.DisplayedEntries);
        Assert.Single(_dialogService.Errors);
    }

    [AvaloniaFact]
    public async Task EditEntry_ChangesTitleAndRating()
    {
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "Inception";
            editor.Rating = 3;
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);
        var added = _viewModel.DisplayedEntries[0].Source;

        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "Interstellar";
            editor.Rating = 5;
            return true;
        };
        await _viewModel.EditEntryCommand.ExecuteAsync(added);

        var item = Assert.Single(_viewModel.DisplayedEntries);
        Assert.Equal("Interstellar", item.Title);
        Assert.Equal(5, item.RatingValue);
    }

    [AvaloniaFact]
    public async Task EditEntry_DateSurvivesRoundTripThroughDatePicker_WithoutOffsetDrift()
    {
        var expectedDate = new DateOnly(1995, 12, 24);
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "Weihnachten in den Bergen";
            editor.OccurredOn = new DateTimeOffset(expectedDate.ToDateTime(TimeOnly.MinValue));
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);
        var added = _viewModel.DisplayedEntries[0].Source;
        Assert.Equal(expectedDate, added.OccurredOn);

        // Re-open the same entry for editing and save again without any changes: the date
        // the editor was pre-filled with must still be exactly the same calendar day.
        EntryEditorViewModel? capturedEditor = null;
        _dialogService.EntryEditorHandler = editor =>
        {
            capturedEditor = editor;
            return true;
        };
        await _viewModel.EditEntryCommand.ExecuteAsync(added);

        Assert.Equal(expectedDate, DateOnly.FromDateTime(capturedEditor!.OccurredOn!.Value.DateTime));
        var reloaded = _viewModel.DisplayedEntries[0].Source;
        Assert.Equal(expectedDate, reloaded.OccurredOn);
    }

    [AvaloniaFact]
    public async Task DeleteEntry_WithConfirmation_RemovesEntry()
    {
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "Inception";
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);
        var added = _viewModel.DisplayedEntries[0].Source;

        _dialogService.ConfirmationHandler = (_, _) => true;
        await _viewModel.DeleteEntryCommand.ExecuteAsync(added);

        Assert.Empty(_viewModel.DisplayedEntries);
    }

    [AvaloniaFact]
    public async Task DeleteEntry_WithoutConfirmation_KeepsEntry()
    {
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "Inception";
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);
        var added = _viewModel.DisplayedEntries[0].Source;

        _dialogService.ConfirmationHandler = (_, _) => false;
        await _viewModel.DeleteEntryCommand.ExecuteAsync(added);

        Assert.Single(_viewModel.DisplayedEntries);
    }

    [AvaloniaFact]
    public async Task SearchText_FiltersDisplayedEntries()
    {
        await AddEntryWithTitle("Inception");
        await AddEntryWithTitle("Interstellar");
        await AddEntryWithTitle("Zelda: Breath of the Wild");

        _viewModel.SearchText = "inter";

        var item = Assert.Single(_viewModel.DisplayedEntries);
        Assert.Equal("Interstellar", item.Title);
    }

    [AvaloniaFact]
    public async Task RatingFilter_ShowsOnlyEntriesAtOrAboveMinimum()
    {
        await AddEntryWithTitleAndRating("Low", 2);
        await AddEntryWithTitleAndRating("High", 5);

        _viewModel.SelectedRatingFilter = RatingFilterItem.All.Single(r => r.MinRating == 4);

        var item = Assert.Single(_viewModel.DisplayedEntries);
        Assert.Equal("High", item.Title);
    }

    [AvaloniaFact]
    public async Task ExportThenImport_RoundTripsThroughRealFile()
    {
        await AddEntryWithTitle("Inception");
        var exportPath = Path.Combine(Path.GetTempPath(), $"lbl-export-{Guid.NewGuid()}.json");
        _dialogService.ExportPath = exportPath;

        await _viewModel.ExportCommand.ExecuteAsync(null);
        Assert.True(File.Exists(exportPath));

        // Simulate a second device: fresh empty database, then import the exported file.
        var freshDbPath = Path.Combine(Path.GetTempPath(), $"lbl-apptest-{Guid.NewGuid()}.db");
        var freshFactory = new SqliteConnectionFactory(freshDbPath);
        await freshFactory.InitializeAsync();
        var freshDialogService = new FakeDialogService { ImportPath = exportPath, ConfirmationHandler = (_, _) => true };
        var freshViewModel = new MainWindowViewModel(
            new SqliteCategoryRepository(freshFactory),
            new SqliteEntryRepository(freshFactory),
            new JsonBackupService(),
            freshDialogService,
            new FakeCoverSearchService(),
            new FakeCoverImageCache());
        await freshViewModel.InitializeAsync();

        await freshViewModel.ImportCommand.ExecuteAsync(null);

        Assert.Contains(freshViewModel.DisplayedEntries, e => e.Title == "Inception");

        File.Delete(exportPath);
        SqliteConnection.ClearAllPools();
        File.Delete(freshDbPath);
    }

    [AvaloniaFact]
    public async Task Import_MapsLegacySpieleBackupCategoryToVideospiele()
    {
        var legacyCategory = new Category { Id = Guid.NewGuid(), Name = "Spiele", SortOrder = 0 };
        var legacyEntry = new Entry
        {
            Id = Guid.NewGuid(),
            CategoryId = legacyCategory.Id,
            Title = "Zelda",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var backupPath = await WriteBackupFileAsync(new BackupData(DateTimeOffset.UtcNow, new[] { legacyCategory }, new[] { legacyEntry }));

        _dialogService.ImportPath = backupPath;
        _dialogService.ConfirmationHandler = (_, _) => true;
        await _viewModel.ImportCommand.ExecuteAsync(null);

        var videoGamesCategory = _viewModel.Categories.Single(c => c.Name == DefaultCategories.VideoGames);
        _viewModel.SelectedCategory = videoGamesCategory;
        Assert.Contains(_viewModel.DisplayedEntries, e => e.Title == "Zelda");

        File.Delete(backupPath);
    }

    [AvaloniaFact]
    public async Task Import_SkipsEntriesFromNoLongerExistingCategoryAndReportsCount()
    {
        var unknownCategory = new Category { Id = Guid.NewGuid(), Name = "Meine Alte Kategorie", SortOrder = 0 };
        var orphanEntry = new Entry
        {
            Id = Guid.NewGuid(),
            CategoryId = unknownCategory.Id,
            Title = "Verwaister Eintrag",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var backupPath = await WriteBackupFileAsync(new BackupData(DateTimeOffset.UtcNow, new[] { unknownCategory }, new[] { orphanEntry }));

        _dialogService.ImportPath = backupPath;
        _dialogService.ConfirmationHandler = (_, _) => true;
        await _viewModel.ImportCommand.ExecuteAsync(null);

        Assert.DoesNotContain(_viewModel.DisplayedEntries, e => e.Title == "Verwaister Eintrag");
        Assert.Contains(_dialogService.Errors, e => e.Message.Contains('1'));

        File.Delete(backupPath);
    }

    private static async Task<string> WriteBackupFileAsync(BackupData data)
    {
        var path = Path.Combine(Path.GetTempPath(), $"lbl-backup-{Guid.NewGuid()}.json");
        await using var stream = File.Create(path);
        await new JsonBackupService().ExportAsync(stream, data);
        return path;
    }

    private async Task AddEntryWithTitle(string title)
    {
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = title;
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);
    }

    private async Task AddEntryWithTitleAndRating(string title, int rating)
    {
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = title;
            editor.Rating = rating;
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);
    }

    [AvaloniaFact]
    public async Task AddDestinationEntry_WithCountry_AddsToVisitedCountryCodes()
    {
        await AddDestinationEntry("Sommerurlaub", "FR");

        Assert.Contains("FR", _viewModel.VisitedCountryCodes);
    }

    [AvaloniaFact]
    public async Task AddDestinationEntry_WithoutCountry_DoesNotAffectVisitedCountryCodes()
    {
        await AddEntryWithTitle("Unbestimmter Trip");

        Assert.Empty(_viewModel.VisitedCountryCodes);
    }

    [AvaloniaFact]
    public async Task AddEntry_InNonDestinationCategory_NeverAppearsOnMap_EvenIfCountrySomehowSet()
    {
        // Defensive: the UI only shows the country picker for Reiseziele, but VisitedCountryCodes
        // must be filtered by category, not merely by "has a CountryCode".
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "Inception";
            editor.SelectedCategory = _viewModel.Categories.Single(c => c.Name == DefaultCategories.Movies);
            editor.SelectedCountry = WorldCountries.FindByCode("FR");
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.VisitedCountryCodes);
    }

    [AvaloniaFact]
    public async Task AddTwoDestinationEntries_SameCountry_OnlyCountedOnce()
    {
        await AddDestinationEntry("Paris-Trip", "FR");
        await AddDestinationEntry("Nizza-Trip", "FR");

        Assert.Single(_viewModel.VisitedCountryCodes, code => code == "FR");
    }

    [AvaloniaFact]
    public async Task DeleteDestinationEntry_RemovesCountryFromMap_WhenNoOtherEntryHasIt()
    {
        await AddDestinationEntry("Sommerurlaub", "FR");
        var added = FindEntryByTitle("Sommerurlaub");

        _dialogService.ConfirmationHandler = (_, _) => true;
        await _viewModel.DeleteEntryCommand.ExecuteAsync(added);

        Assert.DoesNotContain("FR", _viewModel.VisitedCountryCodes);
    }

    [AvaloniaFact]
    public async Task EditDestinationEntry_ChangingCountry_UpdatesVisitedCountryCodes()
    {
        await AddDestinationEntry("Reise", "FR");
        var added = FindEntryByTitle("Reise");

        _dialogService.EntryEditorHandler = editor =>
        {
            editor.SelectedCountry = WorldCountries.FindByCode("JP");
            return true;
        };
        await _viewModel.EditEntryCommand.ExecuteAsync(added);

        Assert.DoesNotContain("FR", _viewModel.VisitedCountryCodes);
        Assert.Contains("JP", _viewModel.VisitedCountryCodes);
    }

    private Entry FindEntryByTitle(string title)
    {
        var wasSelected = _viewModel.SelectedCategory;
        var destinations = _viewModel.Categories.Single(c => c.Name == DefaultCategories.Destinations);
        _viewModel.SelectedCategory = destinations;
        var entry = _viewModel.DisplayedEntries.Single(e => e.Title == title).Source;
        _viewModel.SelectedCategory = wasSelected;
        return entry;
    }

    private async Task AddDestinationEntry(string title, string countryCode)
    {
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = title;
            editor.SelectedCategory = _viewModel.Categories.Single(c => c.Name == DefaultCategories.Destinations);
            editor.SelectedCountry = WorldCountries.FindByCode(countryCode);
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);
    }
}
