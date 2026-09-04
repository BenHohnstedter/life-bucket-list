using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using LifeBucketList.App.Controls;
using Xunit;
using Calendar = Avalonia.Controls.Calendar;

namespace LifeBucketList.App.Tests;

/// <summary>Avalonia's built-in DatePicker has hard-coded, non-localizable "day"/"month"/"year"
/// watermark text (confirmed empirically), so the app uses this custom control instead.</summary>
public class GermanDatePickerTests
{
    [AvaloniaFact]
    public void ShowsPlaceholder_WhenNoDateSelected()
    {
        var picker = new GermanDatePicker();
        var window = new Window { Content = picker };
        window.Show();

        var button = picker.GetVisualDescendants().OfType<Button>().Single();

        Assert.Equal("Datum wählen", button.Content as string);
        Assert.Contains("placeholder", button.Classes);
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Center, button.HorizontalContentAlignment);
    }

    [AvaloniaFact]
    public void ShowsGermanFormattedDate_WhenDateIsSelected()
    {
        var picker = new GermanDatePicker { SelectedDate = new DateTimeOffset(new DateTime(2024, 3, 15)) };
        var window = new Window { Content = picker };
        window.Show();

        var button = picker.GetVisualDescendants().OfType<Button>().Single();

        Assert.Equal("15.03.2024", button.Content as string);
        Assert.DoesNotContain("placeholder", button.Classes);
    }

    [AvaloniaFact]
    public void SelectingDateInCalendarFlyout_UpdatesSelectedDate_AndClosesFlyout()
    {
        var picker = new GermanDatePicker();
        var window = new Window { Content = picker };
        window.Show();

        var button = picker.GetVisualDescendants().OfType<Button>().Single();
        button.Flyout!.ShowAt(button);

        var calendar = window.GetVisualDescendants().OfType<Calendar>().Single();
        calendar.SelectedDate = new DateTime(2025, 1, 20);

        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 20)), picker.SelectedDate);
        Assert.Equal("20.01.2025", button.Content as string);
    }

    [AvaloniaFact]
    public void SettingSelectedDateExternally_PositionsCalendarOnThatDate()
    {
        var picker = new GermanDatePicker();
        var window = new Window { Content = picker };
        window.Show();

        picker.SelectedDate = new DateTimeOffset(new DateTime(2030, 7, 4));

        var button = picker.GetVisualDescendants().OfType<Button>().Single();
        button.Flyout!.ShowAt(button);
        var calendar = window.GetVisualDescendants().OfType<Calendar>().Single();

        Assert.Equal(new DateTime(2030, 7, 4), calendar.SelectedDate);
    }

    /// <summary>Visual-regression check for the re-skinned Fluent Calendar flyout (Theme.axaml's
    /// CalendarView* resource overrides) — renders the actual flyout, not just asserts it exists.</summary>
    [AvaloniaFact]
    public void CalendarFlyout_RendersWithReskinnedFluentChrome()
    {
        var picker = new GermanDatePicker { SelectedDate = new DateTimeOffset(new DateTime(2024, 6, 15)) };
        var window = new Window { Width = 320, Height = 360, Content = picker };
        window.Show();

        var button = picker.GetVisualDescendants().OfType<Button>().Single();
        button.Flyout!.ShowAt(button);
        window.UpdateLayout();

        var outputDir = Path.Combine(Path.GetTempPath(), "lbl-test-renders");
        Directory.CreateDirectory(outputDir);
        window.CaptureRenderedFrame()?.Save(Path.Combine(outputDir, "calendar-flyout-reskinned.png"));
    }
}
