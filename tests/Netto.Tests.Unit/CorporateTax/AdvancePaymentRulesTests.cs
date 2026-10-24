using Netto.Core.CorporateTax;
using Shouldly;

namespace Netto.Tests.CorporateTax;

// The rates the SPF page gives for tax year 2027 follow from the articles' multipliers and the
// companies' reference rate: 2.25 and 3 / 2.5 / 2 / 1.5 times 3%.
public class AdvancePaymentRulesTests
{
    private static readonly AdvancePaymentRules Rules2026 = CorporateTaxRules.IncomeYear2026.AdvancePayments;

    [Fact]
    public void SurchargeRate_2026_is6Point75Percent()
    {
        Rules2026.SurchargeRate.ShouldBe(0.0675m);
    }

    [Fact]
    public void CreditRates_2026_fallFromDeadlineToDeadline()
    {
        Rules2026.Deadlines.Select(deadline => deadline.CreditRate(Rules2026)).ShouldBe([0.09m, 0.075m, 0.06m, 0.045m]);
    }

    // The statutory 10 October and 20 December 2026 fall on a weekend.
    [Fact]
    public void Deadlines_2026_areTheSpfsDates()
    {
        Rules2026.Deadlines.Select(deadline => deadline.Date.Value)
            .ShouldBe([new DateOnly(2026, 4, 10), new DateOnly(2026, 7, 10), new DateOnly(2026, 10, 12), new DateOnly(2026, 12, 21)]);
    }

    // Each deadline's date and multiplier are named by its place in the year, the date read from the
    // SPF's page for companies and the multiplier from art. 159.
    [Fact]
    public void Deadlines_2026_eachNamesItsPlaceAndCitesItsTexts()
    {
        const string page = "corporate-tax/spf-finances_versements-anticipes-societes_2026-10-20.html";
        const string article159 = "corporate-tax/spf-finances_cir92-art-159-revenus-2026_2026-02-09.html";

        Rules2026.Deadlines.Select(deadline => (deadline.Date.Key, deadline.Date.Source.Document, deadline.CreditMultiplier.Key, deadline.CreditMultiplier.Source.Document))
            .ShouldBe(
            [
                ("first_deadline", page, "first_credit_multiplier", article159),
                ("second_deadline", page, "second_credit_multiplier", article159),
                ("third_deadline", page, "third_credit_multiplier", article159),
                ("fourth_deadline", page, "fourth_credit_multiplier", article159),
            ]);
    }
}
