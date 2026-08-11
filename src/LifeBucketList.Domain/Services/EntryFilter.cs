namespace LifeBucketList.Domain.Services;

/// <summary>Criteria for narrowing down an entry list. Null/empty fields are ignored.</summary>
public sealed record EntryFilter(Guid? CategoryId = null, string? SearchText = null, int? MinRating = null);
