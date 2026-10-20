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
}
