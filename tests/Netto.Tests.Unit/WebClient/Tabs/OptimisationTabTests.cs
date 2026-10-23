using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Optimisation tab as a viewer meets it, drawn without a browser (RiverRendering): what Your moves
// held as a card of the river, now a tab of its own, open, under its own head. What « Apply » does is
// MovesViewTests' and ModuleFoldsTests'.
public class OptimisationTabTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    [Fact]
    public async Task Optimisation_noToday_saysHowToStarOne()
    {
        var html = await RenderTabAsync(today: null, RiverInputs.Example);

        (html.Contains("star it as Today"), html.Contains("moves__move")).ShouldBe((true, false));
    }

    [Fact]
    public async Task Optimisation_todayStartingAtThePay_saysWhereTheMovesStart()
    {
        var html = await RenderTabAsync(RiverInputs.Example.SkipCompany(), RiverInputs.Example);

        html.Contains("Today starts at your pay").ShouldBeTrue();
    }

    [Fact]
    public async Task Optimisation_todayRefused_saysToCorrectIt()
    {
        var html = await RenderTabAsync(RiverInputs.Example.WithReserve(-1m), RiverInputs.Example);

        html.Contains("An amount of Today is refused").ShouldBeTrue();
    }

    [Fact]
    public async Task Optimisation_todayWithEveryMoveApplied_isAlreadyTheBest()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var planned = view.Moves.Aggregate(RiverInputs.Example, (river, move) => view.Apply(move, river));

        var html = await RenderTabAsync(planned, planned);

        (html.Contains("Today is already the best Netto finds"), html.Contains("moves__move")).ShouldBe((true, false));
    }

    // Each move with what it adds and « Apply », then what they add together.
    [Fact]
    public async Task Optimisation_withMoves_listsEachMoveWithItsGainAndApplyThenTheTotal()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        (Regex.Matches(html, "<li class=\"moves__move\"").Count, Applies(html), html.Contains($"Together: {Formats.Euros(view.Total)} more than Today."))
            .ShouldBe((view.Moves.Count, view.Moves.Count, true));
    }

    // A move the river already holds says so where its « Apply » was.
    [Fact]
    public async Task Optimisation_moveAlreadyInTheRiver_saysSoInsteadOfApply()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var river = view.Apply(view.Moves[0], RiverInputs.Example);

        var html = await RenderTabAsync(RiverInputs.Example, river);

        (Applies(html), Regex.Matches(html, ">In the river<").Count).ShouldBe((view.Moves.Count - 1, 1));
    }

    // A tab, not a card: no fold to open, its title the page's h1.
    [Fact]
    public async Task Optimisation_isNoFoldAndOpensOnItsTitle()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        (html.Contains("fold__toggle"), Regex.IsMatch(html, "<h1[^>]*>Optimisation</h1>"), html.Contains("Against Today, your current setup"))
            .ShouldBe((false, true, true));
    }

    private static int Applies(string html) => Regex.Matches(html, "aria-label=\"Apply: ").Count;

    private static async Task<string> RenderTabAsync(RiverInputs? today, RiverInputs river) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Today"] = today, ["Found"] = today?.FindMoves(On22October), ["Inputs"] = river,
        }));
}
