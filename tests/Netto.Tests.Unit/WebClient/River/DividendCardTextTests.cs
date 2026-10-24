using Netto.Core.Dividends;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The Dividend card builds resource keys from the withholding's categories and what the company keeps.
// A key without a text in its .resx would show on screen as the key itself.
public class DividendCardTextTests
{
    // The name IStringLocalizer<DividendCard> reads River/DividendCard.resx under.
    private static readonly ResxText Resources = ResxText.For<DividendCard>();

    [Fact]
    public void DividendCardResx_everyWithholdingCategory_hasAText()
    {
        Resources.Missing(Enum.GetValues<WithholdingCategory>().Select(DividendCardText.Category)).ShouldBeEmpty();
    }

    // Once the company keeps some, the dividend's summary starts from what it pays out, the base of its
    // rate, not from all it could: 25,000 of the 40,000 after tax, 15,000 being kept (ModuleViewsTests).
    [Fact]
    public void DividendSummary_reserve_saysWhatIsPaidOut()
    {
        var view = DividendView.From(RiverInputs.Example.WithAdvancePayment(0, 7_500m).WithTaxWithheld(11_334m).WithReserve(15_000m).Run().Year.ShouldNotBeNull());

        var text = Resources.Format(DividendCardText.Summary(view),
            "€25,000.00", "€20,649.94");

        text.ShouldBe("€25,000.00 paid out · €20,649.94 to your pocket");
    }

    [Fact]
    public void DividendSummary_nothingKept_saysWhatComesIn()
    {
        var view = DividendView.From(RiverInputs.Example.Run().Year.ShouldNotBeNull());

        DividendCardText.Summary(view).ShouldBe("Summary");
    }

    // The line of « Your shares and earlier profits »: profits kept or none, and each category.
    [Fact]
    public void DividendCardResx_everyPhraseOfItsSettingsLine_hasAText()
    {
        RiverInputs[] variations =
        [
            RiverInputs.Example,
            .. Enum.GetValues<WithholdingCategory>().Select(category => RiverInputs.Example.WithEarlierProfits(80_000m).WithDividendCategory(category)),
        ];
        var phrases = variations.SelectMany(inputs => DividendSettingsView.From(inputs).Facts);

        Resources.Missing(phrases.Select(phrase => phrase.Key).Append("SharesTitle")).ShouldBeEmpty();
    }

    // What VVPRbis shares are, in the bubble after the rate's label, which draws its text as it is: a term
    // marked in it would show its brackets.
    [Fact]
    public void DividendCardResx_rateBubble_hasATextMarkingNoTerm()
    {
        (Resources.Get(DividendCardText.CategoryHelp) is { } text ? TermText.Split(text).Term : "missing").ShouldBeNull();
    }
}
