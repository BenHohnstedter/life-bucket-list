namespace LifeBucketList.Domain.Services;

public sealed record CategoryStat(Guid CategoryId, string CategoryName, int EntryCount, double? AverageRating);

public sealed record DashboardStats(int TotalEntries, double? OverallAverageRating, IReadOnlyList<CategoryStat> PerCategory);
