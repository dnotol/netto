using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// The year with the social insurance fund's management fee on the director's contributions, at the
// 3.05% one fund advertises, for YearSimulationTests' made-up company: 100,000 of profit before the
// director. The fee is the director's own contribution, deducted with the contributions (GrossToNet); when
// the company pays the contributions it pays the fee too, which costs it as the pay does and is taxable
// pay that art. 215 reads. Expected values are derived from the official amounts, not checked against
// Tax-Calc.
public class YearSimulationFundFeeTests
{
    private const decimal FundFee = 0.0305m;

    public static TheoryData<ContributionsPaidBy> Payers => [ContributionsPaidBy.Director, ContributionsPaidBy.Company];

    // Without a rate the year holds no fee, and every figure is as it was.
    [Fact]
    public void Run_noFeeRate_holdsNoFeeStep()
    {
        var result = Run(Scenario());

        (result.DirectorPay.FundFee, result.Steps.Any(step => step.RuleKey == "fund_fee"), result.SocialContributions).ShouldBe((0m, false, 8_293.36m));
    }

    [Theory]
    [MemberData(nameof(Payers))]
    public void Run_feeRate_socialContributionsAreTheContributionsAndTheFee(ContributionsPaidBy paidBy)
    {
        var result = Run(Scenario(paidBy: paidBy, fundFeeRate: FundFee));

        (result.DirectorPay.FundFee > 0m, result.SocialContributions).ShouldBe((true, result.DirectorPay.SocialContributions + result.DirectorPay.FundFee));
    }

    [Theory]
    [MemberData(nameof(Payers))]
    public void Run_feeRate_layersPocketAndCompanyAddUpToTheProfitBeforePay(ContributionsPaidBy paidBy)
    {
        var result = Run(Scenario(paidBy: paidBy, fundFeeRate: FundFee));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany).ShouldBe(100_000m);
    }

    [Theory]
    [MemberData(nameof(Payers))]
    public void Run_feeRate_directorPayIsWhatGrossToNetGivesWithTheFee(ContributionsPaidBy paidBy)
    {
        var result = Run(Scenario(paidBy: paidBy, fundFeeRate: FundFee));
        var (pay, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, paidBy, fundFeeRate: FundFee);

        result.DirectorPay.ShouldSatisfyAllConditions(
            () => (result.DirectorPay.SocialContributions, result.DirectorPay.FundFee, result.DirectorPay.TaxablePay, result.DirectorPay.NetInPocket)
                .ShouldBe((pay!.SocialContributions, pay.FundFee, pay.TaxablePay, pay.NetInPocket)),
            () => result.Steps.Where(step => step.Event == TaxableEvent.DirectorPay).Select(step => (step.RuleKey, step.Amount))
                .ShouldBe(pay!.Steps.Select(step => (step.RuleKey, step.Amount))));
    }

    // Paid by the company, the fee comes off its profit with the pay and the contributions: 303.24 on a
    // payslip gross of 50,000 (GrossToNetFundFeeTests).
    [Fact]
    public void Run_feeRateCompanyPays_takesTheFeeOffTheCompanysProfitWithThePay()
    {
        var with = Run(Scenario(paidBy: ContributionsPaidBy.Company, fundFeeRate: FundFee));
        var without = Run(Scenario(paidBy: ContributionsPaidBy.Company));

        (with.ProfitAfterPay, without.ProfitAfterPay - with.ProfitAfterPay, without.Company.TaxableIncome - with.Company.TaxableIncome)
            .ShouldBe((100_000m - 60_245.76m, 303.24m, 303.24m));
    }

    // Paid by the director, the fee leaves the pocket, less the tax it saves, and the company is as it was.
    [Fact]
    public void Run_feeRateDirectorPays_leavesTheCompanyAsItWas()
    {
        var with = Run(Scenario(fundFeeRate: FundFee));
        var without = Run(Scenario());

        (with.CorporateTax, with.KeptInCompany, with.Dividend.Dividend).ShouldBe((without.CorporateTax, without.KeptInCompany, without.Dividend.Dividend));
    }

    // On 140,000 of profit before pay, a payslip gross of 42,400 with the company paying 8,431.24 of
    // contributions is 50,831.24 of taxable pay, under art. 215's 51,000; the fee on them, 257.16, is pay
    // too and brings it to 51,088.40, which keeps the reduced rate.
    [Fact]
    public void Run_feeRateCompanyPays_testsTheMinimumPayOnTheTaxablePayWithTheFee()
    {
        var with = Run(Scenario(directorPay: 42_400m, revenue: 160_000m, paidBy: ContributionsPaidBy.Company, fundFeeRate: FundFee));
        var without = Run(Scenario(directorPay: 42_400m, revenue: 160_000m, paidBy: ContributionsPaidBy.Company));

        (without.DirectorPay.TaxablePay, without.Company.Condition, with.DirectorPay.TaxablePay, with.Company.Condition)
            .ShouldBe((50_831.24m, ReducedRateCondition.MinimumPayNotMet, 51_088.40m, ReducedRateCondition.MinimumPayMet));
    }
}
