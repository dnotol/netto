using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The lines of a bucket's card add up to the bucket in the bar, to the cent, whatever the plan holds.
public class BucketCardsTests
{
    private static readonly string[] Names =
    [
        "every lever", "the company paying the contributions", "the example", "the company skipped", "a rent under its floor", "a contribution beyond the car's costs",
    ];

    public static TheoryData<string, string> Buckets
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var river in Names)
                foreach (var key in BucketsView.Keys)
                    data.Add(river, key);
            return data;
        }
    }


    // The pocket card says a running total exactly where the map says one: the stretches of the pocket line
    // MapView leaves said (not Unsaid), each with the amount the map draws, so a drift in either fails.
    [Theory]
    [InlineData("every lever")]
    [InlineData("the company paying the contributions")]
    [InlineData("the example")]
    [InlineData("the company skipped")]
    [InlineData("a rent under its floor")]
    [InlineData("a contribution beyond the car's costs")]
    [InlineData("a car without contribution")]
    [InlineData("every lever empty")]
    [InlineData("the dividend all kept")]
    public void RunningTotals_areTheStretchesTheMapSays(string name)
    {
        var river = MapRendering.River(name);
        var run = river.Run();
        var stretches = MapView.From(river, run, MapNames.English).Links
            .Where(link => link.From.PocketLine is not null && (link.To.PocketLine is not null || link.To.Bucket == "pocket") && !link.Unsaid)
            .Select(link => (link.From.PocketLine!.Value, link.Amount!.Value)).ToList();

        var totals = BucketCardView.From(river, run, "pocket")!.RunningTotals.Select(total => (total.Module, total.Total)).ToList();

        totals.ShouldBe(stretches);
    }

    [Fact]
    public void RunningTotals_theCases_includeOneWhereTheMapLeavesATotalUnsaid()
    {
        var river = MapRendering.River("a contribution beyond the car's costs");
        var all = MapView.From(river, river.Run(), MapNames.English).Links.Count(link => link.From.PocketLine is not null && link.To.PocketLine is not null || link.From.PocketLine is not null && link.To.Bucket == "pocket");

        all.ShouldBeGreaterThan(BucketCardView.From(river, river.Run(), "pocket")!.RunningTotals.Count);
    }

    [Theory]
    [MemberData(nameof(Buckets))]
    public void From_theLines_addUpToTheBarsBucket(string name, string key)
    {
        var river = MapRendering.River(name);
        var run = river.Run();
        var bar = BucketsView.From(run)!.AmountOf(key);

        var view = BucketCardView.From(river, run, key)!;

        (view.Lines.Sum(line => line.Amount), view.Amount).ShouldBe((bar, bar));
    }
}
