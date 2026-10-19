using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// Made-up cases T1 to T6 (Notion, « Netto computes how gross director pay turns into net for income
// year 2026 »): single, resident in Wallonia, director pay in cadre XVI with the flat-rate costs, no
// other income. Income year 2025 expected values are Tax-Calc « exercice 2026 » runs of 2026-10-19
// (calculation version IPP_EX_2026 V_1.8.0), line by line from its calculation detail. Income year
// 2026 expected values are derived from the official amounts, as Tax-Calc does not cover 2026 yet.
public class PersonalTaxCalculatorTests
{
    private static readonly PersonalTaxRules Rules2025 = PersonalTaxRules.IncomeYear2025;
    private static readonly PersonalTaxRules Rules2026 = PersonalTaxRules.IncomeYear2026;

    [Fact]
    public void Compute_2025TaxCalcT1TaxableUnderTaxFreeAmount_isNoTax()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 14_000.00m, 3_500.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 315.00m, TaxableIncome: 10_185.00m,
            BasicTax: 2_546.25m, TaxFreeAmountReduction: 2_546.25m, StateTax: 0.00m,
            ReducedStateTax: 0.00m, RegionalAdditionalTax: 0.00m, IncomeTax: 0.00m, MunicipalSurcharge: 0.00m));
    }

    [Fact]
    public void Compute_2025TaxCalcT2FirstBracket_matchesTaxCalc()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 20_000.00m, 3_600.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 492.00m, TaxableIncome: 15_908.00m,
            BasicTax: 3_977.00m, TaxFreeAmountReduction: 2_727.50m, StateTax: 1_249.50m,
            ReducedStateTax: 937.66m, RegionalAdditionalTax: 311.84m, IncomeTax: 1_249.50m, MunicipalSurcharge: 87.47m));
    }

    // 3% × 29,690.72 = 890.7216: the flat-rate costs are rounded to the cent before they are
    // deducted, which puts the taxable income exactly on the second bracket's upper limit.
    [Fact]
    public void Compute_2025TaxCalcT3TaxableAtSecondBracketLimit_matchesTaxCalc()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 35_000.00m, 5_309.28m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 890.72m, TaxableIncome: 28_800.00m,
            BasicTax: 9_072.00m, TaxFreeAmountReduction: 2_727.50m, StateTax: 6_344.50m,
            ReducedStateTax: 4_761.10m, RegionalAdditionalTax: 1_583.40m, IncomeTax: 6_344.50m, MunicipalSurcharge: 444.12m));
    }

    [Fact]
    public void Compute_2025TaxCalcT4ThirdBracket_matchesTaxCalc()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 50_000.00m, 8_300.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_251.00m, TaxableIncome: 40_449.00m,
            BasicTax: 14_314.05m, TaxFreeAmountReduction: 2_727.50m, StateTax: 11_586.55m,
            ReducedStateTax: 8_694.89m, RegionalAdditionalTax: 2_891.66m, IncomeTax: 11_586.55m, MunicipalSurcharge: 811.06m));
    }

    [Fact]
    public void Compute_2025TaxCalcT5OtherMunicipalRate_changesOnlyTheSurcharge()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 50_000.00m, 8_300.00m, 0.085m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_251.00m, TaxableIncome: 40_449.00m,
            BasicTax: 14_314.05m, TaxFreeAmountReduction: 2_727.50m, StateTax: 11_586.55m,
            ReducedStateTax: 8_694.89m, RegionalAdditionalTax: 2_891.66m, IncomeTax: 11_586.55m, MunicipalSurcharge: 984.86m));
    }

    // The federal share (75.043%) and the regional tax (33.257% of it) are each rounded to the cent,
    // so their sum is 0.03 above the State tax here, and the surcharge applies to that sum.
    [Fact]
    public void Compute_2025TaxCalcT6TopBracketWithCappedFlatRateCosts_matchesTaxCalc()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 130_000.00m, 19_900.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 3_130.00m, TaxableIncome: 106_970.00m,
            BasicTax: 47_105.00m, TaxFreeAmountReduction: 2_727.50m, StateTax: 44_377.50m,
            ReducedStateTax: 33_302.21m, RegionalAdditionalTax: 11_075.32m, IncomeTax: 44_377.53m, MunicipalSurcharge: 3_106.43m));
    }

    [Fact]
    public void Compute_2026DerivedT1TaxableUnderTaxFreeAmount_isNoTax()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 14_000.00m, 3_500.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 315.00m, TaxableIncome: 10_185.00m,
            BasicTax: 2_546.25m, TaxFreeAmountReduction: 2_546.25m, StateTax: 0.00m,
            ReducedStateTax: 0.00m, RegionalAdditionalTax: 0.00m, IncomeTax: 0.00m, MunicipalSurcharge: 0.00m));
    }

    [Fact]
    public void Compute_2026DerivedT2FirstBracket_usesThe2026TaxFreeAmount()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 20_000.00m, 3_600.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 492.00m, TaxableIncome: 15_908.00m,
            BasicTax: 3_977.00m, TaxFreeAmountReduction: 2_887.50m, StateTax: 1_089.50m,
            ReducedStateTax: 817.59m, RegionalAdditionalTax: 271.91m, IncomeTax: 1_089.50m, MunicipalSurcharge: 76.27m));
    }

    [Fact]
    public void Compute_2026DerivedT3SecondBracket_usesThe2026Scale()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 35_000.00m, 5_309.28m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 890.72m, TaxableIncome: 28_800.00m,
            BasicTax: 9_012.00m, TaxFreeAmountReduction: 2_887.50m, StateTax: 6_124.50m,
            ReducedStateTax: 4_596.01m, RegionalAdditionalTax: 1_528.50m, IncomeTax: 6_124.51m, MunicipalSurcharge: 428.72m));
    }

    [Fact]
    public void Compute_2026DerivedT4ThirdBracket_usesThe2026Scale()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 50_000.00m, 8_300.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_251.00m, TaxableIncome: 40_449.00m,
            BasicTax: 14_218.55m, TaxFreeAmountReduction: 2_887.50m, StateTax: 11_331.05m,
            ReducedStateTax: 8_503.16m, RegionalAdditionalTax: 2_827.90m, IncomeTax: 11_331.06m, MunicipalSurcharge: 793.17m));
    }

    [Fact]
    public void Compute_2026DerivedT5OtherMunicipalRate_changesOnlyTheSurcharge()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 50_000.00m, 8_300.00m, 0.085m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_251.00m, TaxableIncome: 40_449.00m,
            BasicTax: 14_218.55m, TaxFreeAmountReduction: 2_887.50m, StateTax: 11_331.05m,
            ReducedStateTax: 8_503.16m, RegionalAdditionalTax: 2_827.90m, IncomeTax: 11_331.06m, MunicipalSurcharge: 963.14m));
    }

    [Fact]
    public void Compute_2026DerivedT6TopBracket_capsFlatRateCostsAt3200()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 130_000.00m, 19_900.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 3_200.00m, TaxableIncome: 106_900.00m,
            BasicTax: 46_913.00m, TaxFreeAmountReduction: 2_887.50m, StateTax: 44_025.50m,
            ReducedStateTax: 33_038.06m, RegionalAdditionalTax: 10_987.47m, IncomeTax: 44_025.53m, MunicipalSurcharge: 3_081.79m));
    }

    // Pay chosen, with 4,000 of contributions, so that the taxable income lands exactly on the
    // tax-free amount and on each bracket's upper limit: 25% × 16,720 = 4,180; + 40% × 12,790 =
    // 9,296; + 45% × 21,560 = 18,998; each minus 25% × 11,550 = 2,887.50.
    public static TheoryData<decimal, decimal, decimal> PayAtTaxFreeAmountAndBracketLimits2026 => new()
    {
        { 15_907.22m, 11_550.00m, 0.00m },
        { 21_237.11m, 16_720.00m, 1_292.50m },
        { 34_422.68m, 29_510.00m, 6_408.50m },
        { 56_649.48m, 51_070.00m, 16_110.50m },
    };

    [Theory]
    [MemberData(nameof(PayAtTaxFreeAmountAndBracketLimits2026))]
    public void Compute_2026TaxableAtTaxFreeAmountOrBracketLimit_isScaleMinusTaxFreeReduction(
        decimal directorPay, decimal taxableIncome, decimal stateTax)
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, directorPay, 4_000.00m, 0.07m);

        (result.TaxableIncome, result.StateTax).ShouldBe((taxableIncome, stateTax));
    }

    [Fact]
    public void Compute_2026ZeroMunicipalRate_isNoSurcharge()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 50_000.00m, 8_300.00m, 0.00m);

        result.MunicipalSurcharge.ShouldBe(0.00m);
    }
}
