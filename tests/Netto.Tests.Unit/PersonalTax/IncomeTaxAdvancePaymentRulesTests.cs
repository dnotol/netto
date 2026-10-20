using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// The rates the SPF pages give for tax year 2027 follow from the articles' multipliers and the
// reference rate of art. 161: the ECB's 2.40% on 1 January 2026, rounded down to 2%; 3.40% on
// 1 January 2025 gives 3% for the Tax-Calc year.
public class IncomeTaxAdvancePaymentRulesTests
{
    private static readonly IncomeTaxAdvancePaymentRules Rules2026 = PersonalTaxRules.IncomeYear2026.AdvancePayments;

    [Theory]
    [InlineData(2025, "0.0675")]
    [InlineData(2026, "0.045")]
    public void SurchargeRate_byIncomeYear_is2Point25TimesTheReferenceRate(int incomeYear, string expected)
    {
        PersonalTaxRules.For(incomeYear)!.AdvancePayments.SurchargeRate.ShouldBe(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CreditRates_2026_fallFromDeadlineToDeadline()
    {
        Rules2026.Deadlines.Select(deadline => deadline.CreditRate(Rules2026)).ShouldBe([0.06m, 0.05m, 0.04m, 0.03m]);
    }

    [Fact]
    public void BonusRates_2026_areHalfTheCredits()
    {
        Rules2026.Deadlines.Select(deadline => deadline.BonusRate(Rules2026)).ShouldBe([0.03m, 0.025m, 0.02m, 0.015m]);
    }

    // The statutory 10 October and 20 December 2026 fall on a weekend.
    [Fact]
    public void Deadlines_2026_areTheSpfsDates()
    {
        Rules2026.Deadlines.Select(deadline => deadline.Date.Value)
            .ShouldBe([new DateOnly(2026, 4, 10), new DateOnly(2026, 7, 10), new DateOnly(2026, 10, 12), new DateOnly(2026, 12, 21)]);
    }
}
