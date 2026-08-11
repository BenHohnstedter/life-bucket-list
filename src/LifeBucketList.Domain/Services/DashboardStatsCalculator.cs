using LifeBucketList.Domain.Models;

namespace LifeBucketList.Domain.Services;

/// <summary>Pure aggregation of entries into simple dashboard metrics.</summary>
public static class DashboardStatsCalculator
{
    public static DashboardStats Compute(IReadOnlyList<Category> categories, IReadOnlyList<Entry> entries)
    {
        var perCategory = categories
            .OrderBy(c => c.SortOrder)
            .Select(category =>
            {
                var categoryEntries = entries.Where(e => e.CategoryId == category.Id).ToList();
                var ratings = categoryEntries.Where(e => e.Rating.HasValue).Select(e => (double)e.Rating!.Value).ToList();
                double? average = ratings.Count > 0 ? ratings.Average() : null;
                return new CategoryStat(category.Id, category.Name, categoryEntries.Count, average);
            })
            .ToList();

        var allRatings = entries.Where(e => e.Rating.HasValue).Select(e => (double)e.Rating!.Value).ToList();
        double? overallAverage = allRatings.Count > 0 ? allRatings.Average() : null;

        return new DashboardStats(entries.Count, overallAverage, perCategory);
    }
}
