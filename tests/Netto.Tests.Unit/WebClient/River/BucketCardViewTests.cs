using Netto.Tests.WebClient.Tabs;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What fills a bucket, as its card says it: the map's links into it, one line per module, net of what
// comes back, in the river's order.
public class BucketCardViewTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    public static TheoryData<string> Keys => [.. BucketsView.Keys];

    private static BucketCardView View(RiverInputs river, string key) => BucketCardView.From(river, river.Run(), key)!;

    public static TheoryData<string, RiverModule[]> Senders => new()
    {
        { "pocket", [RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent, RiverModule.Equipment, RiverModule.Car, RiverModule.Copyright, RiverModule.DirectorPay, RiverModule.Dividend] },
        { "taxes_on_pay", [RiverModule.Rent, RiverModule.Copyright, RiverModule.DirectorPay] },
        { "taxes_on_profit", [RiverModule.CorporateTax, RiverModule.Dividend] },
        { "expenses", [RiverModule.CompanyYear, RiverModule.Equipment, RiverModule.Car] },
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
    public void From_amount_isTheBarsBucket(string key)
    {
        var run = Full.Run();
        var bucket = BucketsView.From(run)!.Buckets.Single(b => b.Key == key);

        var view = BucketCardView.From(Full, run, key)!;

        view.Amount.ShouldBe(bucket.Amount);
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

    // The running totals of the pocket line are the map's alone (MapCardsTests): the card states one amount per
    // source, and no share, the bar's reach being the one percentage.
    [Fact]
    public void BucketCardView_says_oneAmountPerSource_noRunningTotalAndNoShare()
    {
        var members = typeof(BucketCardView).GetProperties().Select(property => property.Name)
            .Concat(typeof(BucketLine).GetProperties().Select(property => property.Name)).ToList();

        members.ShouldNotContain("RunningTotals");
        members.ShouldNotContain("Share");
    }

    // A bucket with a single source has that source's amount as its own: the card names the source rather than
    // list it under a header that says the same.
    [Fact]
    public void From_theExamplesTaxesOnPay_areOneLineOfTheBucketsAmount()
    {
        var view = View(RiverInputs.Example, "taxes_on_pay");

        view.Lines.Select(line => line.Module).ToList().ShouldBe([RiverModule.DirectorPay]);
        view.Lines.Sum(line => line.Amount).ShouldBe(view.Amount);
    }
}
