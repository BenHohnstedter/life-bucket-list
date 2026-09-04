using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;

namespace LifeBucketList.App.Controls;

/// <summary>Loads the 16 German Bundesland shapes from a bundled Natural Earth "Admin 1" GeoJSON
/// asset (public domain, 1:10m resolution — Natural Earth's 110m/50m tiers only cover US/Canada
/// subdivisions, so the finer 10m tier is needed for Germany; filtered down to just Germany's 16
/// states before bundling to keep the asset small) and adds Austria/Switzerland straight out of
/// <see cref="WorldMapGeometryProvider.ShapesByIsoCode"/> rather than bundling a second dataset for
/// two single-shape countries. Both sets are projected with the exact same equirectangular formula
/// (<see cref="WorldMapGeometryProvider.Project"/>), so they align pixel-for-pixel; a translate
/// crops/zooms that shared world-canvas coordinate space down to just the DACH region instead of
/// re-deriving a separate small-scale projection.</summary>
public static class GermanRegionGeometryProvider
{
    private const string AssetUri = "avares://LifeBucketList.App/Assets/germany-states-10m.geojson";

    // A generous DACH bounding box (in degrees) — covers Germany/Austria/Switzerland with margin.
    private const double MinLongitude = 5.5;
    private const double MaxLongitude = 17.5;
    private const double MinLatitude = 45.5;
    private const double MaxLatitude = 55.5;

    private static readonly Lazy<Loaded> Lazy = new(Load);

    public static IReadOnlyDictionary<string, Geometry> ShapesByRegionCode => Lazy.Value.Shapes;

    /// <summary>Top-left corner of the DACH crop window, in <see cref="WorldMapGeometryProvider"/>'s
    /// world-canvas pixel coordinates — apply as a negative Canvas translate so both the freshly-built
    /// state shapes and the borrowed AT/CH shapes (all still in absolute world-canvas coordinates) land
    /// in this control's local 0..CanvasWidth / 0..CanvasHeight space.</summary>
    public static Point CropOrigin => Lazy.Value.CropOrigin;

    public static double CanvasWidth => Lazy.Value.CanvasWidth;

    public static double CanvasHeight => Lazy.Value.CanvasHeight;

    private static Loaded Load()
    {
        var topLeft = WorldMapGeometryProvider.Project(MinLongitude, MaxLatitude);
        var bottomRight = WorldMapGeometryProvider.Project(MaxLongitude, MinLatitude);

        var shapes = new Dictionary<string, Geometry>(StringComparer.OrdinalIgnoreCase);

        using (var stream = AssetLoader.Open(new Uri(AssetUri)))
        using (var document = JsonDocument.Parse(stream))
        {
            foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
            {
                var regionCode = ReadRegionCode(feature.GetProperty("properties"));
                if (regionCode is null)
                {
                    continue;
                }

                var geometry = WorldMapGeometryProvider.BuildGeometry(feature.GetProperty("geometry"));
                if (geometry is not null)
                {
                    shapes[regionCode] = geometry;
                }
            }
        }

        foreach (var code in new[] { "AT", "CH" })
        {
            if (WorldMapGeometryProvider.ShapesByIsoCode.TryGetValue(code, out var geometry))
            {
                shapes[code] = geometry;
            }
        }

        return new Loaded(shapes, topLeft, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
    }

    private static string? ReadRegionCode(JsonElement properties)
    {
        if (!properties.TryGetProperty("iso_3166_2", out var element))
        {
            return null;
        }

        var value = element.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private sealed record Loaded(IReadOnlyDictionary<string, Geometry> Shapes, Point CropOrigin, double CanvasWidth, double CanvasHeight);
}
