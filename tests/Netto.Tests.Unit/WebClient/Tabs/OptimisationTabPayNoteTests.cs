using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The note under the Optimisation tab's pay move, as a viewer meets it (RiverRendering): the river's own best
// pay, once the river at the move's pay is not at it (PayNoteTests), with the search the page hands the tab.
// Today saved with no lever and a royalty that follows the pay added to the river is the browser's finding of
// 2026-10-25: the move is Today's best pay, 20,799, the split card names 51,000 for the river.
public class OptimisationTabPayNoteTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    private static readonly RiverInputs Today = RiverInputs.Example.WithRevenue(168_000m).WithRunningCosts(20_000m);

    private static readonly RiverInputs WithARoyalty = Today.WithCreativeShare(0.8m).AddToPlan(OptionalModule.Copyright);

    private const string Note = "For your river as it stands, the best pay is €51,000.00, not this one.";

    // « Apply » was pressed: the line reads « In the river » by value, and says under it that the river's
    // best pay is another, where the split card names it, which the focused « In the river » is described by.
    [Fact]
    public async Task Optimisation_payAppliedWhileTheRiversBestIsAnother_saysItsBestPayUnderTheMove()
    {
        var html = await RenderTabAsync(WithARoyalty.WithDirectorPay(20_799m));

        var line = PayLine(html);
        (Regex.Count(html, "moves__note"), Regex.IsMatch(line, "<p class=\"moves__note\" id=\"moves-pay-note\"[^>]*>" + Regex.Escape(Note) + "</p>"),
                Regex.IsMatch(line, "<span class=\"moves__applied\"[^>]*aria-describedby=\"moves-pay-note\"[^>]*>In the river</span>"),
                line.Contains("href=\"plan?card=split\""))
            .ShouldBe((1, true, true, true));
    }

    // Before « Apply » too: the move offered is not the river's best pay either, so the line says so before
    // the pay is taken, and has no « In the river » to describe.
    [Fact]
    public async Task Optimisation_payNotAppliedWhileTheRiversBestIsAnother_saysItsBestPayUnderTheMove()
    {
        var html = await RenderTabAsync(WithARoyalty);

        var line = PayLine(html);
        (Regex.Count(html, "moves__note"), line.Contains(Note), line.Contains("aria-label=\"Apply: Pay yourself"), html.Contains("aria-describedby"))
            .ShouldBe((1, true, true, false));
    }

    // The river the moves were found for: the pay's best is the move's, so nothing is said and nothing is described.
    [Fact]
    public async Task Optimisation_payAppliedAtTheRiversBest_saysNothingMore()
    {
        var html = await RenderTabAsync(Today.WithDirectorPay(20_799m));

        (Regex.Count(html, "moves__note"), html.Contains("aria-describedby"), PayLine(html).Contains(">In the river<")).ShouldBe((0, false, true));
    }

    // The note belongs to the pay's line alone, whatever the other moves are in the river.
    [Fact]
    public async Task Optimisation_otherMoves_sayNoBestPay()
    {
        var html = await RenderTabAsync(WithARoyalty);

        Lines(html).Where(line => !line.Contains("Pay yourself")).Where(line => line.Contains("moves__note")).ShouldBeEmpty();
    }

    // A page that hands the tab no search (the company's year skipped has none) leaves it silent.
    [Fact]
    public async Task Optimisation_noSearchGiven_saysNoBestPay()
    {
        var html = await RenderTabAsync(WithARoyalty, search: null);

        Regex.Count(html, "moves__note").ShouldBe(0);
    }

    // From the plan in the river as from Today: its words do not name the plan.
    [Fact]
    public async Task Optimisation_noToday_saysTheRiversBestPayUnderTheMoveToo()
    {
        var from = new MovesBaseline(Today, IsToday: false);

        var html = await RenderTabAsync(WithARoyalty, from);

        PayLine(html).Contains(Note).ShouldBeTrue();
    }

    // The tab announces nothing: the note is plain text, in no live region.
    [Fact]
    public async Task Optimisation_withTheNote_isNoLiveRegion()
    {
        var html = await RenderTabAsync(WithARoyalty.WithDirectorPay(20_799m));

        (html.Contains("moves__note"), Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"")).ShouldBe((true, false));
    }

    private static Task<string> RenderTabAsync(RiverInputs river, MovesBaseline? from = null) => RenderTabAsync(river, river.SearchSplit(), from);

    // The tab drawn with the moves found for the plan they are from, the river as given and the search the
    // page would hand it for that river.
    private static async Task<string> RenderTabAsync(RiverInputs river, SplitSearchResult? search, MovesBaseline? from = null)
    {
        var baseline = from ?? MovesBaseline.Today(Today);
        return WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["From"] = baseline, ["Searching"] = false, ["Found"] = baseline.Plan.FindMoves(On22October), ["Inputs"] = river, ["Search"] = search,
        }));
    }

    // Each move's row of the list.
    private static List<string> Lines(string html) =>
        [.. Regex.Matches(html, "<li class=\"moves__move\".*?</li>", RegexOptions.Singleline).Select(match => match.Value)];

    // The pay's row: the one move whose words start « Pay yourself ».
    private static string PayLine(string html) => Lines(html).Single(line => line.Contains("Pay yourself"));
}
