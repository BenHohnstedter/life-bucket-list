using Avalonia.Headless.XUnit;
using LifeBucketList.App.ViewModels;
using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;
using Xunit;

namespace LifeBucketList.App.Tests;

/// <summary>Cover search must trigger automatically as the title is typed (debounced), without
/// requiring a click on "Cover suchen".</summary>
public class EntryEditorLiveSearchTests
{
    private static readonly TimeSpan ShortDebounce = TimeSpan.FromMilliseconds(30);
    private static readonly TimeSpan SettleWait = TimeSpan.FromMilliseconds(200);

    private static Category MakeCategory(string name) => new() { Id = Guid.NewGuid(), Name = name, SortOrder = 0 };

    private static EntryEditorViewModel CreateEditor(Category category, FakeCoverSearchService coverSearchService) =>
        new(new[] { category }, existingEntry: null, category, coverSearchService, new FakeCoverImageCache(), ShortDebounce);

    [AvaloniaFact]
    public async Task TypingATitle_TriggersSearchAutomatically_WithoutCallingTheCommand()
    {
        var category = MakeCategory(DefaultCategories.Movies);
        var results = new[] { new MediaSearchResult("Inception", "2010", null) };
        var service = new FakeCoverSearchService { Handler = (_, _) => CoverSearchOutcome.Success(results) };
        var editor = CreateEditor(category, service);

        editor.Title = "Inception";
        await Task.Delay(SettleWait);

        Assert.Single(service.Calls);
        Assert.StartsWith("Inception", Assert.Single(editor.CoverSearchResults).DisplayTitle);
    }

    [AvaloniaFact]
    public async Task RapidSuccessiveTitleChanges_OnlySearchOnce_ForTheFinalTitle()
    {
        var category = MakeCategory(DefaultCategories.Movies);
        var service = new FakeCoverSearchService();
        var editor = CreateEditor(category, service);

        editor.Title = "I";
        editor.Title = "In";
        editor.Title = "Ince";
        editor.Title = "Inception";
        await Task.Delay(SettleWait);

        var call = Assert.Single(service.Calls);
        Assert.Equal("Inception", call.Query);
    }

    [AvaloniaFact]
    public async Task ClearingTheTitle_ClearsResults_AndDoesNotSearch()
    {
        var category = MakeCategory(DefaultCategories.Movies);
        var results = new[] { new MediaSearchResult("Inception", "2010", null) };
        var service = new FakeCoverSearchService { Handler = (_, _) => CoverSearchOutcome.Success(results) };
        var editor = CreateEditor(category, service);

        editor.Title = "Inception";
        await Task.Delay(SettleWait);
        Assert.Single(editor.CoverSearchResults);

        editor.Title = "";
        await Task.Delay(SettleWait);

        Assert.Empty(editor.CoverSearchResults);
        Assert.Single(service.Calls); // no second call for the cleared title
    }

    [AvaloniaFact]
    public async Task SwitchingToANonMediaCategory_StopsShowingResults()
    {
        var movies = MakeCategory(DefaultCategories.Movies);
        var destinations = MakeCategory(DefaultCategories.Destinations);
        var results = new[] { new MediaSearchResult("Inception", "2010", null) };
        var service = new FakeCoverSearchService { Handler = (_, _) => CoverSearchOutcome.Success(results) };
        var editor = new EntryEditorViewModel(new[] { movies, destinations }, existingEntry: null, movies, service, new FakeCoverImageCache(), ShortDebounce)
        {
            Title = "Inception",
        };
        await Task.Delay(SettleWait);
        Assert.Single(editor.CoverSearchResults);

        editor.SelectedCategory = destinations;
        await Task.Delay(SettleWait);

        Assert.Empty(editor.CoverSearchResults);
    }

    [AvaloniaFact]
    public async Task StaleInFlightSearch_NeverOverwritesTheNewerSearchsResults()
    {
        var category = MakeCategory(DefaultCategories.Movies);
        var slowQueryStarted = new TaskCompletionSource();
        var releaseSlowQuery = new TaskCompletionSource();

        var service = new FakeCoverSearchService
        {
            AsyncHandler = async (_, query, cancellationToken) =>
            {
                if (query == "slow")
                {
                    slowQueryStarted.SetResult();
                    await Task.WhenAny(releaseSlowQuery.Task, Task.Delay(Timeout.Infinite, cancellationToken));
                    cancellationToken.ThrowIfCancellationRequested();
                    return CoverSearchOutcome.Success(new[] { new MediaSearchResult("Stale Slow Result", null, null) });
                }

                return CoverSearchOutcome.Success(new[] { new MediaSearchResult("Fresh Fast Result", null, null) });
            },
        };
        var editor = CreateEditor(category, service);

        editor.Title = "slow";
        await slowQueryStarted.Task; // the slow request is now in flight, holding the CancellationToken

        editor.Title = "fast"; // supersedes "slow": cancels its token and starts a new debounce
        await Task.Delay(SettleWait);

        releaseSlowQuery.TrySetResult(); // let the stale request finish late, after being cancelled
        await Task.Delay(SettleWait);

        var item = Assert.Single(editor.CoverSearchResults);
        Assert.Equal("Fresh Fast Result", item.DisplayTitle);
    }
}
