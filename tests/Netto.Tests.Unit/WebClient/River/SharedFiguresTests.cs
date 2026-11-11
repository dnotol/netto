using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the split shares, as lines of label, amount and share (SharedFigures), on the river's example as
// SplitSearchViewTests takes it: 100,000 of profit before pay, 25,000 prepaid by 10 April and more
// withheld than any pay's tax, so the figures are SplitSearchTests'. The pay set, 50,000, leaves
// 62,529.24 in the pocket, 62.5% of what leaves the company; the best pay, 20,799, leaves 65,622.81
// with a dividend of 59,400.75, 65.6% of it, 3,093.57 more.
public class SharedFiguresTests
{
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

        var lines = SharedFigures.Sides(view, year);

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

    private static IReadOnlyList<FigureLine> Sides(RiverInputs inputs)
    {
        var run = inputs.Run();
        return SharedFigures.Sides(SplitView.From(inputs, run), run.Year);
    }

    private static List<(string Key, decimal Amount, decimal? Share)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, line.Share))];
}
