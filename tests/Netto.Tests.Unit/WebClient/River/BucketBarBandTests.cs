using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// On the plan page the bar of buckets is the one band at the top of the window, in the site header's place
// (MainLayoutTests): its top row the wordmark, the page's tabs, the plan's actions and the theme switch,
// under it the five buckets. Drawn alone here, with stand-ins for what the page puts in it.
public class BucketBarBandTests
{
    private const string Tabs = "THE-TABS";
    private const string Actions = "THE-ACTIONS";

    // The wordmark goes to the plan, never to /, as the site header's does: a link to / leaves the plan page,
    // which is rebuilt and forgets what it had found.
    [Fact]
    public async Task Band_opensOnTheWordmark_goingToThePlan()
    {
        var html = await RenderBandAsync(RiverInputs.Example.Run());

        Regex.Match(html, "<a [^>]*>").Value.ShouldSatisfyAllConditions(
            link => link.ShouldContain("class=\"wordmark bucket-bar__wordmark\""),
            link => link.ShouldContain("href=\"/plan\""));
    }

    // The top row in the order the keyboard meets it: the wordmark, the tabs, the plan's actions, the theme
    // switch; the buckets after it.
    [Fact]
    public async Task Band_topRow_holdsTheTabsThenTheActionsThenTheThemeSwitch_beforeTheBuckets()
    {
        var html = await RenderBandAsync(RiverInputs.Example.Run());

        int[] order =
        [
            html.IndexOf("bucket-bar__wordmark", StringComparison.Ordinal),
            html.IndexOf(Tabs, StringComparison.Ordinal),
            html.IndexOf(Actions, StringComparison.Ordinal),
            html.IndexOf("aria-label=\"Switch to light theme\"", StringComparison.Ordinal),
            html.IndexOf("<dl class=\"bucket-bar__buckets", StringComparison.Ordinal),
        ];
        (order.All(index => index >= 0), order.SequenceEqual(order.Order())).ShouldBe((true, true));
    }

    // A bucket is told by its swatch and its name: no icon, and no emoji anywhere in the band.
    [Fact]
    public async Task Band_withEveryBucket_drawsNoIconNorEmoji()
    {
        var html = await RenderBandAsync(WithCar.Run());

        (html.Contains("bucket__icon", StringComparison.Ordinal), Regex.IsMatch(html, @"\p{So}|\p{Cs}|️")).ShouldBe((false, false));
    }

    // Each name opens the bubble of what its bucket holds, drawn as the name itself, in sentence case and with
    // no dotted underline (the band's CSS): the trigger is the bucket's own, not a term of running text.
    [Fact]
    public async Task Band_everyBucketsName_isAPlainTriggerOfItsBubble()
    {
        var html = await RenderBandAsync(RiverInputs.Example.Run());

        Regex.Matches(html, "<dt class=\"bucket__label\"[^>]*>.*?</dt>", RegexOptions.Singleline)
            .Select(label => Regex.Match(label.Value, "<button [^>]*class=\"info-bubble__trigger ([^\"]*)\"[^>]*>([^<]*)</button>").Groups is var trigger
                ? (trigger[1].Value.Trim(), trigger[2].Value)
                : default)
            .ShouldBe([
                ("bucket__name", "Your pocket"), ("bucket__name", "Contributions and tax on pay"), ("bucket__name", "Taxes on profit"),
                ("bucket__name", "Kept in company"), ("bucket__name", "Expenses"),
            ]);
    }

    // The band shows one share, the reach, beside the pocket's amount: no bucket says its share of the start,
    // which the stack under them draws.
    [Fact]
    public async Task Band_reach_isTheOneShareInSight_besideThePocket()
    {
        var run = RiverInputs.Example.Run();
        var reach = Formats.WholeShare(BucketsView.From(run)!.Reach!.Value);

        var html = await RenderBandAsync(run);

        var figures = Regex.Match(html, "<dl class=\"bucket-bar__buckets.*?</dl>", RegexOptions.Singleline).Value;
        var pocket = Regex.Match(figures, "<div class=\"bucket bucket--pocket\".*?</dd>", RegexOptions.Singleline).Value;
        (Regex.Count(Visible(figures), "%"), Visible(pocket).Contains($"{reach} reaches you", StringComparison.Ordinal), html.Contains("bucket__share", StringComparison.Ordinal))
            .ShouldBe((1, true, false));
    }

    // The bubble after the reach says what the band is out of and what the reach means; with the company
    // skipped, where the rest of the river is too.
    public static TheoryData<RiverInputs, string[]> ReachBubbles => new()
    {
        { RiverInputs.Example, ["Out of €100,409.30 of profit before INASTI's company contribution and what the company pays you.", "Your pocket, out of what the river starts from"] },
        { RiverInputs.Example.SkipCompany(), ["Out of €", "of gross pay.", "Your pocket, out of what the river starts from", "Enter your company's year to see the rest of the river."] },
    };

