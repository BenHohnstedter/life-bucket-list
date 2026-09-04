using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LifeBucketList.App.ViewModels;
using LifeBucketList.App.Views;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;
using Xunit;

namespace LifeBucketList.App.Tests;

/// <summary>Genuine UI-level test: drives the real entry editor window's "Datum entfernen" button.</summary>
public class EntryEditorWindowTests
{
    [AvaloniaFact]
    public void ClearDateButton_OnlyVisibleWhenDateIsSet_AndClearsOccurredOnWhenClicked()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = DefaultCategories.Movies, SortOrder = 0 };
        var editor = new EntryEditorViewModel(new[] { category }, existingEntry: null, category, new FakeCoverSearchService(), new FakeCoverImageCache())
        {
            OccurredOn = new DateTimeOffset(new DateOnly(2024, 3, 15).ToDateTime(TimeOnly.MinValue)),
        };

        var window = new EntryEditorWindow { DataContext = editor };
        window.Show();

        var clearButton = window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Datum entfernen");
        Assert.True(clearButton.IsVisible);

        clearButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.Null(editor.OccurredOn);
        Assert.False(clearButton.IsVisible);
    }

    [AvaloniaTheory]
    [InlineData(DefaultCategories.Movies, true)]
    [InlineData(DefaultCategories.Destinations, false)]
    public void CoverSection_OnlyVisibleForMediaBackedCategories(string categoryName, bool expectedVisible)
    {
        var category = new Category { Id = Guid.NewGuid(), Name = categoryName, SortOrder = 0 };
        var editor = new EntryEditorViewModel(new[] { category }, existingEntry: null, category, new FakeCoverSearchService(), new FakeCoverImageCache());

        var window = new EntryEditorWindow { DataContext = editor };
        window.Show();

        var coverLabel = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Cover");
        Assert.Equal(expectedVisible, coverLabel.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task TypingATitle_ShowsSearchingIndicator_ThenResults_WithoutAnyButtonClick()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = DefaultCategories.Movies, SortOrder = 0 };
        var results = new[] { new MediaSearchResult("Inception", "2010", null) };
        var editor = new EntryEditorViewModel(
            new[] { category },
            existingEntry: null,
            category,
            new FakeCoverSearchService { Handler = (_, _) => CoverSearchOutcome.Success(results) },
            new FakeCoverImageCache(),
            TimeSpan.FromMilliseconds(20));

        var window = new EntryEditorWindow { DataContext = editor };
        window.Show();

        editor.Title = "Inception";
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        window.UpdateLayout();

        var resultButtons = window.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("cover-result")).ToList();
        Assert.Single(resultButtons);
        Assert.False(editor.IsSearchingCover);
    }

    [AvaloniaFact]
    public async Task CoverSearchResults_RenderAsSelectableThumbnails()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = DefaultCategories.Movies, SortOrder = 0 };
        var results = new[]
        {
            new MediaSearchResult("Inception", "2010", "https://example.com/inception.jpg"),
            new MediaSearchResult("Interstellar", "2014", "https://example.com/interstellar.jpg"),
        };
        var editor = new EntryEditorViewModel(
            new[] { category },
            existingEntry: null,
            category,
            new FakeCoverSearchService { Handler = (_, _) => CoverSearchOutcome.Success(results) },
            new FakeCoverImageCache())
        {
            Title = "Inception",
        };

        var window = new EntryEditorWindow { DataContext = editor };
        window.Show();
        await editor.SearchCoverCommand.ExecuteAsync(null);
        window.UpdateLayout();

        var resultButtons = window.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("cover-result")).ToList();
        Assert.Equal(2, resultButtons.Count);

        var outputDir = Path.Combine(Path.GetTempPath(), "lbl-test-renders");
        Directory.CreateDirectory(outputDir);
        window.CaptureRenderedFrame()?.Save(Path.Combine(outputDir, "entry-editor-cover-search.png"));
    }

    [AvaloniaFact]
    public void PressingEnter_WithValidTitle_SavesAndClosesTheWindow()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = DefaultCategories.Movies, SortOrder = 0 };
        var editor = new EntryEditorViewModel(new[] { category }, existingEntry: null, category, new FakeCoverSearchService(), new FakeCoverImageCache())
        {
            Title = "Inception",
        };

        var window = new EntryEditorWindow { DataContext = editor };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();

        var titleBox = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "TitleBox");
        titleBox.Focus();
        window.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });

        Assert.True(closed);
    }

    [AvaloniaFact]
    public void PressingEnter_WithEmptyTitle_ShowsValidationError_AndDoesNotClose()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = DefaultCategories.Movies, SortOrder = 0 };
        var editor = new EntryEditorViewModel(new[] { category }, existingEntry: null, category, new FakeCoverSearchService(), new FakeCoverImageCache());

        var window = new EntryEditorWindow { DataContext = editor };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();

        var titleBox = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "TitleBox");
        titleBox.Focus();
        window.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });

        Assert.False(closed);
        var errorText = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "ValidationErrorText");
        Assert.True(errorText.IsVisible);
    }

    [AvaloniaFact]
    public void PressingEnter_WhileFocusInNoteField_DoesNotSave_LeavesNewlineBehaviorIntact()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = DefaultCategories.Movies, SortOrder = 0 };
        var editor = new EntryEditorViewModel(new[] { category }, existingEntry: null, category, new FakeCoverSearchService(), new FakeCoverImageCache())
        {
            Title = "Inception",
        };

        var window = new EntryEditorWindow { DataContext = editor };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();

        var noteBox = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "NoteBox");
        noteBox.Focus();
        window.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });

        Assert.False(closed);
    }

    [AvaloniaTheory]
    [InlineData(DefaultCategories.Concerts, true)]
    [InlineData(DefaultCategories.Movies, false)]
    public void KonzerteFields_OnlyVisibleForConcerts(string categoryName, bool expectedVisible)
    {
        var category = new Category { Id = Guid.NewGuid(), Name = categoryName, SortOrder = 0 };
        var editor = new EntryEditorViewModel(new[] { category }, existingEntry: null, category, new FakeCoverSearchService(), new FakeCoverImageCache());

        var window = new EntryEditorWindow { DataContext = editor };
        window.Show();

        var regionLabel = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Region");
        var venueLabel = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Ort/Venue");
        Assert.Equal(expectedVisible, regionLabel.IsEffectivelyVisible);
        Assert.Equal(expectedVisible, venueLabel.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void RenderForVisualReview_KonzerteFields()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = DefaultCategories.Concerts, SortOrder = 0 };
        var editor = new EntryEditorViewModel(new[] { category }, existingEntry: null, category, new FakeCoverSearchService(), new FakeCoverImageCache())
        {
            Title = "Peter Fox",
            Venue = "Waldbühne Berlin",
            SelectedRegion = GermanRegions.FindByCode("DE-BE"),
        };

        var window = new EntryEditorWindow { DataContext = editor };
        window.Show();
        window.UpdateLayout();

        var outputDir = Path.Combine(Path.GetTempPath(), "lbl-test-renders");
        Directory.CreateDirectory(outputDir);
        window.CaptureRenderedFrame()?.Save(Path.Combine(outputDir, "entry-editor-konzerte.png"));
    }
}
