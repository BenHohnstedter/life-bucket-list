using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LifeBucketList.Domain.Models;
using Path = Avalonia.Controls.Shapes.Path;

namespace LifeBucketList.App.Controls;

/// <summary>Self-contained world overview: countries are drawn as real (simplified) outline shapes
/// from a bundled, public-domain Natural Earth dataset — no network access needed. Countries in
/// <see cref="HighlightedCountryCodes"/> are filled in the accent color; everything else stays a
/// neutral, unfilled outline.</summary>
public partial class WorldMapControl : UserControl
{
    public static readonly StyledProperty<IEnumerable<string>?> HighlightedCountryCodesProperty =
        AvaloniaProperty.Register<WorldMapControl, IEnumerable<string>?>(nameof(HighlightedCountryCodes));

    private readonly Dictionary<string, Path> _shapesByCountryCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Canvas? _canvas;

    public WorldMapControl()
    {
        InitializeComponent();
        _canvas = this.FindControl<Canvas>("MapCanvas");
        BuildCountryShapes();
        UpdateHighlights();
    }

    public IEnumerable<string>? HighlightedCountryCodes
    {
        get => GetValue(HighlightedCountryCodesProperty);
        set => SetValue(HighlightedCountryCodesProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != HighlightedCountryCodesProperty)
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

    private void BuildCountryShapes()
    {
        if (_canvas is null)
        {
            return;
        }

        _canvas.Width = WorldMapGeometryProvider.CanvasWidth;
        _canvas.Height = WorldMapGeometryProvider.CanvasHeight;
        _canvas.Children.Clear();
        _shapesByCountryCode.Clear();

        var namesByCode = WorldCountries.All.ToDictionary(c => c.Code, c => c.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var (code, geometry) in WorldMapGeometryProvider.ShapesByIsoCode)
        {
            var shape = new Path
            {
                Data = geometry,
                Classes = { "country-shape" },
            };

            if (namesByCode.TryGetValue(code, out var name))
            {
                ToolTip.SetTip(shape, name);
            }

            _canvas.Children.Add(shape);
            _shapesByCountryCode[code] = shape;
        }
    }

    private void UpdateHighlights()
    {
        var highlighted = new HashSet<string>(HighlightedCountryCodes ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var (code, shape) in _shapesByCountryCode)
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
