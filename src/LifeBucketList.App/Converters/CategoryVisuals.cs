using Avalonia.Media;
using LifeBucketList.Domain.Models;

namespace LifeBucketList.App.Converters;

/// <summary>Per-category icon glyph (hand-drawn Path data, same principle as the app's other icons)
/// and identity accent color, used by the sidebar navigation and the dashboard's category cards. Each
/// category gets its own color instead of everything sharing the single app accent, matching the
/// colour-coded-by-category convention common to dashboard-style apps.</summary>
public static class CategoryVisuals
{
    private static readonly Color DestinationsColor = Color.Parse("#0A84FF");
    private static readonly Color ActivitiesColor = Color.Parse("#FF9F0A");
    private static readonly Color MoviesColor = Color.Parse("#BF5AF2");
    private static readonly Color SeriesColor = Color.Parse("#FF375F");
    private static readonly Color VideoGamesColor = Color.Parse("#30D158");
    private static readonly Color ConcertsColor = Color.Parse("#64D2FF");
    private static readonly Color DefaultColor = Color.Parse("#8E8E93");

    // Every icon is a single solid silhouette (no "hole" subpaths) so the default NonZero fill rule
    // always applies - Avalonia's parsed StreamGeometry doesn't expose a settable FillRule after the
    // fact, so icons needing a punched-out hole (e.g. a ringed map pin) aren't an option here.
    private const string DestinationsIcon = "M8,1 A5,5 0 0,1 13,6 C13,10 8,15 8,15 C8,15 3,10 3,6 A5,5 0 0,1 8,1 Z";
    private const string ActivitiesIcon = "M9,1 L3,9 L7,9 L6,15 L13,6 L9,6 Z";
    private const string MoviesIcon = "M4,2 L4,14 L13,8 Z";
    private const string SeriesIcon = "M2,3 L14,3 L14,10 L2,10 Z M6,12 L10,12 L10,13.5 L6,13.5 Z";
    private const string VideoGamesIcon = "M6,3 L10,3 L10,6 L13,6 L13,10 L10,10 L10,13 L6,13 L6,10 L3,10 L3,6 L6,6 Z";
    private const string ConcertsIcon = "M4,11 A2,1.5 0 1,0 4,14 A2,1.5 0 1,0 4,11 Z M5.8,3.5 L5.8,12.5 L7,12.5 L7,3.5 Z";
    private const string DefaultIcon = "M6,3 L10,3 L10,13 L6,13 Z";

    public static string GetIconData(string? categoryName) => categoryName switch
    {
        DefaultCategories.Destinations => DestinationsIcon,
        DefaultCategories.Activities => ActivitiesIcon,
        DefaultCategories.Movies => MoviesIcon,
        DefaultCategories.Series => SeriesIcon,
        DefaultCategories.VideoGames => VideoGamesIcon,
        DefaultCategories.Concerts => ConcertsIcon,
        _ => DefaultIcon,
    };

    public static IBrush GetAccentBrush(string? categoryName) => new SolidColorBrush(GetAccentColor(categoryName));

    public static Color GetAccentColor(string? categoryName) => categoryName switch
    {
        DefaultCategories.Destinations => DestinationsColor,
        DefaultCategories.Activities => ActivitiesColor,
        DefaultCategories.Movies => MoviesColor,
        DefaultCategories.Series => SeriesColor,
        DefaultCategories.VideoGames => VideoGamesColor,
        DefaultCategories.Concerts => ConcertsColor,
        _ => DefaultColor,
    };
}
