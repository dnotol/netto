using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// Made-up cases T1 to T6 (Notion, « Netto computes how gross director pay turns into net for income
// year 2026 ») and T7, T8 (Notion, « Net in pocket counts the tax credit for low earned income »):
// single, resident in Wallonia, director pay in cadre XVI with the flat-rate costs, no other income.
// Income year 2025 expected values are Tax-Calc « exercice 2026 » runs of 2026-10-19 (calculation
// version IPP_EX_2026 V_1.8.0), line by line from its calculation detail, the credit from its line
// « Faibles revenus d'activités ». Income year 2026 expected values are derived from the official
// amounts, as Tax-Calc does not cover 2026 yet.
public class PersonalTaxCalculatorTests
{
    private static readonly PersonalTaxRules Rules2025 = PersonalTaxRules.IncomeYear2025;
    private static readonly PersonalTaxRules Rules2026 = PersonalTaxRules.IncomeYear2026;

    // No tax to set it against, so the whole credit is refunded.
    [Fact]
    public void Compute_2025TaxCalcT1TaxableUnderTaxFreeAmount_isNoTaxAndTheFullCredit()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 14_000.00m, 3_500.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 315.00m, TaxableIncome: 10_185.00m,
            BasicTax: 2_546.25m, TaxFreeAmountReduction: 2_546.25m, StateTax: 0.00m,
            ReducedStateTax: 0.00m, RegionalAdditionalTax: 0.00m, IncomeTax: 0.00m, MunicipalSurcharge: 0.00m,
            LowEarnedIncomeCredit: 880.00m));
    }

    [Fact]
    public void Compute_2025TaxCalcT2FirstBracket_matchesTaxCalc()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 20_000.00m, 3_600.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 492.00m, TaxableIncome: 15_908.00m,
            BasicTax: 3_977.00m, TaxFreeAmountReduction: 2_727.50m, StateTax: 1_249.50m,
            ReducedStateTax: 937.66m, RegionalAdditionalTax: 311.84m, IncomeTax: 1_249.50m, MunicipalSurcharge: 87.47m,
            LowEarnedIncomeCredit: 880.00m));
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
            ReducedStateTax: 4_761.10m, RegionalAdditionalTax: 1_583.40m, IncomeTax: 6_344.50m, MunicipalSurcharge: 444.12m,
            LowEarnedIncomeCredit: 0.00m));
    }

    [Fact]
    public void Compute_2025TaxCalcT4ThirdBracket_matchesTaxCalc()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 50_000.00m, 8_300.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_251.00m, TaxableIncome: 40_449.00m,
            BasicTax: 14_314.05m, TaxFreeAmountReduction: 2_727.50m, StateTax: 11_586.55m,
            ReducedStateTax: 8_694.89m, RegionalAdditionalTax: 2_891.66m, IncomeTax: 11_586.55m, MunicipalSurcharge: 811.06m,
            LowEarnedIncomeCredit: 0.00m));
    }

    // T4 read as the company-paid case (« The company can pay the director's social contributions on
    // their behalf »): a payslip gross of 41,700 with 8,300 of contributions paid by the company is a
    // pay of 50,000 (cadre XVI, 1) with 8,300 of own contributions deducted (cadre XVI, 10), so it
    // gives T4's figures: the final tax equals the director-pays case for the same taxable pay.
    [Fact]
    public void Compute_2025TaxCalcT4AsCompanyPaidContributions_isThePayPlusThemWithThemDeducted()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 41_700.00m + 8_300.00m, 8_300.00m, 0.07m);

        (result.FlatRateCosts, result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge).ShouldBe((1_251.00m, 40_449.00m, 11_586.55m, 811.06m));
    }

    [Fact]
    public void Compute_2025TaxCalcT5OtherMunicipalRate_changesOnlyTheSurcharge()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 50_000.00m, 8_300.00m, 0.085m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_251.00m, TaxableIncome: 40_449.00m,
            BasicTax: 14_314.05m, TaxFreeAmountReduction: 2_727.50m, StateTax: 11_586.55m,
            ReducedStateTax: 8_694.89m, RegionalAdditionalTax: 2_891.66m, IncomeTax: 11_586.55m, MunicipalSurcharge: 984.86m,
            LowEarnedIncomeCredit: 0.00m));
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
            ReducedStateTax: 33_302.21m, RegionalAdditionalTax: 11_075.32m, IncomeTax: 44_377.53m, MunicipalSurcharge: 3_106.43m,
            LowEarnedIncomeCredit: 0.00m));
    }

    // Where the credit falls: 880 × (28,420 − 22,601) / (28,420 − 21,860) = 780.5976, rounded to the
    // cent, not cut. The surcharge is 7% of the tax before the credit.
    [Fact]
    public void Compute_2025TaxCalcT7CreditFalling_matchesTaxCalc()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 26_800.00m, 3_500.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 699.00m, TaxableIncome: 22_601.00m,
            BasicTax: 6_592.40m, TaxFreeAmountReduction: 2_727.50m, StateTax: 3_864.90m,
            ReducedStateTax: 2_900.34m, RegionalAdditionalTax: 964.57m, IncomeTax: 3_864.91m, MunicipalSurcharge: 270.54m,
            LowEarnedIncomeCredit: 780.60m));
    }

    // Where the credit rises: 880 × (7,178 − 6,550) / (8,740 − 6,550) = 252.3470. The tax-free amount
    // frees only what there is, so there is no tax and Tax-Calc refunds the whole credit.
    [Fact]
    public void Compute_2025TaxCalcT8CreditRising_matchesTaxCalc()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 10_800.00m, 3_400.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 222.00m, TaxableIncome: 7_178.00m,
            BasicTax: 1_794.50m, TaxFreeAmountReduction: 1_794.50m, StateTax: 0.00m,
            ReducedStateTax: 0.00m, RegionalAdditionalTax: 0.00m, IncomeTax: 0.00m, MunicipalSurcharge: 0.00m,
            LowEarnedIncomeCredit: 252.35m));
    }

    [Fact]
    public void Compute_2026DerivedT1TaxableUnderTaxFreeAmount_isNoTax()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 14_000.00m, 3_500.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 315.00m, TaxableIncome: 10_185.00m,
            BasicTax: 2_546.25m, TaxFreeAmountReduction: 2_546.25m, StateTax: 0.00m,
            ReducedStateTax: 0.00m, RegionalAdditionalTax: 0.00m, IncomeTax: 0.00m, MunicipalSurcharge: 0.00m,
            LowEarnedIncomeCredit: 910.00m));
    }

    [Fact]
    public void Compute_2026DerivedT2FirstBracket_usesThe2026TaxFreeAmount()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 20_000.00m, 3_600.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 492.00m, TaxableIncome: 15_908.00m,
            BasicTax: 3_977.00m, TaxFreeAmountReduction: 2_887.50m, StateTax: 1_089.50m,
            ReducedStateTax: 817.59m, RegionalAdditionalTax: 271.91m, IncomeTax: 1_089.50m, MunicipalSurcharge: 76.27m,
            LowEarnedIncomeCredit: 910.00m));
    }

    // 28,800 is past 2025's ceiling of 28,420 but still where the 2026 credit falls:
    // 910 × (29,120 − 28,800) / (29,120 − 22,400) = 43.33.
    [Fact]
    public void Compute_2026DerivedT3SecondBracket_usesThe2026Scale()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 35_000.00m, 5_309.28m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 890.72m, TaxableIncome: 28_800.00m,
            BasicTax: 9_012.00m, TaxFreeAmountReduction: 2_887.50m, StateTax: 6_124.50m,
            ReducedStateTax: 4_596.01m, RegionalAdditionalTax: 1_528.50m, IncomeTax: 6_124.51m, MunicipalSurcharge: 428.72m,
            LowEarnedIncomeCredit: 43.33m));
    }

    [Fact]
    public void Compute_2026DerivedT4ThirdBracket_usesThe2026Scale()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 50_000.00m, 8_300.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_251.00m, TaxableIncome: 40_449.00m,
            BasicTax: 14_218.55m, TaxFreeAmountReduction: 2_887.50m, StateTax: 11_331.05m,
            ReducedStateTax: 8_503.16m, RegionalAdditionalTax: 2_827.90m, IncomeTax: 11_331.06m, MunicipalSurcharge: 793.17m,
            LowEarnedIncomeCredit: 0.00m));
    }

    // The derived T4 read as the company-paid case, like T4 on the 2025 amounts.
    [Fact]
    public void Compute_2026DerivedT4AsCompanyPaidContributions_isThePayPlusThemWithThemDeducted()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 41_700.00m + 8_300.00m, 8_300.00m, 0.07m);

        (result.FlatRateCosts, result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge).ShouldBe((1_251.00m, 40_449.00m, 11_331.06m, 793.17m));
    }

    [Fact]
    public void Compute_2026DerivedT5OtherMunicipalRate_changesOnlyTheSurcharge()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 50_000.00m, 8_300.00m, 0.085m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_251.00m, TaxableIncome: 40_449.00m,
            BasicTax: 14_218.55m, TaxFreeAmountReduction: 2_887.50m, StateTax: 11_331.05m,
            ReducedStateTax: 8_503.16m, RegionalAdditionalTax: 2_827.90m, IncomeTax: 11_331.06m, MunicipalSurcharge: 963.14m,
            LowEarnedIncomeCredit: 0.00m));
    }

    [Fact]
    public void Compute_2026DerivedT6TopBracket_capsFlatRateCostsAt3200()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 130_000.00m, 19_900.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 3_200.00m, TaxableIncome: 106_900.00m,
            BasicTax: 46_913.00m, TaxFreeAmountReduction: 2_887.50m, StateTax: 44_025.50m,
            ReducedStateTax: 33_038.06m, RegionalAdditionalTax: 10_987.47m, IncomeTax: 44_025.53m, MunicipalSurcharge: 3_081.79m,
            LowEarnedIncomeCredit: 0.00m));
    }

    // T7 and T8's pays on the 2026 amounts: 910 × (29,120 − 22,601) / 6,720 = 882.78 and
    // 910 × (7,178 − 6,710) / 2,250 = 189.28.
    public static TheoryData<decimal, decimal, decimal, decimal> T7AndT8On2026Amounts => new()
    {
        { 26_800.00m, 3_500.00m, 22_601.00m, 882.78m },
        { 10_800.00m, 3_400.00m, 7_178.00m, 189.28m },
    };

    [Theory]
    [MemberData(nameof(T7AndT8On2026Amounts))]
    public void Compute_2026DerivedT7AndT8_creditOnThe2026Amounts(
        decimal directorPay, decimal ownContributions, decimal taxableIncome, decimal credit)
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, directorPay, ownContributions, 0.07m);

        (result.TaxableIncome, result.LowEarnedIncomeCredit).ShouldBe((taxableIncome, credit));
    }

    // Activity income at each limit of art. 289ter and halfway between them, on the 2026 amounts:
    // none up to 6,710, rising to 910 at 8,960, 910 up to 22,400, falling to none at 29,120.
    public static TheoryData<decimal, decimal> CreditAtTheLimits2026 => new()
    {
        { 0.00m, 0.00m },
        { 6_710.00m, 0.00m },
        { 6_710.01m, 0.00m },
        { 7_835.00m, 455.00m },
        { 8_960.00m, 910.00m },
        { 22_400.00m, 910.00m },
        { 25_760.00m, 455.00m },
        { 29_119.99m, 0.00m },
        { 29_120.00m, 0.00m },
        { 29_120.01m, 0.00m },
        { 106_900.00m, 0.00m },
    };

    [Theory]
    [MemberData(nameof(CreditAtTheLimits2026))]
    public void LowEarnedIncomeCredit_2026_followsTheLimitsOfTheLaw(decimal activityIncome, decimal credit)
    {
        PersonalTaxCalculator.LowEarnedIncomeCredit(Rules2026, activityIncome).ShouldBe(credit);
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

    // Tax-Calc T13 of 2026-10-20 (Notion, « Rent for the office in my home reaches my pocket, kept at
    // or below the cap »): T4's pay with 810.72 of net rent from a home office let to the company
    // (codes 1109 and 1110: a cadastral income of 144 let for 1,351.20), globalised with the pay's
    // 40,449.00: one scale and one tax-free amount on 41,259.72, the surcharge on the whole tax.
    [Fact]
    public void Compute_2025TaxCalcT13PayAndRent_taxesTheTotalNetIncomeOnOneScale()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 50_000.00m, 8_300.00m, 0.07m, 810.72m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_251.00m, TaxableIncome: 40_449.00m,
            BasicTax: 14_678.87m, TaxFreeAmountReduction: 2_727.50m, StateTax: 11_951.37m,
            ReducedStateTax: 8_968.67m, RegionalAdditionalTax: 2_982.71m, IncomeTax: 11_951.38m, MunicipalSurcharge: 836.60m,
            LowEarnedIncomeCredit: 0.00m, OtherIncome: 810.72m));
    }

    [Fact]
    public void Compute_2025TaxCalcT13PayAndRent_totalNetIncomeIsThePaysTaxableIncomeAndTheRent()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 50_000.00m, 8_300.00m, 0.07m, 810.72m);

        result.TotalNetIncome.ShouldBe(41_259.72m);
    }

    // Tax-Calc T14 of 2026-10-20: 28,033.00 of pay and the same 810.72 of rent, 28,843.72 of total net
    // income, above 2025's ceiling of 28,420: no credit at all, although the pay alone gets one.
    [Fact]
    public void Compute_2025TaxCalcT14RentTakesTheTotalAboveTheCeiling_leavesNoCredit()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 32_500.00m, 3_600.00m, 0.07m, 810.72m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 867.00m, TaxableIncome: 28_033.00m,
            BasicTax: 9_091.67m, TaxFreeAmountReduction: 2_727.50m, StateTax: 6_364.17m,
            ReducedStateTax: 4_775.86m, RegionalAdditionalTax: 1_588.31m, IncomeTax: 6_364.17m, MunicipalSurcharge: 445.49m,
            LowEarnedIncomeCredit: 0.00m, OtherIncome: 810.72m));
    }

    // Tax-Calc T15 of 2026-10-22 (Notion, « An electric company car joins the plan »): 40,000 of pay
    // with a company car's benefit of 2,400, which the director's 281.20 sums with the pay and the
    // return declares with it in code 1400 (cadre XVI has no code of its own for a benefit), 7,000 of
    // own contributions, nothing withheld. The benefit is taxed exactly as pay: flat-rate costs on it,
    // one scale on the 34,338.00 it leaves.
    [Fact]
    public void Compute_2025TaxCalcT15PayWithACarBenefit_taxesTheBenefitAsPay()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 40_000.00m + 2_400.00m, 7_000.00m, 0.07m);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_062.00m, TaxableIncome: 34_338.00m,
            BasicTax: 11_564.10m, TaxFreeAmountReduction: 2_727.50m, StateTax: 8_836.60m,
            ReducedStateTax: 6_631.25m, RegionalAdditionalTax: 2_205.35m, IncomeTax: 8_836.60m, MunicipalSurcharge: 618.56m,
            LowEarnedIncomeCredit: 0.00m));
    }

    // Tax-Calc T16 of 2026-10-22 (Notion, « I can weigh a copyright royalty in my plan »): T4's pay with
    // 10,000 of copyright income (code 1123, no costs in 1124), taxed apart at 15%. The 1,500.00 joins the
    // State tax before the autonomy factor, « Impôt Etat » 13,086.55, which is split into 9,820.54 and
    // 3,266.02 of regional tax: the total tax is 13,086.56, and the municipal surcharge is on it.
    [Fact]
    public void Compute_2025TaxCalcT16PayAndRoyalty_taxesTheRoyaltyApartBeforeTheSplit()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 50_000.00m, 8_300.00m, 0.07m, taxedApart: Royalty2025(10_000.00m));

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_251.00m, TaxableIncome: 40_449.00m,
            BasicTax: 14_314.05m, TaxFreeAmountReduction: 2_727.50m, StateTax: 11_586.55m,
            ReducedStateTax: 9_820.54m, RegionalAdditionalTax: 3_266.02m, IncomeTax: 13_086.56m, MunicipalSurcharge: 916.06m,
            LowEarnedIncomeCredit: 0.00m, IncomeTaxedApart: 10_000.00m, TaxedApart: 1_500.00m));
    }

    // Tax-Calc T17 of 2026-10-22: T2's pay with 8,000 of copyright income. « Ensemble des revenus nets »
    // counts the royalty, 23,908.00, which limits the credit to 880 × (28,420 − 23,908) / 6,560 = 605.27,
    // though its base, « Revenus d'activité », is the pay's 15,908.00 alone.
    [Fact]
    public void Compute_2025TaxCalcT17RoyaltyRaisesTheTotalNetIncome_limitsTheCreditButNotItsBase()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 20_000.00m, 3_600.00m, 0.07m, taxedApart: Royalty2025(8_000.00m));

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 492.00m, TaxableIncome: 15_908.00m,
            BasicTax: 3_977.00m, TaxFreeAmountReduction: 2_727.50m, StateTax: 1_249.50m,
            ReducedStateTax: 1_838.18m, RegionalAdditionalTax: 611.32m, IncomeTax: 2_449.50m, MunicipalSurcharge: 171.47m,
            LowEarnedIncomeCredit: 605.27m, IncomeTaxedApart: 8_000.00m, TaxedApart: 1_200.00m));
    }

    [Fact]
    public void Compute_2025TaxCalcT17RoyaltyRaisesTheTotalNetIncome_countsItThere()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 20_000.00m, 3_600.00m, 0.07m, taxedApart: Royalty2025(8_000.00m));

        result.TotalNetIncome.ShouldBe(23_908.00m);
    }

    // Tax-Calc T18 of 2026-10-22: a royalty of 20,000 beside 30,000 of pay, of which 15,000 within the
    // limits and taxed apart, and 5,000 beyond them declared with the pay in code 1400, 35,000 in all.
    [Fact]
    public void Compute_2025TaxCalcT18RoyaltyBeyondTheLimits_taxesTheExcessWithThePay()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 35_000.00m, 6_000.00m, 0.07m, taxedApart: Royalty2025(15_000.00m));

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 870.00m, TaxableIncome: 28_130.00m,
            BasicTax: 8_804.00m, TaxFreeAmountReduction: 2_727.50m, StateTax: 6_076.50m,
            ReducedStateTax: 6_248.46m, RegionalAdditionalTax: 2_078.05m, IncomeTax: 8_326.51m, MunicipalSurcharge: 582.86m,
            LowEarnedIncomeCredit: 0.00m, IncomeTaxedApart: 15_000.00m, TaxedApart: 2_250.00m));
    }

    // Income taxed apart meets no tax-free amount: on a pay under it, the royalty is still taxed at 15%,
    // derived from the 2026 amounts (25% of 11,550 frees the scale's whole tax on 10,000).
    [Fact]
    public void Compute_2026RoyaltyBesideAPayUnderTheTaxFreeAmount_isStillTaxedAt15Percent()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 10_000.00m, 0m, 0.07m,
            taxedApart: new IncomeTaxedApart(5_000.00m, Core.Copyright.CopyrightRules.IncomeYear2026.TaxedApartRate));

        (result.StateTax, result.TaxedApart, result.IncomeTax).ShouldBe((0m, 750.00m, 750.00m));
    }

    private static IncomeTaxedApart Royalty2025(decimal royalty) => new(royalty, Core.Copyright.CopyrightRules.IncomeYear2025.TaxedApartRate);

    // T14's pay alone, derived from the official amounts (Tax-Calc was not run without the rent):
    // 880 × (28,420 − 28,033) / 6,560 = 51.91.
    [Fact]
    public void Compute_2025DerivedT14PayAlone_getsTheFallingCredit()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 32_500.00m, 3_600.00m, 0.07m);

        (result.TaxableIncome, result.StateTax, result.LowEarnedIncomeCredit).ShouldBe((28_033.00m, 6_037.70m, 51.91m));
    }

    // Art. 289ter on the 2026 amounts, derived: the credit's amount follows the activity income, the
    // pay, but none is due once the total net income exceeds 29,120 (§1), and from 22,400 to 29,120 it
    // is at most 910 × (29,120 − total) / 6,720 (§2, al. 4), rounded to the cent like the credit itself.
    public static TheoryData<decimal, decimal, decimal> CreditLimitedByTheTotalNetIncome2026 => new()
    {
        { 22_000.00m, 22_000.00m, 910.00m },
        { 22_400.00m, 22_400.00m, 910.00m },
        { 22_000.00m, 22_828.00m, 852.04m },
        { 20_000.00m, 23_000.00m, 828.75m },
        { 25_760.00m, 26_588.00m, 342.88m },
        { 7_835.00m, 8_663.00m, 455.00m },
        { 7_835.00m, 28_000.00m, 151.67m },
        { 28_000.00m, 29_120.00m, 0.00m },
        { 22_000.00m, 29_120.01m, 0.00m },
    };

    [Theory]
    [MemberData(nameof(CreditLimitedByTheTotalNetIncome2026))]
    public void LowEarnedIncomeCredit_2026OtherIncome_isLimitedByTheTotalNetIncome(decimal activityIncome, decimal totalNetIncome, decimal credit)
    {
        PersonalTaxCalculator.LowEarnedIncomeCredit(Rules2026, activityIncome, totalNetIncome).ShouldBe(credit);
    }

    // The 2026 rent at the cap on a cadastral income of 144 (RentCalculatorTests) beside a pay whose
    // taxable income is 22,000: the credit is 910 on the pay alone, 852.04 once the 828 of rent takes
    // the total to 22,828.
    [Fact]
    public void Compute_2026DerivedRentAtTheCapBesideALowPay_limitsTheCreditByTheTotal()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 26_680.41m, 4_000.00m, 0.07m, 828.00m);

        (result.TaxableIncome, result.TotalNetIncome, result.LowEarnedIncomeCredit).ShouldBe((22_000.00m, 22_828.00m, 852.04m));
    }

    [Fact]
    public void Compute_2026NoOtherIncome_totalNetIncomeIsTheTaxableIncome()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 50_000.00m, 8_300.00m, 0.07m);

        (result.OtherIncome, result.TotalNetIncome).ShouldBe((0m, 40_449.00m));
    }

    [Fact]
    public void Compute_2026ZeroMunicipalRate_isNoSurcharge()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 50_000.00m, 8_300.00m, 0.00m);

        result.MunicipalSurcharge.ShouldBe(0.00m);
    }
}
