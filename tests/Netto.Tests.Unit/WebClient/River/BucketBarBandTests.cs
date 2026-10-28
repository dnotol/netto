using System.Net;
using System.Text.RegularExpressions;
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

    private static async Task<string> RenderBandAsync(RiverRun run, RiverRun? setup = null) =>
        WebUtility.HtmlDecode(await RenderAsync<BucketBar>(new Folds(new BrowserStorage()), new()
        {
            ["Run"] = run,
            ["Setup"] = setup,
            ["ChildContent"] = Text(Actions),
            ["Tabs"] = Text(Tabs),
        }));
}
