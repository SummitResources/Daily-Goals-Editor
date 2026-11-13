using Daily_Goals_Editor.Core;

namespace Daily_Goals_Editor.Tests;

public class TicketParserTests
{
    private static readonly HashSet<string> NO_KNOWN_PREFIXES = [];

    [Theory]
    [InlineData("Work on PROD-1331", "PROD-1331")]
    [InlineData("PROD-1331", "PROD-1331")]
    [InlineData("ENG-7: fix export", "ENG-7")]
    [InlineData("A1B-42 then PROD-9", "A1B-42")]
    public void Find_UpperCaseKey_IsRecognised(string text, string expected)
    {
        Assert.Equal(expected, TicketParser.Find(text, NO_KNOWN_PREFIXES));
    }

    [Theory]
    [InlineData("Fix utf-8 encoding")]
    [InlineData("prod-1331 with no known prefix")]
    [InlineData("X-1 prefix too short")]
    [InlineData("PROD-1331x glued to letters")]
    [InlineData("Reply to emails")]
    public void Find_NonTicketText_ReturnsNull(string text)
    {
        Assert.Null(TicketParser.Find(text, NO_KNOWN_PREFIXES));
    }

    [Fact]
    public void Find_LowerCaseKeyWithKnownPrefix_IsRecognisedAndUpperCased()
    {
        Assert.Equal("PROD-1400", TicketParser.Find("prod-1400 review PR", new HashSet<string> { "PROD" }));
    }

    [Theory]
    [InlineData("PROD-1331", true)]
    [InlineData("prod-1331", false)]
    [InlineData("PROD-1331 extra", false)]
    [InlineData("PROD", false)]
    public void IsTicket_RequiresTheWholeValueToBeOneUpperCaseKey(string value, bool expected)
    {
        Assert.Equal(expected, TicketParser.IsTicket(value));
    }

    [Theory]
    [InlineData("PROD-1331", "PROD")]
    [InlineData("A1B-42", "A1B")]
    public void Prefix_ReturnsEverythingBeforeTheLastDash(string ticket, string expected)
    {
        Assert.Equal(expected, TicketParser.Prefix(ticket));
    }

    [Fact]
    public void Remove_MatchesTheTicketInAnyCase()
    {
        Assert.Equal(" deploy to staging", TicketParser.Remove("prod-1331 deploy to staging", "PROD-1331"));
    }
}
