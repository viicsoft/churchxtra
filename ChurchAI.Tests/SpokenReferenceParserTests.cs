using ChurchAI.Core.Parsers;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Xunit;

namespace ChurchAI.Tests;

public class SpokenReferenceParserTests
{
    private readonly SpokenReferenceParser _parser;

    public SpokenReferenceParserTests()
    {
        _parser = new SpokenReferenceParser();
    }

    [Theory]
    [InlineData("john three sixteen", "John 3:16")]
    [InlineData("John chapter three verse sixteen", "John 3:16")]
    [InlineData("First Corinthians thirteen four", "1 Corinthians 13:4")]
    [InlineData("Psalm twenty three one", "Psalms 23:1")]
    [InlineData("third john one four", "3 John 1:4")]
    [InlineData("second chronicles seven fourteen", "2 Chronicles 7:14")]
    [InlineData("genesis one one", "Genesis 1:1")]
    [InlineData("the book of revelation twenty two twenty one", "Revelation 22:21")]
    [InlineData("song of solomon two four", "Song of Solomon 2:4")]
    [InlineData("song of songs chapter two verse four", "Song of Solomon 2:4")]
    [InlineData("song solomon one one", "Song of Solomon 1:1")]
    [InlineData("canticles three one", "Song of Solomon 3:1")]
    public void Parse_ValidSpokenReference_ReturnsCorrectBibleReference(string spokenInput, string expectedOutput)
    {
        // Act
        var result = _parser.Parse(spokenInput);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedOutput, result.ToString());
    }

    [Fact]
    public void ParseWithContext_StatefulBookContext_CombinesOrphanedNumbers()
    {
        // Act 1: Pastor says "John"
        var result1 = _parser.ParseWithContext("John");
        Assert.NotNull(result1);
        Assert.Equal("John", result1.BookName);

        // Act 2: Pastor follows up with "three sixteen"
        var result2 = _parser.ParseWithContext("three sixteen");
        Assert.NotNull(result2);
        Assert.Equal("John 3:16", result2.ToString());
    }
}
