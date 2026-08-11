using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using LifeBucketList.App.Controls;
using Xunit;
using Path = Avalonia.Controls.Shapes.Path;

namespace LifeBucketList.App.Tests;

/// <summary>Genuine UI-level test: simulates a real pointer click on the custom star control.</summary>
public class StarRatingControlTests
{
    [AvaloniaFact]
    public void ClickingThirdStar_SetsValueToThree()
    {
        var control = new StarRatingControl { IsReadOnly = false, Value = 0, StarSize = 20 };
        var window = new Window { Content = control, Width = 300, Height = 100 };
        window.Show();

        var stars = control.GetVisualDescendants().OfType<Path>().ToList();
        Assert.Equal(5, stars.Count);

        var thirdStar = stars[2];
        var centerInStar = new Point(thirdStar.Bounds.Width / 2, thirdStar.Bounds.Height / 2);
        var pointInWindow = thirdStar.TranslatePoint(centerInStar, window) ?? default;

        window.MouseDown(pointInWindow, MouseButton.Left);

        Assert.Equal(3, control.Value);
    }

    [AvaloniaFact]
    public void ClickingSameStarTwice_ClearsRating()
    {
        var control = new StarRatingControl { IsReadOnly = false, Value = 0, StarSize = 20 };
        var window = new Window { Content = control, Width = 300, Height = 100 };
        window.Show();

        var firstStar = control.GetVisualDescendants().OfType<Path>().First();
        var center = new Point(firstStar.Bounds.Width / 2, firstStar.Bounds.Height / 2);
        var pointInWindow = firstStar.TranslatePoint(center, window) ?? default;

        window.MouseDown(pointInWindow, MouseButton.Left);
        Assert.Equal(1, control.Value);

        window.MouseDown(pointInWindow, MouseButton.Left);
        Assert.Equal(0, control.Value);
    }

    [AvaloniaFact]
    public void ReadOnly_IgnoresClicks()
    {
        var control = new StarRatingControl { IsReadOnly = true, Value = 2, StarSize = 20 };
        var window = new Window { Content = control, Width = 300, Height = 100 };
        window.Show();

        var lastStar = control.GetVisualDescendants().OfType<Path>().Last();
        var center = new Point(lastStar.Bounds.Width / 2, lastStar.Bounds.Height / 2);
        var pointInWindow = lastStar.TranslatePoint(center, window) ?? default;

        window.MouseDown(pointInWindow, MouseButton.Left);

        Assert.Equal(2, control.Value);
    }
}
