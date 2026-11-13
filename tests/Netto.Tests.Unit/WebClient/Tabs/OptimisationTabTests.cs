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

// The Optimisation tab as a viewer meets it, drawn without a browser (RiverRendering): under its title, a head
// common to its three sub-tabs, what the plan leaves in the pocket, what every move leaves and the gain on one
// line with Apply all, the state line and what the figures are measured by; then the row of sub-tabs, links
// with the chosen one current; then the chosen one alone: the moves, each a compact row with what it changes,
// what it adds, « Apply » and « Why » folded over why it helps and a link to where it acts; the pay curve; or
// When to pay. What « Apply » and « Apply all » set is MovesViewTests'; the figures and sentences are
// MovesViewTests', MoveReasonsTests' and MoveLinksTests'; when the page searches, MovesSourceTests'; what a click
// on Why or a sub-tab does, PlanOptimiseSectionsTests'.
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

    // One « Apply all », at the end of the head's line, on every sub-tab.
    [Theory]
    [InlineData(OptimiseSection.Moves)]
    [InlineData(OptimiseSection.Pay)]
    [InlineData(OptimiseSection.When)]
    public async Task Optimisation_withMoves_offersApplyAllOnceInTheHeadsLine(OptimiseSection section)
    {
        var html = await RenderSectionAsync(RiverInputs.Example, section);

        Regex.Count(Head(html), "<div class=\"optimise__line\".*?</dl>\\s*<div class=\"moves__all\"[^>]*>\\s*<button[^>]*>.*?Apply all.*?</button>\\s*</div>\\s*</div>", RegexOptions.Singleline)
            .ShouldBe(1);
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

    // What the moves add together is the head's gain: no row says what the pocket holds after it, and the list
    // draws no total.
    [Fact]
    public async Task Optimisation_withMoves_saysTheTotalOnceInTheHead()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example);

        (html.Contains("moves__total"), html.Contains("moves__after"), html.Contains("in your pocket after it"), Figures(html)[2].Figure)
            .ShouldBe((false, false, false, Formats.Addition(view.Total)));
    }

    // A tab, not a card: no fold to open, its title the page's h1, and no head above the title.
    [Fact]
    public async Task Optimisation_isNoFoldAndOpensOnItsTitle()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        (html.Contains("fold__toggle"), Regex.IsMatch(html, "<h1[^>]*>Optimise</h1>"), html.Contains("tab-head__eyebrow"), html.Contains("tab-head__lede"))
            .ShouldBe((false, true, false, false));
    }

    // Three figures first, in a description list, each with its label: the plan's pocket, the optimised plan's,
    // and the gain, whose label is for a screen reader alone.
    [Fact]
    public async Task Optimisation_withMoves_opensOnThePlanTheOptimisedPlanAndTheGain()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example);

        html.ShouldSatisfyAllConditions(
            () => Figures(html).ShouldBe([
                ("Your plan", Formats.Euros(view.FromInPocket)),
                ("optimised", Formats.Euros(view.PlanInPocket)),
                ("Certain gain", Formats.Addition(view.Total)),
            ]),
            () => Regex.IsMatch(html, "<dt class=\"moves__term moves__term--unseen\"[^>]*>Certain gain</dt>").ShouldBeTrue());
    }

    // The arrow between the plan and the optimised plan is the stylesheet's: no text of the markup.
    [Fact]
    public async Task Optimisation_withMoves_drawsNoArrowInTheMarkup()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        html.Contains('→').ShouldBeFalse();
    }

    // The head says what the moves are measured by and leave out; the list, once, that they stack.
    [Fact]
    public async Task Optimisation_withMoves_saysWhatTheyAreMeasuredByThenThatTheyStack()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        (StateParagraphs(html).Single(), Paragraph(html, "moves__stacked"), html.IndexOf("moves__stacked", StringComparison.Ordinal) < html.IndexOf("<ol", StringComparison.Ordinal))
            .ShouldBe(("Measured in cash in your pocket this year, with the levers already in your plan; pension accrual and profit kept in the company are not weighed.",
                "Each adjustment counts on top of the ones above it.", true));
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

    [Fact]
    public async Task Optimisation_withMoves_saysEachGain()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example);

        view.Moves.Select(move => Regex.IsMatch(html, $"<span class=\"moves__gain\"[^>]*>{Regex.Escape(Formats.Euros(move.Gain))} more in your pocket</span>")).ShouldAllBe(said => said);
    }

    // Why a move helps, and its link, are folded under « Why », a button saying whether it is open and what it
    // opens, named with its move: folded, neither is drawn, but the frame stays for aria-controls.
    [Fact]
    public async Task Optimisation_withMoves_foldsWhyEachHelpsUnderWhy()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        var html = await RenderTabAsync(RiverInputs.Example);

        html.ShouldSatisfyAllConditions(
            () => Regex.Count(html, "<button type=\"button\" class=\"moves__toggle\" aria-expanded=\"false\" aria-controls=\"[^\"]+\" aria-label=\"Why: ").ShouldBe(view.Moves.Count),
            () => Regex.IsMatch(html, "aria-label=\"Why: Pay yourself €20,799.00 instead of €50,000.00\"").ShouldBeTrue(),
            () => html.ShouldNotContain("moves__why"),
            () => html.ShouldNotContain("moves__link"),
            () => Regex.Count(html, "<div id=\"[^\"]+\" class=\"moves__more\" hidden").ShouldBe(view.Moves.Count));
    }

    // Vouchers out of the plan: no move adds them, so nothing names them.
    [Fact]
    public async Task Optimisation_vouchersOutOfThePlan_proposesNone()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        html.Contains("eco vouchers").ShouldBeFalse();
    }

    // Vouchers in the plan at none: raising them is a retune.
    [Fact]
    public async Task Optimisation_vouchersInThePlan_proposesThem()
    {
        var html = await RenderTabAsync(RiverInputs.Example.AddToPlan(OptionalModule.Vouchers));

        html.Contains("Grant €250.00 of eco vouchers a year").ShouldBeTrue();
    }

    // Without moves to show, the head says why, and shows no figure and no list.
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

    // The tab announces nothing, on any sub-tab: no live region, no status, no alert.
    [Theory]
    [MemberData(nameof(States))]
    public async Task Optimisation_anyState_isNoLiveRegion(string _, RiverInputs plan)
    {
        foreach (var section in OptimiseSections.Order)
        {
            var html = await RenderSectionAsync(plan, section);

            (html.Contains("moves__state"), Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"")).ShouldBe((true, false), $"{section}");
        }
    }

    // The line saying what the tab shows takes the focus once a move is applied, from any sub-tab, so it is in
    // the head and can be focused from code alone.
    [Theory]
    [MemberData(nameof(States))]
    public async Task Optimisation_anyState_letsTheStateLineInTheHeadTakeTheFocus(string _, RiverInputs plan)
    {
        foreach (var section in OptimiseSections.Order)
        {
            var html = await RenderSectionAsync(plan, section);

            Regex.IsMatch(Head(html), "<p class=\"moves__state\" tabindex=\"-1\"").ShouldBeTrue($"{section}");
        }
    }

    // The row of sub-tabs: links in a nav named for a screen reader, after the head, the chosen one current.
    [Theory]
    [InlineData(OptimiseSection.Moves)]
    [InlineData(OptimiseSection.Pay)]
    [InlineData(OptimiseSection.When)]
    public async Task Optimisation_anySubTab_drawsTheRowOfLinksWithTheChosenOneCurrent(OptimiseSection section)
    {
        var html = await RenderSectionAsync(RiverInputs.Example, section);

        var links = Regex.Matches(html, "<a href=\"([^\"]+)\" class=\"optimise__section\"( aria-current=\"page\")?[^>]*>\\s*<span[^>]*>([^<]+)</span>")
            .Select(match => (Href: match.Groups[1].Value, Current: match.Groups[2].Success, Name: match.Groups[3].Value)).ToList();
        html.ShouldSatisfyAllConditions(
            () => Regex.IsMatch(html, "<nav class=\"optimise__sections\" aria-label=\"Optimise sections\"").ShouldBeTrue(),
            () => links.Select(link => (link.Href, link.Name)).ShouldBe([
                ("plan/optimisation", "Adjustments"), ("plan/optimisation?show=pay", "Pay"), ("plan/optimisation?show=when", "When to pay")]),
            () => links.Where(link => link.Current).Select(link => link.Href).ShouldBe([OptimiseSections.Href(section)]),
            () => html.IndexOf("optimise__sections", StringComparison.Ordinal).ShouldBeGreaterThan(html.IndexOf("moves__state", StringComparison.Ordinal)));
    }

    // Adjustments says how many moves were found, When to pay the next open deadline's day: short to the eye, in
    // words for a screen reader, after the name; Pay says nothing.
    [Fact]
    public async Task Optimisation_withMovesAndACalendar_saysTheCountAndTheNextDeadlineAfterTheirNames()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var next = CalendarView.From(RiverInputs.Example, RiverInputs.Example.Run(), On22October).ShouldNotBeNull().Next.ShouldNotBeNull();

        var html = await RenderSectionAsync(RiverInputs.Example, OptimiseSection.Moves);

        var badges = Regex.Matches(html, "<span class=\"optimise__badge\" aria-hidden=\"true\"[^>]*>([^<]+)</span>\\s*<span class=\"optimise__spoken\"[^>]*>([^<]+)</span>")
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value)).ToList();
        badges.ShouldBe([
            ($"{view.Moves.Count}", $"{view.Moves.Count} adjustments"),
            (Formats.ShortDay(next.Date), $"next payment by {Formats.Day(next.Date)}"),
        ]);
    }

    // While the moves are looked for, Adjustments says no count.
    [Fact]
    public async Task Optimisation_searching_saysNoCount()
    {
        var html = await RenderSearchingAsync(RiverInputs.Example);

        html.ShouldNotContain("optimise__badge");
    }

    // Only the chosen sub-tab is drawn.
    [Theory]
    [InlineData(OptimiseSection.Moves, true, false, false)]
    [InlineData(OptimiseSection.Pay, false, true, false)]
    [InlineData(OptimiseSection.When, false, false, true)]
    public async Task Optimisation_aSubTab_drawsItsPanelAlone(OptimiseSection section, bool moves, bool curve, bool calendar)
    {
        var html = await RenderSectionAsync(RiverInputs.Example, section);

        (html.Contains("<ol class=\"moves__list\""), html.Contains("class=\"pay-curve__plot\""), html.Contains("id=\"when-to-pay-title\""))
            .ShouldBe((moves, curve, calendar));
    }

    // « Tune your pay », the Pay sub-tab, also while the moves are looked for, since it reads the split's search
    // alone.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Optimisation_paySubTab_drawsTheCurve(bool searching)
    {
        var plan = RiverInputs.Example;

        var html = await RenderWithCurveAsync(plan, searching);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("<h2 id=\"tune-title\""),
            () => html.ShouldContain("class=\"pay-curve__plot\""));
    }

    // Each sub-tab's headings: the head is the one h1, then its panel's own h2 and h3.
    [Theory]
    [InlineData(OptimiseSection.Moves, new[] { "1" })]
    [InlineData(OptimiseSection.Pay, new[] { "1", "2" })]
    [InlineData(OptimiseSection.When, new[] { "1", "2", "3", "3", "3", "3", "3", "3" })]
    public async Task Optimisation_aSubTab_hasItsHeadingsInOrder(OptimiseSection section, string[] levels)
    {
        var html = await RenderSectionAsync(RiverInputs.Example, section);

        Regex.Matches(html, "<h([123])[ >]").Select(match => match.Groups[1].Value).ShouldBe(levels);
    }

    // When to pay, the calendar the plan holds.
    [Fact]
    public async Task Optimisation_whenSubTab_drawsTheWholeCalendar()
    {
        var html = await RenderSectionAsync(RiverInputs.Example, OptimiseSection.When);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("class=\"calendar__table\""),
            () => html.ShouldContain("class=\"timeline\""),
            () => html.ShouldContain("id=\"river-billed-contributions\""));
    }

    // With no search, the company's year skipped or an amount refused, the Pay sub-tab draws no curve.
    [Fact]
    public async Task Optimisation_noSearch_drawsNoCurve()
    {
        var html = await RenderStateAsync(RiverInputs.Example, RiverInputs.Example.FindMoves(On22October), searching: false, OptimiseSection.Pay);

        (html.Contains("pay-curve__plot"), html.Contains("tune__unavailable")).ShouldBe((false, true));
    }

    // While the page looks for the moves, once painted or after an edit, the head says so, and nothing else: no
    // figure, no move, nothing to apply.
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

    // The head common to the sub-tabs, up to their row.
    private static string Head(string html) =>
        Regex.Match(html, "<div class=\"optimise__head\".*?<nav", RegexOptions.Singleline).Value;

    // The paragraphs that say what the tab shows, in the order the tab draws them.
    private static IReadOnlyList<string> StateParagraphs(string html) =>
        [.. Regex.Matches(html, "<p class=\"moves__state\"[^>]*>(.*?)</p>").Select(match => match.Groups[1].Value)];

    // The text of the first paragraph of that class.
    private static string Paragraph(string html, string paragraph) =>
        Regex.Match(html, $"<p class=\"{paragraph}\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value;

    // Each term of the description list with its figure.
    private static IReadOnlyList<(string Term, string Figure)> Figures(string html) =>
    [
        .. Regex.Matches(html, "<dt class=\"moves__term[^\"]*\"[^>]*>(.*?)</dt>\\s*<dd class=\"moves__figure[^\"]*\"[^>]*>(.*?)</dd>")
            .Select(match => (Regex.Replace(match.Groups[1].Value, "<[^>]+>", "").Trim(), match.Groups[2].Value)),
    ];

    private static int Applies(string html) => Regex.Count(html, "aria-label=\"Apply: ");

    // The tab with what the page found for the plan on screen on 22 October, on its moves.
    private static Task<string> RenderTabAsync(RiverInputs plan) => RenderStateAsync(plan, plan.FindMoves(On22October), searching: false);

    // The tab while the page looks for the moves from the plan on screen, having found none yet.
    private static Task<string> RenderSearchingAsync(RiverInputs plan) => RenderStateAsync(plan, found: null, searching: true);

    // The tab on the Pay sub-tab with the page's search for the plan, for the curve.
    private static async Task<string> RenderWithCurveAsync(RiverInputs plan, bool searching) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = searching, ["Found"] = searching ? null : plan.FindMoves(On22October), ["Inputs"] = plan, ["Search"] = plan.SearchSplit(),
            ["Section"] = OptimiseSection.Pay,
        }));

    // The tab on a sub-tab, with what the page found, its search for the curve and the calendar on its date.
    private static async Task<string> RenderSectionAsync(RiverInputs plan, OptimiseSection section) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = false, ["Found"] = plan.FindMoves(On22October), ["Inputs"] = plan, ["Search"] = plan.SearchSplit(),
            ["Calendar"] = CalendarView.From(plan, plan.Run(), On22October), ["Section"] = section,
        }));

    private static async Task<string> RenderStateAsync(RiverInputs plan, (MovePlan? Value, Refusal? Error)? found, bool searching,
        OptimiseSection section = OptimiseSection.Moves) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = searching, ["Found"] = found, ["Inputs"] = plan, ["Section"] = section,
        }));
}
