namespace LifeBucketList.App.ViewModels;

/// <summary>Display-ready row for the dashboard's per-category breakdown.</summary>
public sealed record CategoryStatRow(string CategoryName, int EntryCount, string AverageRatingText, double BarFraction);
