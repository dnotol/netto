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

    // Every move applied to the example: the plan is already the best Netto finds.
    private static readonly RiverInputs EveryMoveApplied = MovesView.Of(RiverInputs.Example, On22October).ApplyAll(RiverInputs.Example);

    // The plan on screen with moves to show and with none.
    public static TheoryData<string, RiverInputs, bool> WithFigures => new()
    {
        { "a plan with moves", RiverInputs.Example, false },
        { "a plan already the best", EveryMoveApplied, true },
    };

    // Where the tab can say only why it has no moves: no figure, so nothing for a sentence to sit beside.
    public static TheoryData<string, RiverInputs> WithoutFigures => new()
    {
        { "a plan starting at the pay", RiverInputs.Example.SkipCompany() },
        { "a plan refused", RiverInputs.Example.WithReserve(-1m) },
    };

    // The moves, then under the three figures and above the line that opens them: the figures,
    // the sentence beside them, the plan the moves are from, the moves.
    [Fact]
    public async Task Optimisation_withMoves_saysTheObjectiveUnderTheFiguresAndAboveTheMoves()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

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

    // The tab says the objective once, as the one component words it, with moves or already the best.
    [Theory]
    [MemberData(nameof(WithFigures))]
    public async Task Optimisation_withFigures_saysTheObjectiveOnce(string _, RiverInputs from, bool alreadyTheBest)
    {
        var html = await RenderTabAsync(from);

        (Regex.Count(html, "<p class=\"moves__objective\""), Objective(html), html.Contains("<ol", StringComparison.Ordinal))
            .ShouldBe((1, Sentence, !alreadyTheBest));
    }

    // The tab says only why it has no moves, or that it looks for them: no sentence about what they are
    // measured by.
    [Theory]
    [MemberData(nameof(WithoutFigures))]
    public async Task Optimisation_withoutFigures_saysNoObjective(string _, RiverInputs from)
    {
        var html = await RenderTabAsync(from);

        html.Contains("moves__objective", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task Optimisation_whileLookingForTheMoves_saysNoObjective()
    {
        var html = await RenderTabAsync(RiverInputs.Example, searching: true);

        (html.Contains("moves__objective", StringComparison.Ordinal), html.Contains("moves__state", StringComparison.Ordinal)).ShouldBe((false, true));
    }

    // What the figures are measured by, and what they leave out, is read in sight, as plain text: in no
    // bubble, which only opens on demand, and in no live region, since nothing on the tab announces.
    [Fact]
    public async Task Optimisation_objective_isPlainTextInSight()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        var paragraph = Regex.Match(html, "<p class=\"moves__objective\".*?</p>", RegexOptions.Singleline).Value;
        (paragraph.Length > 0, Regex.IsMatch(paragraph, "info-bubble|<button|aria-live|role=")).ShouldBe((true, false));
    }

    // The text of the objective's paragraph as a viewer reads it, without the line ending the component's
    // file may leave in it.
    private static string Objective(string html) =>
        Regex.Match(html, "<p class=\"moves__objective\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value.Trim();

    // The tab with what the page found for the plan on screen, or while it looks for them.
    private static async Task<string> RenderTabAsync(RiverInputs plan, bool searching = false) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = searching,
            ["Found"] = searching ? null : Found(plan),
            ["Inputs"] = plan,
        }));

    private static (MovePlan? Value, Refusal? Error)? Found(RiverInputs plan) => plan.FindMoves(On22October);
}
