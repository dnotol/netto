using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// The plan page as a viewer meets it on first load, drawn whole without a browser (RiverRendering): the
// bar of buckets with its row of tabs, and the view its URL names: My plan, the river with every card folded
// to its summary and the notes under it folded to one line each, or by date, or another view in its place.
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
    public async Task Plan_tabs_linkEveryViewInOrder()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        Links(html).Select(link => (link.Href, link.Name)).ShouldBe([("plan", "My plan"), ("plan/optimisation", "Optimise"), ("plan/pistes", "Ideas")]);
    }

    // The view the URL names is the one current, whatever the case it is typed in; My plan stays current by
    // date, which is its lens and not another view.
    [Theory]
    [InlineData(null, null, "My plan")]
    [InlineData(null, "date", "My plan")]
    [InlineData("optimisation", null, "Optimise")]
    [InlineData("pistes", null, "Ideas")]
    [InlineData("Pistes", null, "Ideas")]
    public async Task Plan_onAView_marksItAloneCurrent(string? tab, string? by, string current)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab, by: by);

        Links(html).Where(link => link.Current).Select(link => link.Name).ShouldBe([current]);
    }

    // Each view opens on its own head: the page's one h1 is the view's title, whatever the lens.
    [Theory]
    [InlineData(null, null, "My plan")]
    [InlineData(null, "date", "My plan")]
    [InlineData("optimisation", null, "Optimise")]
    [InlineData("pistes", null, "Ideas")]
    public async Task Plan_onAView_opensOnItsTitle(string? tab, string? by, string title)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab, by: by);

        Regex.Matches(html, "<h1[^>]*>([^<]*)</h1>").Select(match => match.Groups[1].Value).ShouldBe([title]);
    }

    // A view opens on its title alone: no eyebrow above it, no « In development » chip, no introduction under
    // it, so the view's content starts right under the bar.
    [Theory]
    [InlineData(null)]
    [InlineData("optimisation")]
    [InlineData("pistes")]
    public async Task Plan_onAView_opensOnItsTitleAlone(string? tab)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab);

        (html.Contains("tab-head__eyebrow"), html.Contains("tab-head__lede"), html.Contains("In development", StringComparison.OrdinalIgnoreCase))
            .ShouldBe((false, false, false));
    }

    // My plan is the map, the card of the module chosen beside it, Company year on arriving, and the two
    // notes on what Netto assumes and does not count yet under it; the moves are a view of their own. Under
    // the head come the lens, by bucket, and no calendar.
    [Fact]
    public async Task Plan_onMyPlan_showsTheLensTheMapCompanyYearsCardAndTheNotes()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        (html.Contains("class=\"map-tab\""), html.Contains("id=\"card-company-year\""), html.Contains("<aside class=\"assumed\""),
                html.Contains("<aside class=\"limits\""), html.Contains("moves__state"), html.Contains("calendar__table"), html.Contains("plan-lens"))
            .ShouldBe((true, true, true, true, false, false, true));
    }

    // By date, what the When to pay tab showed takes the place of the map, its card and the notes under it,
    // under the same head and lens.
    [Fact]
    public async Task Plan_onMyPlanByDate_showsTheCalendarInPlaceOfTheMapAndTheNotes()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), by: "date");

        (html.Contains("calendar__table"), html.Contains("class=\"calendar\""), html.Contains("plan-lens"), html.Contains("class=\"map-tab\""),
                html.Contains("id=\"card-company-year\""), html.Contains("<aside class=\"assumed\""), html.Contains("<aside class=\"limits\""))
            .ShouldBe((true, true, true, false, false, false, false));
    }

    // Any value of the query that names no lens is the plan by bucket.
    [Fact]
    public async Task Plan_onMyPlanByAnUnknownLens_isByBucket()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), by: "nonsense");

        (html.Contains("class=\"map-tab\""), html.Contains("calendar__table")).ShouldBe((true, false));
    }

    [Theory]
    [InlineData("optimisation", "moves__state")]
    [InlineData("pistes", "Nothing is measured here yet.")]
    public async Task Plan_onAnotherView_showsItsOwnContentWithoutTheMap(string tab, string content)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab);

        (html.Contains("class=\"map-tab\""), html.Contains("plan-lens"), html.Contains(content)).ShouldBe((false, false, true));
    }

    // The bar sums up the same river on every view and lens: switching changes no input of it.
    [Fact]
    public async Task Plan_everyView_drawsTheSameBuckets()
    {
        var river = RiverInputs.Example.WithDirectorPay(40_000m);
        var folds = new Folds(new BrowserStorage());

        var buckets = new List<string>();
        foreach (var (tab, by) in new (string?, string?)[] { (null, null), (null, "date"), ("optimisation", null), ("pistes", null) })
            buckets.Add(Buckets(await RenderPlanAsync(folds, tab, river, by: by)));

        var pocket = BucketsView.From(river.Run())!.Buckets.Single(bucket => bucket.Key == "pocket").Amount;
        (buckets.Distinct().Count(), buckets[0].Contains(Formats.Euros(pocket))).ShouldBe((1, true));
    }

    // The page draws before it looks for the moves, which it does once the browser has painted what it
    // drew (PlanMovesSearchTests): on first load Optimise says it is looking for them, short after its
    // name and in full for a screen reader, whether a plan is starred Today or not.
    [Fact]
    public async Task Plan_firstDrawWithToday_badgesOptimiseAsLookingForMoves()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), today: RiverInputs.Example);

        var optimise = Links(html).Single(link => link.Name == "Optimise");
        (optimise.Badge, optimise.Spoken).ShouldBe(("…", "looking for moves"));
    }

    [Fact]
    public async Task Plan_firstDrawWithoutToday_badgesOptimiseAsLookingForMoves()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        var optimise = Links(html).Single(link => link.Name == "Optimise");
        (optimise.Badge, optimise.Spoken).ShouldBe(("…", "looking for moves"));
    }

    // On the Optimisation view, the line that opens the moves once found says the page is looking for them
    // from the plan on screen, with no move yet.
    [Fact]
    public async Task Plan_optimisationFirstDrawn_looksForTheMovesFromThePlanOnScreen()
    {
        var html = WebUtility.HtmlDecode(await RenderPlanAsync(new Folds(new BrowserStorage()), "optimisation"));

        (Paragraph(html, "moves__state"), Regex.Count(html, "<li class=\"moves__move\""))
            .ShouldBe(("Looking for the moves from the plan on screen…", 0));
    }

    // A plan starred Today changes nothing of it: Today feeds the band's gain alone.
    [Fact]
    public async Task Plan_optimisationWithToday_looksForTheMovesFromThePlanOnScreenStill()
    {
        var html = WebUtility.HtmlDecode(await RenderPlanAsync(new Folds(new BrowserStorage()), "optimisation", RiverInputs.Example.WithDirectorPay(40_000m),
            today: RiverInputs.Example));

        Paragraph(html, "moves__state").ShouldBe("Looking for the moves from the plan on screen…");
    }

    // That the page looks for the moves is said in no live region: the bar's says nothing yet, the notice of
    // a module taken out nothing, and neither the tabs nor the view's line are one.
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

    // My plan says the next advance payment's day, on every view: the deadline stays in the band.
    [Theory]
    [InlineData(null)]
    [InlineData("optimisation")]
    [InlineData("pistes")]
    public async Task Plan_paymentStillToMake_badgesMyPlanWithItsDay(string? tab)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab);

        var myPlan = Links(html).Single(link => link.Name == "My plan");
        (myPlan.Badge, myPlan.Spoken).ShouldBe(("21 Dec", "next advance payment by 21 December 2026"));
    }

    // A refusal of what the fund bills, a field of the by-date lens, shows on My plan's tab on every view.
    [Theory]
    [InlineData(null)]
    [InlineData("optimisation")]
    public async Task Plan_billedAmountRefused_marksMyPlan(string? tab)
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), tab, river: RiverInputs.Example.WithBilledContributions(-1m));

        var myPlan = Links(html).Single(link => link.Name == "My plan");
        (myPlan.Refused, myPlan.Spoken).ShouldBe((true, "an amount here is refused"));
    }

    // Saved plans are compared in the Plans panel, which counts them itself: the tab bar holds no badge for
    // them, and Ideas none while nothing finds ideas.
    [Fact]
    public async Task Plan_twoSavedPlans_badgeNoTab()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), saved: [RiverInputs.Example, RiverInputs.Example.WithDirectorPay(40_000m)]);

        Links(html).Where(link => link.Name != "My plan" && link.Name != "Optimise").Select(link => link.Badge).ShouldBe([null]);
    }

    // The one line of nudges is drawn only when it has something to say: on first draw the moves are still
    // being looked for and nothing supplies ideas, so none.
    [Fact]
    public async Task Plan_onMyPlanFirstDraw_drawsNoNudgeYet()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        html.ShouldNotContain("plan-nudges");
    }

    // Plans opens on its comparison where the address asks (plan?plans=compare), the panel closed otherwise.
    [Fact]
    public async Task Plan_comparePlansAddress_opensThePlansPanelOnTheComparison()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()), plansMode: "compare");

        (html.Contains("plans__panel plans__panel--compare"), html.Contains("compare-title")).ShouldBe((true, true));
    }

    [Fact]
    public async Task Plan_noComparePlansAddress_leavesThePlansPanelClosed()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        (html.Contains("plans__panel--compare"), html.Contains("compare-title")).ShouldBe((false, false));
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
        bool withoutToday = false, string? by = null, string? plansMode = null) =>
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
            services.AddSingleton(new RiverState(plans, new BrowserSession()) { Inputs = river ?? RiverInputs.Example });
            services.AddSingleton<TimeProvider>(new Clock(On23October));
            // The lens and the Plans panel's mode are the address's queries (SupplyParameterFromQuery).
            var query = string.Join('&', new[] { by is null ? null : $"by={by}", plansMode is null ? null : $"plans={plansMode}" }.OfType<string>());
            if (query.Length > 0)
            {
                services.AddSupplyValueFromQueryProvider();
                services.AddSingleton<NavigationManager>(new AtAddress($"http://localhost/plan?{query}"));
            }
        });

    // The page's address, which the queries are read from; it goes nowhere.
    private sealed class AtAddress : NavigationManager
    {
        public AtAddress(string uri) => Initialize("http://localhost/", uri);

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
        }
    }
}
