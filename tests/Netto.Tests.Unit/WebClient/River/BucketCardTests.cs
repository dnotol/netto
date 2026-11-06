using System.Text.RegularExpressions;
using Netto.Tests.WebClient.Tabs;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// A bucket's card as a viewer reads it: read-only, its total and its share of the bar's start in the heading,
// a line for each module sending money into it.
public class BucketCardTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    private static Task<string> RenderBucketAsync(RiverInputs river, string bucket) =>
        RenderAsync<BucketCard>(new Folds(new BrowserStorage()), new() { ["Inputs"] = river, ["Run"] = river.Run(), ["Bucket"] = bucket });

    [Fact]
    public async Task Card_ofTheExpenses_saysItsTotalItsShareAndALinePerModule()
    {
        var run = Full.Run();
        var buckets = BucketsView.From(run)!;
        var view = BucketCardView.From(Full, run, "expenses")!;

        var html = await RenderBucketAsync(Full, "expenses");

        (CardHtml.Summary(html), CardHtml.Rate(html))
            .ShouldBe((Formats.Euros(buckets.AmountOf("expenses")), $"{Formats.WholeShare(view.Share!.Value)} of the start"));
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

    [Fact]
    public async Task Card_ofThePocket_listsTheRunningTotalsAfterItsLines()
    {
        var view = BucketCardView.From(Full, Full.Run(), "pocket")!;

        var html = await RenderBucketAsync(Full, "pocket");

        CardHtml.Lines(html).Select(line => line.Amount).ToList()
            .ShouldBe([.. view.Lines.Select(line => Formats.Euros(line.Amount)), .. view.RunningTotals.Select(total => Formats.Euros(total.Total))]);
    }

    [Fact]
    public async Task Card_ofAnotherBucket_hasNoRunningTotals()
    {
        var html = await RenderBucketAsync(Full, "taxes_on_pay");

        CardHtml.LineRuns(html).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Card_aRefusedRun_saysThereIsNoFigure()
    {
        var html = await RenderBucketAsync(Full.WithRevenue(-1m), "pocket");

        CardHtml.Lines(html).ShouldBeEmpty();
        CardHtml.Prose(html).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Card_inFrench_isWorded_withNoKeyLeaking()
    {
        using var french = InLanguage.French();

        var html = await RenderBucketAsync(Full, "pocket");

        CardHtml.LeakedKeys(html).ShouldBeEmpty();
        System.Net.WebUtility.HtmlDecode(html).ShouldContain("de départ");
    }
}
