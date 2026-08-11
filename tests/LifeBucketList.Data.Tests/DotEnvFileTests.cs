using Xunit;

namespace LifeBucketList.Data.Tests;

public class DotEnvFileTests
{
    [Fact]
    public void Parse_ReadsSimpleKeyValueLines()
    {
        var result = DotEnvFile.Parse(new[] { "FOO=bar", "BAZ=qux" });

        Assert.Equal("bar", result["FOO"]);
        Assert.Equal("qux", result["BAZ"]);
    }

    [Fact]
    public void Parse_IgnoresCommentsAndBlankLines()
    {
        var result = DotEnvFile.Parse(new[] { "# a comment", "", "   ", "FOO=bar" });

        Assert.Single(result);
        Assert.Equal("bar", result["FOO"]);
    }

    [Fact]
    public void Parse_TrimsWhitespaceAndSurroundingQuotes()
    {
        var result = DotEnvFile.Parse(new[] { "  FOO = \"bar baz\"  " });

        Assert.Equal("bar baz", result["FOO"]);
    }

    [Fact]
    public void Parse_IgnoresLinesWithoutAnEqualsSign()
    {
        var result = DotEnvFile.Parse(new[] { "not-a-valid-line", "FOO=bar" });

        Assert.Single(result);
    }

    [Fact]
    public void LoadFromNearestAncestor_ReturnsEmpty_WhenNoEnvFileExistsAnywhereInAncestry()
    {
        var isolatedDir = Path.Combine(Path.GetTempPath(), $"lbl-dotenv-missing-{Guid.NewGuid()}");
        Directory.CreateDirectory(isolatedDir);
        try
        {
            var result = DotEnvFile.LoadFromNearestAncestor(isolatedDir);
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(isolatedDir, recursive: true);
        }
    }
}
