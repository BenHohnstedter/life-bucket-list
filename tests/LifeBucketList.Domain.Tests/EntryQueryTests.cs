using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;
using Xunit;

namespace LifeBucketList.Domain.Tests;

public class EntryQueryTests
{
    private static readonly Guid MoviesCategory = Guid.NewGuid();
    private static readonly Guid GamesCategory = Guid.NewGuid();

    private static Entry MakeEntry(string title, Guid categoryId, DateOnly? date = null, int? rating = null, string? note = null) => new()
    {
        Id = Guid.NewGuid(),
        CategoryId = categoryId,
        Title = title,
        OccurredOn = date,
        Rating = rating,
        Note = note,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void Apply_FiltersByCategory()
    {
        var entries = new[]
        {
            MakeEntry("Inception", MoviesCategory),
            MakeEntry("Zelda", GamesCategory),
        };

        var result = EntryQuery.Apply(entries, new EntryFilter(CategoryId: MoviesCategory), EntrySortOption.TitleAscending);

        Assert.Single(result);
        Assert.Equal("Inception", result[0].Title);
    }

    [Fact]
    public void Apply_FiltersBySearchTextInTitleOrNote()
    {
        var entries = new[]
        {
            MakeEntry("Inception", MoviesCategory, note: "Mind-bending"),
            MakeEntry("Interstellar", MoviesCategory, note: "Emotional ending"),
            MakeEntry("Zelda", GamesCategory, note: "Open world"),
        };

        var byTitle = EntryQuery.Apply(entries, new EntryFilter(SearchText: "incep"), EntrySortOption.TitleAscending);
        var byNote = EntryQuery.Apply(entries, new EntryFilter(SearchText: "emotional"), EntrySortOption.TitleAscending);

        Assert.Single(byTitle);
        Assert.Equal("Inception", byTitle[0].Title);
        Assert.Single(byNote);
        Assert.Equal("Interstellar", byNote[0].Title);
    }

    [Fact]
    public void Apply_FiltersByMinimumRating()
    {
        var entries = new[]
        {
            MakeEntry("Low", MoviesCategory, rating: 2),
            MakeEntry("High", MoviesCategory, rating: 5),
            MakeEntry("Unrated", MoviesCategory, rating: null),
        };

        var result = EntryQuery.Apply(entries, new EntryFilter(MinRating: 4), EntrySortOption.TitleAscending);

        Assert.Single(result);
        Assert.Equal("High", result[0].Title);
    }

    [Fact]
    public void Apply_SortsByDateDescending_WithUndatedEntriesLast()
    {
        var entries = new[]
        {
            MakeEntry("Old", MoviesCategory, date: new DateOnly(2020, 1, 1)),
            MakeEntry("New", MoviesCategory, date: new DateOnly(2024, 1, 1)),
            MakeEntry("Undated", MoviesCategory, date: null),
        };

        var result = EntryQuery.Apply(entries, new EntryFilter(), EntrySortOption.DateDescending);

        Assert.Equal(new[] { "New", "Old", "Undated" }, result.Select(e => e.Title));
    }

    [Fact]
    public void Apply_SortsByRatingAscending_WithUnratedEntriesLast()
    {
        var entries = new[]
        {
            MakeEntry("Five", MoviesCategory, rating: 5),
            MakeEntry("Two", MoviesCategory, rating: 2),
            MakeEntry("Unrated", MoviesCategory, rating: null),
        };

        var result = EntryQuery.Apply(entries, new EntryFilter(), EntrySortOption.RatingAscending);

        Assert.Equal(new[] { "Two", "Five", "Unrated" }, result.Select(e => e.Title));
    }

    [Fact]
    public void Apply_SortsByTitleAscending_CaseInsensitive()
    {
        var entries = new[]
        {
            MakeEntry("zelda", MoviesCategory),
            MakeEntry("Avatar", MoviesCategory),
        };

        var result = EntryQuery.Apply(entries, new EntryFilter(), EntrySortOption.TitleAscending);

        Assert.Equal(new[] { "Avatar", "zelda" }, result.Select(e => e.Title));
    }

    [Fact]
    public void Apply_CombinesFilterAndSort()
    {
        var entries = new[]
        {
            MakeEntry("Inception", MoviesCategory, rating: 5),
            MakeEntry("Cats", MoviesCategory, rating: 1),
            MakeEntry("Zelda", GamesCategory, rating: 5),
        };

        var result = EntryQuery.Apply(
            entries,
            new EntryFilter(CategoryId: MoviesCategory, MinRating: 3),
            EntrySortOption.TitleAscending);

        Assert.Single(result);
        Assert.Equal("Inception", result[0].Title);
    }
}
