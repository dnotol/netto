using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The split's figures as lines of label, amount and share (SplitFigures), on the river's example as
// SplitSearchViewTests takes it: 100,000 of profit before pay, 25,000 prepaid by 10 April and more
// withheld than any pay's tax, so the figures are SplitSearchTests'. The pay set, 50,000, leaves
// 62,529.24 in the pocket, 62.5% of what leaves the company; the best pay, 20,799, leaves 65,622.81
// with a dividend of 59,400.75, 65.6% of it, 3,093.57 more.
public class SplitFiguresTests
{
    private static readonly RiverInputs Example = RiverInputs.Example.WithAdvancePayment(0, 25_000m).WithTaxWithheld(1_000_000m);

    // At 75% of the time spent creating, the royalty that follows the example's 50,000 of pay is
    // 11,538.46, which leaves the company 38,461.54 of the 100,000 the split shares (ModuleViewsTests).
    private static readonly RiverInputs FollowingRoyalty = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m);

    // What the split shares, each side out of the 100,000 it receives.
    [Fact]
    public void Sides_example_areThePayAndTheCompanysProfitWithTheirShares()
    {
        Lines(Sides(RiverInputs.Example)).ShouldBe([("Figure_Pay", 50_000m, (decimal?)0.5m), ("Figure_Company", 50_000m, (decimal?)0.5m)]);
    }

    [Fact]
    public void Sides_royaltyFollowingThePay_standsBetweenThePayAndTheCompany()
    {
        Lines(Sides(FollowingRoyalty))
            .ShouldBe([("Figure_Pay", 50_000m, (decimal?)0.5m), ("Figure_Royalty", 11_538.46m, (decimal?)0.1153846m), ("Figure_Company", 38_461.54m, (decimal?)0.3846154m)]);
    }

    // When the company pays the director's 9,942.52 of contributions, its cost of the 50,000 is
    // 59,942.52 (ModuleViewsTests): the contributions are a side of their own beside the pay, and the
    // company keeps 40,057.48 of the 100,000.
    [Fact]
    public void Sides_companyPaysTheContributions_namesThemBesideThePay()
    {
        Lines(Sides(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company))).ShouldBe(
        [
            ("Figure_Pay", 50_000m, (decimal?)0.5m),
            ("Figure_CompanyContributions", 9_942.52m, (decimal?)0.0994252m),
            ("Figure_Company", 40_057.48m, (decimal?)0.4005748m),
        ]);
    }

    // With the fund's fee at 3.05% the company pays 303.24 of it with the 9,942.52 of contributions
    // (GrossToNetFundFeeTests): a side of its own after them, as Director pay draws it, and the company
    // keeps 39,754.24.
    [Fact]
    public void Sides_companyPaysTheContributionsAndTheFee_namesTheFeeAfterThem()
    {
        Lines(Sides(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.0305m))).ShouldBe(
        [
            ("Figure_Pay", 50_000m, (decimal?)0.5m),
            ("Figure_CompanyContributions", 9_942.52m, (decimal?)0.0994252m),
            ("Figure_CompanyFundFee", 303.24m, (decimal?)0.0030324m),
            ("Figure_Company", 39_754.24m, (decimal?)0.3975424m),
        ]);
    }

    // Whoever pays the contributions and whatever the royalty, the sides are all of what the split
    // receives.
    public static TheoryData<string, RiverInputs> Received => new()
    {
        { "the example", RiverInputs.Example },
        { "the company paying the contributions", RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company) },
        { "the company paying the contributions and the fund's fee", RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.0305m) },
        { "beside a royalty that follows the pay", FollowingRoyalty.WithContributionsPaidBy(ContributionsPaidBy.Company) },
        { "beside a fixed royalty", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m).WithContributionsPaidBy(ContributionsPaidBy.Company) },
    };

    [Theory]
    [MemberData(nameof(Received))]
    public void Sides_anySplit_addUpToWhatTheSplitReceives(string _, RiverInputs inputs)
    {
        var run = inputs.Run();
        var view = SplitView.From(inputs, run);
        var year = run.Year.ShouldNotBeNull();

        var lines = SplitFigures.Sides(view, year);

        (lines.Sum(line => line.Amount), lines.Sum(line => line.Share ?? 0m)).ShouldBe((view.Royalty?.Shared ?? year.ProfitBeforePay, 1m));
    }

    // A royalty that is none, waiting for the creative share, says why in its own field: the split
    // draws no line of nothing.
    [Fact]
    public void Sides_royaltyOfNothing_drawsNoLineForIt()
    {
        Sides(RiverInputs.Example.AddToPlan(OptionalModule.Copyright)).Select(line => line.Key).ShouldBe(["Figure_Pay", "Figure_Company"]);
    }

    // While an amount is refused there is no figure to share: the refusal stands in their place.
    [Fact]
    public void Sides_amountRefused_drawNoLines()
    {
        Sides(RiverInputs.Example.WithReserve(-1m)).ShouldBeEmpty();
    }

    [Fact]
    public void Best_example_isTheBestPayWhatItLeavesAndWhatItGainsOverThePaySet()
    {
        Lines(Best(Example)).ShouldBe(
        [
            ("Figure_Best", 20_799m, null),
            ("Figure_Dividend", 59_400.75m, null),
            ("Figure_InPocketThere", 65_622.81m, (decimal?)0.6562281m),
            ("Figure_MoreThanYours", 3_093.57m, null),
            ("Figure_InPocketAtYours", 62_529.24m, (decimal?)0.6252924m),
        ]);
    }

    // The pay set is the best: what it leaves, with no gain to weigh.
    [Fact]
    public void Best_paySetAtTheBest_saysItIsYoursWithWhatItLeaves()
    {
        Lines(Best(Example.WithDirectorPay(20_799m)))
            .ShouldBe([("Figure_BestIsYours", 20_799m, null), ("Figure_Dividend", 59_400.75m, null), ("Figure_InPocket", 65_622.81m, (decimal?)0.6562281m)]);
    }

    // A pay set within a euro of the best is the best (SplitSearchViewTests): its lines are the pay set's
    // own, the pay the sides above show, its dividend and what reaches the pocket at it, never the
    // search's best pay a euro away.
    [Fact]
    public void Best_paySetAEuroFromTheBest_saysThePaySetsOwnFigures()
    {
        var inputs = Example.WithDirectorPay(20_800m);
        var run = inputs.Run();
        var year = run.Year.ShouldNotBeNull();
        var search = SplitSearchView.From(inputs, run, inputs.SearchSplit()).ShouldNotBeNull();

        Lines(Best(inputs))
            .ShouldBe([("Figure_BestIsYours", 20_800m, null), ("Figure_Dividend", year.Dividend.Dividend, null), ("Figure_InPocket", year.NetInPocket, search.Reach)]);
    }

    // At 50,000 the profit after tax, 40,000, cannot keep 60,000: the best pay that keeps it, 18,890,
    // leaves 16,183.13 with a dividend of 832.50 (SplitSearchTests), against what the pay set leaves in
    // the pocket and in the company, with no gain and no reach, the two not leaving the same company
    // behind.
    [Fact]
    public void Best_paySetCannotKeepTheReserve_weighsTheBestPayKeepingItAgainstWhatThePaySetKeeps()
    {
        var inputs = Example.WithReserve(60_000m);
        var year = inputs.Run().Year.ShouldNotBeNull();

        var lines = Best(inputs);

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).ShouldBe(
            [
                ("Figure_BestKeepingReserve", 18_890m, null),
                ("Figure_Dividend", 832.50m, null),
                ("Figure_InPocketThere", 16_183.13m, null),
                ("Figure_InPocketAtYours", year.NetInPocket, null),
                ("Figure_KeptAtYours", 40_000m, null),
            ]),
            () => lines[0].Args.ShouldBe(["€60,000.00"]));
    }

    // A royalty that follows the pay is another at the best pay; a fixed one is the same at every pay,
    // and the split's side says it.
    [Fact]
    public void Best_royaltyFollowingThePay_namesTheRoyaltyAtTheBestPay()
    {
        var inputs = FollowingRoyalty;
        var best = inputs.SearchSplit().ShouldNotBeNull().Best.ShouldNotBeNull();

        Best(inputs).Where(line => line.Key == "Figure_Royalty").Select(line => line.Amount).ShouldBe([best.Royalty]);
    }

    [Fact]
    public void Best_fixedRoyalty_namesNoRoyalty()
    {
        Best(RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m)).Select(line => line.Key).ShouldNotContain("Figure_Royalty");
    }

    // The most the year can keep is 75,000, at no pay: no pay keeps 80,000, and the card says so.
    [Fact]
    public void Best_noPayKeepsTheReserve_drawsNoLines()
    {
        Best(Example.WithReserve(80_000m)).ShouldBeEmpty();
    }

    // Away from the curve, the pointer reads nothing: its line keeps its room, empty.
    [Fact]
    public void Pointer_pointingAtNoPay_readsNothing()
    {
        SplitFigures.Pointer(null, null).ShouldBeNull();
    }

    [Fact]
    public void Pointer_atAPayKeepingTheReserve_readsWhatReachesThePocketThere()
    {
        var reading = SplitFigures.Pointer(Level(keepsReserve: true), null).ShouldNotBeNull();

        reading.ShouldSatisfyAllConditions(
            () => reading.Key.ShouldBe("PointerAt"),
            () => reading.Args.ShouldBe(["€40,000.00", "€61,000.00", "€0.00"]));
    }

    [Fact]
    public void Pointer_atAPayMissingTheReserve_readsWhatTheCompanyKeepsThere()
    {
        var reading = SplitFigures.Pointer(Level(keepsReserve: false), null).ShouldNotBeNull();

        reading.ShouldSatisfyAllConditions(
            () => reading.Key.ShouldBe("PointerMissesReserve"),
            () => reading.Args.ShouldBe(["€40,000.00", "€61,000.00", "€5,000.00", "€0.00"]));
    }

    // While the royalty follows the pay, each pay of the curve has its own beside it.
    [Fact]
    public void Pointer_royaltyFollowingThePay_readsTheRoyaltyBesideThePay()
    {
        var royalty = SplitView.From(FollowingRoyalty, FollowingRoyalty.Run()).Royalty;

        var reading = SplitFigures.Pointer(Level(keepsReserve: true) with { Royalty = 9_230.77m }, royalty).ShouldNotBeNull();

        reading.ShouldSatisfyAllConditions(
            () => reading.Key.ShouldBe("PointerAt_Royalty"),
            () => reading.Args.ShouldBe(["€40,000.00", "€61,000.00", "€9,230.77"]));
    }

    private static PayLevel Level(bool keepsReserve) => new(40_000m, 61_000m, 10_000m, 5_000m, ReducedRateCondition.MinimumPayNotMet, keepsReserve);

    private static IReadOnlyList<FigureLine> Sides(RiverInputs inputs)
    {
        var run = inputs.Run();
        return SplitFigures.Sides(SplitView.From(inputs, run), run.Year);
    }

    private static IReadOnlyList<FigureLine> Best(RiverInputs inputs)
    {
        var run = inputs.Run();
        var search = SplitSearchView.From(inputs, run, inputs.SearchSplit()).ShouldNotBeNull();
        return SplitFigures.Best(search, run.Year.ShouldNotBeNull(), SplitView.From(inputs, run).Royalty, inputs.Reserve.Value);
    }

    private static List<(string Key, decimal Amount, decimal? Share)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, line.Share))];
}
