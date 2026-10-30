using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Company year, the split and Corporate tax as a viewer reads them among the river's cards, all open
// (CardHtml): what their sentences and bubbles say holds for what the engine does. The river's example
// (RiverInputsTests) earns 100,000 once its running costs and INASTI's 409.30 are paid, and pays 50,000.
public class CompanyAndSplitCardsTests
{
    private static readonly ResxText CorporateTexts = ResxText.For<CorporateTaxCard>();

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

    private static async Task<string> CardAsync(RiverInputs river, string card) => CardHtml.Card(await CardHtml.RenderCardsOpenAsync(river), card);
}
