using Netto.Core.Simulation;
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
}
