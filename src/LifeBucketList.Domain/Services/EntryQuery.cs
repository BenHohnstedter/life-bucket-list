using LifeBucketList.Domain.Models;

namespace LifeBucketList.Domain.Services;

/// <summary>Pure, testable filtering and sorting of entry lists (no I/O).</summary>
public static class EntryQuery
{
    public static IReadOnlyList<Entry> Apply(IEnumerable<Entry> entries, EntryFilter filter, EntrySortOption sort)
    {
        var query = entries.AsEnumerable();

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(e => e.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var needle = filter.SearchText.Trim();
            query = query.Where(e =>
                e.Title.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                (e.Note is not null && e.Note.Contains(needle, StringComparison.OrdinalIgnoreCase)));
        }

        if (filter.MinRating is { } minRating)
        {
            query = query.Where(e => e.Rating is { } r && r >= minRating);
        }

        return Sort(query, sort).ToList();
    }

    private static IEnumerable<Entry> Sort(IEnumerable<Entry> entries, EntrySortOption sort) => sort switch
    {
        EntrySortOption.DateDescending => entries.OrderByDescending(e => e.OccurredOn.HasValue).ThenByDescending(e => e.OccurredOn),
        EntrySortOption.DateAscending => entries.OrderByDescending(e => e.OccurredOn.HasValue).ThenBy(e => e.OccurredOn),
        EntrySortOption.RatingDescending => entries.OrderByDescending(e => e.Rating.HasValue).ThenByDescending(e => e.Rating),
        EntrySortOption.RatingAscending => entries.OrderByDescending(e => e.Rating.HasValue).ThenBy(e => e.Rating),
        EntrySortOption.TitleAscending => entries.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase),
        EntrySortOption.TitleDescending => entries.OrderByDescending(e => e.Title, StringComparer.OrdinalIgnoreCase),
        _ => throw new ArgumentOutOfRangeException(nameof(sort)),
    };
}
