using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.DirectorPay;

// The social insurance fund's management fee on the director's contributions, income year 2026 at a 7%
// municipal rate, at the 3.05% one fund advertises. Its rate is the user's, on each quarter the fund bills
// (AR n° 38, art. 20 §4), and the director deducts it with the contributions as their own (CIR 92, art. 52,
// 7°, read with Com.IR 52/124), Netto's reading where no text names the fee as deductible: it lowers the
// very income the contributions are owed on, so the two are solved together, and when the company pays
// them it is pay too, then deducted. Expected values are derived from the official amounts by hand, not checked against
// Tax-Calc: the personal tax sees a larger own contribution (code 1405), which PersonalTaxCalculatorTests
// already hold to it. The company withholds more than any tax these tests reach, unless a test says
// otherwise.
public class GrossToNetFundFeeTests
{
    private const decimal FundFee = 0.0305m;

    private static readonly SocialContributionRules Rules = SocialContributionRules.IncomeYear2026;

    public static TheoryData<decimal> GrossPays => [0m, 10_000m, 30_000m, 50_000m, 160_000m];

    public static TheoryData<decimal> FeeRatesOutOfRange => [-0.0001m, 0.1001m];

    public static TheoryData<decimal> FeeRatesAtTheLimits => [0m, 0.10m];

    // Without a rate the pay is as it was: no fee, no step of one, nothing in the next 1,000.
    [Fact]
    public void Compute_noFeeRate_holdsNoFeeNorItsStep()
    {
        var result = Compute2026(50_000m, null);

        (result.FundFee, result.NextThousand.FundFee, result.Steps.Any(step => step.RuleKey == "fund_fee"), result.SocialContributions)
            .ShouldBe((0m, 0m, false, 8_293.36m));
    }

