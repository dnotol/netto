using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// Whether the year keeps the reserve the user keeps in the company: on the made-up company at 50,000 of
// pay, prepaid, the profit after tax is 40,000 (YearSimulationTests), all of it the most the year can pay
// out, so a reserve up to 40,000 is kept and one a cent above it is not. The split's search reads the
// same rule at each pay (PayLevel.KeepsReserve), and the moves never take a move that breaks it.
public class YearResultTests
{
    public static TheoryData<decimal, bool> Reserves => new()
    {
        { 0m, true },
        { 39_999.99m, true },
        { 40_000m, true },
        { 40_000.01m, false },
        { 100_000m, false },
    };

    [Theory]
    [MemberData(nameof(Reserves))]
    public void KeepsReserve_reserve_isWhetherTheMostTheYearPaysOutCoversIt(decimal reserve, bool keeps)
    {
        var year = Run(Scenario(reserve: reserve));

        (year.MaximumDividend, year.KeepsReserve).ShouldBe((40_000m, keeps));
    }

    public static TheoryData<decimal, decimal> PaysAndReserves => new()
    {
        { 0m, 0m },
        { 20_799m, 60_000m },
        { 20_799m, 70_000m },
        { 50_000m, 40_000m },
        { 50_000m, 40_000.01m },
        { 100_000m, 1m },
    };

    // The search's level at a pay says what the year run at that pay says.
    [Theory]
    [MemberData(nameof(PaysAndReserves))]
    public void KeepsReserve_anyPay_isTheSearchLevelsAtThatPay(decimal pay, decimal reserve)
    {
        var scenario = Scenario(directorPay: pay, reserve: reserve);

        Run(scenario).KeepsReserve.ShouldBe(YearSimulation.Level(scenario).ShouldNotBeNull().KeepsReserve);
    }

    // The made-up company's whole profit before pay, 100,000, paid as pay with the contributions the
    // company bears for the director costs it more than it earns: the company ends the year at a loss,
    // which keeps no reserve, not even one of 0, in the year or in the search's level at that pay.
    [Fact]
    public void KeepsReserve_payLeavingTheCompanyAtALoss_isNotKept()
    {
        var scenario = Scenario(directorPay: 100_000m, paidBy: ContributionsPaidBy.Company);

        var year = Run(scenario);

        year.ShouldSatisfyAllConditions(
            () => year.ProfitAfterTax.ShouldBeLessThan(0m),
            () => year.KeepsReserve.ShouldBeFalse(),
            () => YearSimulation.Level(scenario).ShouldNotBeNull().KeepsReserve.ShouldBeFalse());
    }

    // Profits kept from earlier years absorb the year's loss first: while they cover it and the
    // reserve, the reserve is kept.
    [Fact]
    public void KeepsReserve_lossCoveredByEarlierProfits_isKept()
    {
        var year = Run(Scenario(directorPay: 100_000m, paidBy: ContributionsPaidBy.Company, earlierProfits: 100_000m));

        (year.ProfitAfterTax < 0m, year.KeepsReserve).ShouldBe((true, true));
    }
}
