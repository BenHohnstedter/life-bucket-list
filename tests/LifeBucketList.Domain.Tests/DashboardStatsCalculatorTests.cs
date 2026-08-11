using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;
using Xunit;

namespace LifeBucketList.Domain.Tests;

public class DashboardStatsCalculatorTests
{
    [Fact]
    public void Compute_ReturnsZeroedStats_ForEmptyData()
    {
        var stats = DashboardStatsCalculator.Compute(Array.Empty<Category>(), Array.Empty<Entry>());

        Assert.Equal(0, stats.TotalEntries);
        Assert.Null(stats.OverallAverageRating);
        Assert.Empty(stats.PerCategory);
    }

    [Fact]
    public void Compute_CountsEntriesPerCategoryAndAveragesRatings()
    {
        var movies = new Category { Id = Guid.NewGuid(), Name = "Filme", SortOrder = 0 };
        var games = new Category { Id = Guid.NewGuid(), Name = "Videospiele", SortOrder = 1 };

        var entries = new[]
        {
            MakeEntry(movies.Id, rating: 4),
            MakeEntry(movies.Id, rating: 2),
            MakeEntry(movies.Id, rating: null),
            MakeEntry(games.Id, rating: 5),
        };

        var stats = DashboardStatsCalculator.Compute(new[] { movies, games }, entries);

        Assert.Equal(4, stats.TotalEntries);
        Assert.Equal((4 + 2 + 5) / 3.0, stats.OverallAverageRating);

        var movieStat = stats.PerCategory.Single(s => s.CategoryId == movies.Id);
        Assert.Equal(3, movieStat.EntryCount);
        Assert.Equal(3.0, movieStat.AverageRating);

        var gameStat = stats.PerCategory.Single(s => s.CategoryId == games.Id);
        Assert.Equal(1, gameStat.EntryCount);
        Assert.Equal(5.0, gameStat.AverageRating);
    }

    private static Entry MakeEntry(Guid categoryId, int? rating) => new()
    {
        Id = Guid.NewGuid(),
        CategoryId = categoryId,
        Title = "Test",
        Rating = rating,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };
}
