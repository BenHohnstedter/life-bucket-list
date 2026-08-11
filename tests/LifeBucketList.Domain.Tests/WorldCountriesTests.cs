using LifeBucketList.Domain.Models;
using Xunit;

namespace LifeBucketList.Domain.Tests;

public class WorldCountriesTests
{
    [Fact]
    public void All_HasNoDuplicateCodes()
    {
        var duplicates = WorldCountries.All
            .GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void All_HasNoDuplicateNames()
    {
        var duplicates = WorldCountries.All
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Theory]
    [MemberData(nameof(AllCountries))]
    public void All_EveryCountry_HasTwoLetterCodeAndValidCoordinates(CountryInfo country)
    {
        Assert.Equal(2, country.Code.Length);
        Assert.Equal(country.Code, country.Code.ToUpperInvariant());
        Assert.False(string.IsNullOrWhiteSpace(country.Name));
        Assert.InRange(country.Latitude, -90, 90);
        Assert.InRange(country.Longitude, -180, 180);
    }

    public static IEnumerable<object[]> AllCountries() => WorldCountries.All.Select(c => new object[] { c });

    [Fact]
    public void All_ContainsAtLeast150Countries()
    {
        Assert.True(WorldCountries.All.Count >= 150, $"Expected at least 150 countries, got {WorldCountries.All.Count}.");
    }

    [Fact]
    public void FindByCode_IsCaseInsensitive()
    {
        Assert.Equal("Deutschland", WorldCountries.FindByCode("de")?.Name);
        Assert.Equal("Deutschland", WorldCountries.FindByCode("DE")?.Name);
    }

    [Fact]
    public void FindByCode_ReturnsNull_ForUnknownOrNullCode()
    {
        Assert.Null(WorldCountries.FindByCode("XX"));
        Assert.Null(WorldCountries.FindByCode(null));
    }
}
