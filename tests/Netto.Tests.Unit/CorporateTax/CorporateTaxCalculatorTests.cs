using Netto.Core.CorporateTax;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.CorporateTax;

// Income year 2026 (CIR 92 art. 215 for income 2026; minimum director pay 51,000 from the assessment
// year 2027 indexation notice). Made-up companies; expected values derived from the article.
public class CorporateTaxCalculatorTests
{
    private static readonly CorporateTaxRules Rules2026 = CorporateTaxRules.IncomeYear2026;

    [Fact]
    public void Compute_2026NotSmallCompany_pays25PercentOnEverything()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 80_000m, 60_000m, new CompanyStatus(SmallCompany: false, FirstFourTaxPeriods: true));

        (result.Condition, result.CorporateTax).ShouldBe((ReducedRateCondition.NotSmallCompany, 20_000.00m));
    }

    [Fact]
    public void Compute_2026SmallInFirstFourTaxPeriods_keeps20PercentWithoutPay()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 80_000m, 0m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: true));

        (result.Condition, result.CorporateTax).ShouldBe((ReducedRateCondition.FirstFourTaxPeriods, 16_000.00m));
    }

    [Fact]
    public void Compute_2026PayAtTheMinimum_keeps20Percent()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 80_000m, 51_000m, SmallPastStartUp);

        (result.Condition, result.CorporateTax).ShouldBe((ReducedRateCondition.MinimumPayMet, 16_000.00m));
    }

    [Fact]
    public void Compute_2026PayUnderTheMinimumAndTaxableAbovePay_pays25PercentOnEverything()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 80_000m, 50_999.99m, SmallPastStartUp);

        (result.Condition, result.CorporateTax).ShouldBe((ReducedRateCondition.MinimumPayNotMet, 20_000.00m));
    }

    [Fact]
    public void Compute_2026TaxableEqualToPayUnderTheMinimum_keeps20Percent()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 30_000m, 30_000m, SmallPastStartUp);

        (result.Condition, result.CorporateTax).ShouldBe((ReducedRateCondition.TaxableIncomeNotAbovePay, 6_000.00m));
    }

    // 25% × 30,000.01 = 7,500.0025.
    [Fact]
    public void Compute_2026TaxableOneCentAbovePayUnderTheMinimum_pays25Percent()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 30_000.01m, 30_000m, SmallPastStartUp);

        (result.Condition, result.CorporateTax).ShouldBe((ReducedRateCondition.MinimumPayNotMet, 7_500.00m));
    }

    [Fact]
    public void Compute_2026ReducedRateAbove100000_pays25PercentOnTheRest()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 150_000m, 60_000m, SmallPastStartUp);

        result.CorporateTax.ShouldBe(32_500.00m);
    }

    // 20% × 12,345.67 = 2,469.134.
    [Fact]
    public void Compute_2026ReducedRate_roundsToTheCent()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 12_345.67m, 51_000m, SmallPastStartUp);

        result.CorporateTax.ShouldBe(2_469.13m);
    }

    // 25% × 12,345.67 = 3,086.4175.
    [Fact]
    public void Compute_2026StandardRate_roundsHalfAwayFromZero()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 12_345.67m, 0m, new CompanyStatus(SmallCompany: false, FirstFourTaxPeriods: false));

        result.CorporateTax.ShouldBe(3_086.42m);
    }

    [Fact]
    public void Compute_2026_saysTheMinimumPayItTestedAgainst()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 80_000m, 30_000m, SmallPastStartUp);

        result.MinimumDirectorPay.ShouldBe(51_000m);
    }

    [Fact]
    public void Compute_2026Loss_paysNoTax()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, -10_000m, 51_000m, SmallPastStartUp);

        (result.TaxableIncome, result.CorporateTax).ShouldBe((-10_000m, 0m));
    }

    // The pay meets the condition once it reaches the minimum, or once the taxable income, the profit
    // before pay less the pay, does not exceed it: at half the profit.
    public static TheoryData<decimal, decimal> SmallestPays => new()
    {
        { 100_000m, 50_000m },
        { 102_000m, 51_000m },
        { 150_000m, 51_000m },
        { 75_000.01m, 37_500.01m },
        { 75_000.03m, 37_500.02m },
    };

    public static TheoryData<decimal> Profits => new() { 100_000m, 150_000m, 75_000.03m };

    [Theory]
    [MemberData(nameof(SmallestPays))]
    public void SmallestPayKeepingReducedRate_2026SmallPastStartUp_isTheMinimumOrHalfTheProfit(decimal profitBeforePay, decimal expected)
    {
        CorporateTaxCalculator.SmallestPayKeepingReducedRate(Rules2026, profitBeforePay, SmallPastStartUp).ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(Profits))]
    public void SmallestPayKeepingReducedRate_2026_keepsTheRateWhereACentLessLosesIt(decimal profitBeforePay)
    {
        var pay = CorporateTaxCalculator.SmallestPayKeepingReducedRate(Rules2026, profitBeforePay, SmallPastStartUp)!.Value;

        (Applies(profitBeforePay, pay), Applies(profitBeforePay, pay - 0.01m)).ShouldBe((true, false));
    }

    [Fact]
    public void SmallestPayKeepingReducedRate_2026Loss_isNoPay()
    {
        CorporateTaxCalculator.SmallestPayKeepingReducedRate(Rules2026, -10_000m, SmallPastStartUp).ShouldBe(0m);
    }

    // The rate does not depend on the pay: kept whatever it is, or lost whatever it is.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void SmallestPayKeepingReducedRate_2026RateNotSetByThePay_isNull(bool smallCompany, bool firstFourTaxPeriods)
    {
        CorporateTaxCalculator.SmallestPayKeepingReducedRate(Rules2026, 100_000m, new CompanyStatus(smallCompany, firstFourTaxPeriods)).ShouldBeNull();
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(2027)]
    public void For_yearWithoutRules_isNull(int incomeYear)
    {
        CorporateTaxRules.For(incomeYear).ShouldBeNull();
    }

    // Art. 215 al. 3, 5° with art. 2 §1, 25°: flat-rate benefits worth more than 20% of the total pay,
    // the benefits in it, cost the reduced rate. The memorandum's example (DOC 56 1243/001): 30,000 in
    // cash, a car of 6,000 and housing of 20,000, so 26,000 of benefits on a total of 56,000, lose it
    // although the pay reaches the minimum.
    [Fact]
    public void Compute_2026MemorandumsExample_losesTheRateOnItsBenefits()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 80_000m, 56_000m, SmallPastStartUp, 26_000m);

        (result.Condition, result.CorporateTax).ShouldBe((ReducedRateCondition.ExcessBenefits, 20_000m));
    }

    // 20% of 60,000 is 12,000: a cent more loses the rate, 12,000 itself keeps it.
    public static TheoryData<decimal, ReducedRateCondition> BenefitsAroundTwentyPercent => new()
    {
        { 12_000m, ReducedRateCondition.MinimumPayMet },
        { 12_000.01m, ReducedRateCondition.ExcessBenefits },
    };

    [Theory]
    [MemberData(nameof(BenefitsAroundTwentyPercent))]
    public void Compute_2026BenefitsAtTwentyPercent_keepTheRateAndACentMoreLosesIt(decimal benefits, ReducedRateCondition expected)
    {
        CorporateTaxCalculator.Compute(Rules2026, 80_000m, 60_000m, SmallPastStartUp, benefits).Condition.ShouldBe(expected);
    }

    // The first four tax periods exempt a company from the minimum pay only (4°), not from the cap on
    // benefits (5°).
    [Fact]
    public void Compute_2026ExcessBenefitsInFirstFourTaxPeriods_loseTheRate()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 80_000m, 10_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: true), 2_400m);

        result.Condition.ShouldBe(ReducedRateCondition.ExcessBenefits);
    }

    [Fact]
    public void Compute_2026NotSmallCompanyWithExcessBenefits_saysItIsNotSmall()
    {
        CorporateTaxCalculator.Compute(Rules2026, 80_000m, 10_000m, new CompanyStatus(SmallCompany: false, FirstFourTaxPeriods: false), 2_400m)
            .Condition.ShouldBe(ReducedRateCondition.NotSmallCompany);
    }

    [Fact]
    public void Compute_2026_saysThePayAndTheBenefitsItTestedAndTheirLimit()
    {
        var result = CorporateTaxCalculator.Compute(Rules2026, 80_000m, 56_000m, SmallPastStartUp, 26_000m);

        (result.DirectorPay, result.FlatRateBenefits, result.ExcessBenefitsShare).ShouldBe((56_000m, 26_000m, 0.20m));
    }

    // In the pay the split moves, the gross and the contributions the company pays for the director,
    // without the benefit B: the minimum less B, or half of what the taxable income at no pay exceeds
    // B by, and at least 4 B, so that B stays within 20% of the total.
    public static TheoryData<decimal, decimal, decimal> SmallestPaysWithBenefits => new()
    {
        { 150_000m, 2_400m, 48_600m },
        { 100_000m, 2_400m, 48_600m },
        { 80_000m, 2_400m, 38_800m },
        { 30_000m, 5_000m, 20_000m },
        { 75_000.03m, 2_400.01m, 36_300.01m },
        { 10_000m, 60_000m, 240_000m },
    };

    [Theory]
    [MemberData(nameof(SmallestPaysWithBenefits))]
    public void SmallestPayKeepingReducedRate_2026WithBenefits_isTheLargerOfThePayTestAndTheBenefitsCap(
        decimal taxableIncomeAtNoPay, decimal benefits, decimal expected)
    {
        CorporateTaxCalculator.SmallestPayKeepingReducedRate(Rules2026, taxableIncomeAtNoPay, SmallPastStartUp, benefits).ShouldBe(expected);
    }

    public static TheoryData<decimal, decimal> ProfitsAndBenefits => new()
    {
        { 150_000m, 2_400m },
        { 80_000m, 2_400m },
        { 30_000m, 5_000m },
        { 75_000.03m, 2_400.01m },
        { 120_000m, 1_714.29m },
    };

    [Theory]
    [MemberData(nameof(ProfitsAndBenefits))]
    public void SmallestPayKeepingReducedRate_2026WithBenefits_keepsTheRateWhereACentLessLosesIt(decimal taxableIncomeAtNoPay, decimal benefits)
    {
        var pay = CorporateTaxCalculator.SmallestPayKeepingReducedRate(Rules2026, taxableIncomeAtNoPay, SmallPastStartUp, benefits)!.Value;

        (Applies(taxableIncomeAtNoPay, pay, benefits), Applies(taxableIncomeAtNoPay, pay - 0.01m, benefits)).ShouldBe((true, false));
    }

    // In the first four tax periods only the cap on benefits depends on the pay.
    [Fact]
    public void SmallestPayKeepingReducedRate_2026FirstFourTaxPeriodsWithBenefits_isFourTimesTheBenefits()
    {
        CorporateTaxCalculator.SmallestPayKeepingReducedRate(Rules2026, 100_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: true), 2_400m)
            .ShouldBe(9_600m);
    }

    private static bool Applies(decimal profitBeforePay, decimal pay) =>
        CorporateTaxCalculator.Compute(Rules2026, profitBeforePay - pay, pay, SmallPastStartUp).Condition.AppliesReducedRate();

    private static bool Applies(decimal taxableIncomeAtNoPay, decimal pay, decimal benefits) =>
        CorporateTaxCalculator.Compute(Rules2026, taxableIncomeAtNoPay - pay, pay + benefits, SmallPastStartUp, benefits).Condition.AppliesReducedRate();
}
