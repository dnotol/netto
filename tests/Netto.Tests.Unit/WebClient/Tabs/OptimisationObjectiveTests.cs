using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Optimisation tab says what its figures are measured by: one sentence under them (River/ObjectiveNote:
// the objective, what is not weighed yet, the accountant's check), drawn where the tab draws its figures
// and nowhere else. The first recommendation a visitor meets, « pay yourself less », had nothing beside it
// (review of 2026-10-24). What the sentence says is ObjectiveNoteTests'; the rest of the tab is
// OptimisationTabTests'.
public class OptimisationObjectiveTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    // The sentence as the one component holds it.
    private static readonly string Sentence = ResxText.For<ObjectiveNote>().Get("Sentence").ShouldNotBeNull();

    // Every move from Today applied to it: the plan is already the best Netto finds.
    private static readonly RiverInputs EveryMoveApplied = ApplyEveryMove(RiverInputs.Example);

    // The plan the moves are measured from, whichever it is, with moves to show and with none.
    public static TheoryData<string, MovesBaseline, bool> WithFigures => new()
    {
        { "Today with moves", MovesBaseline.Today(RiverInputs.Example), false },
        { "Today already the best", MovesBaseline.Today(EveryMoveApplied), true },
        { "the plan in the river with moves", new MovesBaseline(RiverInputs.Example, IsToday: false), false },
        { "the plan in the river already the best", new MovesBaseline(EveryMoveApplied, IsToday: false), true },
        { "a named plan with moves", new MovesBaseline(RiverInputs.Example, IsToday: false, "Plan 2"), false },
        { "a named plan already the best", new MovesBaseline(EveryMoveApplied, IsToday: false, "Plan 2"), true },
    };

    // Where the tab can say only why it has no moves: no figure, so nothing for a sentence to sit beside.
    public static TheoryData<string, MovesBaseline> WithoutFigures => new()
    {
        { "Today starting at the pay", MovesBaseline.Today(RiverInputs.Example.SkipCompany()) },
        { "Today refused", MovesBaseline.Today(RiverInputs.Example.WithReserve(-1m)) },
        { "the plan in the river starting at the pay", new MovesBaseline(RiverInputs.Example.SkipCompany(), IsToday: false) },
        { "the plan in the river refused", new MovesBaseline(RiverInputs.Example.WithReserve(-1m), IsToday: false) },
    };

    // The moves from Today, then under the three figures and above the line that opens them: the figures,
    // the sentence beside them, the plan the moves are from, the moves.
    [Fact]
    public async Task Optimisation_withMoves_saysTheObjectiveUnderTheFiguresAndAboveTheMoves()
    {
        var html = await RenderTabAsync(MovesBaseline.Today(RiverInputs.Example));

        var places = new[]
        {
            html.IndexOf("</dl>", StringComparison.Ordinal),
            html.IndexOf("<p class=\"moves__objective\"", StringComparison.Ordinal),
            html.IndexOf("<p class=\"moves__state\"", StringComparison.Ordinal),
            html.IndexOf("<ol", StringComparison.Ordinal),
        };
        places.ShouldSatisfyAllConditions(
            () => places.ShouldAllBe(place => place > -1),
            () => places.ShouldBe([.. places.Order()]));
    }

    // The objective is the same whichever plan the moves are from, and the tab says it once, as the one
    // component words it.
    [Theory]
    [MemberData(nameof(WithFigures))]
    public async Task Optimisation_withFigures_saysTheSameObjectiveOnceWhicheverPlanTheMovesAreFrom(string _, MovesBaseline from, bool alreadyTheBest)
    {
        var html = await RenderTabAsync(from);

        (Regex.Count(html, "<p class=\"moves__objective\""), Objective(html), html.Contains("<ol", StringComparison.Ordinal))
            .ShouldBe((1, Sentence, !alreadyTheBest));
    }

    // The tab says only why it has no moves, or that it looks for them: no sentence about what they are
    // measured by.
    [Theory]
    [MemberData(nameof(WithoutFigures))]
    public async Task Optimisation_withoutFigures_saysNoObjective(string _, MovesBaseline from)
    {
        var html = await RenderTabAsync(from);

        html.Contains("moves__objective", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(Baselines))]
    public async Task Optimisation_whileLookingForTheMoves_saysNoObjective(string _, MovesBaseline from)
    {
        var html = await RenderTabAsync(from, searching: true);

        (html.Contains("moves__objective", StringComparison.Ordinal), html.Contains("moves__state", StringComparison.Ordinal)).ShouldBe((false, true));
    }

    public static TheoryData<string, MovesBaseline> Baselines => new()
    {
        { "Today", MovesBaseline.Today(RiverInputs.Example) },
        { "the plan in the river", new MovesBaseline(RiverInputs.Example, IsToday: false) },
        { "a named plan", new MovesBaseline(RiverInputs.Example, IsToday: false, "Plan 2") },
    };

    // What the figures are measured by, and what they leave out, is read in sight, as plain text: in no
    // bubble, which only opens on demand, and in no live region, since nothing on the tab announces.
    [Fact]
    public async Task Optimisation_objective_isPlainTextInSight()
    {
        var html = await RenderTabAsync(MovesBaseline.Today(RiverInputs.Example));

        var paragraph = Regex.Match(html, "<p class=\"moves__objective\".*?</p>", RegexOptions.Singleline).Value;
        (paragraph.Length > 0, Regex.IsMatch(paragraph, "info-bubble|<button|aria-live|role=")).ShouldBe((true, false));
    }

    // The text of the objective's paragraph as a viewer reads it, without the line ending the component's
    // file may leave in it.
    private static string Objective(string html) =>
        Regex.Match(html, "<p class=\"moves__objective\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value.Trim();

    private static RiverInputs ApplyEveryMove(RiverInputs today)
    {
        var view = MovesView.Of(today, On22October);
        return view.Moves.Aggregate(today, (river, move) => view.Apply(move, river));
    }

    // The tab with what the page found for the plan the moves are from, or while it looks for them.
    private static async Task<string> RenderTabAsync(MovesBaseline from, bool searching = false) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["From"] = from,
            ["Searching"] = searching,
            ["Found"] = searching ? null : Found(from),
            ["Inputs"] = from.Plan,
        }));

    private static (MovePlan? Value, Refusal? Error)? Found(MovesBaseline from) => from.Plan.FindMoves(On22October);
}
