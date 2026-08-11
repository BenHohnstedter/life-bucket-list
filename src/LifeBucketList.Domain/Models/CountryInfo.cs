namespace LifeBucketList.Domain.Models;

/// <summary>A country, for the Reiseziele map: ISO 3166-1 alpha-2 code, German display name, and an
/// approximate map position (equirectangular latitude/longitude of the country's rough centroid).</summary>
public sealed record CountryInfo(string Code, string Name, double Latitude, double Longitude);
