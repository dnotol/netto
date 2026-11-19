using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// The split's search beside the social insurance fund's management fee, at 3.05%, for SplitSearchTests'
// made-up company: every level is the year Run gives at that pay with the fee, whoever pays the
// contributions, and when the company pays them the smallest payslip gross that keeps the reduced rate
// counts the fee in the taxable pay art. 215 reads.
public class SplitSearchFundFeeTests
{
    private const decimal FundFee = 0.0305m;

    public static TheoryData<ContributionsPaidBy> Payers => [ContributionsPaidBy.Director, ContributionsPaidBy.Company];

    [Theory]
    [MemberData(nameof(Payers))]
    public void Run_feeRate_eachLevelIsTheYearAtThatPayWithTheFee(ContributionsPaidBy paidBy)
    {
        var scenario = Scenario(paidBy: paidBy, fundFeeRate: FundFee);

        var curve = Search(scenario).Curve;

        curve.ShouldSatisfyAllConditions(
            () => curve.ShouldNotBeEmpty(),
            () => curve.Where(level => level != LevelOf(scenario with { DirectorPay = level.Pay })).ShouldBeEmpty());
    }

    // The fee raises the taxable pay of every payslip gross, so the smallest one keeping the rate is lower
    // than without it (41,706.64, SplitSearchTests), and a cent less no longer keeps it.
    [Fact]
    public void Run_feeRateCompanyPays_saysTheSmallestPayslipGrossKeepingTheReducedRateWithTheFee()
    {
        var scenario = Scenario(paidBy: ContributionsPaidBy.Company, fundFeeRate: FundFee);

        var smallest = Search(scenario).SmallestPayKeepingReducedRate.ShouldNotBeNull();

        (smallest < 41_706.64m, KeepsTheRate(scenario with { DirectorPay = smallest }), KeepsTheRate(scenario with { DirectorPay = smallest - 0.01m }))
            .ShouldBe((true, true, false));
    }

    private static bool KeepsTheRate(Scenario scenario) => Run(scenario).Company.Condition.AppliesReducedRate();

    private static PayLevel LevelOf(Scenario scenario)
    {
        var year = Run(scenario);

        return new PayLevel(scenario.DirectorPay, year.NetInPocket, year.Dividend.Dividend, year.KeptInCompany, year.Company.Condition,
            year.KeepsReserve);
    }
}
