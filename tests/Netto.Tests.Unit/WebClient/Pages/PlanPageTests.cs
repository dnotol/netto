using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// The plan page as a viewer meets it on first load, drawn whole without a browser (RiverRendering): the
// bar of buckets with its row of tabs, and the tab its URL names: the river, every card folded to its
// summary and the notes under it folded to one line each, or another tab in its place.
public class PlanPageTests
{
    // The deadline still open on this date is December's.
    private static readonly DateTimeOffset On23October = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    // The plan opens on Company year's card beside the map, pinned open, with what it folds (its conditions
    // and where its figures come from) folded, and the two notes under the map folded too.
    [Fact]
    public async Task Plan_firstLoad_foldsWhatCompanyYearsCardFoldsAndBothNotes()
    {
        var toggles = Toggles(await RenderPlanAsync(new Folds(new BrowserStorage())));

        (toggles.Count, toggles.Count(toggle => toggle.Contains("aria-expanded=\"false\""))).ShouldBe((4, 4));
    }

    [Fact]
    public async Task Plan_notes_sayWhatTheyHold()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        (html.Contains("9 assumptions Netto makes"), html.Contains("5 things the figures leave out")).ShouldBe((true, true));
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

        Regex.Count(html, "<aside class=\"(assumed|limits)\" aria-labelledby=\"(assumed|limits)-title\"[^>]*><h2 [^>]*><button [^>]*><span id=\"(assumed|limits)-title\"")
            .ShouldBe(2);
    }

    // The tabs are in the band's top row, after its wordmark and before its buttons, so they stick at the top
    // of the window with it; a nav of links named for the keyboard and a screen reader.
    [Fact]
    public async Task Plan_tabs_areANamedNavInTheBandsTopRow()
    {
        var bar = Bar(await RenderPlanAsync(new Folds(new BrowserStorage())));

        var top = Regex.Match(bar, "<div class=\"bucket-bar__top\".*?<dl class=\"bucket-bar__buckets", RegexOptions.Singleline).Value;
        var nav = Regex.Match(top, "<nav [^>]*aria-label=\"Your plan\"");
        (nav.Success, nav.Index > top.IndexOf("bucket-bar__wordmark", StringComparison.Ordinal), nav.Index < top.IndexOf("bucket-bar__actions", StringComparison.Ordinal))
            .ShouldBe((true, true, true));
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
    [InlineData(null, "Plan")]
    [InlineData("optimisation", "Optimisation")]
    [InlineData("when-to-pay", "When to pay")]
    [InlineData("compare", "Compare plans")]
    public async Task Plan_onATab_opensOnItsTitle(string? tab, string title)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab);

        Regex.Matches(html, "<h1[^>]*>([^<]*)</h1>").Select(match => match.Groups[1].Value).ShouldBe([title]);
    }

    // A tab opens on its title alone: no eyebrow above it, no « In development » chip, no introduction under
    // it, so the tab's content starts right under the bar.
    [Theory]
    [InlineData(null)]
    [InlineData("optimisation")]
    [InlineData("when-to-pay")]
    [InlineData("compare")]
    public async Task Plan_onATab_opensOnItsTitleAlone(string? tab)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab);

        (html.Contains("tab-head__eyebrow"), html.Contains("tab-head__lede"), html.Contains("In development", StringComparison.OrdinalIgnoreCase))
            .ShouldBe((false, false, false));
    }

    // The plan is the map, the card of the module chosen beside it, Company year on arriving, and the two
    // notes on what Netto assumes and does not count yet under it; Your moves and When to pay are tabs of
    // their own.
    [Fact]
    public async Task Plan_onPlan_showsTheMapCompanyYearsCardAndTheNotes()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        (html.Contains("class=\"map-tab\""), html.Contains("id=\"card-company-year\""), html.Contains("<aside class=\"assumed\""),
                html.Contains("<aside class=\"limits\""), html.Contains("moves__state"), html.Contains("calendar__table"))
            .ShouldBe((true, true, true, true, false, false));
    }

    [Theory]
    [InlineData("optimisation", "moves__state")]
    [InlineData("when-to-pay", "calendar__table")]
    [InlineData("compare", "There is no plan to compare yet")]
    public async Task Plan_onAnotherTab_showsItsOwnContentWithoutTheMap(string tab, string content)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab);

        (html.Contains("class=\"map-tab\""), html.Contains(content)).ShouldBe((false, true));
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

    // The page draws before it looks for the moves, which it does once the browser has painted what it
    // drew (PlanMovesSearchTests): on first load Optimisation says it is looking for them, short after its
    // name and in full for a screen reader, whether a plan is starred Today or not.
    [Fact]
    public async Task Plan_firstDrawWithToday_badgesOptimisationAsLookingForMoves()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), today: RiverInputs.Example);

        var optimisation = Links(html).Single(link => link.Name == "Optimisation");
        (optimisation.Badge, optimisation.Spoken).ShouldBe(("…", "looking for moves"));
    }

    [Fact]
    public async Task Plan_firstDrawWithoutToday_badgesOptimisationAsLookingForMoves()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        var optimisation = Links(html).Single(link => link.Name == "Optimisation");
        (optimisation.Badge, optimisation.Spoken).ShouldBe(("…", "looking for moves"));
    }

    // On the Optimisation tab, the head says which plan the moves are from, here the example the page opens
    // on, and the line that opens them once found says the page is looking for them, with no move yet.
    [Fact]
    public async Task Plan_optimisationTabWithoutTodayFirstDrawn_looksForTheMovesFromThePlanInTheRiver()
    {
        var html = WebUtility.HtmlDecode(await RenderPlanAsync(new Folds(new BrowserStorage()), "optimisation"));

        (Paragraph(html, "moves__state"), Regex.Count(html, "<li class=\"moves__move\""))
            .ShouldBe(("Looking for the moves from the plan in your river, as you opened or saved it…", 0));
    }

    // Today deleted, the river on the plan it was opened from: the moves are from that plan, by its name.
    [Fact]
    public async Task Plan_optimisationTabWithAnOpenedPlanAndNoToday_namesIt()
    {
        var opened = RiverInputs.Example.WithDirectorPay(40_000m);

        var html = WebUtility.HtmlDecode(await RenderPlanAsync(new Folds(new BrowserStorage()), "optimisation", opened,
            saved: [RiverInputs.Example, opened], withoutToday: true));

        Paragraph(html, "moves__state").ShouldBe("Looking for the moves from « Plan 2 », as you opened or saved it…");
    }

    // Once a plan is starred Today, the moves are from it, as the tab says.
    [Fact]
    public async Task Plan_optimisationTabWithToday_saysToday()
    {
        var html = WebUtility.HtmlDecode(await RenderPlanAsync(new Folds(new BrowserStorage()), "optimisation", RiverInputs.Example.WithDirectorPay(40_000m),
            today: RiverInputs.Example));

        Paragraph(html, "moves__state").ShouldBe("Looking for the moves from Today, your current setup…");
    }

    // That the page looks for the moves is said in no live region: the bar's says nothing yet, the notice of
    // a module taken out nothing, and neither the tabs nor the tab's line are one.
    [Fact]
    public async Task Plan_firstDraw_saysTheSearchInNoLiveRegion()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), "optimisation");

        var said = Regex.Match(html, "<nav [^>]*aria-label=\"Your plan\".*?</nav>", RegexOptions.Singleline).Value
            + Regex.Match(html, "<section [^>]*class=\"plan-tab\".*?</section>", RegexOptions.Singleline).Value;
        (Regex.IsMatch(html, "<p class=\"bucket-bar__live\"[^>]*>\\s*</p>"), Regex.IsMatch(html, "<div class=\"taken-out\" role=\"status\"[^>]*>\\s*</div>"),
                said.Contains("looking for moves", StringComparison.Ordinal), said.Contains("Looking for the moves", StringComparison.Ordinal),
                Regex.IsMatch(said, "aria-live|role=\"(status|alert)\""))
            .ShouldBe((true, true, true, true, false));
    }

    // When to pay says the next advance payment's day, on every tab.
    [Theory]
    [InlineData(null)]
    [InlineData("optimisation")]
    public async Task Plan_paymentStillToMake_badgesWhenToPayWithItsDay(string? tab)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab);

        var whenToPay = Links(html).Single(link => link.Name == "When to pay");
        (whenToPay.Badge, whenToPay.Spoken).ShouldBe(("21 Dec", "next advance payment by 21 December 2026"));
    }

    // A refusal on When to pay shows on its tab while the viewer is on another.
    [Fact]
    public async Task Plan_billedAmountRefused_marksWhenToPayOnThePlanTab()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), river: RiverInputs.Example.WithBilledContributions(-1m));

        var whenToPay = Links(html).Single(link => link.Name == "When to pay");
        (whenToPay.Refused, whenToPay.Spoken).ShouldBe((true, "an amount here is refused"));
    }

    // Compare plans says how many saved plans can be compared, short after its name and in full for a
    // screen reader, from two.
    [Theory]
    [InlineData(null)]
    [InlineData("compare")]
    public async Task Plan_twoSavedPlans_badgesCompareWithTheirCount(string? tab)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab, saved: [RiverInputs.Example, RiverInputs.Example.WithDirectorPay(40_000m), RiverInputs.Example]);

        var compare = Links(html).Single(link => link.Name == "Compare plans");
        (compare.Badge, compare.Spoken).ShouldBe(("3", "3 saved plans to compare"));
    }

    [Fact]
    public async Task Plan_onlyOneSavedPlan_badgesNoCompare()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), saved: [RiverInputs.Example]);

        Links(html).Single(link => link.Name == "Compare plans").Badge.ShouldBeNull();
    }

    private static List<string> Toggles(string html) => [.. Regex.Matches(html, "<button[^>]*class=\"fold__toggle\"[^>]*>").Select(match => match.Value)];

    // The band at the top of the plan page, its top row of tabs and buttons and its figures, to its live region.
    private static string Bar(string html) =>
        Regex.Match(html, "<div class=\"bucket-bar[^\"]*\".*?<p class=\"bucket-bar__live\".*?</p>", RegexOptions.Singleline).Value;

    // The bar's figures, without the ids its bubbles draw anew each time.
    private static string Buckets(string html) => WebUtility.HtmlDecode(Regex.Replace(
        Regex.Match(html, "<dl class=\"bucket-bar__buckets.*?</dl>", RegexOptions.Singleline).Value, "\\s(id|aria-[a-z]+)=\"[^\"]*\"", ""));

    // Each tab's link: where it goes, its name, whether it is current, and its badge as shown, as spoken,
    // and whether it marks a refusal.
    private static List<(string Href, string Name, bool Current, string? Badge, string? Spoken, bool Refused)> Links(string html) =>
    [
        .. Regex.Matches(html, "<a [^>]*class=\"plan-tab-bar__tab\"[^>]*>.*?</a>", RegexOptions.Singleline).Select(match => (
            Regex.Match(match.Value, "href=\"([^\"]*)\"").Groups[1].Value,
            Text(match.Value, "plan-tab-bar__name")!,
            match.Value.Contains("aria-current=\"page\""),
            Text(match.Value, "plan-tab-bar__badge"),
            Text(match.Value, "plan-tab-bar__spoken"),
            match.Value.Contains("plan-tab-bar__badge--refused"))),
    ];

    // The text of the link's span of that class, the visible badge's text without the icon a refusal
    // draws; none without such a span.
    private static string? Text(string link, string span)
    {
        var found = Regex.Match(link, $"<span [^>]*class=\"{span}[ \"][^>]*>(.*?)</span>", RegexOptions.Singleline);
        return found.Success ? WebUtility.HtmlDecode(Regex.Replace(found.Groups[1].Value, "<[^>]*>", "")).Trim() : null;
    }

    // The text of the first paragraph of that class.
    private static string Paragraph(string html, string paragraph) =>
        Regex.Match(html, $"<p class=\"{paragraph}\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value;

    // The river, the plan starred Today and the plans saved, the first starred unless one is; without Today,
    // the plan starred is deleted once they are all saved.
    internal static Task<string> RenderPlanAsync(Folds folds, string? tab = null, RiverInputs? river = null, RiverInputs? today = null, RiverInputs[]? saved = null,
        bool withoutToday = false) =>
        RenderAsync(typeof(Plan), folds, tab is null ? [] : new() { ["Tab"] = tab }, services =>
        {
            var storage = new BrowserStorage();
            var plans = new SavedPlans(storage);
            if (today is not null)
                plans.Star(plans.Save(today, number => $"Plan {number}").Id);
            foreach (var plan in saved ?? [])
                plans.Save(plan, number => $"Plan {number}");
            if (withoutToday && plans.Today is { } starred)
                plans.Delete(starred);
            services.AddSingleton<IBrowserStorage>(storage);
            services.AddSingleton(plans);
            services.AddSingleton(new RiverState(plans) { Inputs = river ?? RiverInputs.Example });
            services.AddSingleton<TimeProvider>(new Clock(On23October));
        });
}
