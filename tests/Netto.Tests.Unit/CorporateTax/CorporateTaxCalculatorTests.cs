using Netto.Core.CorporateTax;
using Shouldly;

namespace Netto.Tests.CorporateTax;

// Income year 2026 (CIR 92 art. 215 for income 2026; minimum director pay 51,000 from the assessment
// year 2027 indexation notice). Made-up companies; expected values derived from the article.
public class CorporateTaxCalculatorTests
{
    private static readonly CorporateTaxRules Rules2026 = CorporateTaxRules.IncomeYear2026;

    private static readonly CompanyStatus SmallPastStartUp = new(SmallCompany: true, FirstFourTaxPeriods: false);

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

    private static bool Applies(decimal profitBeforePay, decimal pay) =>
        CorporateTaxCalculator.Compute(Rules2026, profitBeforePay - pay, pay, SmallPastStartUp).Condition.AppliesReducedRate();
}
