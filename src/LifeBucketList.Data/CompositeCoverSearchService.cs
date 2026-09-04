using LifeBucketList.Domain.Models;
using LifeBucketList.Domain.Services;

namespace LifeBucketList.Data;

/// <summary>Dispatches cover search to whichever backing service handles the given <see cref="MediaKind"/>:
/// Spotify for artists (Konzerte), TMDb/IGDB for everything else. Kept separate from
/// <see cref="TmdbIgdbCoverSearchService"/> rather than folding Spotify into it, so that class's name
/// and dependency list stay accurate and its existing tests stay untouched.</summary>
public sealed class CompositeCoverSearchService : ICoverSearchService
{
    private readonly TmdbIgdbCoverSearchService _tmdbIgdb;
    private readonly SpotifyCoverSearchService _spotify;

    public CompositeCoverSearchService(TmdbIgdbCoverSearchService tmdbIgdb, SpotifyCoverSearchService spotify)
    {
        _tmdbIgdb = tmdbIgdb;
        _spotify = spotify;
    }

    public Task<CoverSearchOutcome> SearchAsync(MediaKind kind, string query, CancellationToken cancellationToken = default) =>
        kind == MediaKind.Artist
            ? _spotify.SearchAsync(kind, query, cancellationToken)
            : _tmdbIgdb.SearchAsync(kind, query, cancellationToken);
}
