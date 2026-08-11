using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Path = Avalonia.Controls.Shapes.Path;

namespace LifeBucketList.App.Controls;

/// <summary>Displays a 1-5 star rating; supports an editable, click-to-rate mode.</summary>
public partial class StarRatingControl : UserControl
{
    // A simple 5-point star, drawn in a 24x24 box (public-domain geometric shape).
    private const string StarGeometry = "M12,2 L14.9,9.1 L22,9.6 L16.5,14.5 L18.2,21.5 L12,17.7 L5.8,21.5 L7.5,14.5 L2,9.6 L9.1,9.1 Z";
    private const int StarCount = 5;

    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<StarRatingControl, int>(nameof(Value), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<StarRatingControl, bool>(nameof(IsReadOnly), true);

    public static readonly StyledProperty<double> StarSizeProperty =
        AvaloniaProperty.Register<StarRatingControl, double>(nameof(StarSize), 20d);

    private readonly List<Path> _stars = new();
    private StackPanel? _starsPanel;

    public StarRatingControl()
    {
        InitializeComponent();
        _starsPanel = this.FindControl<StackPanel>("StarsPanel");
        BuildStars();
    }

    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, 0, StarCount));
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public double StarSize
    {
        get => GetValue(StarSizeProperty);
        set => SetValue(StarSizeProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            UpdateStarFillState();
        }
        else if (change.Property == IsReadOnlyProperty || change.Property == StarSizeProperty)
        {
            BuildStars();
        }
    }

    private void BuildStars()
    {
        if (_starsPanel is null)
        {
            return;
        }

        _starsPanel.Children.Clear();
        _stars.Clear();

        for (var i = 0; i < StarCount; i++)
        {
            var starIndex = i + 1;
            var star = new Path
            {
                Data = Geometry.Parse(StarGeometry),
                Width = StarSize,
                Height = StarSize,
                Classes = { "star" },
                Cursor = IsReadOnly ? Cursor.Default : new Cursor(StandardCursorType.Hand),
            };

            if (!IsReadOnly)
            {
                star.Classes.Add("editable");
                star.PointerPressed += (_, e) => OnStarClicked(starIndex, e);
            }

            _stars.Add(star);
            _starsPanel.Children.Add(star);
        }

        UpdateStarFillState();
    }

    private void OnStarClicked(int starIndex, PointerPressedEventArgs e)
    {
        Value = Value == starIndex ? 0 : starIndex;
        e.Handled = true;
    }

    private void UpdateStarFillState()
    {
        for (var i = 0; i < _stars.Count; i++)
        {
            var filled = i < Value;
            var star = _stars[i];
            if (filled)
            {
                star.Classes.Add("filled");
            }
            else
            {
                star.Classes.Remove("filled");
            }
        }
    }
}
