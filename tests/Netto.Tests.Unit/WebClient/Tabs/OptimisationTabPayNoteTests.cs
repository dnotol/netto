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
// The moves are measured from the plan on screen, so the pay's move is the split's best for it unless a move
// taken before it changed what the pay is best at.
public class OptimisationTabPayNoteTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    private static readonly RiverInputs WithARoyalty = RiverInputs.Example.WithRevenue(168_000m).WithRunningCosts(20_000m)
        .WithCreativeShare(0.8m).AddToPlan(OptionalModule.Copyright);

    // The pay's move first, the split's best for the plan on screen: nothing more to say.
    [Fact]
    public async Task Optimisation_payMoveAtTheRiversBest_saysNothingMore()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        (PayLine(html).Length > 0, Regex.Count(html, "moves__note")).ShouldBe((true, 0));
    }

    // The note belongs to the pay's line alone, whatever the other moves are.
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

    private static Task<string> RenderTabAsync(RiverInputs river) => RenderTabAsync(river, river.SearchSplit());

    // The tab drawn with the moves found for the plan on screen and the search the page would hand it.
    private static async Task<string> RenderTabAsync(RiverInputs river, SplitSearchResult? search) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = false, ["Found"] = river.FindMoves(On22October), ["Inputs"] = river, ["Search"] = search,
        }));

    // Each move's row of the list.
    private static List<string> Lines(string html) =>
        [.. Regex.Matches(html, "<li class=\"moves__move\".*?</li>", RegexOptions.Singleline).Select(match => match.Value)];

    // The pay's row: the one move whose words start « Pay yourself ».
    private static string PayLine(string html) => Lines(html).SingleOrDefault(line => line.Contains("Pay yourself")) ?? "";
}
