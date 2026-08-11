namespace LifeBucketList.Domain.Models;

/// <summary>One cover-search hit from an external media database (TMDb/IGDB).</summary>
public sealed record MediaSearchResult(string Title, string? Year, string? ThumbnailUrl);
