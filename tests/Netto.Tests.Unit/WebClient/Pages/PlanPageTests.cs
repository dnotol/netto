using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// The plan page as a viewer meets it on first load, drawn whole without a browser (RiverRendering): the
// bar of buckets with its row of tabs, and the tab its URL names: the river, every card folded to its
// summary and the notes under it folded to one line each, or another tab in its place.
public class PlanPageTests
{
    // The example opens with Company year, the split, Director pay, Corporate tax and Dividend: five
    // cards, and the two notes under them.
    [Fact]
    public async Task Plan_firstLoad_foldsEveryCardAndBothNotes()
    {
        var toggles = Toggles(await RenderPlanAsync(new Folds(new BrowserStorage())));

        (toggles.Count, toggles.Count(toggle => toggle.Contains("aria-expanded=\"false\""))).ShouldBe((7, 7));
    }

    [Fact]
    public async Task Plan_notes_sayWhatTheyHold()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        (html.Contains("8 assumptions Netto makes"), html.Contains("4 things the figures leave out")).ShouldBe((true, true));
    }

    // The notes are folds like the cards: opened, they show their lines.
    [Fact]
    public async Task Plan_notesOpenedByTheViewer_showTheirLines()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(PlanText.AssumedFold, true);
        folds.Set(PlanText.LimitsFold, true);

        var html = await RenderPlanAsync(folds);

        (html.Contains("Resident in Wallonia"), html.Contains("management fee")).ShouldBe((true, true));
    }

    [Fact]
    public async Task Plan_notesFolded_hideTheirLines()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        (html.Contains("Resident in Wallonia"), html.Contains("management fee")).ShouldBe((false, false));
    }

    // The page's headings stay as they were: each note's title is the h2 holding its button, the id its
    // aside is named by.
    [Fact]
    public async Task Plan_notes_areNamedByTheirH2()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        Regex.Matches(html, "<aside class=\"(assumed|limits)\" aria-labelledby=\"(assumed|limits)-title\"[^>]*><h2 [^>]*><button [^>]*><span id=\"(assumed|limits)-title\"")
            .Count.ShouldBe(2);
    }

    // The tabs are the bar's last row, after its figures and its buttons, so they stick under the site
    // header with it; a nav of links named for the keyboard and a screen reader.
    [Fact]
    public async Task Plan_tabs_areANamedNavInTheBarsLastRow()
    {
        var bar = Bar(await RenderPlanAsync(new Folds(new BrowserStorage())));

        var nav = Regex.Match(bar, "<nav [^>]*aria-label=\"Your plan\"");
        (nav.Success, nav.Index > bar.IndexOf("bucket-bar__actions", StringComparison.Ordinal)).ShouldBe((true, true));
    }

    [Fact]
    public async Task Plan_tabs_linkEveryTabInOrder()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        Links(html).Select(link => (link.Href, link.Name)).ShouldBe(
            [("plan", "Plan"), ("plan/optimisation", "Optimisation"), ("plan/when-to-pay", "When to pay"), ("plan/compare", "Compare plans")]);
    }

    // The tab the URL names is the one current, whatever the case it is typed in.
    [Theory]
    [InlineData(null, "Plan")]
    [InlineData("optimisation", "Optimisation")]
    [InlineData("when-to-pay", "When to pay")]
    [InlineData("compare", "Compare plans")]
    [InlineData("When-To-Pay", "When to pay")]
    public async Task Plan_onATab_marksItAloneCurrent(string? tab, string current)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab);

        Links(html).Where(link => link.Current).Select(link => link.Name).ShouldBe([current]);
    }

    // Each tab opens on its own head: the page's one h1 is the tab's title.
    [Theory]
    [InlineData(null, "From what your company earns to what reaches you")]
    [InlineData("optimisation", "Optimisation")]
    [InlineData("when-to-pay", "When to pay")]
    [InlineData("compare", "Compare plans")]
    public async Task Plan_onATab_opensOnItsTitle(string? tab, string title)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab);

        Regex.Matches(html, "<h1[^>]*>([^<]*)</h1>").Select(match => match.Groups[1].Value).ShouldBe([title]);
    }

    // The river and its outline are the Plan tab's; Your moves and When to pay are tabs of their own,
    // no longer cards of the river.
    [Fact]
    public async Task Plan_onPlan_showsTheRiverWithoutTheMovesOrThePayments()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        (html.Contains("id=\"river\""), html.Contains("river-outline"), html.Contains("moves__state"), html.Contains("calendar__table"))
            .ShouldBe((true, true, false, false));
    }

    [Theory]
    [InlineData("optimisation", "moves__state")]
    [InlineData("when-to-pay", "calendar__table")]
    [InlineData("compare", "Your saved plans side by side")]
    public async Task Plan_onAnotherTab_showsItsOwnContentWithoutTheRiver(string tab, string content)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab);

        (html.Contains("id=\"river\""), html.Contains("river-outline"), html.Contains(content)).ShouldBe((false, false, true));
    }

    // The bar sums up the same river on every tab: switching tabs changes no input of it.
    [Fact]
    public async Task Plan_everyTab_drawsTheSameBuckets()
    {
        var river = RiverInputs.Example.WithDirectorPay(40_000m);
        var folds = new Folds(new BrowserStorage());

        var buckets = new List<string>();
        foreach (var tab in new[] { null, "optimisation", "when-to-pay", "compare" })
            buckets.Add(Buckets(await RenderPlanAsync(folds, tab, river)));

        var pocket = BucketsView.From(river.Run())!.Buckets.Single(bucket => bucket.Key == "pocket").Amount;
        (buckets.Distinct().Count(), buckets[0].Contains(Formats.Euros(pocket))).ShouldBe((1, true));
    }

    private static List<string> Toggles(string html) => [.. Regex.Matches(html, "<button[^>]*class=\"fold__toggle\"[^>]*>").Select(match => match.Value)];

    // The bar of buckets, its figures, its buttons and its row of tabs.
    private static string Bar(string html) =>
        Regex.Match(html, "<div class=\"bucket-bar[^\"]*\".*?<p class=\"bucket-bar__live\".*?</nav>", RegexOptions.Singleline).Value;

    // The bar's figures, without the ids its bubbles draw anew each time.
    private static string Buckets(string html) => WebUtility.HtmlDecode(Regex.Replace(
        Regex.Match(html, "<dl class=\"bucket-bar__buckets.*?</dl>", RegexOptions.Singleline).Value, "\\s(id|aria-[a-z]+)=\"[^\"]*\"", ""));

    private static List<(string Href, string Name, bool Current)> Links(string html) =>
    [
        .. Regex.Matches(html, "<a [^>]*class=\"plan-tab-bar__tab\"[^>]*>.*?</a>", RegexOptions.Singleline).Select(match => (
            Regex.Match(match.Value, "href=\"([^\"]*)\"").Groups[1].Value,
            WebUtility.HtmlDecode(Regex.Match(match.Value, "<span [^>]*class=\"plan-tab-bar__name\"[^>]*>([^<]*)</span>").Groups[1].Value),
            match.Value.Contains("aria-current=\"page\""))),
    ];

    private static Task<string> RenderPlanAsync(Folds folds, string? tab = null, RiverInputs? river = null) =>
        RenderAsync(typeof(Plan), folds, tab is null ? [] : new() { ["Tab"] = tab }, services =>
        {
            var storage = new BrowserStorage();
            var plans = new SavedPlans(storage);
            services.AddSingleton<IBrowserStorage>(storage);
            services.AddSingleton(plans);
            services.AddSingleton(new RiverState(plans) { Inputs = river ?? RiverInputs.Example });
            services.AddSingleton(TimeProvider.System);
        });
}
