using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The expense allowances' figure: what the company pays for them all, the internet and computer allowances
// and the phone costs it reimburses, 200.00 + 200.00 + 600.00.
public class AllowanceFiguresTests
{
    // The name IStringLocalizer<AllowanceCard> reads River/AllowanceCard.resx under.
    private static readonly ResxText Resources = ResxText.For<AllowanceCard>();

    private static readonly RiverInputs AllowancesInPlan = RiverInputs.Example.AddToPlan(OptionalModule.Allowances);

    private static readonly RiverInputs AllowancesPaid = AllowancesInPlan.WithInternetAllowance(200m).WithComputerAllowance(200m).WithPhoneCosts(600m);

    [Fact]
    public void From_allowancesPaid_isWhatTheCompanyPaysForThemAll()
    {
        Lines(AllowancesPaid).ShouldBe([(AllowanceFigures.CompanyPays, 1_000m)]);
    }

    // The phone costs alone: their one drain line to the pocket carries the payout whole, so no line says it
    // again. The map reads the payout all the same (Payout).
    [Fact]
    public void From_oneAllowance_leavesThePayoutToItsDrainLine()
    {
        var inputs = AllowancesInPlan.WithPhoneCosts(600m);

        (Lines(inputs).Count, AllowanceFigures.Payout(View(inputs)).Amount).ShouldBe((0, 600m));
    }

    // Nothing paid: no line of nothing.
    [Fact]
    public void From_noAllowance_isNoLine()
    {
        Lines(AllowancesInPlan).ShouldBeEmpty();
    }

    [Fact]
    public void AllowanceCardResx_everyLine_hasALabel()
    {
        new[] { AllowanceFigures.CompanyPays }
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    private static AllowanceView View(RiverInputs inputs) => AllowanceView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition);

    private static List<(string Key, decimal Amount)> Lines(RiverInputs inputs) =>
        [.. AllowanceFigures.From(View(inputs)).Select(line => (line.Key, line.Amount))];
}
