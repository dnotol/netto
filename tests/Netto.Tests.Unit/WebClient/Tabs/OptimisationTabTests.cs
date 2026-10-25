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
// from Today, or while no plan is starred from the plan in the river as it was opened or saved, under its
// own head: what that plan leaves in the pocket, what every move leaves and the gain, then each move,
// numbered, with why it helps, what it adds, the pocket after it, a link to where it acts and « Apply ».
// What « Apply » does is MovesViewTests' and ModuleFoldsTests'; the figures and sentences are
// MovesViewTests', MoveReasonsTests' and MoveLinksTests'; which plan the moves are from, MovesSourceTests'.
public class OptimisationTabTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

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

        (Regex.Count(html, "<li class=\"moves__move\""), Applies(html), html.Contains($"Together: {Formats.Euros(view.Total)} more than Today."))
            .ShouldBe((view.Moves.Count, view.Moves.Count, true));
    }

    // A move the river already holds says so where its « Apply » was.
    [Fact]
    public async Task Optimisation_moveAlreadyInTheRiver_saysSoInsteadOfApply()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var river = view.Apply(view.Moves[0], RiverInputs.Example);

        var html = await RenderTabAsync(RiverInputs.Example, river);

        (Applies(html), Regex.Count(html, ">In the river<")).ShouldBe((view.Moves.Count - 1, 1));
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

        (Regex.Count(html, ">In the river<"), html.Contains("href=\"plan?card=vouchers\"")).ShouldBe((1, true));
    }

    // Without moves to show, the tab says why, and shows no figure and no list.
    public static TheoryData<string, RiverInputs> Silent => new()
    {
        { "Today starting at the pay", RiverInputs.Example.SkipCompany() },
        { "Today refused", RiverInputs.Example.WithReserve(-1m) },
    };

    [Theory]
    [MemberData(nameof(Silent))]
    public async Task Optimisation_noMovesToShow_saysWhyAlone(string _, RiverInputs today)
    {
        var html = await RenderTabAsync(today, RiverInputs.Example);

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

    // What the tab draws from: Today with moves, starting at the pay or refused, and the plan in the river
    // with the river changed since, which shows the note under the first line.
    public static TheoryData<string, MovesBaseline, RiverInputs> States => new()
    {
        { "the example from Today", MovesBaseline.Today(RiverInputs.Example), RiverInputs.Example },
        { "Today starting at the pay", MovesBaseline.Today(RiverInputs.Example.SkipCompany()), RiverInputs.Example },
        { "Today refused", MovesBaseline.Today(RiverInputs.Example.WithReserve(-1m)), RiverInputs.Example },
        { "the plan in the river, changed since", new MovesBaseline(RiverInputs.Example, IsToday: false), RiverInputs.Example.WithDirectorPay(40_000m) },
        { "Today, the river holding other facts", MovesBaseline.Today(RiverInputs.Example), RiverInputs.Example.WithRevenue(168_000m) },
    };

    // The tab announces nothing: no live region, no status, no alert.
    [Theory]
    [MemberData(nameof(States))]
    public async Task Optimisation_anyState_isNoLiveRegion(string _, MovesBaseline from, RiverInputs river)
    {
        var html = await RenderFromAsync(from, river);

        (html.Contains("moves__state"), Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"")).ShouldBe((true, false));
    }

    // While the page looks for the moves, once painted, the tab says from which plan, under its head, and
    // nothing else: no figure, no move, and no word of the river changed since, that plan's moves not known
    // yet.
    public static TheoryData<string, MovesBaseline, string> LookedFor => new()
    {
        { "Today", MovesBaseline.Today(RiverInputs.Example), "Looking for the moves from Today, your current setup…" },
        { "the plan in the river", new MovesBaseline(RiverInputs.Example, IsToday: false), "Looking for the moves from the plan in your river, as you opened or saved it…" },
        { "a saved plan", new MovesBaseline(RiverInputs.Example, IsToday: false, "Plan 2"), "Looking for the moves from « Plan 2 », as you opened or saved it…" },
    };

    [Theory]
    [MemberData(nameof(LookedFor))]
    public async Task Optimisation_searching_saysWhichPlansMovesItLooksForAlone(string _, MovesBaseline from, string said)
    {
        var html = await RenderSearchingAsync(from, RiverInputs.Example.WithDirectorPay(40_000m));

        (Regex.Count(html, "<p class=\"moves__state\""), Paragraph(html, "moves__state"), html.Contains("<dl"), html.Contains("<ol"))
            .ShouldBe((1, said, false, false));
    }

    // Nor does it announce that it looks: what the moves add is the badge's to say once found.
    [Fact]
    public async Task Optimisation_searching_isNoLiveRegion()
    {
        var html = await RenderSearchingAsync(MovesBaseline.Today(RiverInputs.Example), RiverInputs.Example);

        (html.Contains("moves__state"), Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"")).ShouldBe((true, false));
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

    // Without moves to show from the plan in the river, the tab says why, of that plan: what it says, for a
    // plan already the best, one starting at the pay and one refused.
    public static TheoryData<string, RiverInputs> SilentPlans => new()
    {
        { "The plan in your river is already the best Netto finds.", EveryMoveApplied() },
        { "The plan in your river starts at your pay: enter its company's year in the river and save it to see the moves.", RiverInputs.Example.SkipCompany() },
        { "An amount of the plan in your river is refused: correct it in the river and save it to see the moves.", RiverInputs.Example.WithReserve(-1m) },
    };

    [Theory]
    [MemberData(nameof(SilentPlans))]
    public async Task Optimisation_noMovesFromThePlanInTheRiver_saysWhyOfThatPlan(string said, RiverInputs plan)
    {
        var html = await RenderFromAsync(new MovesBaseline(plan, IsToday: false), plan);

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

        (view.Moves.Count > 0, Regex.Count(html, "<li class=\"moves__move\""), Applies(html)).ShouldBe((true, view.Moves.Count, view.Moves.Count));
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

    // The note under the first line, the paragraphs that say which plan the moves are from and how the river
    // differs, in the order the tab draws them.
    private static IReadOnlyList<string> StateParagraphs(string html) =>
        [.. Regex.Matches(html, "<p class=\"moves__state\"[^>]*>(.*?)</p>").Select(match => match.Groups[1].Value)];

    // Found in the browser on 2026-10-25: Today saved with no lever, then a royalty that follows the pay at
    // 80% added to the river. The split searched the river and the tab Today, and nothing said why; the tab
    // now names the card, and that Today holds no such lever.
    [Fact]
    public async Task Optimisation_fromTodayWithARoyaltyAddedToTheRiver_namesTheRoyaltyAsNotInToday()
    {
        var today = RiverInputs.Example.WithRevenue(168_000m).WithRunningCosts(20_000m);

        var html = await RenderTabAsync(today, today.WithCreativeShare(0.8m).AddToPlan(OptionalModule.Copyright));

        StateParagraphs(html).ShouldBe([
            "From Today, your current setup:",
            "Your river differs from Today in Copyright royalty (not in Today), beyond what the moves set: "
            + "the moves are still found for Today. Save the river and star it as Today to find them for it.",
        ]);
    }

    // Each card whose facts differ is named, in the river's order, three names at most and then how many are
    // left; a lever Today holds that the river does not says so too.
    [Fact]
    public async Task Optimisation_fromTodayWithFourCardsDifferent_namesTwoAndCountsTheRest()
    {
        var river = RiverInputs.Example.WithRevenue(168_000m).WithMealVoucherDays(200).AddToPlan(OptionalModule.Vouchers)
            .WithCadastralIncome(1_200m).WithReserve(1_000m);

        var html = await RenderTabAsync(RiverInputs.Example, river);

        StateParagraphs(html)[1].ShouldBe("Your river differs from Today in Company year, Vouchers (not in Today) and 2 more, beyond what the moves set: "
            + "the moves are still found for Today. Save the river and star it as Today to find them for it.");
    }

    [Fact]
    public async Task Optimisation_fromTodayWithALeverTheRiverTookOut_namesItAsNotInTheRiver()
    {
        var today = RiverInputs.Example.WithMealVoucherDays(200).AddToPlan(OptionalModule.Vouchers);

        var html = await RenderTabAsync(today, today.TakeOutOfPlan(OptionalModule.Vouchers));

        StateParagraphs(html)[1].ShouldStartWith("Your river differs from Today in Vouchers (not in your river), beyond what the moves set: ");
    }

    // What the moves set is the river's to move: it says nothing, the pay, a payment and a lever's amount
    // among them, nor does the river as Today; and no resource key reaches the page for want of a text.
    [Fact]
    public async Task Optimisation_fromTodayRiverMovedByWhatTheMovesSet_saysNothingOfADifference()
    {
        var river = RiverInputs.Example.WithDirectorPay(40_000m).WithAdvancePayment(3, 5_000m).WithEcoVouchers(250m).AddToPlan(OptionalModule.Vouchers);

        var html = await RenderTabAsync(RiverInputs.Example, river);

        StateParagraphs(html).ShouldBe(["From Today, your current setup:"]);
        Regex.IsMatch(html, "RiverChanged_|Module_|Difference_").ShouldBeFalse();
    }

    // A river that names a card leaks no key, whichever the number of cards.
    [Fact]
    public async Task Optimisation_fromTodayWithCardsDifferent_leaksNoResourceKey()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverInputs.Example.WithRevenue(168_000m).WithReserve(1_000m));

        (Regex.IsMatch(html, "RiverChanged_|Module_|Difference_"), StateParagraphs(html)[1].Contains("Company year and Dividend", StringComparison.Ordinal)).ShouldBe((false, true));
    }

    // The tab draws the moves the page found for the plan they are from, whatever the river holds since: a
    // change of the river, here its pay, changes none of them. The page finds them once per plan and date
    // (MovesSourceTests); a drag of the pay on the page is the browser check's.
    [Fact]
    public async Task Optimisation_riverChangedSinceThePlanInIt_listsTheSameMoves()
    {
        var from = new MovesBaseline(RiverInputs.Example, IsToday: false);
        var found = from.Plan.FindMoves(On22October);

        var asOpened = Moves(await RenderFoundAsync(from, found, RiverInputs.Example));
        var changed = Moves(await RenderFoundAsync(from, found, RiverInputs.Example.WithDirectorPay(40_000m)));

        (asOpened.Count > 0, changed.SequenceEqual(asOpened)).ShouldBe((true, true));
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

    // Each term of the description list with its figure, the star's icon left out.
    private static IReadOnlyList<(string Term, string Figure)> Figures(string html) =>
    [
        .. Regex.Matches(html, "<dt class=\"moves__term\"[^>]*>(.*?)</dt>\\s*<dd class=\"moves__figure[^\"]*\"[^>]*>(.*?)</dd>")
            .Select(match => (Regex.Replace(match.Groups[1].Value, "<svg.*?</svg>|<[^>]+>", "", RegexOptions.Singleline).Trim(), match.Groups[2].Value)),
    ];

    private static int Applies(string html) => Regex.Count(html, "aria-label=\"Apply: ");

    // The tab with its moves from Today.
    private static Task<string> RenderTabAsync(RiverInputs today, RiverInputs river) => RenderFromAsync(MovesBaseline.Today(today), river);

    private static Task<string> RenderFromAsync(MovesBaseline from, RiverInputs river) => RenderFoundAsync(from, from.Plan.FindMoves(On22October), river);

    // The tab with what the page found for the plan the moves are from.
    private static Task<string> RenderFoundAsync(MovesBaseline from, (MovePlan? Value, Refusal? Error)? found, RiverInputs river) =>
        RenderStateAsync(from, found, river, searching: false);

    // The tab while the page looks for the moves from that plan, having found none yet.
    private static Task<string> RenderSearchingAsync(MovesBaseline from, RiverInputs river) => RenderStateAsync(from, found: null, river, searching: true);

    private static async Task<string> RenderStateAsync(MovesBaseline from, (MovePlan? Value, Refusal? Error)? found, RiverInputs river, bool searching) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["From"] = from, ["Searching"] = searching, ["Found"] = found, ["Inputs"] = river,
        }));

    // Each move's words and what it adds, in the tab's order.
    private static List<(string What, string Gain)> Moves(string html) =>
    [
        .. Regex.Matches(html, "<p class=\"moves__what\"[^>]*>(.*?)</p>.*?<span class=\"moves__gain\"[^>]*>(.*?)</span>", RegexOptions.Singleline)
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value)),
    ];
}
