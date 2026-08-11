using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Calendar = Avalonia.Controls.Calendar;

namespace LifeBucketList.App.Controls;

/// <summary>Replaces Avalonia's built-in <see cref="DatePicker"/>: that control's "day"/"month"/"year"
/// placeholder text is hard-coded English and does not follow the current culture (verified empirically
/// against Avalonia 11.3.20), so it cannot be localized to German. This control instead shows the date
/// as centered, German-formatted text on a button that opens a <see cref="Calendar"/> flyout — and
/// Calendar's month/weekday names ARE culture-aware.</summary>
public partial class GermanDatePicker : UserControl
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public static readonly StyledProperty<DateTimeOffset?> SelectedDateProperty =
        AvaloniaProperty.Register<GermanDatePicker, DateTimeOffset?>(nameof(SelectedDate), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private readonly Button? _triggerButton;
    private readonly Calendar? _calendar;
    private bool _isUpdatingFromCalendar;

    public GermanDatePicker()
    {
        InitializeComponent();
        _triggerButton = this.FindControl<Button>("TriggerButton");
        _calendar = this.FindControl<Calendar>("CalendarControl");

        if (_calendar is not null)
        {
            _calendar.PropertyChanged += OnCalendarPropertyChanged;
        }

        UpdateButtonContent();
    }

    public DateTimeOffset? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != SelectedDateProperty || _isUpdatingFromCalendar)
        {
            return;
        }

        UpdateButtonContent();
        SyncCalendarSelection();
    }

    private void OnCalendarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Calendar.SelectedDateProperty || _calendar is null)
        {
            return;
        }

        _isUpdatingFromCalendar = true;
        try
        {
            SelectedDate = _calendar.SelectedDate is { } date ? new DateTimeOffset(date) : null;
        }
        finally
        {
            _isUpdatingFromCalendar = false;
        }

        UpdateButtonContent();
        _triggerButton?.Flyout?.Hide();
    }

    private void SyncCalendarSelection()
    {
        if (_calendar is null)
        {
            return;
        }

        var dateTime = SelectedDate?.DateTime;
        _calendar.SelectedDate = dateTime;
        _calendar.DisplayDate = dateTime ?? DateTime.Today;
    }

    private void UpdateButtonContent()
    {
        if (_triggerButton is null)
        {
            return;
        }

        _triggerButton.Content = SelectedDate is { } date ? date.ToString("dd.MM.yyyy", German) : "Datum wählen";
        _triggerButton.Classes.Set("placeholder", SelectedDate is null);
    }
}
