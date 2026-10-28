using Netto.Core.DirectorPay;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// The marital quotient of annex III to the AR/CIR 92 (n° 34; key formula ESS-SR/2025-0928, n° 11 b)
// on made-up monthly pays of income year 2026, for a director whose spouse or legal cohabitant has no
// professional income: the key formula's lines to the annual net taxable as for a single person, then
// 30% of it imputed to the partner, at most 13,790 to October 2026 and, from 1 November 2026, 11,780,
// or 12,790 when both partners have reached 66 on 1 January 2027 (n° 34.1.1); the tax on the imputed
// income and the tax on the rest, each on the scale of n° 31 (n° 34.1.2 and 34.1.3); their sum less
// 5,975.96 to October, 6,179.26 from November (n° 34.2), never below zero (n° 21); divided by 12. Each
// step is rounded to the cent half away from zero (n° 12), in the chain the SPF simulator « Précompte
// professionnel 2026 », sheet « Dirigeants d'entreprise », rows 34 and 35 follows:
// I35 = round(min(H × 30%, 13,790)); I34 = H − I35; J34 = round(scale(I34)); J35 = round(scale(I35));
// K34 = max(0, J34 + J35 − 5,975.96); O34 = round(K34 ÷ 12).
//
// Expected values are derived from the official amounts by that chain: Tax-Calc for income year 2026
// is not online, and it takes the withholding as an input (code 1407) anyway. They agree with the
// simulator's row 34 wherever the rest stays within the scale's third band. Above 51,050 of rest, from
// about 7,000 a month, the simulator's row 34 is stale: it takes 20,320 + 53.50% of the slice over
// 49,820, where n° 31 gives 20,320.38 + 53.50% of the slice over 51,050, and so withholds 54.81 a month
// more than the annex. These tests follow the annex. The November amounts have no simulator of their
// own to check against: they apply the same chain with the coordinated annex's amounts.
public class WithholdingCalculatorQuotientTests
{
    private static readonly WithholdingRules January2026 = WithholdingRules.IncomeYear2026;

    private static readonly WithholdingRules November2026 = WithholdingRules.IncomeYear2026.InMonth(11);

    // Up to 2,500 the two taxes stay under the 5,975.96 taken off them. 5,000 imputes 13,461.56 to
    // October, under its maximum, but 11,780 from November, so the month withholds more from November.
    public static TheoryData<decimal, decimal> JanuaryWithholdingByPay => new()
    {
        { 1_000m, 0m },
        { 2_500m, 0m },
        { 3_000m, 120.41m },
        { 3_475m, 257.80m },
        { 4_166.67m, 457.86m },
        { 5_000m, 707.41m },
        { 6_310m, 1_181.84m },
        { 8_000m, 1_896.04m },
        { 10_000m, 2_836.87m },
        { 12_000m, 3_894.87m },
    };

    public static TheoryData<decimal, decimal> NovemberWithholdingByPay => new()
    {
        { 1_000m, 0m },
        { 2_500m, 0m },
        { 3_000m, 103.47m },
        { 3_475m, 240.86m },
        { 4_166.67m, 440.92m },
        { 5_000m, 720.45m },
        { 6_310m, 1_200.75m },
        { 8_000m, 1_923.91m },
        { 10_000m, 2_864.74m },
        { 12_000m, 3_922.74m },
    };

    // Under 11,780 imputed, the same as any partner without income; above, 12,790 at most.
    public static TheoryData<decimal, decimal> NovemberBothAged66WithholdingByPay => new()
    {
        { 4_166.67m, 440.92m },
        { 5_000m, 702.44m },
        { 6_310m, 1_182.74m },
        { 10_000m, 2_842.22m },
    };

