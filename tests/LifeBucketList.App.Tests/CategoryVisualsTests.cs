using Avalonia.Headless.XUnit;
using LifeBucketList.App.Converters;
using LifeBucketList.Domain.Models;
using Xunit;

namespace LifeBucketList.App.Tests;

/// <summary>[AvaloniaFact] because parsing icon Geometry requires the Avalonia dispatcher thread.</summary>
public class CategoryVisualsTests
{
    [Fact]
    public void EveryFixedCategory_HasADistinctAccentColor()
    {
        var colors = DefaultCategories.Names.Select(CategoryVisuals.GetAccentColor).ToList();

        Assert.Equal(colors.Count, colors.Distinct().Count());
    }

    [AvaloniaTheory]
    [MemberData(nameof(FixedCategoryNames))]
    public void EveryFixedCategory_HasNonEmptyIconData(string categoryName)
    {
        var data = CategoryVisuals.GetIconData(categoryName);

        Assert.False(string.IsNullOrWhiteSpace(data));
        var geometry = Avalonia.Media.Geometry.Parse(data);
        Assert.True(geometry.Bounds.Width > 0 && geometry.Bounds.Height > 0);
    }

    public static IEnumerable<object[]> FixedCategoryNames() => DefaultCategories.Names.Select(n => new object[] { n });

    [Fact]
    public void UnknownCategoryName_FallsBackToDefaultColorAndIcon_WithoutThrowing()
    {
        var color = CategoryVisuals.GetAccentColor("Irgendwas");
        var icon = CategoryVisuals.GetIconData("Irgendwas");

        Assert.NotEqual(default, color);
        Assert.False(string.IsNullOrWhiteSpace(icon));
    }
}
