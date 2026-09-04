using Avalonia.Headless.XUnit;
using LifeBucketList.App.Controls;
using Xunit;

namespace LifeBucketList.App.Tests;

/// <summary>Uses [AvaloniaFact]/[AvaloniaTheory] (not plain [Fact]/[Theory]): building a
/// StreamGeometry requires the Avalonia dispatcher thread, which only these attributes provide.</summary>
public class GermanRegionGeometryProviderTests
{
    [AvaloniaFact]
    public void ShapesByRegionCode_ParsesTheBundledGeoJson_WithExactly18Regions()
    {
        // 16 Bundesländer (from the bundled, Germany-filtered Natural Earth file) + Austria + Switzerland
        // (re-keyed from WorldMapGeometryProvider).
        Assert.Equal(18, GermanRegionGeometryProvider.ShapesByRegionCode.Count);
    }

    [AvaloniaTheory]
    [InlineData("DE-BY")]
    [InlineData("DE-BE")]
    [InlineData("DE-NW")]
    [InlineData("AT")]
    [InlineData("CH")]
    public void ShapesByRegionCode_ContainsWellKnownRegions_WithNonEmptyBounds(string regionCode)
    {
        var geometry = Assert.Contains(regionCode, GermanRegionGeometryProvider.ShapesByRegionCode);

        Assert.True(geometry.Bounds.Width > 0);
        Assert.True(geometry.Bounds.Height > 0);

        // Every projected coordinate must fall within the (uncropped) DACH crop window, in
        // WorldMapGeometryProvider's world-canvas coordinate space.
        var origin = GermanRegionGeometryProvider.CropOrigin;
        Assert.InRange(geometry.Bounds.Left, origin.X, origin.X + GermanRegionGeometryProvider.CanvasWidth);
        Assert.InRange(geometry.Bounds.Right, origin.X, origin.X + GermanRegionGeometryProvider.CanvasWidth);
        Assert.InRange(geometry.Bounds.Top, origin.Y, origin.Y + GermanRegionGeometryProvider.CanvasHeight);
        Assert.InRange(geometry.Bounds.Bottom, origin.Y, origin.Y + GermanRegionGeometryProvider.CanvasHeight);
    }

    [AvaloniaFact]
    public void ShapesByRegionCode_KeysAreLookedUpCaseInsensitively()
    {
        Assert.True(GermanRegionGeometryProvider.ShapesByRegionCode.ContainsKey("de-by"));
        Assert.True(GermanRegionGeometryProvider.ShapesByRegionCode.ContainsKey("DE-BY"));
    }
}
