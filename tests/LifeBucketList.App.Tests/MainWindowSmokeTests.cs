using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using LifeBucketList.App.Services;
using LifeBucketList.App.ViewModels;
using LifeBucketList.App.Views;
using LifeBucketList.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace LifeBucketList.App.Tests;

/// <summary>Boots the real MainWindow against a real (temp) database to catch binding/resource
/// errors that only surface at runtime, not at compile time.</summary>
public class MainWindowSmokeTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"lbl-smoketest-{Guid.NewGuid()}.db");
    private MainWindowViewModel _viewModel = null!;
    private FakeDialogService _dialogService = null!;

    public async Task InitializeAsync()
    {
        var connectionFactory = new SqliteConnectionFactory(_dbPath);
        await connectionFactory.InitializeAsync();

        _dialogService = new FakeDialogService();
        _viewModel = new MainWindowViewModel(
            new SqliteCategoryRepository(connectionFactory),
            new SqliteEntryRepository(connectionFactory),
            new JsonBackupService(),
            _dialogService,
            new FakeCoverSearchService(),
            new FakeCoverImageCache());
        await _viewModel.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        return Task.CompletedTask;
    }

    [AvaloniaFact]
    public void MainWindow_LoadsAndBindsWithoutErrors()
    {
        var window = new MainWindow { DataContext = _viewModel };
        window.Show();

        var titleBlock = window.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(t => t.Text == "Life Bucket List");
        Assert.NotNull(titleBlock);

        var tabLabels = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Contains(LifeBucketList.Domain.Models.DefaultCategories.Names[0], tabLabels);

        SaveRenderForReview(window, "main-window.png");
    }

    [AvaloniaFact]
    public async Task MainWindow_WithEntryAndDashboard_RendersWithoutErrors()
    {
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "Inception";
            editor.Rating = 5;
            editor.Note = "Grandioser Twist am Ende.";
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);

        var window = new MainWindow { DataContext = _viewModel };
        window.Show();
        SaveRenderForReview(window, "main-window-with-entry.png");

        _viewModel.SelectDashboardCommand.Execute(null);
        window.UpdateLayout();
        SaveRenderForReview(window, "main-window-dashboard.png");
    }

    [AvaloniaFact]
    public async Task MainWindow_DefaultsToReisezieleTab_WithMapVisible()
    {
        // Reiseziele is now the first tab (new order), so it should be selected by default.
        Assert.Equal("Reiseziele", _viewModel.SelectedCategory?.Name);
        Assert.True(_viewModel.IsDestinationsCategorySelected);

        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "Sommerurlaub";
            editor.SelectedCountry = LifeBucketList.Domain.Models.WorldCountries.FindByCode("JP");
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);

        var window = new MainWindow { DataContext = _viewModel };
        window.Show();

        var tabLabels = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        // Verifies both the new tab order and that all five fixed categories render as tabs.
        Assert.Equal(
            LifeBucketList.Domain.Models.DefaultCategories.Names,
            tabLabels.Where(t => LifeBucketList.Domain.Models.DefaultCategories.Names.Contains(t)));

        var map = window.GetVisualDescendants().OfType<LifeBucketList.App.Controls.WorldMapControl>().Single();
        Assert.True(map.IsEffectivelyVisible);

        SaveRenderForReview(window, "main-window-reiseziele-with-map.png");
    }

    [AvaloniaFact]
    public async Task MainWindow_KonzerteTabSelected_ShowsRegionMap()
    {
        _dialogService.EntryEditorHandler = editor =>
        {
            editor.Title = "Peter Fox";
            editor.SelectedCategory = _viewModel.Categories.Single(c => c.Name == LifeBucketList.Domain.Models.DefaultCategories.Concerts);
            editor.Venue = "Waldbühne Berlin";
            editor.SelectedRegion = LifeBucketList.Domain.Models.GermanRegions.FindByCode("DE-BE");
            return true;
        };
        await _viewModel.AddEntryCommand.ExecuteAsync(null);

        _viewModel.SelectedCategory = _viewModel.Categories.Single(c => c.Name == LifeBucketList.Domain.Models.DefaultCategories.Concerts);

        var window = new MainWindow { DataContext = _viewModel };
        window.Show();

        Assert.True(_viewModel.IsConcertsCategorySelected);
        var map = window.GetVisualDescendants().OfType<LifeBucketList.App.Controls.GermanRegionMapControl>().Single();
        Assert.True(map.IsEffectivelyVisible);

        var venueText = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Contains("Waldbühne Berlin", venueText);

        SaveRenderForReview(window, "main-window-konzerte-with-map.png");
    }

    private static void SaveRenderForReview(Window window, string fileName)
    {
        var frame = window.CaptureRenderedFrame();
        if (frame is null)
        {
            return;
        }

        var outputDir = Path.Combine(Path.GetTempPath(), "lbl-test-renders");
        Directory.CreateDirectory(outputDir);
        frame.Save(Path.Combine(outputDir, fileName));
    }
}
