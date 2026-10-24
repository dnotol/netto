using Netto.Core.SocialContributions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the split draws from the search, on the river's example (SplitSearchTests): 100,000 of profit
// before pay, where 20,799 leaves the most, 65,622.81, and gives up the reduced corporate rate, which
// 50,000, the pay set, keeps for 62,529.24. The net in pocket runs from 50,368.00 (all of it as pay)
// to that peak. The example prepays 25,000 by 10 April here, whose credit, 2,250, covers the
// advance-payment surcharge at every pay, and withholds more than any pay's tax, so the figures are
// SplitSearchTests'; the last test runs the example as it opens, with nothing prepaid or withheld.
public class SplitSearchViewTests
{
    private static readonly RiverInputs Example = RiverInputs.Example.WithAdvancePayment(0, 25_000m).WithTaxWithheld(1_000_000m);
    [Fact]
    public void Example_namesTheBestPayAndWhatItGainsOverThePaySet()
    {
        var best = View(Example).Best.ShouldNotBeNull();

        (best.Level.Pay, best.Level.Dividend, best.Gain, best.IsYours).ShouldBe((20_799m, 59_400.75m, 3_093.57m, false));
    }

    // Out of the 100,000, nothing being kept: 65,622.81 at the best pay, 62,529.24 at the pay set.
    [Fact]
    public void Example_saysWhatReachesThePocketAtTheBestPayAndAtThePaySet()
    {
        var view = View(Example);

        (view.Best.ShouldNotBeNull().Reach, view.Reach).ShouldBe(((decimal?)0.6562281m, (decimal?)0.6252924m));
    }

    // Each out of what leaves the company at that pay, what it keeps being the director's still.
    [Fact]
    public void Reserve_reachLeavesOutWhatTheCompanyKeepsAtEachPay()
    {
        var view = View(Example.WithReserve(10_000m));
        var best = view.Best.ShouldNotBeNull().Level;

        view.Best.Reach.ShouldBe(best.NetInPocket / (100_000m - best.KeptInCompany));
    }

    // With YearSimulationCarTests' electric car, the river starts after its 8,000 of costs, a cost of
    // the company like its running costs, from 92,000: the reach at the pay set is the bar's, and the
    // best pay's is out of the same start, less what the company keeps at that pay.
    [Fact]
    public void Car_reachIsOutOfTheStartTheBarDrawsAfterItsCosts()
    {
        var inputs = Example.AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);
        var run = inputs.Run();

        var view = SplitSearchView.From(inputs, run, inputs.SearchSplit()).ShouldNotBeNull();
        var best = view.Best.ShouldNotBeNull().Level;

