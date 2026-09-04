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

public class GermanRegionMapControlTests
{
    [AvaloniaFact]
    public void RendersOneOutlineShapePerRegionInTheBundledMapData()
    {
        var control = new GermanRegionMapControl();
        var window = new Window { Content = control };
        window.Show();

        var shapes = control.GetVisualDescendants().OfType<ShapePath>().ToList();

        Assert.Equal(GermanRegionGeometryProvider.ShapesByRegionCode.Count, shapes.Count);
        Assert.Equal(18, shapes.Count);
    }

    [AvaloniaFact]
    public void HighlightedRegionCodes_MarksMatchingRegionAsVisited()
    {
        var control = new GermanRegionMapControl { HighlightedRegionCodes = new[] { "DE-BY", "AT" } };
        var window = new Window { Content = control };
        window.Show();

        var shapes = control.GetVisualDescendants().OfType<ShapePath>().ToList();
        var visitedCount = shapes.Count(s => s.Classes.Contains("visited"));

        Assert.Equal(2, visitedCount);
    }

    [AvaloniaFact]
    public void HighlightedRegionCodes_ObservableCollectionChanges_UpdateShapesWithoutRebinding()
    {
        var highlighted = new ObservableCollection<string>();
        var control = new GermanRegionMapControl { HighlightedRegionCodes = highlighted };
        var window = new Window { Content = control };
        window.Show();

        Assert.Empty(control.GetVisualDescendants().OfType<ShapePath>().Where(s => s.Classes.Contains("visited")));

        highlighted.Add("DE-BE");

        var visited = control.GetVisualDescendants().OfType<ShapePath>().Where(s => s.Classes.Contains("visited")).ToList();
        Assert.Single(visited);

        highlighted.Remove("DE-BE");

        Assert.Empty(control.GetVisualDescendants().OfType<ShapePath>().Where(s => s.Classes.Contains("visited")));
    }

    [AvaloniaFact]
    public void RenderForVisualReview()
    {
        var control = new GermanRegionMapControl
        {
            Width = 420,
            Height = 340,
            HighlightedRegionCodes = new[] { "DE-BY", "DE-BE", "DE-NW", "AT" },
        };
        var window = new Window { Content = control, Width = 420, Height = 340 };
        window.Show();

        var outputDir = Path.Combine(Path.GetTempPath(), "lbl-test-renders");
        Directory.CreateDirectory(outputDir);
        window.CaptureRenderedFrame()?.Save(Path.Combine(outputDir, "german-region-map.png"));
    }
}