    // At 50,000 the contributions C solve C = 20.50% × 97% × (50,000 − 1.0305 C): 8,251.6199, billed
    // 2,062.90 a quarter, 8,251.60; the fee is 3.05% of each quarter, 62.92, so 251.68; the flat-rate
    // costs are 3% of the 41,496.72 they leave, 1,244.90, and the taxable income 40,251.82.
    [Fact]
    public void Compute_2026Gross50000_solvesTheContributionsWithTheFeeDeducted()
    {
        var result = Compute2026(50_000m, FundFee);

        (result.SocialContributions, result.FundFee, result.FlatRateCosts, result.TaxableIncome).ShouldBe((8_251.60m, 251.68m, 1_244.90m, 40_251.82m));
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_feeRate_isTheRateOnEachQuarterOfTheContributions(decimal grossPay)
    {
        var result = Compute2026(grossPay, FundFee);

        result.FundFee.ShouldBe(SocialContributionCalculator.Fee(result.SocialContributions, FundFee));
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_feeRateDirectorPays_taxesThePayLessTheContributionsAndTheFee(decimal grossPay)
    {
        var result = Compute2026(grossPay, FundFee);

        var deducted = PersonalTaxCalculator.Compute(PersonalTaxRules.IncomeYear2026, grossPay, result.SocialContributions + result.FundFee, 0.07m);

        (result.FlatRateCosts, result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge, result.LowEarnedIncomeCredit)
            .ShouldBe((deducted.FlatRateCosts, deducted.TaxableIncome, deducted.IncomeTax, deducted.MunicipalSurcharge, deducted.LowEarnedIncomeCredit));
    }

    // Four rounded quarters move the contributions in steps of 0.04, so the base they leave can ask 0.04
    // more or less, as without a fee (GrossToNetTests).
    [Theory]
    [InlineData(30_000)]
    [InlineData(50_000)]
    [InlineData(70_000)]
    public void Compute_feeRateDirectorPays_contributionsAreThoseOwedOnTheIncomeTheFeeLowersWithinFourCents(int grossPay)
    {
        var result = Compute2026(grossPay, FundFee);

        var recomputed = SocialContributionCalculator.Annual(Rules, result.TaxableIncome);

        Math.Abs(recomputed - result.SocialContributions).ShouldBeLessThanOrEqualTo(0.04m);
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_feeRateDirectorPays_netInPocketIsGrossLessWhatIsPaidOut(decimal grossPay)
    {
        var result = Compute2026(grossPay, FundFee);

        (grossPay - result.Steps.Where(step => step.Track == Track.PaidOut).Sum(step => step.Amount)).ShouldBe(result.NetInPocket);
    }

    // Paid by the company, the contributions on 50,000 are owed on the gross less its flat-rate costs,
    // 48,500.00: 2,485.63 a quarter, 9,942.52, and the fee 75.81 a quarter, 303.24. Both are pay, so the
    // taxable pay and the pay's cost to the company are 60,245.76, and both are deducted: the taxable income
    // stays 48,500.00.
    [Fact]
    public void Compute_2026CompanyPaysGross50000_paysTheFeeAsPayThenDeductsIt()
    {
        var result = CompanyPays2026(50_000m, FundFee);

        (result.SocialContributions, result.FundFee, result.ContributionsPaidByCompany, result.TaxablePay, result.CompanyCost, result.TaxableIncome)
            .ShouldBe((9_942.52m, 303.24m, 10_245.76m, 60_245.76m, 60_245.76m, 48_500.00m));
    }

    // The fee the company bears is taxable pay deducted again, so the director's tax and net are those of
    // the same pay without it.
    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_feeRateCompanyPays_leavesTheDirectorsTaxAndNetAsWithoutTheFee(decimal grossPay)
    {
        var with = CompanyPays2026(grossPay, FundFee);
        var without = CompanyPays2026(grossPay, null);

        (with.TaxableIncome, with.IncomeTax, with.MunicipalSurcharge, with.NetInPocket, with.SocialContributions)
            .ShouldBe((without.TaxableIncome, without.IncomeTax, without.MunicipalSurcharge, without.NetInPocket, without.SocialContributions));
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_feeRateCompanyPays_companyCostLessWhatIsPaidOutIsTheNetInPocket(decimal grossPay)
    {
        var result = CompanyPays2026(grossPay, FundFee);

        (result.CompanyCost - result.Steps.Where(step => step.Track == Track.PaidOut).Sum(step => step.Amount)).ShouldBe(result.NetInPocket);
    }

    // The ledger holds the fee as a step of its own, right after the contributions, so the pay slip's tax
    // base deducts it with them: the text it is deducted under, the rate the user gave citing the text that
    // sets each fund's fee, and the commentary Netto reads it with.
    [Fact]
    public void Compute_feeRate_ledgerHoldsTheFeeAfterTheContributionsWithItsTexts()
    {
        var result = Compute2026(50_000m, FundFee);

        var fee = result.Steps.Single(step => step.RuleKey == "fund_fee");

        result.Steps.ShouldSatisfyAllConditions(
            () => result.Steps.Select(step => step.RuleKey).Take(3).ShouldBe(["social_contributions", "fund_fee", "flat_rate_costs"]),
            () => (fee.Layer, fee.Event, fee.Track, fee.Amount).ShouldBe((Layer.Social, TaxableEvent.DirectorPay, Track.PaidOut, 251.68m)),
            () => (fee.Source, fee.AmountsUsed.Single(), fee.AlsoCites.Single()).ShouldBe((Rules.FundFeeDeduction, Rules.FundFeeRate(FundFee), Rules.FundFeeCommentary)));
    }

    // A rate of nothing the user typed is theirs: the step stands, at nothing.
    [Fact]
    public void Compute_feeRateOfNothing_holdsTheStepAtNothing()
    {
        var result = Compute2026(50_000m, 0m);

        (result.FundFee, result.Steps.Single(step => step.RuleKey == "fund_fee").Amount, result.SocialContributions).ShouldBe((0m, 0m, 8_293.36m));
    }

    // The next 1,000 adds to the fee as to the contributions, and the seven parts still add up to 1,000.
    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_feeRateDirectorPays_nextThousandPartsAddUpToOneThousand(decimal grossPay)
    {
        var result = Compute2026(grossPay, FundFee);
        var next = result.NextThousand;

        (next.SocialContributions + next.FundFee + next.IncomeTax + next.MunicipalSurcharge + next.LowEarnedIncomeCredit + next.AdvancePayments + next.NetInPocket)
            .ShouldBe(1_000m);
    }

    [Fact]
    public void Compute_2026Gross50000_nextThousandAddsTheFeeOnTheContributionsItAdds()
    {
        var result = Compute2026(50_000m, FundFee);
        var next = Compute2026(51_000m, FundFee);

        result.NextThousand.FundFee.ShouldBe(next.FundFee - result.FundFee);
    }

    // The statement shows what the company pays the fund for the director each month, the fee with it.
    [Fact]
    public void Compute_feeRateCompanyPays_statementShowsTheContributionsAndTheFeeTheCompanyPays()
    {
        var result = CompanyPays2026(50_000m, FundFee);

        result.Statement.Months.ShouldAllBe(month => month.ContributionsPaidByCompany == 853.81m);
    }

    [Theory]
    [MemberData(nameof(FeeRatesOutOfRange))]
    public void Compute_feeRateOutsideZeroToTenPercent_isRefused(decimal rate)
    {
        var (value, error) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, fundFeeRate: rate);

        (value, error).ShouldBe((null, GrossToNet.FundFeeRateOutOfRange));
    }

    [Theory]
    [MemberData(nameof(FeeRatesAtTheLimits))]
    public void Compute_feeRateAtTheLimits_isAccepted(decimal rate)
    {
        var (value, error) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, fundFeeRate: rate);

        (value is null, error).ShouldBe((false, null));
    }

    private static GrossToNetResult Compute2026(decimal grossPay, decimal? fundFeeRate)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, fundFeeRate: fundFeeRate);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    private static GrossToNetResult CompanyPays2026(decimal grossPay, decimal? fundFeeRate)
    {
        var (value, error) = GrossToNet.Compute(
            2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, ContributionsPaidBy.Company, fundFeeRate: fundFeeRate);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
