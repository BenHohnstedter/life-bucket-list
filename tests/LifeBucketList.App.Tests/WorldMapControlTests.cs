using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using LifeBucketList.App.Controls;
using Xunit;
using Path = System.IO.Path;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace LifeBucketList.App.Tests;

public class WorldMapControlTests
{
    [AvaloniaFact]
    public void RendersOneOutlineShapePerCountryInTheBundledMapData()
    {
        var control = new WorldMapControl();
        var window = new Window { Content = control };
        window.Show();

        var shapes = control.GetVisualDescendants().OfType<ShapePath>().ToList();

        Assert.Equal(WorldMapGeometryProvider.ShapesByIsoCode.Count, shapes.Count);
        Assert.True(shapes.Count > 100, $"Expected a substantial number of country shapes, got {shapes.Count}.");
    }

    [AvaloniaFact]
    public void CountryShapes_AreRealOutlines_NotSinglePoints()
    {
        // A real country border has many segments; guards against silently falling back to a dot/marker.
        var germany = WorldMapGeometryProvider.ShapesByIsoCode["DE"];

        Assert.True(germany.Bounds.Width > 0 && germany.Bounds.Height > 0);
    }

    [AvaloniaFact]
    public void HighlightedCountryCodes_MarksMatchingCountryAsVisited()
    {
        var control = new WorldMapControl { HighlightedCountryCodes = new[] { "FR", "JP" } };
        var window = new Window { Content = control };
        window.Show();

        var shapes = control.GetVisualDescendants().OfType<ShapePath>().ToList();
        var visitedCount = shapes.Count(s => s.Classes.Contains("visited"));

        Assert.Equal(2, visitedCount);
    }

    [AvaloniaFact]
    public void HighlightedCountryCodes_ObservableCollectionChanges_UpdateShapesWithoutRebinding()
    {
        var highlighted = new ObservableCollection<string>();
        var control = new WorldMapControl { HighlightedCountryCodes = highlighted };
        var window = new Window { Content = control };
        window.Show();

        Assert.Empty(control.GetVisualDescendants().OfType<ShapePath>().Where(s => s.Classes.Contains("visited")));

        highlighted.Add("DE");

        var visited = control.GetVisualDescendants().OfType<ShapePath>().Where(s => s.Classes.Contains("visited")).ToList();
        Assert.Single(visited);

        highlighted.Remove("DE");

        Assert.Empty(control.GetVisualDescendants().OfType<ShapePath>().Where(s => s.Classes.Contains("visited")));
    }

    [AvaloniaFact]
    public void RenderForVisualReview()
    {
        var control = new WorldMapControl
        {
            Width = 700,
            Height = 340,
            HighlightedCountryCodes = new[] { "DE", "FR", "JP", "US", "AU", "BR", "ZA", "TH" },
        };
        var window = new Window { Content = control, Width = 700, Height = 340 };
        window.Show();

        var outputDir = Path.Combine(Path.GetTempPath(), "lbl-test-renders");
        Directory.CreateDirectory(outputDir);
        window.CaptureRenderedFrame()?.Save(Path.Combine(outputDir, "world-map.png"));
    }
}
