namespace LifeBucketList.Domain.Models;

/// <summary>A region for the Konzerte DACH map: a German Bundesland (ISO 3166-2 code, e.g. "DE-BW")
/// or Austria/Switzerland as a whole country ("AT"/"CH"), German display name, and an approximate map
/// position (equirectangular latitude/longitude of the region's rough centroid).</summary>
public sealed record GermanRegionInfo(string Code, string Name, double Latitude, double Longitude);
