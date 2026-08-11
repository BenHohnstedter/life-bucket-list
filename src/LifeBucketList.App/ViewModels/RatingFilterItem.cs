namespace LifeBucketList.App.ViewModels;

/// <summary>Display-friendly "minimum rating" filter option, for use in a ComboBox.</summary>
public sealed record RatingFilterItem(int? MinRating, string Label)
{
    public static readonly IReadOnlyList<RatingFilterItem> All = new[]
    {
        new RatingFilterItem(null, "Alle Bewertungen"),
        new RatingFilterItem(1, "★ oder besser"),
        new RatingFilterItem(2, "★★ oder besser"),
        new RatingFilterItem(3, "★★★ oder besser"),
        new RatingFilterItem(4, "★★★★ oder besser"),
        new RatingFilterItem(5, "★★★★★ nur Top-Bewertungen"),
    };

    public override string ToString() => Label;
}
