using System.Net;
using Netto.Core.CompanyCar;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Company year, the split and Corporate tax as a viewer reads them among the river's cards, all open
// (CardHtml): what their sentences and bubbles say holds for what the engine does. The river's example
// (RiverInputsTests) earns 100,000 once its running costs and INASTI's 409.30 are paid, and pays 50,000.
public class CompanyAndSplitCardsTests
{
    private static readonly ResxText CorporateTexts = ResxText.For<CorporateTaxCard>();

    private static readonly ResxText SplitTexts = ResxText.For<SplitCard>();

    // Revenue of 15,000 leaves a loss of 5,000 before any pay: no pay, and a taxable income of −5,000.
    private static readonly RiverInputs Loss = RiverInputs.Example.WithRevenue(15_000m);

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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Split_companyPaysContributions_hintNamesThem(bool withRoyalty)
    {
        var river = RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company);
        if (withRoyalty)
            river = river.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

        var hint = CardHtml.BubbleAfter(await CardAsync(river, "split"), SplitTexts.Format("Title"));

        hint.ShouldContain("contributions");
    }

    private static async Task<string> CardAsync(RiverInputs river, string card) => CardHtml.Card(await CardHtml.RenderCardsOpenAsync(river), card);
}
