using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the split draws from the search, on the river's example (SplitSearchTests): 100,000 of profit
// before pay, where 20,799 leaves the most, 65,622.81, and gives up the reduced corporate rate, which
// 50,000, the pay set, keeps for 62,529.24. The net in pocket runs from 50,368.00 (all of it as pay)
// to that peak.
public class SplitSearchViewTests
{
    [Fact]
    public void Example_namesTheBestPayAndWhatItGainsOverThePaySet()
    {
        var best = View(RiverInputs.Example).Best.ShouldNotBeNull();

        (best.Level.Pay, best.Level.Dividend, best.Gain, best.IsYours).ShouldBe((20_799m, 59_400.75m, 3_093.57m, false));
    }

    [Fact]
    public void Example_bestGivesUpTheReducedRateAgainstTheBestPayKeepingIt()
    {
        var rate = View(RiverInputs.Example).Rate.ShouldNotBeNull();

        (rate.Choice, rate.SmallestPay, rate.Other?.Pay, rate.Other?.NetInPocket, rate.Difference)
            .ShouldBe((RateChoice.GivesItUp, (decimal?)50_000m, (decimal?)50_000m, (decimal?)62_529.24m, (decimal?)3_093.57m));
    }

    [Fact]
    public void PaySetAtTheBest_isTheBest()
    {
        var best = View(RiverInputs.Example.WithDirectorPay(20_799m)).Best.ShouldNotBeNull();

        (best.Gain, best.IsYours).ShouldBe((0m, true));
    }

    // Less than a euro under the best is no move worth offering.
    [Fact]
    public void PaySetAEuroFromTheBest_isTheBest()
    {
        View(RiverInputs.Example.WithDirectorPay(20_800m)).Best.ShouldNotBeNull().IsYours.ShouldBeTrue();
    }

    // At 50,000 the profit after tax, 40,000, cannot keep 60,000: the pay set leaves more in the
    // pocket than the best pay, 18,890 (SplitSearchTests), only by eating into the reserve.
    [Fact]
    public void PaySetCannotKeepTheReserve_isNotTheBest()
    {
        var best = View(RiverInputs.Example.WithReserve(60_000m)).Best.ShouldNotBeNull();

        (best.Level.Pay, best.YoursKeepsReserve, best.IsYours).ShouldBe((18_890m, false, false));
    }

    // On 10,000 of profit all of it as pay leaves 6,438.32 and keeps the rate; the best pay under the
    // 5,000 that keeps it, 4,999, leaves 4,662.87.
    [Fact]
    public void BestKeepsTheReducedRate_weighsGivingItUp()
    {
        var rate = View(RiverInputs.Example.WithRevenue(30_000m)).Rate.ShouldNotBeNull();

        (rate.Choice, rate.SmallestPay, rate.Other?.Pay, rate.Difference)
            .ShouldBe((RateChoice.KeepsIt, (decimal?)5_000m, (decimal?)4_999m, (decimal?)1_775.45m));
    }

    [Fact]
    public void FirstFourTaxPeriods_doesNotWeighTheRate()
    {
        var view = View(RiverInputs.Example.WithFirstFourTaxPeriods(true));

        (view.Rate?.Choice, view.Rate?.Other, view.Curve.ThresholdAt).ShouldBe(((RateChoice?)RateChoice.NotWeighed, null, (decimal?)null));
    }

    // The most the year can keep is 75,000, its whole profit after tax at no pay.
    [Fact]
    public void ReserveNoPayCanKeep_namesNoBestPayAndTheMostTheCompanyCanKeep()
    {
        var view = View(RiverInputs.Example.WithReserve(80_000m));

        (view.Best, view.Rate, view.MostKept).ShouldBe(((BestPayView?)null, (RateWeighing?)null, 75_000m));
    }

    [Fact]
    public void PayRefused_drawsNoCurve()
    {
        From(RiverInputs.Example.WithDirectorPay(-1m)).ShouldBeNull();
    }

    [Fact]
    public void OtherInputRefused_drawsNoCurve()
    {
        From(RiverInputs.Example.WithReserve(-1m)).ShouldBeNull();
    }

    [Fact]
    public void NoProfitBeforePay_drawsNoCurve()
    {
        From(RiverInputs.Example.WithRevenue(20_000m)).ShouldBeNull();
    }

    [Fact]
    public void CompanySkipped_drawsNoCurve()
    {
        From(RiverInputs.Example.SkipCompany()).ShouldBeNull();
    }

    // Across the plot, from no pay to the profit before pay.
    [Fact]
    public void Example_placesThePaySetTheBestPayAndTheThreshold()
    {
        var curve = View(RiverInputs.Example).Curve;

        (curve.PayAt, curve.BestAt?.X, curve.ThresholdAt).ShouldBe((0.5m, (decimal?)0.20799m, (decimal?)0.5m));
    }

    // Up the plot, from the lowest round amount under the curve to the highest above it.
    [Fact]
    public void Example_bestSitsOnTheCurveAtItsNet()
    {
        View(RiverInputs.Example).Curve.BestAt.ShouldNotBeNull().Y.ShouldBe(15_622.81m / 20_000m);
    }

    [Fact]
    public void Example_netAxisRunsInRoundStepsAroundTheCurve()
    {
        View(RiverInputs.Example).Curve.NetTicks.ShouldBe(new[]
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
        View(RiverInputs.Example).Curve.PayTicks.ShouldBe(new[]
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
        var inputs = RiverInputs.Example;
        var curve = View(inputs).Curve;

        (curve.Runs.Count, curve.Runs[0].KeepsReserve, curve.Runs[0].Points.Count, curve.SomeMissReserve)
            .ShouldBe((1, true, inputs.SearchSplit().ShouldNotBeNull().Curve.Count, false));
    }

    // Up to 20,000 the profit after tax covers 60,000 (SplitSearchTests); the segment that leaves
    // the reserve is drawn with the pays that miss it.
    [Fact]
    public void Reserve60000_drawsThePaysThatMissItApart()
    {
        var runs = View(RiverInputs.Example.WithReserve(60_000m)).Curve.Runs;

        runs.Select(run => (run.KeepsReserve, run.Points[0].X, run.Points[^1].X)).ShouldBe(new[] { (true, 0m, 0.2m), (false, 0.2m, 1m) });
    }

    [Fact]
    public void Reserve60000_saysSomePaysMissIt()
    {
        View(RiverInputs.Example.WithReserve(60_000m)).Curve.SomeMissReserve.ShouldBeTrue();
    }

    [Fact]
    public void Example_bandsCoverThePlotWithoutGaps()
    {
        var bands = View(RiverInputs.Example).Curve.Bands;

        (bands[0].From, bands[^1].To, bands.Zip(bands.Skip(1)).All(pair => pair.First.To == pair.Second.From))
            .ShouldBe((0m, 1m, true));
    }

    [Fact]
    public void Example_eachBandHoldsOnePayOfTheCurve()
    {
        var inputs = RiverInputs.Example;

        View(inputs).Curve.Bands.Select(band => band.Level).ShouldBe(inputs.SearchSplit().ShouldNotBeNull().Curve);
    }

    private static SplitSearchView? From(RiverInputs inputs) => SplitSearchView.From(inputs, inputs.Run(), inputs.SearchSplit());

    private static SplitSearchView View(RiverInputs inputs) => From(inputs).ShouldNotBeNull();
}
