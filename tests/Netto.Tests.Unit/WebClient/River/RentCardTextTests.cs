using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The rent card builds resource keys from what it shows. A key without a text in its .resx would show
// on screen as the key itself.
public class RentCardTextTests
{
    // The name IStringLocalizer<RentCard> reads River/RentCard.resx under.
    private static readonly ResxText Resources = ResxText.For<RentCard>();

    // The rent's summary ends on what the split shares: « before your pay », and « before your pay and
    // royalty » once the royalty is in the plan.
    [Fact]
    public void RentCardResx_everySummary_hasAText()
    {
        Resources.Missing([RentCardText.Summary(false), RentCardText.Summary(true)]).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, "Summary")]
    [InlineData(true, "Summary_Royalty")]
    public void RentSummary_royaltyInThePlan_saysBeforeYourPayAndRoyalty(bool royalty, string key)
    {
        RentCardText.Summary(royalty).ShouldBe(key);
    }

    // What the cadastral income is and how the cap is set, each in the bubble after its field's label.
    [Fact]
    public void RentCardResx_bubblesOfTheFields_haveTheirTexts()
    {
        Resources.Missing([RentCardText.CadastralIncomeHelp, RentCardText.CapHelp]).ShouldBeEmpty();
    }

    // A bubble draws its text as it is: a term marked in it would show its brackets.
    [Fact]
    public void RentCardResx_bubbles_markNoTerm()
    {
        new[] { RentCardText.CadastralIncomeHelp, RentCardText.CapHelp, RentFigures.FloorHelp, RentFigures.FlatChargesHelp, RentFigures.JoinsTaxableIncomeHelp, "Hint" }
            .Where(key => Resources.Get(key) is { } text && TermText.Split(text).Term is not null)
            .ShouldBeEmpty();
    }

    // The lines of « Your home »: the example's facts and facts given.
    [Fact]
    public void RentCardResx_everyPhraseOfItsSettingsLine_hasAText()
    {
        RiverInputs[] variations = [RiverInputs.Example, RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m)];
        var phrases = variations.SelectMany(inputs => RentSettingsView.From(inputs).Home);

        Resources.Missing(phrases.Select(phrase => phrase.Key).Append("HomeTitle")).ShouldBeEmpty();
    }
}
