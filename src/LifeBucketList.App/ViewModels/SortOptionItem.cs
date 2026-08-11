using LifeBucketList.Domain.Services;

namespace LifeBucketList.App.ViewModels;

/// <summary>Display-friendly wrapper for <see cref="EntrySortOption"/>, for use in a ComboBox.</summary>
public sealed record SortOptionItem(EntrySortOption Value, string Label)
{
    public static readonly IReadOnlyList<SortOptionItem> All = new[]
    {
        new SortOptionItem(EntrySortOption.DateDescending, "Datum (neueste zuerst)"),
        new SortOptionItem(EntrySortOption.DateAscending, "Datum (älteste zuerst)"),
        new SortOptionItem(EntrySortOption.RatingDescending, "Bewertung (höchste zuerst)"),
        new SortOptionItem(EntrySortOption.RatingAscending, "Bewertung (niedrigste zuerst)"),
        new SortOptionItem(EntrySortOption.TitleAscending, "Titel (A-Z)"),
        new SortOptionItem(EntrySortOption.TitleDescending, "Titel (Z-A)"),
    };

    public override string ToString() => Label;
}
