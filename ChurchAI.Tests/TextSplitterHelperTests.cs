using Xunit;
using ChurchAI.Core.Helpers;

namespace ChurchAI.Tests;

public class TextSplitterHelperTests
{
    [Fact]
    public void CleanScriptureText_RemovesMarginalNotesAndUnwrapsBraces()
    {
        string raw = "Tell me, O thou whom my soul loveth, where thou feedest, where thou makest {thy flock} to rest at noon: for why should I be as one that turneth aside by the flocks of thy companions? {as one...: or, as one that is veiled}";
        
        string cleaned = TextSplitterHelper.CleanScriptureText(raw);

        Assert.DoesNotContain("{", cleaned);
        Assert.DoesNotContain("}", cleaned);
        Assert.DoesNotContain("as one that is veiled", cleaned);
        Assert.Contains("thy flock", cleaned);
        Assert.StartsWith("Tell me, O thou", cleaned);
    }

    [Fact]
    public void SplitTextIfNeeded_BypassesShortText()
    {
        string text = "Short 1 line verse text here.";
        var result = TextSplitterHelper.SplitTextIfNeeded(text, autoSplitEnabled: true, maxLines: 4, maxChars: 220);

        Assert.Single(result);
        Assert.Equal("Short 1 line verse text here.", result[0]);
    }

    [Fact]
    public void SplitTextIfNeeded_SplitsLongMultiLineVerse()
    {
        string longVerse = "Line 1 of verse text\nLine 2 of verse text\nLine 3 of verse text\nLine 4 of verse text\nLine 5 of verse text\nLine 6 of verse text";
        var result = TextSplitterHelper.SplitTextIfNeeded(longVerse, autoSplitEnabled: true, maxLines: 4, maxChars: 80);

        Assert.Equal(2, result.Count);
        Assert.Contains("Line 1", result[0]);
        Assert.Contains("Line 5", result[1]);
    }
}
