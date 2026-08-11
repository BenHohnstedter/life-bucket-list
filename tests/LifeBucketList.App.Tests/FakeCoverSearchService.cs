using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;

namespace LifeBucketList.App.Tests;

public sealed class FakeCoverSearchService : ICoverSearchService
{
    public Func<MediaKind, string, CoverSearchOutcome> Handler { get; set; } = (_, _) => CoverSearchOutcome.Success(Array.Empty<MediaSearchResult>());

    /// <summary>Overrides <see cref="Handler"/> when set; use for tests that need to control timing
    /// or observe/react to cancellation (e.g. debounce/live-search scenarios).</summary>
    public Func<MediaKind, string, CancellationToken, Task<CoverSearchOutcome>>? AsyncHandler { get; set; }

    public List<(MediaKind Kind, string Query)> Calls { get; } = new();

    public Task<CoverSearchOutcome> SearchAsync(MediaKind kind, string query, CancellationToken cancellationToken = default)
    {
        Calls.Add((kind, query));
        return AsyncHandler is not null
            ? AsyncHandler(kind, query, cancellationToken)
            : Task.FromResult(Handler(kind, query));
    }
}
