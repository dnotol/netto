using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient;

// The plan is a map with one card beside it (2026-10-27), no longer a column of cards one under another:
// no text names a card, a module or the split as above or below another. The pattern is narrow on
// purpose: « above the cap », « marked below » and the order of a list within its tab say nothing of
// where a card is.
public class PositionWordsTests
{
    private static readonly Regex CardPosition = new(@"\b(?:cards?|modules?|split)\s+(?:above|below)\b|\b(?:above|below)\s+them\b|\bthe (?:cards?|modules?) (?:above|below)\b",
        RegexOptions.IgnoreCase);

    [Fact]
    public void Resources_everyText_namesNoCardAboveOrBelowAnother()
    {
        var texts = InterfaceTexts.All().ToList();

        texts.ShouldSatisfyAllConditions(
            () => texts.ShouldContain(text => text.Key == "Bucket_pocket"),
            () => texts.Where(text => CardPosition.IsMatch(text.Value)).Select(text => $"{text.Resource}: {text.Key}").ShouldBeEmpty());
    }

    [Theory]
    [InlineData("The split above pays it beside your pay.")]
    [InlineData("less what the cards above pay you and its car costs")]
    [InlineData("the river counts below them")]
    [InlineData("Computed from the modules above, or published")]
    [InlineData("on the Copyright royalty card below")]
    public void Pattern_aCardReferredToByItsPlace_isCaught(string text)
    {
        CardPosition.IsMatch(text).ShouldBeTrue();
    }

    [Theory]
    [InlineData("your four previous years averaged above the cap")]
    [InlineData("No figures until the amount marked below is corrected.")]
    [InlineData("on top of the moves above it")]
    [InlineData("Pay {0} below the {1} minimum")]
    public void Pattern_aValueOrAListPlace_isLeftAlone(string text)
    {
        CardPosition.IsMatch(text).ShouldBeFalse();
    }
}
