using LifeBucketList.Domain.Services;
using Xunit;

namespace LifeBucketList.Domain.Tests;

public class EntryValidatorTests
{
    [Fact]
    public void ValidateTitle_TrimsWhitespace()
    {
        var result = EntryValidator.ValidateTitle("  Inception  ");

        Assert.Equal("Inception", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTitle_RejectsEmpty(string? title)
    {
        Assert.Throws<DomainValidationException>(() => EntryValidator.ValidateTitle(title));
    }

    [Fact]
    public void ValidateTitle_RejectsTooLong()
    {
        var tooLong = new string('a', EntryValidator.MaxTitleLength + 1);

        Assert.Throws<DomainValidationException>(() => EntryValidator.ValidateTitle(tooLong));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(null)]
    public void ValidateRating_AcceptsValidValues(int? rating)
    {
        var exception = Record.Exception(() => EntryValidator.ValidateRating(rating));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void ValidateRating_RejectsOutOfRange(int rating)
    {
        Assert.Throws<DomainValidationException>(() => EntryValidator.ValidateRating(rating));
    }

    [Fact]
    public void ValidateNote_ReturnsNullForBlank()
    {
        Assert.Null(EntryValidator.ValidateNote("   "));
        Assert.Null(EntryValidator.ValidateNote(null));
    }

    [Fact]
    public void ValidateNote_RejectsTooLong()
    {
        var tooLong = new string('a', EntryValidator.MaxNoteLength + 1);

        Assert.Throws<DomainValidationException>(() => EntryValidator.ValidateNote(tooLong));
    }
}
