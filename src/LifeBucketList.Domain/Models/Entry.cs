namespace LifeBucketList.Domain.Models;

public sealed class Entry
{
    public required Guid Id { get; init; }
    public required Guid CategoryId { get; set; }
    public required string Title { get; set; }
    public DateOnly? OccurredOn { get; set; }
    public int? Rating { get; set; }
    public string? Note { get; set; }

    /// <summary>Remote URL of a cover/poster image found via cover search (Filme/Serien/Videospiele), if any.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code (Reiseziele only), used to highlight the country on the map.</summary>
    public string? CountryCode { get; set; }

    /// <summary>Free-text venue/city (Konzerte only).</summary>
    public string? Venue { get; set; }

    /// <summary>German Bundesland (ISO 3166-2, e.g. "DE-BW") or plain "AT"/"CH" region code for the
    /// Konzerte DACH map (Konzerte only). See GermanRegions.</summary>
    public string? RegionCode { get; set; }

    /// <summary>Whether this Konzerte entry is a festival with no single performer, as opposed to a
    /// specific artist (Konzerte only). Null for every other category.</summary>
    public bool? IsFestival { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
}
