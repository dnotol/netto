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

    // Each deadline's date and multipliers are named by its place in the year; the credits come from art.
    // 159, the bonuses from art. 177, and the dates from art. 159 in 2025 and the SPF's page for the
    // self-employed in 2026, whose two last dates move off the weekend.
    public static TheoryData<int, string, string, string> DeadlineTextsByYear => new()
    {
        {
            2025, "personal-tax/spf-finances_cir92-art-159-revenus-2025_2025-01-16.html", "personal-tax/spf-finances_cir92-art-159-revenus-2025_2025-01-16.html",
            "personal-tax/spf-finances_cir92-art-177-revenus-2025_2025-01-16.html"
        },
        {
            2026, "personal-tax/spf-finances_versements-anticipes-independants_2026-10-20.html", "corporate-tax/spf-finances_cir92-art-159-revenus-2026_2026-02-09.html",
            "personal-tax/spf-finances_cir92-art-177-revenus-2026_2026-07-31.html"
        },
    };

    [Theory]
    [MemberData(nameof(DeadlineTextsByYear))]
    public void Deadlines_byIncomeYear_eachNamesItsPlaceAndCitesItsTexts(int incomeYear, string dates, string credits, string bonuses)
    {
        var deadlines = PersonalTaxRules.For(incomeYear).ShouldNotBeNull().AdvancePayments.Deadlines;

        deadlines.Select(deadline => (deadline.Date.Key, deadline.CreditMultiplier.Key, deadline.BonusMultiplier.Key, deadline.Date.Source.Document,
                deadline.CreditMultiplier.Source.Document, deadline.BonusMultiplier.Source.Document))
            .ShouldBe(
            [
                ("first_deadline", "first_credit_multiplier", "first_bonus_multiplier", dates, credits, bonuses),
                ("second_deadline", "second_credit_multiplier", "second_bonus_multiplier", dates, credits, bonuses),
                ("third_deadline", "third_credit_multiplier", "third_bonus_multiplier", dates, credits, bonuses),
                ("fourth_deadline", "fourth_credit_multiplier", "fourth_bonus_multiplier", dates, credits, bonuses),
            ]);
    }

    // The statutory 10 October and 20 December 2026 fall on a weekend.
    [Fact]
    public void Deadlines_2026_areTheSpfsDates()
    {
        Rules2026.Deadlines.Select(deadline => deadline.Date.Value)
            .ShouldBe([new DateOnly(2026, 4, 10), new DateOnly(2026, 7, 10), new DateOnly(2026, 10, 12), new DateOnly(2026, 12, 21)]);
    }
}
