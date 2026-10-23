using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Optimisation tab as a viewer meets it, drawn without a browser (RiverRendering): one page of moves
// from Today, under its own head: what Today leaves in the pocket, what every move leaves and the gain,
// then each move, numbered, with why it helps, what it adds, the pocket after it, a link to where it acts
// and « Apply ». What « Apply » does is MovesViewTests' and ModuleFoldsTests'; the figures and sentences
// are MovesViewTests', MoveReasonsTests' and MoveLinksTests'.
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

    // Three figures first, in a description list: Today's pocket, the pocket with every move, and the gain.
    [Fact]
    public async Task Optimisation_withMoves_opensOnTodayThePlanAndTheGain()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var figures = Figures(await RenderTabAsync(RiverInputs.Example, RiverInputs.Example));

        figures.ShouldBe([
            ("Today ★, in your pocket", Formats.Euros(view.TodayInPocket)),
            ("With every move below", Formats.Euros(view.PlanInPocket)),
            ("More than Today", Formats.Addition(view.Total)),
        ]);
    }

    // The star says which plan Today is; a screen reader hears « Today, in your pocket ».
    [Fact]
    public async Task Optimisation_todaysStar_isHiddenFromScreenReaders()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        Regex.IsMatch(html, "<dt class=\"moves__term\"[^>]*>Today <span class=\"moves__star\" aria-hidden=\"true\"[^>]*>★</span>, in your pocket</dt>").ShouldBeTrue();
    }

    // The moves in the engine's order, numbered for the eye: the list says its own count and place.
    [Fact]
    public async Task Optimisation_withMoves_numbersEachMoveForTheEyeOnly()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        Regex.Matches(html, "<span class=\"moves__number\" aria-hidden=\"true\"[^>]*>(\\d+)</span>").Select(match => match.Groups[1].Value)
            .ShouldBe(view.Moves.Select((_, index) => $"{index + 1}"));
    }

    // Each move says what it adds and what the pocket holds after it: the last, what the plan leaves.
    [Fact]
    public async Task Optimisation_withMoves_saysEachGainAndThePocketAfterIt()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        view.Moves.Select((move, index) => html.Contains($"{Formats.Euros(move.Gain)} more")
                && html.Contains($"{Formats.Euros(view.InPocketAfter(index))} in your pocket after it"))
            .ShouldAllBe(said => said);
    }

    // The pay's move says why it helps, under it: where the money goes, and the rate it gives up.
    [Fact]
    public async Task Optimisation_payMove_saysWhyItHelps()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        Regex.IsMatch(html, "<p class=\"moves__why\"[^>]*>" + Regex.Escape(
            "Your pay goes from €50,000.00 to €20,799.00, and the dividend paid out from €39,325.00 to €58,064.23. "
            + "With it, the company gives up the reduced rate of corporate tax.") + "</p>").ShouldBeTrue();
    }

    // A link named first by what it shows, then by the move.
    [Fact]
    public async Task Optimisation_payMove_linksTheSplit()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        Regex.IsMatch(html, Regex.Escape("<a class=\"moves__link\" href=\"plan?card=split\" aria-label=\"See it on The split: Pay yourself €20,799.00 instead of €50,000.00\"")
            + "[^>]*>See it on The split</a>").ShouldBeTrue();
    }

    [Fact]
    public async Task Optimisation_paymentMove_linksWhenToPay()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        Regex.IsMatch(html, "<a class=\"moves__link\" href=\"plan/when-to-pay\" aria-label=\"See it in When to pay: Pay €[\\d,.]+ by 21 December 2026 on the company's corporate tax\"[^>]*>See it in When to pay</a>")
            .ShouldBeTrue();
    }

    // Vouchers are out of the example's plan: no card to see them on until « Apply » adds it.
    [Fact]
    public async Task Optimisation_leverMoveNotApplied_linksNowhere()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        html.Contains("See it on Vouchers").ShouldBeFalse();
    }

    [Fact]
    public async Task Optimisation_leverMoveApplied_saysSoAndLinksItsCard()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var river = view.Apply(view.Moves.First(move => move.Move is EcoVouchersMove), RiverInputs.Example);

        var html = await RenderTabAsync(RiverInputs.Example, river);

        (Regex.Matches(html, ">In the river<").Count, html.Contains("href=\"plan?card=vouchers\"")).ShouldBe((1, true));
    }

    // Without moves to show, the tab says why, and shows no figure and no list.
    public static TheoryData<string> Silent => new() { "no Today", "Today starting at the pay", "Today refused" };

    [Theory]
    [MemberData(nameof(Silent))]
    public async Task Optimisation_noMovesToShow_saysWhyAlone(string today)
    {
        var html = await RenderTabAsync(SilentToday(today), RiverInputs.Example);

        (html.Contains("<dl"), html.Contains("<ol"), html.Contains("moves__state")).ShouldBe((false, false, true));
    }

    // Every move applied: Today's pocket alone, and that it is the best Netto finds.
    [Fact]
    public async Task Optimisation_todayAlreadyTheBest_showsTodaysPocketAlone()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var planned = view.Moves.Aggregate(RiverInputs.Example, (river, move) => view.Apply(move, river));

        var html = await RenderTabAsync(planned, planned);

        (Figures(html).Single(), html.Contains("<ol"))
            .ShouldBe((("Today ★, in your pocket", Formats.Euros(MovesView.Of(planned, On22October).TodayInPocket)), false));
    }

    // The tab announces nothing: no live region, no status, no alert.
    [Theory]
    [MemberData(nameof(Silent))]
    [InlineData("the example")]
    public async Task Optimisation_anyState_isNoLiveRegion(string today)
    {
        var html = await RenderTabAsync(today == "the example" ? RiverInputs.Example : SilentToday(today), RiverInputs.Example);

        Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"").ShouldBeFalse();
    }

    private static RiverInputs? SilentToday(string today) => today switch
    {
        "no Today" => null,
        "Today starting at the pay" => RiverInputs.Example.SkipCompany(),
        "Today refused" => RiverInputs.Example.WithReserve(-1m),
        _ => throw new ArgumentOutOfRangeException(nameof(today), today, null),
    };

    // Each term of the description list with its figure, the star's markup left out.
    private static IReadOnlyList<(string Term, string Figure)> Figures(string html) =>
    [
        .. Regex.Matches(html, "<dt class=\"moves__term\"[^>]*>(.*?)</dt>\\s*<dd class=\"moves__figure[^\"]*\"[^>]*>(.*?)</dd>")
            .Select(match => (Regex.Replace(match.Groups[1].Value, "<[^>]+>", ""), match.Groups[2].Value)),
    ];

    private static int Applies(string html) => Regex.Matches(html, "aria-label=\"Apply: ").Count;

    private static async Task<string> RenderTabAsync(RiverInputs? today, RiverInputs river) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Today"] = today, ["Found"] = today?.FindMoves(On22October), ["Inputs"] = river,
        }));
}