        view.ShouldSatisfyAllConditions(
            () => view.Reach.ShouldBe(BucketsView.From(run).ShouldNotBeNull().Reach),
            () => view.Best.Reach.ShouldBe(Shares.Of(best.NetInPocket, 92_000m - best.KeptInCompany)));
    }

    [Fact]
    public void Example_bestGivesUpTheReducedRateAgainstTheBestPayKeepingIt()
    {
        var rate = View(Example).Rate.ShouldNotBeNull();

        (rate.Choice, rate.SmallestPay, rate.Other?.Pay, rate.Other?.NetInPocket, rate.Difference)
            .ShouldBe((RateChoice.GivesItUp, (decimal?)50_000m, (decimal?)50_000m, (decimal?)62_529.24m, (decimal?)3_093.57m));
    }

    [Fact]
    public void PaySetAtTheBest_isTheBest()
    {
        var best = View(Example.WithDirectorPay(20_799m)).Best.ShouldNotBeNull();

        (best.Gain, best.IsYours).ShouldBe((0m, true));
    }

    // Less than a euro under the best is no move worth offering.
    [Fact]
    public void PaySetAEuroFromTheBest_isTheBest()
    {
        View(Example.WithDirectorPay(20_800m)).Best.ShouldNotBeNull().IsYours.ShouldBeTrue();
    }

    // At 50,000 the profit after tax, 40,000, cannot keep 60,000: the pay set leaves more in the
    // pocket than the best pay, 18,890 (SplitSearchTests), only by eating into the reserve.
    [Fact]
    public void PaySetCannotKeepTheReserve_isNotTheBest()
    {
        var best = View(Example.WithReserve(60_000m)).Best.ShouldNotBeNull();

        (best.Level.Pay, best.YoursKeepsReserve, best.IsYours).ShouldBe((18_890m, false, false));
    }

    // The split's button moves the pay to the best pay as a typed pay is set, yours, and changes nothing
    // else of the river.
    [Fact]
    public void MoveTo_setsThePayToTheBestPayAsYours()
    {
        var best = View(Example).Best.ShouldNotBeNull();

        var moved = best.MoveTo(Example);

        (moved.DirectorPay, moved.SameBesidesPay(Example)).ShouldBe((new Input<decimal>(20_799m, InputOrigin.Yours), true));
    }

    // The river runs at that pay, leaving what the search said there, and the pay is then the best:
    // the button has nothing more to offer.
    [Fact]
    public void MoveTo_theRiverRunsAtTheBestPayAndTheOfferEnds()
    {
        var best = View(Example).Best.ShouldNotBeNull();

        var moved = best.MoveTo(Example);

        (moved.Run().Year.ShouldNotBeNull().NetInPocket, View(moved).Best.ShouldNotBeNull().IsYours).ShouldBe((best.Level.NetInPocket, true));
    }

    // A pay whose profit cannot keep the reserve moves to the best pay that does.
    [Fact]
    public void MoveTo_paySetCannotKeepTheReserve_movesToTheBestPayThatDoes()
    {
        var reserve60000 = Example.WithReserve(60_000m);
        var best = View(reserve60000).Best.ShouldNotBeNull();

        var moved = View(best.MoveTo(reserve60000)).Best.ShouldNotBeNull();

        (moved.Level.Pay, moved.YoursKeepsReserve, moved.IsYours).ShouldBe((18_890m, true, true));
    }

    // A royalty that follows the pay is the one a ruling accepts at the new pay, as when the pay is typed.
    [Fact]
    public void MoveTo_royaltyFollowingThePay_movesWithIt()
    {
        var following = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m);
        var best = View(following).Best.ShouldNotBeNull();
        var royaltyBefore = following.Run().Year.ShouldNotBeNull().Copyright.Royalty;

        var royalty = best.MoveTo(following).Run().Year.ShouldNotBeNull().Copyright.Royalty;

        (royalty, royalty == royaltyBefore).ShouldBe((best.Level.Royalty, false));
    }

    // Who pays the contributions moves the best pay: the pay that was the best is not any more, and the
    // button offers the new one (SplitSearchTests: 17,237 on the payslip with the company paying).
    [Fact]
    public void ContributionsPayerChanged_theBestPayMovesAndThePaySetIsNotTheBest()
    {
        var atTheBest = Example.WithDirectorPay(20_799m);
        var changed = atTheBest.WithContributionsPaidBy(ContributionsPaidBy.Company);

        var before = View(atTheBest).Best.ShouldNotBeNull();
        var after = View(changed).Best.ShouldNotBeNull();

        (before.IsYours, after.IsYours, after.Level.Pay).ShouldBe((true, false, 17_237m));
    }

    [Fact]
    public void ContributionsPayerChanged_movingToTheNewBestPayEndsTheOfferAgain()
    {
        var changed = Example.WithDirectorPay(20_799m).WithContributionsPaidBy(ContributionsPaidBy.Company);
        var best = View(changed).Best.ShouldNotBeNull();

        var moved = View(best.MoveTo(changed)).Best.ShouldNotBeNull();

        (moved.IsYours, moved.Level.Pay).ShouldBe((true, 17_237m));
    }

    // On 10,000 of profit all of it as pay leaves 6,438.32 and keeps the rate; the best pay under the
    // 5,000 that keeps it, 4,999, leaves 4,662.87.
    [Fact]
    public void BestKeepsTheReducedRate_weighsGivingItUp()
    {
        var rate = View(Example.WithRevenue(30_000m)).Rate.ShouldNotBeNull();

        (rate.Choice, rate.SmallestPay, rate.Other?.Pay, rate.Difference)
            .ShouldBe((RateChoice.KeepsIt, (decimal?)5_000m, (decimal?)4_999m, (decimal?)1_775.45m));
    }

    [Fact]
    public void FirstFourTaxPeriods_doesNotWeighTheRate()
    {
        var view = View(Example.WithFinancialYear(FinancialYear.Fourth));

        (view.Rate?.Choice, view.Rate?.Other, view.Curve.ThresholdAt).ShouldBe(((RateChoice?)RateChoice.NotWeighed, null, (decimal?)null));
    }

    // The most the year can keep is 75,000, its whole profit after tax at no pay.
    [Fact]
    public void ReserveNoPayCanKeep_namesNoBestPayAndTheMostTheCompanyCanKeep()
    {
        var view = View(Example.WithReserve(80_000m));

        (view.Best, view.Rate, view.MostKept).ShouldBe(((BestPayView?)null, (RateWeighing?)null, 75_000m));
    }

    [Fact]
    public void PayRefused_drawsNoCurve()
    {
        From(Example.WithDirectorPay(-1m)).ShouldBeNull();
    }

    [Fact]
    public void OtherInputRefused_drawsNoCurve()
    {
        From(Example.WithReserve(-1m)).ShouldBeNull();
    }

    [Fact]
    public void NoProfitBeforePay_drawsNoCurve()
    {
        From(Example.WithRevenue(20_000m)).ShouldBeNull();
    }

    [Fact]
    public void CompanySkipped_drawsNoCurve()
    {
        From(Example.SkipCompany()).ShouldBeNull();
    }

    // Across the plot, from no pay to the profit before pay.
    [Fact]
    public void Example_placesThePaySetTheBestPayAndTheThreshold()
    {
        var curve = View(Example).Curve;

        (curve.PayAt, curve.BestAt?.X, curve.ThresholdAt).ShouldBe((0.5m, (decimal?)0.20799m, (decimal?)0.5m));
    }

    // Up the plot, from the lowest round amount under the curve to the highest above it.
    [Fact]
    public void Example_bestSitsOnTheCurveAtItsNet()
    {
        View(Example).Curve.BestAt.ShouldNotBeNull().Y.ShouldBe(15_622.81m / 20_000m);
    }

    [Fact]
    public void Example_netAxisRunsInRoundStepsAroundTheCurve()
    {
        View(Example).Curve.NetTicks.ShouldBe(new[]
        {
            new AxisTick(0m, 50_000m),
            new AxisTick(5_000m / 20_000m, 55_000m),
            new AxisTick(10_000m / 20_000m, 60_000m),
            new AxisTick(15_000m / 20_000m, 65_000m),
            new AxisTick(1m, 70_000m),
        });
    }

    [Fact]
    public void Example_payAxisRunsFromNoPayToTheProfitBeforePay()
    {
        View(Example).Curve.PayTicks.ShouldBe(new[]
        {
            new AxisTick(0m, 0m),
            new AxisTick(0.25m, 25_000m),
            new AxisTick(0.5m, 50_000m),
            new AxisTick(0.75m, 75_000m),
            new AxisTick(1m, 100_000m),
        });
    }

    [Fact]
    public void Example_drawsTheWholeCurveAsOneRunThatKeepsTheReserve()
    {
        var inputs = Example;
        var curve = View(inputs).Curve;

        (curve.Runs.Count, curve.Runs[0].KeepsReserve, curve.Runs[0].Points.Count, curve.SomeMissReserve)
            .ShouldBe((1, true, inputs.SearchSplit().ShouldNotBeNull().Curve.Count, false));
    }

    // Up to 20,000 the profit after tax covers 60,000 (SplitSearchTests); the segment that leaves
    // the reserve is drawn with the pays that miss it.
    [Fact]
    public void Reserve60000_drawsThePaysThatMissItApart()
    {
        var runs = View(Example.WithReserve(60_000m)).Curve.Runs;

        runs.Select(run => (run.KeepsReserve, run.Points[0].X, run.Points[^1].X)).ShouldBe(new[] { (true, 0m, 0.2m), (false, 0.2m, 1m) });
    }

    [Fact]
    public void Reserve60000_saysSomePaysMissIt()
    {
        View(Example.WithReserve(60_000m)).Curve.SomeMissReserve.ShouldBeTrue();
    }

    [Fact]
    public void Example_bandsCoverThePlotWithoutGaps()
    {
        var bands = View(Example).Curve.Bands;

        (bands[0].From, bands[^1].To, bands.Zip(bands.Skip(1)).All(pair => pair.First.To == pair.Second.From))
            .ShouldBe((0m, 1m, true));
    }

    [Fact]
    public void Example_eachBandHoldsOnePayOfTheCurve()
    {
        var inputs = Example;

        View(inputs).Curve.Bands.Select(band => band.Level).ShouldBe(inputs.SearchSplit().ShouldNotBeNull().Curve);
    }

    // SplitSearchTests: the company's surcharge, 1,336.52 at the best pay and 675.00 at 50,000, moves
    // neither, and the pay's tax, withheld as the key formula says at every pay, leaves a surcharge
    // under the minimum at both, so the gain is over the 61,975.74 of the pay set.
    [Fact]
    public void ExampleNothingPrepaid_namesTheBestPayAndWhatItGainsWithTheSurcharge()
    {
        var best = View(RiverInputs.Example).Best.ShouldNotBeNull();

        (best.Level.Pay, best.Level.NetInPocket, best.Gain).ShouldBe((20_799m, 64_526.87m, 2_551.13m));
    }

    // What reaches the pocket at the pay set is the bar's reach, out of what leaves the company once its
    // car is paid, what it keeps left out: here with a car, a rent, a royalty and a reserve.
    [Fact]
    public void CarRentRoyaltyAndReserve_reachAtThePaySetIsTheBars()
    {
        var inputs = Example.WithReserve(5_000m)
            .AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
            .AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
            .AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m);
        var bar = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        (View(inputs).Reach, bar.AmountOf("kept_in_company") > 0m).ShouldBe((bar.Reach, true));
    }

    private static SplitSearchView? From(RiverInputs inputs) => SplitSearchView.From(inputs, inputs.Run(), inputs.SearchSplit());

    private static SplitSearchView View(RiverInputs inputs) => From(inputs).ShouldNotBeNull();
}
