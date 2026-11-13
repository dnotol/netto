using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The plan page as the browser runs it (LivePlan), its paint held until the test lets the browser paint:
// the page draws the bar, the river and Optimisation looking for the moves, asks to be told once that is
// painted (nettoPaint.after), and only then searches the moves from the plan on screen, here the example the
// page opens on, and draws them. What the first draw shows is PlanPageTests'; when the page searches,
// MovesSourceTests'.
public class PlanMovesSearchTests
{
    private static readonly DateOnly On23October = new(2026, 10, 23);


    [Fact]
    public async Task Plan_loaded_waitsForThePaintBeforeSearchingTheMoves()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), holdPaint: true);

        await page.LoadAsync();

        var bar = await page.FindAsync<PlanTabBar>();
        (page.Calls.Count(call => call == "nettoPaint.after"), bar.Badges[PlanTab.Optimisation][0], page.Errors.Count)
            .ShouldBe((1, new TabBadge(TabBadgeKind.Searching), 0));
    }

    // Painted, the badge says what the moves add to the plan on screen; the page waits for no other paint.
    [Fact]
    public async Task Plan_painted_badgesOptimisationWithWhatTheMovesAdd()
    {
        var total = MovesView.Of(RiverInputs.Example, On23October).Total;
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), holdPaint: true);
        await page.LoadAsync();

        await page.PaintAsync();

        var bar = await page.FindAsync<PlanTabBar>();
        (bar.Badges[PlanTab.Optimisation][0], page.Calls.Count(call => call == "nettoPaint.after"), page.Errors.Count)
            .ShouldBe((new TabBadge(TabBadgeKind.MoreThanPlan, Amount: total), 1, 0));
    }

    // On the Optimisation tab, opened directly: looking for the moves until the paint, then the moves the
    // engine finds for that plan on the page's date.
    [Fact]
    public async Task Plan_paintedOnOptimisation_drawsTheMovesFound()
    {
        var expected = MovesView.Of(RiverInputs.Example, On23October);
        await using var page = LivePlan.At("http://localhost/plan/optimisation", new BrowserStorage(), holdPaint: true);
        await page.LoadAsync("optimisation");
        var searchingBeforeThePaint = (await page.FindAsync<OptimisationTab>()).Searching;

        await page.PaintAsync();

        var tab = await page.FindAsync<OptimisationTab>();
        var drawn = MovesView.Of(tab.Inputs, tab.Found);
        page.ShouldSatisfyAllConditions(
            () => (searchingBeforeThePaint, tab.Searching, drawn.State, drawn.Total).ShouldBe((true, false, MovesState.Moves, expected.Total)),
            () => drawn.Moves.Select(move => (move.Move, move.Gain)).ShouldBe(expected.Moves.Select(move => (move.Move, move.Gain))),
            () => page.Errors.ShouldBeEmpty());
    }

    // Found in the browser on 2026-11-06: the example starred Today, a royalty following the pay at a quarter
    // of the time added to the river and not saved. The tab said « this plan: 61,975.74 » while the band said
    // 62,624.32: the moves were measured from Today. They are measured from the plan on screen, whatever is
    // starred, and the vouchers, out of the plan, are no move. The figures are Fixtures.ExampleWithRoyalty's.
    [Fact]
    public async Task Plan_riverEditedWithTodayStarred_measuresTheMovesFromThePlanOnScreen()
    {
        var storage = new BrowserStorage();
        new SavedPlans(storage).Save(RiverInputs.Example, number => $"Plan {number}");
        var river = Fixtures.ExampleWithRoyalty;
        await using var page = LivePlan.At("http://localhost/plan/optimisation", storage, holdPaint: true, river: river);
        await page.LoadAsync("optimisation");

        await page.PaintAsync();

        var drawn = MovesView.Of((await page.FindAsync<OptimisationTab>()).Inputs, (await page.FindAsync<OptimisationTab>()).Found);
        var bar = await page.FindAsync<BucketBar>();
        page.ShouldSatisfyAllConditions(
            () => (drawn.FromInPocket, bar.Run.Year?.NetInPocket, drawn.Total).ShouldBe((62_624.32m, 62_624.32m, 3_309.67m)),
            () => drawn.Moves.Select(move => move.Move.GetType()).ShouldBe([typeof(Netto.Core.Simulation.PayMove), typeof(Netto.Core.Simulation.CompanyAdvancePaymentMove)]),
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
