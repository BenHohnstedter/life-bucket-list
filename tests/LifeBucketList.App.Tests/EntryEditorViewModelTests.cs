using Avalonia.Headless.XUnit;
using LifeBucketList.App.ViewModels;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;
using Xunit;

namespace LifeBucketList.App.Tests;

public class EntryEditorViewModelTests
{
    private static Category MakeCategory(string name) => new() { Id = Guid.NewGuid(), Name = name, SortOrder = 0 };

    private static EntryEditorViewModel CreateEditor(IReadOnlyList<Category> categories, Category preselected, Entry? existingEntry = null) =>
        new(categories, existingEntry, preselected, new FakeCoverSearchService(), new FakeCoverImageCache());

    [AvaloniaTheory]
    [InlineData(DefaultCategories.Movies, "z. B. Inception")]
    [InlineData(DefaultCategories.Series, "z. B. Breaking Bad")]
    [InlineData(DefaultCategories.VideoGames, "z. B. The Legend of Zelda")]
    [InlineData(DefaultCategories.Destinations, "z. B. Kyoto, Japan")]
    [InlineData(DefaultCategories.Activities, "z. B. Bungee Jumping")]
    public void TitlePlaceholder_MatchesPreselectedCategory(string categoryName, string expectedPlaceholder)
    {
        var category = MakeCategory(categoryName);
        var editor = CreateEditor(new[] { category }, category);

        Assert.Equal(expectedPlaceholder, editor.TitlePlaceholder);
    }

    [AvaloniaFact]
    public void TitlePlaceholder_UpdatesWhenSelectedCategoryChanges()
    {
        var movies = MakeCategory(DefaultCategories.Movies);
        var games = MakeCategory(DefaultCategories.VideoGames);
        var editor = CreateEditor(new[] { movies, games }, movies);

        Assert.Equal("z. B. Inception", editor.TitlePlaceholder);

        editor.SelectedCategory = games;

        Assert.Equal("z. B. The Legend of Zelda", editor.TitlePlaceholder);
    }

    [AvaloniaFact]
    public void TitlePlaceholder_UpdateNotification_FiresForBoundProperty()
    {
        var movies = MakeCategory(DefaultCategories.Movies);
        var games = MakeCategory(DefaultCategories.VideoGames);
        var editor = CreateEditor(new[] { movies, games }, movies);

        var raisedProperties = new List<string?>();
        editor.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName);

        editor.SelectedCategory = games;

