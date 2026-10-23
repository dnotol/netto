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
            ("Today, in your pocket", Formats.Euros(view.FromInPocket)),
            ("With every move below", Formats.Euros(view.PlanInPocket)),
            ("More than Today", Formats.Addition(view.Total)),
        ]);
    }

    // The star the Plans panel marks Today with says which plan this is, drawn for the eye: a screen reader
    // hears « Today, in your pocket ».
    [Fact]
    public async Task Optimisation_todaysStar_isThePlansPanelsIconHiddenFromScreenReaders()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        var term = Regex.Match(html, "<dt class=\"moves__term\"[^>]*>(.*?)</dt>", RegexOptions.Singleline).Groups[1].Value;
        (Regex.IsMatch(term, "^<svg[^>]*aria-hidden=\"true\"[^>]*>.*</svg>\\s*Today, in your pocket$", RegexOptions.Singleline),
                term.Contains(MudBlazor.Icons.Material.Rounded.Star, StringComparison.Ordinal))
            .ShouldBe((true, true));
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
            .ShouldBe((("Today, in your pocket", Formats.Euros(MovesView.Of(planned, On22October).FromInPocket)), false));
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

    // While no plan is starred Today, the moves are from the plan in the river as it was opened or saved:
    // the head and the first line say so, and the moves add up over that plan.
    [Fact]
    public async Task Optimisation_noToday_measuresFromThePlanInTheRiver()
    {
        var from = new MovesBaseline(RiverInputs.Example, IsToday: false);
        var view = MovesView.Of(from, On22October);

        var html = await RenderFromAsync(from, RiverInputs.Example);

        (Paragraph(html, "tab-head__eyebrow"), Paragraph(html, "tab-head__lede").StartsWith("What Netto would change in the plan in your river, as you opened or saved it,", StringComparison.Ordinal),
                Paragraph(html, "moves__state"), Paragraph(html, "moves__total"))
            .ShouldBe(("Against the plan in your river", true, "From the plan in your river, as you opened or saved it:", $"Together: {Formats.Euros(view.Total)} more than this plan."));
    }

    // Three figures first: the plan's pocket, the pocket with every move, and the gain over the plan.
    [Fact]
    public async Task Optimisation_noToday_opensOnThePlansPocketThePocketWithEveryMoveAndTheGain()
    {
        var from = new MovesBaseline(RiverInputs.Example, IsToday: false);
        var view = MovesView.Of(from, On22October);

        var figures = Figures(await RenderFromAsync(from, RiverInputs.Example));

        figures.ShouldBe([
            ("This plan, in your pocket", Formats.Euros(view.FromInPocket)),
            ("With every move below", Formats.Euros(view.PlanInPocket)),
            ("More than this plan", Formats.Addition(view.Total)),
        ]);
    }

    // The river shows a saved plan as it was saved: the tab names it.
    [Fact]
    public async Task Optimisation_noTodayOnASavedPlan_namesItInTheHeadAndTheFirstLine()
    {
        var html = await RenderFromAsync(new MovesBaseline(RiverInputs.Example, IsToday: false, "Plan 2"), RiverInputs.Example);

        (Paragraph(html, "tab-head__eyebrow"), Paragraph(html, "moves__state"))
            .ShouldBe(("Against « Plan 2 », the plan in your river", "From « Plan 2 », as you opened or saved it:"));
    }

    // Without moves to show from the plan in the river, the tab says why, of that plan.
    public static TheoryData<string, string> SilentPlans => new()
    {
        { "already the best", "The plan in your river is already the best Netto finds." },
        { "starting at the pay", "The plan in your river starts at your pay: enter its company's year in the river and save it to see the moves." },
        { "refused", "An amount of the plan in your river is refused: correct it in the river and save it to see the moves." },
    };

    [Theory]
    [MemberData(nameof(SilentPlans))]
    public async Task Optimisation_noMovesFromThePlanInTheRiver_saysWhyOfThatPlan(string plan, string said)
    {
        var inputs = plan switch
        {
            "already the best" => EveryMoveApplied(),
            "starting at the pay" => RiverInputs.Example.SkipCompany(),
            "refused" => RiverInputs.Example.WithReserve(-1m),
            _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, null),
        };

        var html = await RenderFromAsync(new MovesBaseline(inputs, IsToday: false), inputs);

        (Paragraph(html, "moves__state"), html.Contains("<ol")).ShouldBe((said, false));
    }

    // The star marks the plan starred Today alone.
    [Fact]
    public async Task Optimisation_noToday_drawsNoStar()
    {
        var html = await RenderFromAsync(new MovesBaseline(RiverInputs.Example, IsToday: false), RiverInputs.Example);

        (html.Contains("moves__star"), Figures(html)[0].Term).ShouldBe((false, "This plan, in your pocket"));
    }

    // Every move from the plan in the river is applied as from Today (MovesViewTests).
    [Fact]
    public async Task Optimisation_noToday_offersApplyOnEveryMove()
    {
        var from = new MovesBaseline(RiverInputs.Example, IsToday: false);
        var view = MovesView.Of(from, On22October);

        var html = await RenderFromAsync(from, RiverInputs.Example);

        (view.Moves.Count > 0, Regex.Matches(html, "<li class=\"moves__move\"").Count, Applies(html)).ShouldBe((true, view.Moves.Count, view.Moves.Count));
    }

    // Once a plan is starred Today, the moves are from it, and the head and the first line say so.
    [Fact]
    public async Task Optimisation_fromToday_saysTodayInTheHeadAndTheFirstLine()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example);

        (Paragraph(html, "tab-head__eyebrow"), Paragraph(html, "moves__state")).ShouldBe(("Against Today, your current setup", "From Today, your current setup:"));
    }

    // The river changed since its plan was opened or saved: the moves are still from that plan, and the tab
    // says how to measure them from the river, under the first line.
    [Fact]
    public async Task Optimisation_riverChangedSinceThePlanInIt_saysToSaveIt()
    {
        var html = await RenderFromAsync(new MovesBaseline(RiverInputs.Example, IsToday: false), RiverInputs.Example.WithDirectorPay(40_000m));

        Regex.Matches(html, "<p class=\"moves__state\"[^>]*>(.*?)</p>").Select(match => match.Groups[1].Value).ShouldBe([
            "From the plan in your river, as you opened or saved it:",
            "The river has changed since you opened or saved this plan: save it to measure the moves from it.",
        ]);
    }

    [Fact]
    public async Task Optimisation_riverAsThePlanInIt_saysNothingOfAChange()
    {
        var html = await RenderFromAsync(new MovesBaseline(RiverInputs.Example, IsToday: false), RiverInputs.Example);

        html.Contains("The river has changed").ShouldBeFalse();
    }

    // Every move from Today applied to it.
    private static RiverInputs EveryMoveApplied()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        return view.Moves.Aggregate(RiverInputs.Example, (river, move) => view.Apply(move, river));
    }

    // The text of the first paragraph of that class.
    private static string Paragraph(string html, string paragraph) =>
        Regex.Match(html, $"<p class=\"{paragraph}\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value;

    private static RiverInputs? SilentToday(string today) => today switch
    {
        "no Today" => null,
        "Today starting at the pay" => RiverInputs.Example.SkipCompany(),
        "Today refused" => RiverInputs.Example.WithReserve(-1m),
        _ => throw new ArgumentOutOfRangeException(nameof(today), today, null),
    };

    // Each term of the description list with its figure, the star's icon left out.
    private static IReadOnlyList<(string Term, string Figure)> Figures(string html) =>
    [
        .. Regex.Matches(html, "<dt class=\"moves__term\"[^>]*>(.*?)</dt>\\s*<dd class=\"moves__figure[^\"]*\"[^>]*>(.*?)</dd>")
            .Select(match => (Regex.Replace(match.Groups[1].Value, "<svg.*?</svg>|<[^>]+>", "", RegexOptions.Singleline).Trim(), match.Groups[2].Value)),
    ];

    private static int Applies(string html) => Regex.Matches(html, "aria-label=\"Apply: ").Count;

    // The tab with its moves from Today, none while no plan is starred.
    private static Task<string> RenderTabAsync(RiverInputs? today, RiverInputs river) =>
        RenderFromAsync(today is null ? null : MovesBaseline.Today(today), river);

    private static async Task<string> RenderFromAsync(MovesBaseline? from, RiverInputs river) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["From"] = from, ["Found"] = from?.Plan.FindMoves(On22October), ["Inputs"] = river,
        }));
}
