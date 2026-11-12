using System.Text.RegularExpressions;
using Netto.Tests.WebClient.Tabs;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// A bucket's card as a viewer reads it: read-only, its total in the heading, a line for each module sending
// money into it, and no share: the bar's reach is the one percentage.
public class BucketCardTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    private static Task<string> RenderBucketAsync(RiverInputs river, string bucket) =>
        RenderAsync<BucketCard>(new Folds(new BrowserStorage()), new() { ["Inputs"] = river, ["Run"] = river.Run(), ["Bucket"] = bucket });

    [Fact]
    public async Task Card_ofTheExpenses_saysItsTotalAndALinePerModule()
    {
        var run = Full.Run();
        var buckets = BucketsView.From(run)!;
        var view = BucketCardView.From(Full, run, "expenses")!;

        var html = await RenderBucketAsync(Full, "expenses");

        (CardHtml.Summary(html), CardHtml.Rate(html)).ShouldBe((Formats.Euros(buckets.AmountOf("expenses")), null));
        CardHtml.Lines(html).Select(line => line.Amount).ToList().ShouldBe(view.Lines.Select(line => Formats.Euros(line.Amount)).ToList());
    }

    [Fact]
    public async Task Card_isReadOnly_noInputNoButtonNoRemove()
    {
        var html = await RenderBucketAsync(Full, "pocket");

        html.ShouldSatisfyAllConditions(
            () => html.ShouldNotContain("<input"),
            () => html.ShouldNotContain("<select"),
            () => Regex.Matches(html, "<button[^>]*>").Select(button => button.Value).ShouldAllBe(button => button.Contains("More about")),
            () => html.ShouldNotContain("Remove"));
    }

    [Fact]
    public async Task Card_isPinned_withItsHeadingAndItsSectionNamedAfterTheBucket()
    {
        var html = await RenderBucketAsync(Full, "kept_in_company");

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("id=\"card-kept_in_company\""),
            () => html.ShouldContain("<h2"),
            () => html.ShouldContain("tabindex=\"-1\""));
    }

    // One amount per source: the pocket line's running totals stay on the map.
    [Fact]
    public async Task Card_ofThePocket_hasOneAmountPerSourceAndNoRunningTotals()
    {
        var view = BucketCardView.From(Full, Full.Run(), "pocket")!;

        var html = await RenderBucketAsync(Full, "pocket");

        CardHtml.Lines(html).Select(line => (line.Label, line.Amount)).ToList()
            .ShouldBe([.. view.Lines.Select(line => (ResxText.For<MapTab>().Get(MapTabText.Module(line.Module))!, Formats.Euros(line.Amount)))]);
        html.ShouldNotContain("<h3");
        (CardHtml.LineRuns(html).Count).ShouldBe(1);
    }

    // The bar's reach is the one share: no bucket's card says one, in the heading or on a line.
    [Theory]
    [MemberData(nameof(Buckets))]
    public async Task Card_ofAnyBucket_givesNoShare(string bucket, bool french)
    {
        using var language = french ? InLanguage.French() : null;

        var html = await RenderBucketAsync(Full, bucket);

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Rate(html).ShouldBeNull(),
            () => CardHtml.Lines(html).ShouldAllBe(line => line.Share == null),
            () => System.Net.WebUtility.HtmlDecode(html).ShouldNotContain("%"));
    }

    public static TheoryData<string, bool> Buckets
    {
        get
        {
            var data = new TheoryData<string, bool>();
            foreach (var french in new[] { false, true })
                foreach (var key in BucketsView.Keys)
                    data.Add(key, french);
            return data;
        }
    }

    // A source alone is not listed under a header giving its amount: the card says where it all comes from.
    [Fact]
    public async Task Card_withASingleSource_namesItInsteadOfRepeatingTheHeader()
    {
        var html = await RenderBucketAsync(RiverInputs.Example, "taxes_on_pay");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).ShouldBeEmpty(),
            () => CardHtml.Prose(html).ToList().ShouldBe([$"All of it comes from the {ResxText.For<MapTab>().Get(MapTabText.Module(RiverModule.DirectorPay))} module."]));
    }

    [Fact]
    public async Task Card_withASingleSource_inFrench_namesItToo()
    {
        using var french = InLanguage.French();

        var html = await RenderBucketAsync(RiverInputs.Example, "taxes_on_pay");

        System.Net.WebUtility.HtmlDecode(CardHtml.Prose(html).Single())
            .ShouldBe($"Tout vient du module {ResxText.French<MapTab>().Get(MapTabText.Module(RiverModule.DirectorPay))}.");
    }

    [Fact]
    public async Task Card_aRefusedRun_saysThereIsNoFigure()
    {
        var html = await RenderBucketAsync(Full.WithRevenue(-1m), "pocket");

        CardHtml.Lines(html).ShouldBeEmpty();
        CardHtml.Prose(html).ShouldBe([ResxText.For<BucketBar>().Get(BucketBarText.Refused)!]);
    }

    [Fact]
    public async Task Card_inFrench_isWorded_withNoKeyLeaking()
    {
        using var french = InLanguage.French();

        var html = await RenderBucketAsync(Full, "pocket");

        CardHtml.LeakedKeys(html).ShouldBeEmpty();
        System.Net.WebUtility.HtmlDecode(html).ShouldNotContain("de départ");
    }
}
