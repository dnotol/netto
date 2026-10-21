using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// A card's sentence marks a term of art between square brackets in its .resx text, for the term to
// open an info bubble that says what it means.
public class TermTextTests
{
    [Fact]
    public void Split_markedTerm_givesTheTermAndTheTextAroundIt()
    {
        var parts = TermText.Split("It keeps the [reduced corporate rate], which takes a pay of at least €50,000.00.");

        parts.ShouldBe(("It keeps the ", "reduced corporate rate", ", which takes a pay of at least €50,000.00."));
    }

    [Fact]
    public void Split_termOpeningTheSentence_givesNothingBeforeIt()
    {
        TermText.Split("[VVPRbis shares] pay the ordinary rate.").ShouldBe(("", "VVPRbis shares", " pay the ordinary rate."));
    }

    [Theory]
    [InlineData("Your amount replaces the pay slip's in the year.")]
    [InlineData("An unclosed [bracket marks no term.")]
    public void Split_noTermMarked_givesTheWholeSentence(string sentence)
    {
        TermText.Split(sentence).ShouldBe((sentence, null, ""));
    }
}
