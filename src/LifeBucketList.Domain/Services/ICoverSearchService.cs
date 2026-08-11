using LifeBucketList.Domain.Models;

namespace LifeBucketList.Domain.Services;

public enum CoverSearchStatus
{
    Success,
    ApiKeyMissing,
    RequestFailed,
}

public sealed record CoverSearchOutcome(CoverSearchStatus Status, IReadOnlyList<MediaSearchResult> Results)
{
    public static readonly CoverSearchOutcome NoApiKey = new(CoverSearchStatus.ApiKeyMissing, Array.Empty<MediaSearchResult>());
    public static readonly CoverSearchOutcome Failed = new(CoverSearchStatus.RequestFailed, Array.Empty<MediaSearchResult>());

    public static CoverSearchOutcome Success(IReadOnlyList<MediaSearchResult> results) => new(CoverSearchStatus.Success, results);
}

/// <summary>Searches an external media database (TMDb for movies/series, IGDB for video games) for
/// cover/poster candidates matching a free-text title. Never throws: network/API failures and a
/// missing API key are reported through <see cref="CoverSearchOutcome.Status"/> instead, so the UI
/// can always fall back to manual entry without a cover.</summary>
public interface ICoverSearchService
{
    Task<CoverSearchOutcome> SearchAsync(MediaKind kind, string query, CancellationToken cancellationToken = default);
}
