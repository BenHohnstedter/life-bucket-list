namespace LifeBucketList.Domain.Models;

/// <summary>The fixed set of categories. Categories are no longer user-extensible; this is the
/// complete, permanent list.</summary>
public static class DefaultCategories
{
    public const string Movies = "Filme";
    public const string Series = "Serien";
    public const string VideoGames = "Videospiele";
    public const string Destinations = "Reiseziele";
    public const string Activities = "Aktivitäten";

    /// <summary>Categories whose entries can show a searched cover image (movie/TV/game databases).</summary>
    public static readonly IReadOnlyList<string> CoverSearchEnabledCategories = new[] { Movies, Series, VideoGames };

    public static readonly IReadOnlyList<string> Names = new[]
    {
        Destinations,
        Activities,
        Movies,
        Series,
        VideoGames,
    };
}
