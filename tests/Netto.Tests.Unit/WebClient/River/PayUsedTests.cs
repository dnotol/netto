using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The pay the river runs: the pay set, never above what the split can give (RiverInputs.PayCeiling), the
// pay set staying as it is and coming back once the profit rises again; with the company skipped, the
// pay set, which has a slider of its own. On the example, 100,000 of profit before pay and 50,000 of pay
// (RiverInputsTests); 60,000 of revenue leaves 40,000. The year, the moves' scenario and the moves' view
// all run that one pay.
public class PayUsedTests
{
    private static readonly Dictionary<string, RiverInputs> Company = new()
    {
        ["below the ceiling"] = RiverInputs.Example,
        ["above the ceiling"] = RiverInputs.Example.WithRevenue(60_000m),
        ["a royalty following it to its ceiling"] = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m).WithDirectorPay(90_000m),
    };

    public static TheoryData<string, decimal> Pays => new()
    {
        { "below the ceiling", 50_000m },
        { "above the ceiling", 40_000m },
        { "a royalty following it to its ceiling", 81_250m },
    };

    [Theory]
    [MemberData(nameof(Pays))]
    public void PayUsed_company_isThePaySetUpToTheCeiling(string river, decimal pay)
    {
        Company[river].PayUsed.ShouldBe(pay);
    }

    [Theory]
    [MemberData(nameof(Pays))]
    public void PayUsed_company_isThePayTheYearTheMovesAndTheirViewRun(string river, decimal pay)
    {
        var inputs = Company[river];

        var moves = MovesView.Of(inputs, (new MovePlan(0m, [], 0m), null));

        (inputs.Run().Year.ShouldNotBeNull().Scenario.DirectorPay, inputs.MoveScenario().DirectorPay, moves.FromPay).ShouldBe((pay, pay, pay));
    }

    [Fact]
    public void PayUsed_companySkipped_isThePaySet()
    {
        var inputs = RiverInputs.Example.SkipCompany().WithDirectorPay(140_000m);

        (inputs.PayUsed, inputs.Run().Pay.ShouldNotBeNull().GrossPay).ShouldBe((140_000m, 140_000m));
    }

    // A negative pay goes through, for the engine to refuse.
    [Fact]
    public void PayUsed_negativePay_isThePaySet()
    {
        RiverInputs.Example.WithDirectorPay(-1m).PayUsed.ShouldBe(-1m);
    }
}
