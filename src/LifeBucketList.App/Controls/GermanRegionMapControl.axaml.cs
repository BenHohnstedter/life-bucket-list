using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using LifeBucketList.Domain.Models;
using Path = Avalonia.Controls.Shapes.Path;

namespace LifeBucketList.App.Controls;

/// <summary>The Konzerte counterpart to <see cref="WorldMapControl"/>: same self-contained,
/// real-outline-shapes-from-bundled-GeoJSON approach, scoped to Germany's 16 Bundesländer plus
/// Austria/Switzerland instead of the whole world.</summary>
public partial class GermanRegionMapControl : UserControl
{
    public static readonly StyledProperty<IEnumerable<string>?> HighlightedRegionCodesProperty =
        AvaloniaProperty.Register<GermanRegionMapControl, IEnumerable<string>?>(nameof(HighlightedRegionCodes));

    private readonly Dictionary<string, Path> _shapesByRegionCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Canvas? _canvas;

    public GermanRegionMapControl()
    {
        InitializeComponent();
        _canvas = this.FindControl<Canvas>("RegionMapCanvas");
        BuildRegionShapes();
        UpdateHighlights();
    }

    public IEnumerable<string>? HighlightedRegionCodes
    {
        get => GetValue(HighlightedRegionCodesProperty);
        set => SetValue(HighlightedRegionCodesProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != HighlightedRegionCodesProperty)
        {
            return;
        }

        if (change.OldValue is INotifyCollectionChanged oldObservable)
        {
            oldObservable.CollectionChanged -= OnHighlightedCollectionChanged;
        }

        if (change.NewValue is INotifyCollectionChanged newObservable)
        {
            newObservable.CollectionChanged += OnHighlightedCollectionChanged;
        }

        UpdateHighlights();
    }

    private void OnHighlightedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateHighlights();

    private void BuildRegionShapes()
    {
        if (_canvas is null)
        {
            return;
        }

        _canvas.Width = GermanRegionGeometryProvider.CanvasWidth;
        _canvas.Height = GermanRegionGeometryProvider.CanvasHeight;
        var cropOrigin = GermanRegionGeometryProvider.CropOrigin;
        _canvas.RenderTransform = new TranslateTransform(-cropOrigin.X, -cropOrigin.Y);
        _canvas.Children.Clear();
        _shapesByRegionCode.Clear();

        var namesByCode = GermanRegions.All.ToDictionary(r => r.Code, r => r.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var (code, geometry) in GermanRegionGeometryProvider.ShapesByRegionCode)
        {
            var shape = new Path
            {
                Data = geometry,
                Classes = { "region-shape" },
            };

            if (namesByCode.TryGetValue(code, out var name))
            {
                ToolTip.SetTip(shape, name);
            }

            _canvas.Children.Add(shape);
            _shapesByRegionCode[code] = shape;
        }
    }

    private void UpdateHighlights()
    {
        var highlighted = new HashSet<string>(HighlightedRegionCodes ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var (code, shape) in _shapesByRegionCode)
        {
            if (highlighted.Contains(code))
            {
                shape.Classes.Add("visited");
            }
            else
            {
                shape.Classes.Remove("visited");
            }
        }
    }
}
