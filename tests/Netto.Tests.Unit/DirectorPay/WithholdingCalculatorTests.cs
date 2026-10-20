using Netto.Core.DirectorPay;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// The key formula ESS-SR/2025-0928 on made-up monthly pays of income year 2026: the reduction of
// n° 7, 1°, times 12; the flat-rate costs of n° 8, 2°; the scale of annexe 1 less the 2,987.98 of
// n° 11, a; divided by 12 (n° 16), never below zero (n° 21); each step rounded to the cent half away
// from zero (n° 3), in the chain the SPF simulator « Précompte professionnel 2026 », sheet
// « Dirigeants d'entreprise », follows: E = round(reduction); F = round(pay − E) × 12;
// G = min(round(F × 3%), 3,200); H = F − G; I = round(scale(H) − 2,987.98); N = max(0, I);
// O = round(N ÷ 12). Expected values are derived from the formula and cross-checked against that
// chain: no Tax-Calc run covers the withholding, which Tax-Calc takes as an input (code 1407).
public class WithholdingCalculatorTests
{
    private static readonly WithholdingRules Rules2026 = WithholdingRules.IncomeYear2026;

    // 1,465 gives 374.82 of annual tax, whose twelfth, 31.235, rounds up to 31.24.
    public static TheoryData<decimal, decimal> WithholdingByPay => new()
    {
        { 1_000m, 0m },
        { 1_465m, 31.24m },
        { 1_465.01m, 31.24m },
        { 2_000m, 150.23m },
        { 3_475m, 637.35m },
        { 4_166.67m, 890.94m },
        { 6_310m, 1_702.59m },
        { 8_000m, 2_452.44m },
        { 9_290m, 3_024.82m },
        { 9_290.01m, 3_024.82m },
        { 10_000m, 3_393.27m },
        { 12_000m, 4_451.27m },
    };

    // N° 25.2 at each band and its edges. At 6,311 the table's own 1,426.68 plus 0.145 gives
    // 1,426.825, rounded to 1,426.83; 385 plus 21.50% of 4,845 would give 1,426.675 plus 0.145,
    // rounded to 1,426.82.
    public static TheoryData<decimal, decimal> ReductionByPay => new()
    {
        { 1_000m, 385m },
        { 1_465m, 385m },
        { 1_465.01m, 385m },
        { 2_000m, 500.03m },
        { 3_475m, 817.15m },
        { 4_166.67m, 965.86m },
        { 6_310m, 1_426.68m },
        { 6_311m, 1_426.83m },
        { 8_000m, 1_671.73m },
        { 9_290m, 1_858.78m },
        { 9_290.01m, 1_858.78m },
        { 10_000m, 1_858.78m },
    };

    [Theory]
    [MemberData(nameof(WithholdingByPay))]
    public void Compute_2026MonthlyPay_withholdsTheKeyFormulasAmount(decimal monthlyPay, decimal expected)
    {
        var result = Compute2026(monthlyPay);

        result.MonthlyWithholding.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(ReductionByPay))]
    public void Compute_2026MonthlyPay_reducesItByTheTableOfNumber25Point2(decimal monthlyPay, decimal expected)
    {
        var result = Compute2026(monthlyPay);

        result.Reduction.ShouldBe(expected);
    }

    // 615 × 12 of gross, 3% of it, 26.75% of the net: 1,914.93 is under the 2,987.98 taken off it.
    [Fact]
    public void Compute_2026Pay1000_listsEveryLineAndOwesNoTax()
    {
        var result = Compute2026(1_000m);

        (result.Reduction, result.AnnualGross, result.FlatRateCosts, result.AnnualNetTaxable, result.BasicTax, result.TaxFreeAmountReduction, result.AnnualTax, result.MonthlyWithholding)
            .ShouldBe((385m, 7_380m, 221.40m, 7_158.60m, 1_914.93m, 2_987.98m, 0m, 0m));
    }

    // 385 + 21.50% of 2,010; 2,657.85 × 12; 9,944.05 + 48.15% of 1,437.37.
    [Fact]
    public void Compute_2026Pay3475_listsEveryLine()
    {
        var result = Compute2026(3_475m);

        (result.Reduction, result.AnnualGross, result.FlatRateCosts, result.AnnualNetTaxable, result.BasicTax, result.TaxFreeAmountReduction, result.AnnualTax, result.MonthlyWithholding)
            .ShouldBe((817.15m, 31_894.20m, 956.83m, 30_937.37m, 10_636.14m, 2_987.98m, 7_648.16m, 637.35m));
    }

    // The item's reference case: 965.859 rounded, 3,200.81 × 12, 1,152.2916 rounded, 9,944.05 +
    // 48.15% of 7,757.43.
    [Fact]
    public void Compute_2026Pay4166Point67_listsEveryLine()
    {
        var result = Compute2026(4_166.67m);

        (result.Reduction, result.AnnualGross, result.FlatRateCosts, result.AnnualNetTaxable, result.BasicTax, result.TaxFreeAmountReduction, result.AnnualTax, result.MonthlyWithholding)
            .ShouldBe((965.86m, 38_409.72m, 1_152.29m, 37_257.43m, 13_679.25m, 2_987.98m, 10_691.27m, 890.94m));
    }

    // 8,141.22 × 12; 20,320.38 + 53.50% of 43,713.80.
    [Fact]
    public void Compute_2026Pay10000_listsEveryLine()
    {
        var result = Compute2026(10_000m);

        (result.Reduction, result.AnnualGross, result.FlatRateCosts, result.AnnualNetTaxable, result.BasicTax, result.TaxFreeAmountReduction, result.AnnualTax, result.MonthlyWithholding)
            .ShouldBe((1_858.78m, 97_694.64m, 2_930.84m, 94_763.80m, 43_707.26m, 2_987.98m, 40_719.28m, 3_393.27m));
    }

    // 3% of 121,694.64 would be 3,650.84.
    [Fact]
    public void Compute_2026Pay12000_capsTheFlatRateCostsAt3200()
    {
        var result = Compute2026(12_000m);

        (result.FlatRateCosts, result.AnnualTax, result.MonthlyWithholding).ShouldBe((3_200m, 53_415.28m, 4_451.27m));
    }

    // The formula takes the 385 off a pay under it as it does off any other: the year's figures
    // fall below zero, the tax is floored at it, and nothing is withheld.
    [Fact]
    public void Compute_2026PayUnderTheReduction_withholdsNothing()
    {
        var result = Compute2026(300m);

        (result.Reduction, result.AnnualTax, result.MonthlyWithholding).ShouldBe((385m, 0m, 0m));
    }

    [Fact]
    public void Compute_2026_echoesThePay()
    {
        Compute2026(4_166.67m).MonthlyTaxablePay.ShouldBe(4_166.67m);
    }

    [Fact]
    public void Compute_negativePay_isRefused()
    {
        var (value, error) = WithholdingCalculator.Compute(Rules2026, -0.01m);

        (value, error).ShouldBe((null, WithholdingCalculator.MonthlyPayNegative));
    }

    private static WithholdingResult Compute2026(decimal monthlyPay)
    {
        var (value, error) = WithholdingCalculator.Compute(Rules2026, monthlyPay);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
