using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The plan page as the browser runs it (LivePlan), its paint held until the test lets the browser paint:
// the page draws the bar, the river and Optimisation looking for the moves, asks to be told once that is
// painted (nettoPaint.after), and only then searches the moves from the plan they are measured from, here
// the example the page opens on, and draws them. What the first draw shows is PlanPageTests'; which plan is
// searched and when, MovesSourceTests'.
public class PlanMovesSearchTests
{
    private static readonly DateOnly On23October = new(2026, 10, 23);

    private static readonly MovesBaseline FromTheExample = new(RiverInputs.Example, IsToday: false);

    [Fact]
    public async Task Plan_loaded_waitsForThePaintBeforeSearchingTheMoves()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), holdPaint: true);

        await page.LoadAsync();

        var bar = await page.FindAsync<PlanTabBar>();
        (page.Calls.Count(call => call == "nettoPaint.after"), bar.Badges.GetValueOrDefault(PlanTab.Optimisation), page.Errors.Count)
            .ShouldBe((1, new TabBadge(TabBadgeKind.Searching), 0));
    }

    // Painted, the badge says what the moves add to the plan in the river, as the page opened on it, and the
    // page waits for no other paint.
    [Fact]
    public async Task Plan_painted_badgesOptimisationWithWhatTheMovesAdd()
    {
        var total = MovesView.Of(FromTheExample, On23October).Total;
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), holdPaint: true);
        await page.LoadAsync();

        await page.PaintAsync();

        var bar = await page.FindAsync<PlanTabBar>();
        (bar.Badges.GetValueOrDefault(PlanTab.Optimisation), page.Calls.Count(call => call == "nettoPaint.after"), page.Errors.Count)
            .ShouldBe((new TabBadge(TabBadgeKind.MoreThanPlan, Amount: total), 1, 0));
    }

    // On the Optimisation tab, opened directly: looking for the moves until the paint, then the moves the
    // engine finds for that plan on the page's date.
    [Fact]
    public async Task Plan_paintedOnOptimisation_drawsTheMovesFound()
    {
        var expected = MovesView.Of(FromTheExample, On23October);
        await using var page = LivePlan.At("http://localhost/plan/optimisation", new BrowserStorage(), holdPaint: true);
        await page.LoadAsync("optimisation");
        var searchingBeforeThePaint = (await page.FindAsync<OptimisationTab>()).Searching;

        await page.PaintAsync();

        var tab = await page.FindAsync<OptimisationTab>();
        var drawn = MovesView.Of(tab.From, tab.Found);
        page.ShouldSatisfyAllConditions(
            () => (searchingBeforeThePaint, tab.Searching, drawn.State, drawn.Total).ShouldBe((true, false, MovesState.Moves, expected.Total)),
            () => drawn.Moves.Select(move => (move.Move, move.Gain)).ShouldBe(expected.Moves.Select(move => (move.Move, move.Gain))),
            () => page.Errors.ShouldBeEmpty());
    }

    // The search changes no figure of the river: the bar is handed the same run, so its live region, which
    // speaks only when what reaches the pocket changes, says nothing of the moves being found.
    [Fact]
    public async Task Plan_painted_handsTheBarTheSameRun()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), holdPaint: true);
        await page.LoadAsync();
        var before = (await page.FindAsync<BucketBar>()).Run;

        await page.PaintAsync();

        var after = (await page.FindAsync<BucketBar>()).Run;
        page.ShouldSatisfyAllConditions(
            () => after.ShouldBeSameAs(before),
            () => page.Errors.ShouldBeEmpty());
    }
}
