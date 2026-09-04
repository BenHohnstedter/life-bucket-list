namespace LifeBucketList.Domain.Models;

/// <summary>Fixed reference list of regions for the Konzerte map: the 16 German Bundesländer at
/// state-level granularity (most concerts happen in Germany, so finer resolution is worth it there),
/// plus Austria and Switzerland each as a single whole-country region — giving DACH-wide coverage
/// without a separate country picker.</summary>
public static class GermanRegions
{
    public static readonly IReadOnlyList<GermanRegionInfo> All = new[]
    {
        new GermanRegionInfo("DE-BW", "Baden-Württemberg", 48.6616, 9.3501),
        new GermanRegionInfo("DE-BY", "Bayern", 48.7904, 11.4979),
        new GermanRegionInfo("DE-BE", "Berlin", 52.5200, 13.4050),
        new GermanRegionInfo("DE-BB", "Brandenburg", 52.4125, 12.5316),
        new GermanRegionInfo("DE-HB", "Bremen", 53.0793, 8.8017),
        new GermanRegionInfo("DE-HH", "Hamburg", 53.5511, 9.9937),
        new GermanRegionInfo("DE-HE", "Hessen", 50.6521, 9.1624),
        new GermanRegionInfo("DE-MV", "Mecklenburg-Vorpommern", 53.6127, 12.4296),
        new GermanRegionInfo("DE-NI", "Niedersachsen", 52.6367, 9.8451),
        new GermanRegionInfo("DE-NW", "Nordrhein-Westfalen", 51.4332, 7.6616),
        new GermanRegionInfo("DE-RP", "Rheinland-Pfalz", 49.9129, 7.4438),
        new GermanRegionInfo("DE-SL", "Saarland", 49.3964, 7.0230),
        new GermanRegionInfo("DE-SN", "Sachsen", 51.1045, 13.2017),
        new GermanRegionInfo("DE-ST", "Sachsen-Anhalt", 51.9503, 11.6923),
        new GermanRegionInfo("DE-SH", "Schleswig-Holstein", 54.2194, 9.6961),
        new GermanRegionInfo("DE-TH", "Thüringen", 50.9848, 11.0299),
        new GermanRegionInfo("AT", "Österreich", 48.2082, 16.3738),
        new GermanRegionInfo("CH", "Schweiz", 46.9480, 7.4474),
    };

    public static GermanRegionInfo? FindByCode(string? code) =>
        code is null ? null : All.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase));
}
