using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// Each bucket of the river has its icon, set in one place (BucketIcons): beside its name in the bar,
// and on a glance of the outline that lands in it. An icon is decoration: the name stays the text.
public class BucketIconsTests
{
    private static readonly string[] AllBuckets = [.. BucketsView.Keys, BucketsView.Muddy];

    public static TheoryData<string> Buckets => [.. AllBuckets];

    [Theory]
    [MemberData(nameof(Buckets))]
    public void For_everyBucket_hasAnIcon(string bucket)
    {
        BucketIcons.For(bucket).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void For_twoBuckets_neverShareAnIcon()
    {
        AllBuckets.Select(BucketIcons.For).Distinct().Count().ShouldBe(5);
    }

    // With a car the bar shows its five buckets, each icon after its bucket's name, the last thing on the
    // name's line, so that where the column has no room for both it is the icon that goes (the bar's
    // CSS), never a part of the name. A screen reader skips it.
    [Fact]
    public async Task BucketBar_everyBucket_showsItsIconAfterItsName()
    {
        var withCar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
            .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

        var html = WebUtility.HtmlDecode(await RenderAsync<BucketBar>(new Folds(new BrowserStorage()), new() { ["Run"] = withCar.Run() }));

        Regex.Matches(html, "<dt[^>]*class=\"bucket__label\"[^>]*>(.*?)</dt>", RegexOptions.Singleline)
            .Select(label => Regex.Match(label.Groups[1].Value,
                "class=\"info-bubble\".*<span[^>]*class=\"bucket__icon\"[^>]*aria-hidden=\"true\"[^>]*><span[^>]*>([^<]+)</span></span>\\s*$",
                RegexOptions.Singleline).Groups[1].Value)
            .ShouldBe(new[] { "pocket", BucketsView.Muddy, "taxes_on_pay", "taxes_on_profit", "kept_in_company" }.Select(BucketIcons.For));
    }
}
