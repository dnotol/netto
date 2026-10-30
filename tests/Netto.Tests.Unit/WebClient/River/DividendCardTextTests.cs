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

        text.ShouldSatisfyAllConditions(
            () => DividendCardText.Summary(view).ShouldBe("Summary_PaidOut"),
            () => CardHtml.Amounts(text).ShouldBe(["€25,000.00", "€20,649.94"]),
            () => CardHtml.SummaryFaults(text).ShouldBeEmpty());
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

    // The reduced rates count accounting years after the year of the contribution (art. 269 §2), not
    // distributions: an SRL founded in 2025 that pays every year is in the first year after it in 2026, so the
    // chips offer the third and second year's rates with the rules' 18% and 20%, then the ordinary 30%, and
    // none of them speaks of a distribution. The chips are in the disclosure a refusal opens.
    [Fact]
    public async Task Card_rateChips_nameTheAccountingYearAfterTheContribution()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(RiverInputs.Example.WithEarlierProfits(-1m)), "dividend");

        CardHtml.Chips(html).Select(chip => System.Net.WebUtility.HtmlDecode(chip.Text)).ShouldBe(
        [
            "18% · VVPRbis, third accounting year after the contribution or later",
            "20% · VVPRbis, second accounting year after the contribution",
            "30% · ordinary shares",
        ]);
    }

    // The bubble after the rate's label says the contribution's own year and the next pay the ordinary rate.
    [Fact]
    public async Task Card_rateBubble_namesTheAccountingYearAfterTheContribution()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(RiverInputs.Example.WithEarlierProfits(-1m)), "dividend");

        var bubble = CardHtml.BubbleAfter(html, Resources.Format("CategoryLabel"));

        bubble.ShouldSatisfyAllConditions(
            () => bubble.ShouldContain("second accounting year after the one of the contribution"),
            () => bubble.ShouldNotContain("distribution"));
    }

    // The line of the disclosure's fold names the same years.
    [Theory]
    [InlineData(WithholdingCategory.VvprbisSecondYear, "VVPRbis shares, second accounting year after the contribution, 20% withheld")]
    [InlineData(WithholdingCategory.VvprbisThirdYearOnwards, "VVPRbis shares, third accounting year after the contribution or later, 18% withheld")]
    public void SettingsLine_vvprbisCategory_namesTheAccountingYearAfterTheContribution(WithholdingCategory category, string line)
    {
        var fact = DividendSettingsView.From(RiverInputs.Example.WithDividendCategory(category)).Facts[^1];

        Resources.Format(fact.Key, [.. fact.Args]).ShouldBe(line);
    }

    // What VVPRbis shares are, in the bubble after the rate's label, which draws its text as it is: a term
    // marked in it would show its brackets.
    [Fact]
    public void DividendCardResx_rateBubble_hasATextMarkingNoTerm()
    {
        (Resources.Get(DividendCardText.CategoryHelp) is { } text ? TermText.Split(text).Term : "missing").ShouldBeNull();
    }
}
