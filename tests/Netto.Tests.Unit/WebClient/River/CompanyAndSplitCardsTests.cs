using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.CompanyCar;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Company year, the split and Corporate tax as a viewer reads them among the river's cards, all open
// (CardHtml): what their sentences and bubbles say holds for what the engine does. The river's example
// (RiverInputsTests) earns 100,000 once its running costs and INASTI's 409.30 are paid, and pays 50,000.
public class CompanyAndSplitCardsTests
{
    private static readonly ResxText CorporateTexts = ResxText.For<CorporateTaxCard>();

    private static readonly ResxText SplitTexts = ResxText.For<SplitCard>();

    private static readonly ResxText CompanyYearTexts = ResxText.For<CompanyYearCard>();

    private static readonly ResxText ConditionTexts = ResxText.For<ConditionList>();

    // Revenue of 15,000 leaves a loss of 5,000 before any pay: no pay, and a taxable income of −5,000.
    private static readonly RiverInputs Loss = RiverInputs.Example.WithRevenue(15_000m);

    private static readonly RiverInputs CompanyPays = RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company);

    // A year with no taxable profit still pays a dividend out of the profits kept from earlier years:
    // 60,000 of them less this year's loss of 5,000.
    [Fact]
    public async Task CorporateTax_noTaxableProfitWithEarlierProfits_namesTheDividendTheyPay()
    {
        var html = await CardAsync(Loss.WithEarlierProfits(60_000m), "corporate-tax");

        CardHtml.Prose(html).ShouldContain(CorporateTexts.Format("NoTaxableProfit_EarlierProfits", "€55,000.00"));
    }

    [Fact]
    public async Task CorporateTax_noTaxableProfitNorEarlierProfits_saysNoDividend()
    {
        var html = await CardAsync(Loss, "corporate-tax");

        CardHtml.Prose(html).ShouldContain(CorporateTexts.Format("NoTaxableProfit"));
    }

    // Art. 215 tests the pay without a royalty's part taxed as pay: beside a fixed royalty of 30,000, whose
    // 6,000 beyond its limits Director pay counts in a taxable pay of 56,000, the pay tested is the 50,000
    // of gross, under the 51,000 minimum but above the 20,000 of taxable income left.
    [Fact]
    public async Task CorporateTax_royaltyTaxedAsPay_sentenceNamesThePayTested()
    {
        var river = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

        var html = await CardAsync(river, "corporate-tax");

        ShouldNameThePayTested(html, "Condition_TaxableIncomeNotAbovePay", "€50,000.00");
    }

    // The contributions the company pays count in the pay tested, as in Director pay's taxable pay: the
    // 50,000 of gross and 9,942.52 of them reach the minimum the gross alone does not.
    [Fact]
    public async Task CorporateTax_companyPaysContributions_sentenceNamesThePayWithThem()
    {
        var html = await CardAsync(CompanyPays, "corporate-tax");

        ShouldNameThePayTested(html, "Condition_MinimumPayMet", "€59,942.52");
    }

    // Beside a royalty taxed as pay with the company paying the contributions, Director pay's taxable pay
    // counts the royalty's part and the contributions and fund fee on it, which the pay tested leaves out:
    // the sentence's pay and what its bubble says is left out add up to it.
    [Fact]
    public async Task CorporateTax_royaltyTaxedAsPayAndCompanyPays_bubbleNamesAllTheTestLeavesOut()
    {
        var river = CompanyPays.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

        var cards = await CardHtml.RenderCardsOpenAsync(river);
        var corporateTax = CardHtml.Card(cards, "corporate-tax");
        var tested = Euros(CardHtml.Amounts(CardHtml.Prose(corporateTax).Single(paragraph => paragraph.Contains("tested pay", StringComparison.Ordinal)))[0]);
        var leftOut = Euros(CardHtml.Amounts(TermBubble(corporateTax, "tested pay"))[0]);
        var taxablePay = Euros(CardHtml.Lines(CardHtml.Card(cards, "director-pay")).Single(line => line.Label == "Taxable pay").Amount);

        (tested + leftOut).ShouldBe(taxablePay);
    }

    // A small company in its second tax period needs no minimum pay, but a company car's benefit of 1,714.29
    // must stay within 20% of the pay with it: the split weighs the rate from 6,857.16, and the bubble of
    // the reduced corporate rate says why, with the share its rule holds.
    [Fact]
    public async Task Split_carInSecondPeriod_termBubbleNamesTheBenefitsTest()
    {
        var river = RiverInputs.Example.WithFinancialYear(FinancialYear.Second).AddToPlan(OptionalModule.Car)
            .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m).WithCarEngine(CarEngine.Electric);

        var html = WebUtility.HtmlDecode(await CardAsync(river, "split"));

        var meaning = SplitTexts.Format(SplitCardText.ReducedRateTerm, "20%");

        meaning.ShouldSatisfyAllConditions(
            () => meaning.ShouldContain("20%"),
            () => html.ShouldContain(meaning));
    }

    // What the pay costs the company comes off its profit: with the company paying the contributions, each
    // euro of gross takes more than a euro of it, which the split's bubble says, with a royalty or without.
    [Fact]
    public async Task Split_companyPaysContributions_hintNamesThem()
    {
        (await SplitHintAsync(CompanyPays)).ShouldContain("contributions");
    }

    [Fact]
    public async Task Split_companyPaysContributionsBesideARoyalty_hintNamesThem()
    {
        (await SplitHintAsync(CompanyPays.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m))).ShouldContain("contributions");
    }

    // The conditions Company year lists are those under which the company owes INASTI's contribution: an
    // exempt company meets one of the two exemptions instead, which the Exempt choice's bubble names, so
    // it lists none.
    [Theory]
    [InlineData(CompanyContributionBand.Lower, true)]
    [InlineData(CompanyContributionBand.Upper, true)]
    [InlineData(CompanyContributionBand.Exempt, false)]
    public async Task CompanyYear_band_listsTheConditionsOfOwingOnlyWhileItOwes(CompanyContributionBand band, bool listed)
    {
        var html = await CardAsync(RiverInputs.Example.WithCompanyContributionBand(band), "company-year");

        html.Contains(ConditionTexts.Format(ConditionListText.Heading(2), 2), StringComparison.Ordinal).ShouldBe(listed);
    }

    // What another card of the plan counts comes off the company's profit there: the running costs leave it
    // out, as the pay and INASTI's contribution, or it counts twice. Their bubble names each lever's card.
    [Fact]
    public async Task CompanyYear_runningCostsBubble_namesEveryLeverCard()
    {
        var hint = CardHtml.BubbleAfter(await CardAsync(RiverInputs.Example, "company-year"), CompanyYearTexts.Format("RunningCostsLabel"));
        string[] levers =
        [
            ResxText.For<VoucherCard>().Format("Title"), ResxText.For<AllowanceCard>().Format("Title"), ResxText.For<RentCard>().Format("Title"),
            ResxText.For<CarCard>().Format("Title"), ResxText.For<CopyrightCard>().Format("Title"),
        ];

        levers.Where(title => !hint.Contains(title, StringComparison.OrdinalIgnoreCase)).ShouldBeEmpty();
    }

    // The card draws the condition's sentence, which names the pay tested: as drawn without benefits, the
    // 51,000 minimum, no benefits within 20% of the pay tested, and that pay, its term drawn as the text of
    // its bubble's trigger.
    private static void ShouldNameThePayTested(string html, string key, string payTested)
    {
        var sentence = CorporateTexts.Format(key, "€51,000.00", "€0.00", "20%", payTested)
            .Replace("[", "", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal);

        sentence.ShouldSatisfyAllConditions(
            () => sentence.ShouldContain(payTested),
            () => CardHtml.Prose(html).ShouldContain(sentence));
    }

    private static async Task<string> SplitHintAsync(RiverInputs river) => CardHtml.BubbleAfter(await CardAsync(river, "split"), SplitTexts.Format("Title"));

    // The text of the bubble a term in running text opens, as a screen reader is given it: the description
    // right after the term's trigger.
    private static string TermBubble(string html, string term)
    {
        var bubble = Regex.Match(WebUtility.HtmlDecode(html), $@"<button[^>]*>\s*{Regex.Escape(term)}\s*</button><span [^>]*class=""info-bubble__text""[^>]*>(?<text>[^<]*)</span>");
        bubble.Success.ShouldBeTrue($"No bubble follows « {term} ».");

        return bubble.Groups["text"].Value;
    }

    private static decimal Euros(string amount) => decimal.Parse(amount.TrimStart('€'), NumberStyles.Number, CultureInfo.InvariantCulture);

    private static async Task<string> CardAsync(RiverInputs river, string card) => CardHtml.Card(await CardHtml.RenderCardsOpenAsync(river), card);
}
