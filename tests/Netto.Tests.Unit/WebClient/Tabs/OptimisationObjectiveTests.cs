using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Optimisation tab says what its figures are measured by once, in sight, on every sub-tab: with moves found,
// the head's state line is that one muted sentence (what reaches the pocket this year, with the levers in the
// plan, and what is not weighed), and no other paragraph stands under the head's line. The first recommendation
// a visitor meets, « pay yourself less », had nothing beside it (review of 2026-10-24); the operator asked for
// one sentence under the head (2026-11-13). The band's gain says ObjectiveNote's in a bubble
// (BucketBarGainBubbleTests); the rest of the tab is OptimisationTabTests'.
public class OptimisationObjectiveTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    // The measure, as the tab words it with moves found.
    private const string Measure =
        "Measured in cash in your pocket this year, with the levers already in your plan; pension accrual and profit kept in the company are not weighed.";

    // Every move applied to the example: the plan is already the best Netto finds.
    private static readonly RiverInputs EveryMoveApplied = MovesView.Of(RiverInputs.Example, On22October).ApplyAll(RiverInputs.Example);

    // Where the tab can say only why it has no moves.
    public static TheoryData<string, RiverInputs> WithoutFigures => new()
    {
        { "a plan starting at the pay", RiverInputs.Example.SkipCompany() },
        { "a plan refused", RiverInputs.Example.WithReserve(-1m) },
    };

    // With moves found, on every sub-tab, under the head's line there is one paragraph, the state line, and it
    // is the measure.
    [Theory]
    [InlineData(OptimiseSection.Moves)]
    [InlineData(OptimiseSection.Pay)]
    [InlineData(OptimiseSection.When)]
    public async Task Optimisation_withMoves_saysTheMeasureOnceInTheStateLine(OptimiseSection section)
    {
        var html = await RenderTabAsync(RiverInputs.Example, section: section);

        html.ShouldSatisfyAllConditions(
            () => Paragraphs(Head(html)).ShouldBe([$"moves__state:{Measure}"]),
            () => html.ShouldNotContain("moves__objective"));
    }

    // In French, worded as the glossary says.
    [Fact]
    public async Task Optimisation_withMovesInFrench_saysTheMeasureInFrench()
    {
        using var french = InLanguage.French();

        var html = await RenderTabAsync(RiverInputs.Example);

        Paragraphs(Head(html)).ShouldBe([
            "moves__state:Mesuré en argent dans votre poche cette année, avec les leviers déjà dans votre plan ; la constitution de pension et le bénéfice gardé dans la société ne sont pas pesés.",
        ]);
    }

    // Already the best: its own sentence, and nothing appended.
    [Fact]
    public async Task Optimisation_alreadyTheBest_saysItsOwnSentenceAlone()
    {
        var html = await RenderTabAsync(EveryMoveApplied);

        Paragraphs(Head(html)).ShouldBe(["moves__state:Your plan is already the best Netto finds with the facts you gave."]);
    }

    // The tab says only why it has no moves, or that it looks for them: no word of the measure.
    [Theory]
    [MemberData(nameof(WithoutFigures))]
    public async Task Optimisation_withoutFigures_saysNoMeasure(string _, RiverInputs from)
    {
        var html = await RenderTabAsync(from);

        (html.Contains("Measured in cash", StringComparison.Ordinal), html.Contains("moves__objective", StringComparison.Ordinal)).ShouldBe((false, false));
    }

    [Fact]
    public async Task Optimisation_whileLookingForTheMoves_saysNoMeasure()
    {
        var html = await RenderTabAsync(RiverInputs.Example, searching: true);

        (html.Contains("Measured in cash", StringComparison.Ordinal), html.Contains("moves__state", StringComparison.Ordinal)).ShouldBe((false, true));
    }

    // The measure is read in sight, as plain text: in no bubble, which only opens on demand, and in no live
    // region, since nothing on the tab announces.
    [Fact]
    public async Task Optimisation_measure_isPlainTextInSight()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        var paragraph = Regex.Match(Head(html), "<p class=\"moves__state\".*?</p>", RegexOptions.Singleline).Value;
        (paragraph.Contains(Measure, StringComparison.Ordinal), Regex.IsMatch(paragraph, "info-bubble|<button|aria-live|role=")).ShouldBe((true, false));
    }

    // The head common to the sub-tabs, up to their row.
    private static string Head(string html) =>
        Regex.Match(html, "<div class=\"optimise__head\".*?<nav", RegexOptions.Singleline).Value;

    // Each paragraph, its class and its text as a viewer reads it.
    private static IReadOnlyList<string> Paragraphs(string html) =>
    [
        .. Regex.Matches(html, "<p class=\"([^\"]+)\"[^>]*>(.*?)</p>", RegexOptions.Singleline)
            .Select(match => $"{match.Groups[1].Value}:{match.Groups[2].Value.Trim()}"),
    ];

    // The tab with what the page found for the plan on screen, or while it looks for them.
    private static async Task<string> RenderTabAsync(RiverInputs plan, bool searching = false, OptimiseSection section = OptimiseSection.Moves) =>
        WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = searching,
            ["Found"] = searching ? null : Found(plan),
            ["Inputs"] = plan,
            ["Section"] = section,
        }));

    private static (MovePlan? Value, Refusal? Error)? Found(RiverInputs plan) => plan.FindMoves(On22October);
}