    [Theory]
    [MemberData(nameof(JanuaryWithholdingByPay))]
    public void Compute_2026PartnerWithoutIncome_withholdsTheQuotientsAmount(decimal monthlyPay, decimal expected)
    {
        var result = Compute(January2026, monthlyPay, WithholdingHousehold.PartnerWithoutIncome);

        result.MonthlyWithholding.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(NovemberWithholdingByPay))]
    public void Compute_2026FromNovemberPartnerWithoutIncome_withholdsTheQuotientsAmount(decimal monthlyPay, decimal expected)
    {
        var result = Compute(November2026, monthlyPay, WithholdingHousehold.PartnerWithoutIncome);

        result.MonthlyWithholding.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(NovemberBothAged66WithholdingByPay))]
    public void Compute_2026FromNovemberPartnersBothAged66_withholdsTheQuotientsAmountUpTo12790(decimal monthlyPay, decimal expected)
    {
        var result = Compute(November2026, monthlyPay, WithholdingHousehold.PartnerWithoutIncomeBothAged66);

        result.MonthlyWithholding.ShouldBe(expected);
    }

    // The key formula's reference case: 30% of 37,257.43 is 11,177.229, rounded to 11,177.23; 26.75%
    // of it; 4,469.93 + 42.80% of the 9,370.20 of the rest over 16,710; their sum less 5,975.96.
    [Fact]
    public void Compute_2026Pay4166Point67PartnerWithoutIncome_listsEveryLine()
    {
        var result = Compute(January2026, 4_166.67m, WithholdingHousehold.PartnerWithoutIncome);

        (result.Reduction, result.AnnualGross, result.FlatRateCosts, result.AnnualNetTaxable, result.ImputedToPartner, result.PartnerTax, result.BasicTax,
                result.TaxFreeAmountReduction, result.AnnualTax, result.MonthlyWithholding)
            .ShouldBe((965.86m, 38_409.72m, 1_152.29m, 37_257.43m, 11_177.23m, 2_989.91m, 8_480.38m, 5_975.96m, 5_494.33m, 457.86m));
    }

    // The same lines from November, 6,179.26 taken off them.
    [Fact]
    public void Compute_2026FromNovemberPay4166Point67PartnerWithoutIncome_listsEveryLine()
    {
        var result = Compute(November2026, 4_166.67m, WithholdingHousehold.PartnerWithoutIncome);

        (result.ImputedToPartner, result.PartnerTax, result.BasicTax, result.TaxFreeAmountReduction, result.AnnualTax, result.MonthlyWithholding)
            .ShouldBe((11_177.23m, 2_989.91m, 8_480.38m, 6_179.26m, 5_291.03m, 440.92m));
    }

    // The SPF simulator's own example, rows 34 and 35: 30% of 22,028.35 is 6,608.505, rounded up to
    // 6,608.51; the two taxes, 1,767.78 and 4,124.81, stay under the 5,975.96 taken off them.
    [Fact]
    public void Compute_2026Pay2500PartnerWithoutIncome_isTheSimulatorsExampleAndWithholdsNothing()
    {
        var result = Compute(January2026, 2_500m, WithholdingHousehold.PartnerWithoutIncome);

        (result.AnnualNetTaxable, result.ImputedToPartner, result.PartnerTax, result.BasicTax, result.AnnualTax, result.MonthlyWithholding)
            .ShouldBe((22_028.35m, 6_608.51m, 1_767.78m, 4_124.81m, 0m, 0m));
    }

    // The rest, 80,973.80, is taxed on the scale's fourth band: 20,320.38 + 53.50% of its slice over
    // 51,050. The simulator's stale band would give 36,987.28 and withhold 2,891.68.
    [Fact]
    public void Compute_2026Pay10000PartnerWithoutIncome_taxesTheRestOnTheFourthBandOfNumber31()
    {
        var result = Compute(January2026, 10_000m, WithholdingHousehold.PartnerWithoutIncome);

        (result.AnnualNetTaxable, result.ImputedToPartner, result.PartnerTax, result.BasicTax, result.AnnualTax, result.MonthlyWithholding)
            .ShouldBe((94_763.80m, 13_790m, 3_688.83m, 36_329.61m, 34_042.48m, 2_836.87m));
    }

    // 30% of 56,841.84 is beyond every maximum: each period imputes its own.
    [Theory]
    [InlineData(1, WithholdingHousehold.PartnerWithoutIncome, 13_790)]
    [InlineData(1, WithholdingHousehold.PartnerWithoutIncomeBothAged66, 13_790)]
    [InlineData(11, WithholdingHousehold.PartnerWithoutIncome, 11_780)]
    [InlineData(11, WithholdingHousehold.PartnerWithoutIncomeBothAged66, 12_790)]
    public void Compute_2026Pay6310_imputesThePeriodsMaximum(int month, WithholdingHousehold household, int maximum)
    {
        var result = Compute(WithholdingRules.IncomeYear2026.InMonth(month), 6_310m, household);

        result.ImputedToPartner.ShouldBe(maximum);
    }

    // Before November the annex has one maximum whatever the partners' ages.
    [Theory]
    [InlineData(4_166.67)]
    [InlineData(6_310)]
    [InlineData(10_000)]
    public void Compute_2026PartnersBothAged66_isAsAnyPartnerWithoutIncomeToOctober(decimal monthlyPay)
    {
        var bothAged66 = Compute(January2026, monthlyPay, WithholdingHousehold.PartnerWithoutIncomeBothAged66);

        bothAged66.ShouldBe(Compute(January2026, monthlyPay, WithholdingHousehold.PartnerWithoutIncome) with { Household = WithholdingHousehold.PartnerWithoutIncomeBothAged66 });
    }

    // Under the single method no income is imputed and no partner taxed: the lines are those of n° 32.
    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void Compute_2026AsSingle_isTheSingleMethodWithNothingImputed(int month)
    {
        var rules = WithholdingRules.IncomeYear2026.InMonth(month);

        var result = Compute(rules, 4_166.67m, WithholdingHousehold.AsSingle);

        (result.Household, result.ImputedToPartner, result.PartnerTax, result.TaxFreeAmountReduction).ShouldBe((WithholdingHousehold.AsSingle, 0m, 0m, rules.TaxFreeAmountReduction.Value));
    }

    // Not saying the household is the single method, as before the quotient.
    [Fact]
    public void Compute_householdNotSaid_isAsSingle()
    {
        var (value, _) = WithholdingCalculator.Compute(January2026, 4_166.67m);

        value.ShouldBe(Compute(January2026, 4_166.67m, WithholdingHousehold.AsSingle));
    }

    [Theory]
    [InlineData(WithholdingHousehold.PartnerWithoutIncome)]
    [InlineData(WithholdingHousehold.PartnerWithoutIncomeBothAged66)]
    public void Compute_2026_echoesTheHousehold(WithholdingHousehold household)
    {
        Compute(January2026, 4_166.67m, household).Household.ShouldBe(household);
    }

    [Fact]
    public void Compute_negativePayPartnerWithoutIncome_isRefused()
    {
        var (value, error) = WithholdingCalculator.Compute(January2026, -0.01m, WithholdingHousehold.PartnerWithoutIncome);

        (value, error).ShouldBe((null, WithholdingCalculator.MonthlyPayNegative));
    }

    [Fact]
    public void Compute_householdNoCaseNames_throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => WithholdingCalculator.Compute(January2026, 4_166.67m, (WithholdingHousehold)99));
    }

    private static WithholdingResult Compute(WithholdingRules rules, decimal monthlyPay, WithholdingHousehold household)
    {
        var (value, error) = WithholdingCalculator.Compute(rules, monthlyPay, household);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
