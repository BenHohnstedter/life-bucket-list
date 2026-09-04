using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LifeBucketList.Domain.Models;

namespace LifeBucketList.App.Converters;

/// <summary>Binds a Category's Name to its icon's Path.Data, via <see cref="CategoryVisuals"/>. Every
/// icon is a plain solid silhouette (no "hole" subpaths), so the default NonZero fill rule always
/// applies — no per-icon FillRule handling needed.</summary>
public sealed class CategoryIconDataConverter : IValueConverter
{
    public static readonly CategoryIconDataConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Geometry.Parse(CategoryVisuals.GetIconData(value as string));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Binds a Category's Name to its identity accent Brush, via <see cref="CategoryVisuals"/>.</summary>
public sealed class CategoryAccentBrushConverter : IValueConverter
{
    public static readonly CategoryAccentBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        CategoryVisuals.GetAccentBrush(value as string);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
