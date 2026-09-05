using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;

namespace LifeBucketList.App.Controls;

/// <summary>Loads country outline shapes from a bundled Natural Earth GeoJSON asset (public domain,
/// 1:110m resolution — simplified on purpose, to match the app's minimalist style and keep the app
/// self-contained/offline) and projects them onto <see cref="WorldMapControl"/>'s equirectangular
/// canvas. Parsed once and cached; countries are keyed by ISO 3166-1 alpha-2 code.</summary>
public static class WorldMapGeometryProvider
{
    public const double CanvasWidth = 720;
    public const double CanvasHeight = 360;

    private const string AssetUri = "avares://LifeBucketList.App/Assets/countries-110m.geojson";

    private static readonly Lazy<IReadOnlyDictionary<string, Geometry>> Lazy = new(Load);

    public static IReadOnlyDictionary<string, Geometry> ShapesByIsoCode => Lazy.Value;

    private static IReadOnlyDictionary<string, Geometry> Load()
    {
        var shapes = new Dictionary<string, Geometry>(StringComparer.OrdinalIgnoreCase);

        using var stream = AssetLoader.Open(new Uri(AssetUri));
        using var document = JsonDocument.Parse(stream);

        foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
        {
            var properties = feature.GetProperty("properties");
            var isoCode = ReadCode(properties, "ISO_A2") ?? ReadCode(properties, "ISO_A2_EH");
            if (isoCode is null)
            {
                continue;
            }

            var geometry = BuildGeometry(feature.GetProperty("geometry"));
            if (geometry is not null)
            {
                shapes[isoCode] = geometry;
            }
        }

        return shapes;
    }

    /// <summary>Reads an ISO code property, treating Natural Earth's "-99" sentinel (used e.g. for
    /// France and Norway in the "ISO_A2" field — "ISO_A2_EH" has the real code) as absent.</summary>
    private static string? ReadCode(JsonElement properties, string propertyName)
    {
        if (!properties.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        var value = element.GetString();
        return string.IsNullOrWhiteSpace(value) || value == "-99" ? null : value;
    }

    /// <summary>Generic GeoJSON Polygon/MultiPolygon -> StreamGeometry projection, parameterized on the
    /// projection function so <see cref="GermanRegionGeometryProvider"/> can reuse the exact same
    /// parsing logic with its own latitude-corrected projection instead of this class's whole-world
    /// equirectangular one (which visibly stretches a single country/region badly at this zoom level).</summary>
    internal static StreamGeometry? BuildGeometry(JsonElement geometryElement, Func<double, double, Point> project)
    {
        var type = geometryElement.GetProperty("type").GetString();
        var coordinates = geometryElement.GetProperty("coordinates");

        var streamGeometry = new StreamGeometry();
        using var context = streamGeometry.Open();

        var figureCount = type switch
        {
            "Polygon" => AddPolygon(context, coordinates, project),
            "MultiPolygon" => coordinates.EnumerateArray().Sum(polygon => AddPolygon(context, polygon, project)),
            _ => 0,
        };

        return figureCount > 0 ? streamGeometry : null;
    }

    private static StreamGeometry? BuildGeometry(JsonElement geometryElement) => BuildGeometry(geometryElement, Project);

    /// <summary>Draws only the outer ring of a GeoJSON polygon (index 0); interior rings (holes, e.g.
    /// enclaves) are skipped as an acceptable simplification for a schematic overview map.</summary>
    private static int AddPolygon(StreamGeometryContext context, JsonElement polygonCoordinates, Func<double, double, Point> project)
    {
        using var rings = polygonCoordinates.EnumerateArray().GetEnumerator();
        if (!rings.MoveNext())
        {
            return 0;
        }

        var points = rings.Current.EnumerateArray().Select(pair => ToPoint(pair, project)).ToList();
        if (points.Count < 2)
        {
            return 0;
        }

        context.BeginFigure(points[0], isFilled: true);
        foreach (var point in points.Skip(1))
        {
            context.LineTo(point);
        }
        context.EndFigure(isClosed: true);

        return 1;
    }

    private static Point ToPoint(JsonElement coordinatePair, Func<double, double, Point> project)
    {
        var values = coordinatePair.EnumerateArray().ToArray();
        return project(values[0].GetDouble(), values[1].GetDouble());
    }

    private static Point Project(double longitude, double latitude) => new(
        (longitude + 180) / 360 * CanvasWidth,
        (90 - latitude) / 180 * CanvasHeight);
}
