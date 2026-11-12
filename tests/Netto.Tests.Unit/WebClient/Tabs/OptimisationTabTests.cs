using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Optimisation tab as a viewer meets it, drawn without a browser (RiverRendering): one page of moves
// that retune the plan on screen, under its own head: what the plan leaves in the pocket, what every move
// leaves and the gain, Apply all, then each move, numbered, with why it helps, what it adds, the pocket
// after it, a link to where it acts and « Apply ». What « Apply » and « Apply all » set is MovesViewTests';
// the figures and sentences are MovesViewTests', MoveReasonsTests' and MoveLinksTests'; when the page
// searches, MovesSourceTests'.
public class OptimisationTabTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    [Fact]
    public async Task Optimisation_planStartingAtThePay_saysWhereTheMovesStart()
    {
        var html = await RenderTabAsync(RiverInputs.Example.SkipCompany());

        Paragraph(html, "moves__state").ShouldBe("Your plan starts at your pay: enter its company's year to see the moves.");
    }

    [Fact]
    public async Task Optimisation_planRefused_saysToCorrectIt()
    {
        var html = await RenderTabAsync(RiverInputs.Example.WithReserve(-1m));

        Paragraph(html, "moves__state").ShouldBe("An amount of your plan is refused: correct it to see the moves.");
    }

    [Fact]
    public async Task Optimisation_planWithEveryMoveApplied_isAlreadyTheBest()
    {
        var html = await RenderTabAsync(EveryMoveApplied());

        (Paragraph(html, "moves__state"), html.Contains("moves__move")).ShouldBe(("Your plan is already the best Netto finds with the facts you gave.", false));
    }

    // Each move with what it adds and « Apply ».
    [Fact]
    public async Task Optimisation_withMoves_listsEachMoveWithItsGainAndApply()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example);

        (view.Moves.Count > 1, Regex.Count(html, "<li class=\"moves__move\""), Applies(html)).ShouldBe((true, view.Moves.Count, view.Moves.Count));
    }

    // One « Apply all », with the figures it lands on.
    [Fact]
    public async Task Optimisation_withMoves_offersApplyAllOnce()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        Regex.Count(html, "<div class=\"moves__all\"[^>]*>\\s*<button[^>]*>.*?Apply all.*?</button>\\s*</div>", RegexOptions.Singleline).ShouldBe(1);
    }

    // Nothing to apply: no « Apply all ».
    public static TheoryData<string, RiverInputs> NothingToApply => new()
    {
        { "already the best", EveryMoveApplied() },
        { "starting at the pay", RiverInputs.Example.SkipCompany() },
        { "refused", RiverInputs.Example.WithReserve(-1m) },
    };

    [Theory]
    [MemberData(nameof(NothingToApply))]
    public async Task Optimisation_noMoves_offersNoApplyAll(string _, RiverInputs plan)
    {
        (await RenderTabAsync(plan)).Contains("Apply all").ShouldBeFalse();
    }

    // What the moves add together is the tile's gain above the list: the list does not say it a second time.
    [Fact]
    public async Task Optimisation_withMoves_drawsNoTotalUnderTheList()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example);

        (html.Contains("moves__total"), html.Contains("Together"), Figures(html)[2].Figure).ShouldBe((false, false, Formats.Addition(view.Total)));
    }

    // A tab, not a card: no fold to open, its title the page's h1, and no head above the title.
    [Fact]
    public async Task Optimisation_isNoFoldAndOpensOnItsTitle()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        (html.Contains("fold__toggle"), Regex.IsMatch(html, "<h1[^>]*>Optimise</h1>"), html.Contains("tab-head__eyebrow"), html.Contains("tab-head__lede"))
            .ShouldBe((false, true, false, false));
    }

    // Three figures first, in a description list: the plan's pocket, the optimised plan's, and the gain.
    [Fact]
    public async Task Optimisation_withMoves_opensOnThePlanTheOptimisedPlanAndTheGain()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var figures = Figures(await RenderTabAsync(RiverInputs.Example));

        figures.ShouldBe([
            ("Your plan", Formats.Euros(view.FromInPocket)),
            ("Optimised", Formats.Euros(view.PlanInPocket)),
            ("Certain gain", Formats.Addition(view.Total)),
        ]);
    }

    // The first line says what the moves are measured from and what they touch.
    [Fact]
    public async Task Optimisation_withMoves_saysTheyAreMeasuredFromThePlanOnScreen()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        StateParagraphs(html).ShouldBe([
            "Measured from the plan on screen. Netto only retunes the levers already in your plan, with the facts you gave; each move on top of the ones above it:",
        ]);
    }

    // The moves in the engine's order, numbered for the eye: the list says its own count and place.
    [Fact]
    public async Task Optimisation_withMoves_numbersEachMoveForTheEyeOnly()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example);

        Regex.Matches(html, "<span class=\"moves__number\" aria-hidden=\"true\"[^>]*>(\\d+)</span>").Select(match => match.Groups[1].Value)
            .ShouldBe(view.Moves.Select((_, index) => $"{index + 1}"));
    }

    // Each move says what it adds and what the pocket holds after it: the last, what the plan leaves.
    [Fact]
    public async Task Optimisation_withMoves_saysEachGainAndThePocketAfterIt()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example);

        view.Moves.Select((move, index) => html.Contains($"{Formats.Euros(move.Gain)} more in your pocket</span>")
                && html.Contains($"{Formats.Euros(view.InPocketAfter(index))} in your pocket after it"))
            .ShouldAllBe(said => said);
    }

    // The pay's move says why it helps, under it: where the money goes, and the rate it gives up.
    [Fact]
    public async Task Optimisation_payMove_saysWhyItHelps()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        Regex.IsMatch(html, "<p class=\"moves__why\"[^>]*>" + Regex.Escape(
            "Your pay goes from €50,000.00 to €20,799.00, and the dividend paid out from €39,325.00 to €58,064.23. "
            + "With it, the company gives up the reduced rate of corporate tax.") + "</p>").ShouldBeTrue();
    }

    // A voucher's sentence and the gain beside it agree: the vouchers that reach the pocket, the rest the same
    // money would have reached it as otherwise, and the gain the move shows, said as the same figure.
    [Fact]
    public async Task Optimisation_sportCultureVouchersMove_saysTheRestAndTheGainTheMoveShows()
    {
        var plan = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);
        var move = MovesView.Of(plan, On22October).Moves.Single(line => line.Move is SportCultureVouchersMove);

        var html = await RenderTabAsync(plan);

        var line = Regex.Matches(html, "<li class=\"moves__move\".*?</li>", RegexOptions.Singleline).Select(match => match.Value)
            .Single(item => item.Contains("sport and culture vouchers"));
        (line.Contains($"€100.00 more reaches you tax-free, {Formats.Euros(100m - move.Gain)} less from the rest: {Formats.Euros(move.Gain)} more in your pocket."),
                line.Contains($"{Formats.Euros(move.Gain)} more in your pocket</span>"))
            .ShouldBe((true, true));
    }

    // A link named first by what it shows, then by the move.
    [Fact]
    public async Task Optimisation_payMove_linksTheDirectorPayCard()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        Regex.IsMatch(html, Regex.Escape("<a class=\"moves__link\" href=\"plan?card=director-pay\" aria-label=\"See it on Director pay: Pay yourself €20,799.00 instead of €50,000.00\"")
            + "[^>]*>See it on Director pay</a>").ShouldBeTrue();
    }

    [Fact]
    public async Task Optimisation_paymentMove_linksTheWhenToPaySection()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        Regex.IsMatch(html, "<a class=\"moves__link\" href=\"plan/optimisation\\?show=when\" aria-label=\"See when to pay: Pay €[\\d,.]+ by 21 December 2026 on the company's corporate tax\"[^>]*>See when to pay</a>")
            .ShouldBeTrue();
    }

    // Vouchers out of the plan: no move adds them, so nothing names them.
    [Fact]
    public async Task Optimisation_vouchersOutOfThePlan_proposesNone()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        (html.Contains("eco vouchers"), html.Contains("See it on Vouchers")).ShouldBe((false, false));
    }

    // Vouchers in the plan at none: raising them is a retune, linked to their card.
    [Fact]
    public async Task Optimisation_vouchersInThePlan_proposesThemAndLinksTheirCard()
    {
        var html = await RenderTabAsync(RiverInputs.Example.AddToPlan(OptionalModule.Vouchers));

        (html.Contains("Grant €250.00 of eco vouchers a year"), html.Contains("href=\"plan?card=vouchers\"")).ShouldBe((true, true));
    }

    // Without moves to show, the tab says why, and shows no figure and no list.
    public static TheoryData<string, RiverInputs> Silent => new()
    {
        { "a plan starting at the pay", RiverInputs.Example.SkipCompany() },
        { "a plan refused", RiverInputs.Example.WithReserve(-1m) },
    };

    [Theory]
    [MemberData(nameof(Silent))]
    public async Task Optimisation_noMovesToShow_saysWhyAlone(string _, RiverInputs plan)
    {
        var html = await RenderTabAsync(plan);

        (html.Contains("<dl"), html.Contains("<ol"), html.Contains("moves__state")).ShouldBe((false, false, true));
    }

    // Every move applied: the plan's pocket alone, and that it is the best Netto finds.
    [Fact]
    public async Task Optimisation_planAlreadyTheBest_showsItsPocketAlone()
    {
        var planned = EveryMoveApplied();

        var html = await RenderTabAsync(planned);

        (Figures(html).Single(), html.Contains("<ol"))
            .ShouldBe((("Your plan", Formats.Euros(MovesView.Of(planned, On22October).FromInPocket)), false));
    }

    // What the tab draws from: moves, a plan starting at the pay, a refused one.
    public static TheoryData<string, RiverInputs> States => new()
    {
        { "the example", RiverInputs.Example },
        { "a plan starting at the pay", RiverInputs.Example.SkipCompany() },
        { "a plan refused", RiverInputs.Example.WithReserve(-1m) },
    };

    // The tab announces nothing: no live region, no status, no alert.
    [Theory]
    [MemberData(nameof(States))]
    public async Task Optimisation_anyState_isNoLiveRegion(string _, RiverInputs plan)
    {
        var html = await RenderTabAsync(plan);

        (html.Contains("moves__state"), Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"")).ShouldBe((true, false));
    }

    // The line saying what the tab shows takes the focus once a move is applied, so it can be focused from
    // code alone.
    [Theory]
    [MemberData(nameof(States))]
    public async Task Optimisation_anyState_letsTheStateLineTakeTheFocus(string _, RiverInputs plan)
    {
        var html = await RenderTabAsync(plan);

        Regex.IsMatch(html, "<p class=\"moves__state\" tabindex=\"-1\"").ShouldBeTrue();
    }

    // « Tune your pay » is the section under the moves, before When to pay, once the page hands the tab its search: the
    // curve of the pocket at every pay, also while the moves are looked for, since it reads that search alone.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Optimisation_withTheSearch_drawsTheCurveAfterTheMoves(bool searching)
    {
        var plan = RiverInputs.Example;

        var html = await RenderWithCurveAsync(plan, searching);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("<h2 id=\"tune-title\""),
            () => html.ShouldContain("class=\"pay-curve__plot\""),
            () => html.IndexOf("class=\"tune\"", StringComparison.Ordinal).ShouldBeGreaterThan(html.IndexOf("class=\"moves\"", StringComparison.Ordinal)));
    }

    // « When to pay » is the tab's last section, the whole calendar the plan holds, under the moves and the
    // pay curve; the head is the one h1, the section the next h2 after the curve's, and its parts h3.
    [Fact]
    public async Task Optimisation_withTheCalendar_drawsWhenToPayLastWithHeadingsInOrder()
    {
        var html = await RenderWithCalendarAsync(RiverInputs.Example, openingIt: false);

        html.ShouldSatisfyAllConditions(
            () => Regex.Matches(html, "<h([123])[ >]").Select(match => match.Groups[1].Value).ShouldBe(["1", "2", "2", "3", "3", "3", "3", "3", "3"]),
            () => html.IndexOf("id=\"when-to-pay-title\"", StringComparison.Ordinal).ShouldBeGreaterThan(html.IndexOf("class=\"tune\"", StringComparison.Ordinal)),
            () => html.ShouldContain("class=\"calendar__table\""),
            () => html.ShouldContain("class=\"timeline\""),
            () => html.ShouldContain("id=\"river-billed-contributions\""));
    }

    // With no search, the company's year skipped or an amount refused, the tab draws no curve.
    [Fact]
    public async Task Optimisation_noSearch_drawsNoCurve()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        html.ShouldNotContain("pay-curve__plot");
    }

    // While the page looks for the moves, once painted or after an edit, the tab says so under its head, and
    // nothing else: no figure, no move, nothing to apply.
    [Fact]
    public async Task Optimisation_searching_saysItLooksForTheMovesAlone()
    {
        var html = await RenderSearchingAsync(RiverInputs.Example);

        (Regex.Count(html, "<p class=\"moves__state\""), Paragraph(html, "moves__state"), html.Contains("<dl"), html.Contains("<ol"), html.Contains("Apply"))
            .ShouldBe((1, "Looking for the moves from the plan on screen…", false, false, false));
    }

    // Nor does it announce that it looks: what the moves add is the badge's to say once found.
    [Fact]
    public async Task Optimisation_searching_isNoLiveRegion()
    {
        var html = await RenderSearchingAsync(RiverInputs.Example);

        (html.Contains("moves__state"), Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"")).ShouldBe((true, false));
    }

    // Every move applied to the example.
    private static RiverInputs EveryMoveApplied() => MovesView.Of(RiverInputs.Example, On22October).ApplyAll(RiverInputs.Example);

    // The paragraphs that say what the tab shows, in the order the tab draws them.
    private static IReadOnlyList<string> StateParagraphs(string html) =>
        [.. Regex.Matches(html, "<p class=\"moves__state\"[^>]*>(.*?)</p>").Select(match => match.Groups[1].Value)];

    // The text of the first paragraph of that class.
    private static string Paragraph(string html, string paragraph) =>
        Regex.Match(html, $"<p class=\"{paragraph}\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value;

    // Each term of the description list with its figure.
    private static IReadOnlyList<(string Term, string Figure)> Figures(string html) =>
    [
        .. Regex.Matches(html, "<dt class=\"moves__term\"[^>]*>(.*?)</dt>\\s*<dd class=\"moves__figure[^\"]*\"[^>]*>(.*?)</dd>")
            .Select(match => (Regex.Replace(match.Groups[1].Value, "<[^>]+>", "").Trim(), match.Groups[2].Value)),
    ];

    private static int Applies(string html) => Regex.Count(html, "aria-label=\"Apply: ");

    // The tab with what the page found for the plan on screen on 22 October.
    private static Task<string> RenderTabAsync(RiverInputs plan) => RenderStateAsync(plan, plan.FindMoves(On22October), searching: false);

    // The tab while the page looks for the moves from the plan on screen, having found none yet.
    private static Task<string> RenderSearchingAsync(RiverInputs plan) => RenderStateAsync(plan, found: null, searching: true);

    // The tab with the page's search for the plan, for the curve under the moves.
    private static async Task<string> RenderWithCurveAsync(RiverInputs plan, bool searching) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = searching, ["Found"] = searching ? null : plan.FindMoves(On22October), ["Inputs"] = plan, ["Search"] = plan.SearchSplit(),
        }));

    // The tab with the calendar the page built for the plan on its date, and the search for the curve.
    private static async Task<string> RenderWithCalendarAsync(RiverInputs plan, bool openingIt) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = false, ["Found"] = plan.FindMoves(On22October), ["Inputs"] = plan, ["Search"] = plan.SearchSplit(),
            ["Calendar"] = CalendarView.From(plan, plan.Run(), On22October), ["OpensCalendar"] = openingIt,
        }));

    private static async Task<string> RenderStateAsync(RiverInputs plan, (MovePlan? Value, Refusal? Error)? found, bool searching) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = searching, ["Found"] = found, ["Inputs"] = plan,
        }));
}
