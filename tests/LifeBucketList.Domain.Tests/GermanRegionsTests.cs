using LifeBucketList.Domain.Models;
using Xunit;

namespace LifeBucketList.Domain.Tests;

public class GermanRegionsTests
{
    [Fact]
    public void All_HasNoDuplicateCodes()
    {
        var duplicates = GermanRegions.All
            .GroupBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void All_HasNoDuplicateNames()
    {
        var duplicates = GermanRegions.All
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Theory]
    [MemberData(nameof(AllRegions))]
    public void All_EveryRegion_HasValidCodeAndCoordinates(GermanRegionInfo region)
    {
        // German Bundesländer use ISO 3166-2 codes like "DE-BW"; Austria/Switzerland use plain "AT"/"CH".
        Assert.True(
            System.Text.RegularExpressions.Regex.IsMatch(region.Code, "^(DE-[A-Z]{2}|AT|CH)$"),
            $"Unexpected region code format: {region.Code}");
        Assert.Equal(region.Code, region.Code.ToUpperInvariant());
        Assert.False(string.IsNullOrWhiteSpace(region.Name));
        Assert.InRange(region.Latitude, -90, 90);
        Assert.InRange(region.Longitude, -180, 180);
    }

    public static IEnumerable<object[]> AllRegions() => GermanRegions.All.Select(r => new object[] { r });

    [Fact]
    public void All_ContainsExactly18Regions()
    {
        // 16 Bundesländer + Austria + Switzerland.
        Assert.Equal(18, GermanRegions.All.Count);
    }

    [Fact]
    public void FindByCode_IsCaseInsensitive()
    {
        Assert.Equal("Berlin", GermanRegions.FindByCode("de-be")?.Name);
        Assert.Equal("Berlin", GermanRegions.FindByCode("DE-BE")?.Name);
        Assert.Equal("Österreich", GermanRegions.FindByCode("at")?.Name);
    }

    [Fact]
    public void FindByCode_ReturnsNull_ForUnknownOrNullCode()
    {
        Assert.Null(GermanRegions.FindByCode("XX"));
        Assert.Null(GermanRegions.FindByCode(null));
    }
}