        Assert.Contains(nameof(EntryEditorViewModel.TitlePlaceholder), raisedProperties);
    }

    [AvaloniaTheory]
    [InlineData(DefaultCategories.Movies, true)]
    [InlineData(DefaultCategories.Series, true)]
    [InlineData(DefaultCategories.VideoGames, true)]
    [InlineData(DefaultCategories.Concerts, true)]
    [InlineData(DefaultCategories.Destinations, false)]
    [InlineData(DefaultCategories.Activities, false)]
    public void ShowCoverSearch_OnlyTrueForMediaBackedCategories(string categoryName, bool expected)
    {
        var category = MakeCategory(categoryName);
        var editor = CreateEditor(new[] { category }, category);

        Assert.Equal(expected, editor.ShowCoverSearch);
    }

    [AvaloniaFact]
    public void ShowCoverSearch_FalseForConcerts_WhenInFestivalMode()
    {
        var category = MakeCategory(DefaultCategories.Concerts);
        var editor = CreateEditor(new[] { category }, category);

        Assert.True(editor.ShowCoverSearch);

        editor.IsFestivalMode = true;

        Assert.False(editor.ShowCoverSearch);
    }

    [AvaloniaTheory]
    [InlineData(DefaultCategories.Concerts, true)]
    [InlineData(DefaultCategories.Movies, false)]
    [InlineData(DefaultCategories.Destinations, false)]
    public void ShowRegionPicker_OnlyTrueForConcerts(string categoryName, bool expected)
    {
        var category = MakeCategory(categoryName);
        var editor = CreateEditor(new[] { category }, category);

        Assert.Equal(expected, editor.ShowRegionPicker);
    }

    [AvaloniaFact]
    public void TitlePlaceholder_DiffersBetweenArtistAndFestivalMode()
    {
        var category = MakeCategory(DefaultCategories.Concerts);
        var editor = CreateEditor(new[] { category }, category);

        Assert.Equal("z. B. Peter Fox", editor.TitlePlaceholder);

        editor.IsFestivalMode = true;

        Assert.Equal("z. B. Rock am Ring", editor.TitlePlaceholder);
    }

    [AvaloniaFact]
    public void Constructor_WithExistingConcertEntry_HydratesVenueRegionAndFestivalMode()
    {
        var category = MakeCategory(DefaultCategories.Concerts);
        var existingEntry = new Entry
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Title = "Wacken Open Air",
            Venue = "Wacken",
            RegionCode = "DE-SH",
            IsFestival = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var editor = CreateEditor(new[] { category }, category, existingEntry);

        Assert.Equal("Wacken", editor.Venue);
        Assert.Equal("DE-SH", editor.SelectedRegion?.Code);
        Assert.True(editor.IsFestivalMode);
    }

    [AvaloniaFact]
    public async Task SearchCoverCommand_NoApiKey_SetsMessageAndNoResults()
    {
        var category = MakeCategory(DefaultCategories.Movies);
        var editor = new EntryEditorViewModel(
            new[] { category },
            existingEntry: null,
            category,
            new FakeCoverSearchService { Handler = (_, _) => CoverSearchOutcome.NoApiKey },
            new FakeCoverImageCache())
        {
            Title = "Inception",
        };

        await editor.SearchCoverCommand.ExecuteAsync(null);

        Assert.Empty(editor.CoverSearchResults);
        Assert.Contains("API-Key", editor.CoverSearchMessage);
    }

    [AvaloniaFact]
    public async Task SearchCoverCommand_WithResults_PopulatesCoverSearchResults()
    {
        var category = MakeCategory(DefaultCategories.Movies);
        var results = new[] { new MediaSearchResult("Inception", "2010", "https://example.com/poster.jpg") };
        var editor = new EntryEditorViewModel(
            new[] { category },
            existingEntry: null,
            category,
            new FakeCoverSearchService { Handler = (_, _) => CoverSearchOutcome.Success(results) },
            new FakeCoverImageCache())
        {
            Title = "Inception",
        };

        await editor.SearchCoverCommand.ExecuteAsync(null);

        var result = Assert.Single(editor.CoverSearchResults);
        Assert.Equal("Inception (2010)", result.DisplayTitle);
        Assert.Null(editor.CoverSearchMessage);
    }

    [AvaloniaFact]
    public async Task SelectCoverCommand_SetsCoverImageUrlFromResult()
    {
        var category = MakeCategory(DefaultCategories.Movies);
        var results = new[] { new MediaSearchResult("Inception", "2010", "https://example.com/poster.jpg") };
        var editor = new EntryEditorViewModel(
            new[] { category },
            existingEntry: null,
            category,
            new FakeCoverSearchService { Handler = (_, _) => CoverSearchOutcome.Success(results) },
            new FakeCoverImageCache())
        {
            Title = "Inception",
        };
        await editor.SearchCoverCommand.ExecuteAsync(null);

        editor.SelectCoverCommand.Execute(editor.CoverSearchResults[0]);

        Assert.Equal("https://example.com/poster.jpg", editor.CoverImageUrl);
    }

    [AvaloniaFact]
    public void ClearCoverCommand_ResetsCoverImageUrlAndPreview()
    {
        var category = MakeCategory(DefaultCategories.Movies);
        var editor = CreateEditor(new[] { category }, category);
        editor.CoverImageUrl = "https://example.com/poster.jpg";

        editor.ClearCoverCommand.Execute(null);

        Assert.Null(editor.CoverImageUrl);
        Assert.Null(editor.CoverPreview);
    }

    [AvaloniaFact]
    public void AvailableCountries_AreSortedAlphabeticallyByGermanName()
    {
        var category = MakeCategory(DefaultCategories.Destinations);
        var editor = CreateEditor(new[] { category }, category);

        var names = editor.AvailableCountries.Select(c => c.Name).ToList();
        var expected = names.OrderBy(n => n, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("de-DE"), ignoreCase: false)).ToList();

        Assert.Equal(expected, names);
        // Sanity check: not just accidentally already-sorted input data.
        Assert.NotEqual(WorldCountries.All.Select(c => c.Name).ToList(), names);
    }

    [AvaloniaFact]
    public void AvailableRegions_AreSortedAlphabeticallyByGermanName()
    {
        var category = MakeCategory(DefaultCategories.Concerts);
        var editor = CreateEditor(new[] { category }, category);

        var names = editor.AvailableRegions.Select(r => r.Name).ToList();
        var expected = names.OrderBy(n => n, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("de-DE"), ignoreCase: false)).ToList();

        Assert.Equal(expected, names);
        Assert.NotEqual(GermanRegions.All.Select(r => r.Name).ToList(), names);
    }
}