    [Theory]
    [MemberData(nameof(ReachBubbles))]
    public async Task Band_reachBubble_saysWhatTheBandIsOutOfAndWhatTheReachMeans(RiverInputs inputs, string[] said)
    {
        var html = await RenderBandAsync(inputs.Run());

        var bubble = CardHtml.Bubbles(html).Single(bubble => bubble.Label == "what reaches you").Text;
        said.Where(part => !bubble.Contains(part, StringComparison.Ordinal)).ShouldBeEmpty();
    }

    // Where the band draws the shares, the stack says them, and the sentence of what it is out of is in the
    // reach's bubble alone.
    [Fact]
    public async Task Band_sharesDrawn_showsTheStackAndNoSentence()
    {
        var html = await RenderBandAsync(RiverInputs.Example.Run());

        (html.Contains("bucket-bar__stack", StringComparison.Ordinal), CardHtml.Prose(html).Any(text => text.StartsWith("Out of", StringComparison.Ordinal)))
            .ShouldBe((true, false));
    }

    // Where it cannot, the stack's place says why in words: no figure while the engine refuses an amount, or
    // what the river is out of where a share would mean nothing, a loss kept in the company.
    public static TheoryData<RiverInputs, string> NoStack => new()
    {
        { RiverInputs.Example.WithRevenue(-1m), "No figures until the amount marked as refused is corrected." },
        { RiverInputs.Example.WithRevenue(10_000m), "Out of" },
    };

    [Theory]
    [MemberData(nameof(NoStack))]
    public async Task Band_noShares_saysInTheStacksPlaceWhy(RiverInputs inputs, string said)
    {
        var html = await RenderBandAsync(inputs.Run());

        var slot = Regex.Match(html, "<div class=\"bucket-bar__slot\"[^>]*>(.*?)</div>", RegexOptions.Singleline).Groups[1].Value;
        (slot.Contains("bucket-bar__stack", StringComparison.Ordinal), Visible(slot).Trim().StartsWith(said, StringComparison.Ordinal)).ShouldBe((false, true));
    }

    // The band's rows are the same whatever the plan says, so its height never depends on it: a gain over
    // Today on the pocket's line, a refusal or a loss in the stack's place, never a row of their own.
    public static TheoryData<string, RiverRun, RiverRun?> Plans => new()
    {
        { "the example", RiverInputs.Example.Run(), null },
        { "a gain over Today", RiverInputs.Example.WithDirectorPay(20_799m).Run(), RiverInputs.Example.Run() },
        { "not comparable", RiverInputs.Example.SkipCompany().Run(), RiverInputs.Example.Run() },
        { "a refusal", RiverInputs.Example.WithRevenue(-1m).Run(), null },
        { "a loss kept", RiverInputs.Example.WithRevenue(10_000m).Run(), null },
        { "at the pay", RiverInputs.Example.SkipCompany().Run(), null },
    };

    [Theory]
    [MemberData(nameof(Plans))]
    public async Task Band_rows_areTheSameWhateverThePlanSays(string _, RiverRun run, RiverRun? setup)
    {
        var html = await RenderBandAsync(run, setup);

        Regex.Matches(html, "class=\"(bucket-bar__[a-z]+)")
            .Select(match => match.Groups[1].Value)
            .Where(row => row is not ("bucket-bar__stack" or "bucket-bar__segment" or "bucket-bar__from"))
            .Distinct()
            .ShouldBe(["bucket-bar__top", "bucket-bar__tabs", "bucket-bar__actions", "bucket-bar__figures", "bucket-bar__buckets", "bucket-bar__slot",
                "bucket-bar__live"]);
    }

    // YearSimulationCarTests' electric car on the example, so every bucket holds an amount.
    private static RiverInputs WithCar => RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    // What a viewer reads of a piece of the band: its tags and its bubbles' texts left out.
    private static string Visible(string html) =>
        Regex.Replace(Regex.Replace(html, "<span [^>]*class=\"info-bubble__text\"[^>]*>.*?</span>", "", RegexOptions.Singleline), "<[^>]*>", "");

    private static async Task<string> RenderBandAsync(RiverRun run, RiverRun? setup = null) =>
        WebUtility.HtmlDecode(await RenderAsync<BucketBar>(new Folds(new BrowserStorage()), new()
        {
            ["Run"] = run,
            ["Setup"] = setup,
            ["ChildContent"] = Text(Actions),
            ["Tabs"] = Text(Tabs),
        }));
}
