using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;

namespace LifeBucketList.App.Controls;

/// <summary>Loads the 16 German Bundesland shapes from a bundled Natural Earth "Admin 1" GeoJSON asset
/// (public domain, 1:10m resolution — Natural Earth's 110m/50m tiers only cover US/Canada
/// subdivisions, so the finer 10m tier is needed for Germany; filtered down to just Germany's 16
/// states before bundling to keep the asset small), plus Austria/Switzerland parsed straight out of
/// the already-bundled world countries file rather than a second dataset for two countries.
///
/// Uses its own projection — a simple equirectangular formula corrected for a standard parallel near
/// Germany's latitude (longitude scaled by cos(standardParallel)) — instead of reusing
/// <see cref="WorldMapGeometryProvider"/>'s whole-world formula. At true world scale that formula's
/// distortion is invisible and expected (it's how every flat world map looks); cropped and zoomed in
/// on a single region, the same distortion visibly stretches everything east-west and looks wrong
/// compared to any map people are used to seeing of this specific area.</summary>
public static class GermanRegionGeometryProvider
{
    private const string GermanyAssetUri = "avares://LifeBucketList.App/Assets/germany-states-10m.geojson";
    private const string WorldAssetUri = "avares://LifeBucketList.App/Assets/countries-110m.geojson";

    private const double StandardParallelDegrees = 50.5; // roughly the centre latitude of DE/AT/CH
    private const double PixelsPerDegree = 40;
    private const double Margin = 6;

    private static readonly Lazy<Loaded> Lazy = new(Load);

    public static IReadOnlyDictionary<string, Geometry> ShapesByRegionCode => Lazy.Value.Shapes;

    public static double CanvasWidth => Lazy.Value.CanvasWidth;

    public static double CanvasHeight => Lazy.Value.CanvasHeight;

    private static Loaded Load()
    {
        using var germanyStream = AssetLoader.Open(new Uri(GermanyAssetUri));
        using var germanyDocument = JsonDocument.Parse(germanyStream);
        using var worldStream = AssetLoader.Open(new Uri(WorldAssetUri));
        using var worldDocument = JsonDocument.Parse(worldStream);

        var features = new List<(string Code, JsonElement Geometry)>();

        foreach (var feature in germanyDocument.RootElement.GetProperty("features").EnumerateArray())
        {
            var code = ReadStringProperty(feature.GetProperty("properties"), "iso_3166_2");
            if (!string.IsNullOrWhiteSpace(code))
            {
                features.Add((code, feature.GetProperty("geometry")));
            }
        }

        foreach (var feature in worldDocument.RootElement.GetProperty("features").EnumerateArray())
        {
            var properties = feature.GetProperty("properties");
            var isoCode = ReadStringProperty(properties, "ISO_A2");
            if (isoCode is "-99" or null)
            {
                isoCode = ReadStringProperty(properties, "ISO_A2_EH");
            }

            if (isoCode is "AT" or "CH")
            {
                features.Add((isoCode, feature.GetProperty("geometry")));
            }
        }

        // First pass: project every coordinate with the raw (untranslated) formula to find the
        // combined bounding box, so the second pass can bake a translate that lands everything in
        // positive, canvas-local coordinates.
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var (_, geometryElement) in features)
        {
            foreach (var (longitude, latitude) in EnumerateCoordinates(geometryElement))
            {
                var point = ProjectRaw(longitude, latitude);
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }
        }

        var offsetX = -minX + Margin;
        var offsetY = -minY + Margin;
        Point Project(double longitude, double latitude)
        {
            var raw = ProjectRaw(longitude, latitude);
            return new Point(raw.X + offsetX, raw.Y + offsetY);
        }

        var shapes = new Dictionary<string, Geometry>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, geometryElement) in features)
        {
            var geometry = WorldMapGeometryProvider.BuildGeometry(geometryElement, Project);
            if (geometry is not null)
            {
                shapes[code] = geometry;
            }
        }

        return new Loaded(shapes, maxX - minX + 2 * Margin, maxY - minY + 2 * Margin);
    }

    private static Point ProjectRaw(double longitude, double latitude) => new(
        longitude * Math.Cos(StandardParallelDegrees * Math.PI / 180.0) * PixelsPerDegree,
        -latitude * PixelsPerDegree);

    private static IEnumerable<(double Longitude, double Latitude)> EnumerateCoordinates(JsonElement geometryElement)
    {
        var type = geometryElement.GetProperty("type").GetString();
        var coordinates = geometryElement.GetProperty("coordinates");

        return type switch
        {
            "Polygon" => EnumeratePolygon(coordinates),
            "MultiPolygon" => coordinates.EnumerateArray().SelectMany(EnumeratePolygon),
            _ => Enumerable.Empty<(double, double)>(),
        };
    }

    private static IEnumerable<(double Longitude, double Latitude)> EnumeratePolygon(JsonElement polygonCoordinates) =>
        polygonCoordinates.EnumerateArray().SelectMany(ring => ring.EnumerateArray().Select(pair =>
        {
            var values = pair.EnumerateArray().ToArray();
            return (values[0].GetDouble(), values[1].GetDouble());
        }));

    private static string? ReadStringProperty(JsonElement properties, string propertyName) =>
        properties.TryGetProperty(propertyName, out var element) ? element.GetString() : null;

    private sealed record Loaded(IReadOnlyDictionary<string, Geometry> Shapes, double CanvasWidth, double CanvasHeight);
}
