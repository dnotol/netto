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
