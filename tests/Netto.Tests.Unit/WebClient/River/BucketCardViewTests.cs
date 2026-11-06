using Netto.Tests.WebClient.Tabs;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What fills a bucket, as its card says it: the map's links into it, one line per module, net of what
// comes back, in the river's order, and for the pocket the running totals of the gold line.
public class BucketCardViewTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    public static TheoryData<string> Keys => [.. BucketsView.Keys];

    private static BucketCardView View(RiverInputs river, string key) => BucketCardView.From(river, river.Run(), key)!;

    public static TheoryData<string, RiverModule[]> Senders => new()
    {
        { "pocket", [RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent, RiverModule.Car, RiverModule.Copyright, RiverModule.DirectorPay, RiverModule.Dividend] },
        { "taxes_on_pay", [RiverModule.Rent, RiverModule.Copyright, RiverModule.DirectorPay] },
        { "taxes_on_profit", [RiverModule.CorporateTax, RiverModule.Dividend] },
        { "expenses", [RiverModule.CompanyYear, RiverModule.Car] },
    };

    private static readonly string[] CompanysBuckets = ["taxes_on_profit", "kept_in_company", "expenses"];

    [Theory]
    [MemberData(nameof(Senders))]
    public void From_everyLever_hasOneLinePerModuleSendingMoneyInto_inTheRiversOrder(string key, RiverModule[] modules)
    {
        View(Full, key).Lines.Select(line => line.Module).ShouldBe(modules);
    }

    [Fact]
    public void From_theDividendAllKept_hasTheDividendAsTheLineOfWhatIsKept()
    {
        View(Full.WithReserve(1_000_000m), "kept_in_company").Lines.Select(line => line.Module).ShouldBe([RiverModule.Dividend]);
    }

    [Fact]
    public void From_theExample_leavesOutLinesOfNothing()
    {
        BucketsView.Keys.SelectMany(key => View(RiverInputs.Example, key).Lines).Where(line => Math.Round(line.Amount, 2) == 0m).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public void From_amountAndShare_areTheBarsBucket(string key)
    {
        var run = Full.Run();
        var bucket = BucketsView.From(run)!.Buckets.Single(b => b.Key == key);

        var view = BucketCardView.From(Full, run, key)!;

        (view.Amount, view.Share).ShouldBe((bucket.Amount, bucket.Share));
    }

    [Fact]
    public void From_aLine_hasItsShareOfTheBarsStart()
    {
        var run = Full.Run();

        var line = BucketCardView.From(Full, run, "pocket")!.Lines.Single(l => l.Module == RiverModule.DirectorPay);

        line.Share.ShouldBe(Shares.Of(line.Amount, BucketsView.From(run)!.Start));
    }

    [Fact]
    public void From_aCarsContribution_isANegativeLineOfThePocket()
    {
        View(Full.WithCarContribution(1_000m), "pocket").Lines.Single(line => line.Module == RiverModule.Car).Amount.ShouldBeLessThan(0m);
    }

    [Fact]
    public void From_theCompanySkipped_hasNoLinesIntoTheCompanysBuckets()
    {
        var river = Full.SkipCompany();

        CompanysBuckets.SelectMany(key => View(river, key).Lines).ShouldBeEmpty();
    }

    [Fact]
    public void From_aRefusedRun_isNone()
    {
        var river = Full.WithRevenue(-1m);
        var run = river.Run();

        (run.Error is null, BucketCardView.From(river, run, "pocket")).ShouldBe((false, null));
    }

    [Fact]
    public void From_aBucketTheBarDoesNotHave_throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BucketCardView.From(Full, Full.Run(), "nowhere"));
    }

    [Fact]
    public void RunningTotals_ofThePocket_endOnThePocket()
    {
        var run = Full.Run();

        BucketCardView.From(Full, run, "pocket")!.RunningTotals[^1].Total.ShouldBe(BucketsView.From(run)!.AmountOf("pocket"));
    }

    // Money only leaves the pocket line at the car's point, so the map says no total there: nor does the card.
    [Fact]
    public void RunningTotals_aCarsContributionLeavingThePocketLine_haveNoTotalAtTheCar()
    {
        var river = Full.WithCarContribution(1_000m);

        View(river, "pocket").RunningTotals.Select(total => total.Module).ShouldNotContain(RiverModule.Car);
    }

    [Fact]
    public void RunningTotals_ofAnyOtherBucket_areNone()
    {
        BucketsView.Keys.Where(key => key != "pocket").Select(key => View(Full, key).RunningTotals.Count).ShouldAllBe(count => count == 0);
    }
}
