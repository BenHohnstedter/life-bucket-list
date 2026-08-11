using Avalonia.Headless.XUnit;
using Avalonia.Media;
using LifeBucketList.App.Controls;
using Xunit;

namespace LifeBucketList.App.Tests;

/// <summary>Uses [AvaloniaFact]/[AvaloniaTheory] (not plain [Fact]/[Theory]): building a
/// StreamGeometry requires the Avalonia dispatcher thread, which only these attributes provide.</summary>
public class WorldMapGeometryProviderTests
{
    [AvaloniaFact]
    public void ShapesByIsoCode_ParsesTheBundledGeoJson_WithPlausibleCountryCount()
    {
        var shapes = WorldMapGeometryProvider.ShapesByIsoCode;

        // Natural Earth 110m resolution intentionally omits some micro-states; ~170-190 is expected.
        Assert.InRange(shapes.Count, 150, 200);
    }

    [AvaloniaTheory]
    [InlineData("DE")]
    [InlineData("US")]
    [InlineData("FR")]
    [InlineData("JP")]
    [InlineData("AU")]
    [InlineData("BR")]
    public void ShapesByIsoCode_ContainsWellKnownCountries_WithNonEmptyBounds(string isoCode)
    {
        var geometry = Assert.Contains(isoCode, WorldMapGeometryProvider.ShapesByIsoCode);

        Assert.True(geometry.Bounds.Width > 0);
        Assert.True(geometry.Bounds.Height > 0);
        // Every projected coordinate must fall within the canvas.
        Assert.InRange(geometry.Bounds.Left, 0, WorldMapGeometryProvider.CanvasWidth);
        Assert.InRange(geometry.Bounds.Right, 0, WorldMapGeometryProvider.CanvasWidth);
        Assert.InRange(geometry.Bounds.Top, 0, WorldMapGeometryProvider.CanvasHeight);
        Assert.InRange(geometry.Bounds.Bottom, 0, WorldMapGeometryProvider.CanvasHeight);
    }

    [AvaloniaFact]
    public void ShapesByIsoCode_KeysAreLookedUpCaseInsensitively()
    {
        Assert.True(WorldMapGeometryProvider.ShapesByIsoCode.ContainsKey("de"));
        Assert.True(WorldMapGeometryProvider.ShapesByIsoCode.ContainsKey("DE"));
    }
}
