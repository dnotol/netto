using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The plan page measuring the ideas as the browser runs it (LivePlan), its paint held until the test lets the
// browser paint: once the Optimiser has searched the plan on screen, the ideas are measured, one per paint,
// for the Ideas view, its badge and My plan's nudge; a trial changes the river only once kept, through the
// page's one edit. When the ideas search is PistesSourceTests'; their figures PisteSearchTests'.
public class PlanPistesTests
{
    // Before the paint, nothing is measured: the badge says the ideas are being measured.
    [Fact]
    public async Task Plan_beforeThePaint_badgesIdeasAsBeingMeasured()
    {
        await using var page = LivePlan.At("http://localhost/plan/pistes", new BrowserStorage(), holdPaint: true);

        await page.LoadAsync("pistes");

        var bar = await page.FindAsync<PlanTabBar>();
        (bar.Badges.GetValueOrDefault(PlanTab.Pistes), page.Errors.Count).ShouldBe((new TabBadge(TabBadgeKind.PistesSearching), 0));
    }

    // Painted, the vouchers are measured and the rent and the royalty ask: the badge and the nudge say one
    // priced, two asking.
    [Fact]
    public async Task Plan_painted_countsTheIdeasPricedAndAsking()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), holdPaint: true);
        await page.LoadAsync();

        await page.PaintAsync();

        var bar = await page.FindAsync<PlanTabBar>();
        var nudges = await page.FindAsync<PlanNudges>();
        page.ShouldSatisfyAllConditions(
            () => bar.Badges.GetValueOrDefault(PlanTab.Pistes).ShouldBe(new TabBadge(TabBadgeKind.Pistes, Count: 1, Questions: 2)),
            () => nudges.Pistes.ShouldBe(new PistesCount(1, 2)),
            () => page.Errors.ShouldBeEmpty());
    }

    // On the Ideas view, painted: the vouchers priced, the rent and the royalty asking.
    [Fact]
    public async Task Plan_paintedOnIdeas_drawsTheIdeasMeasured()
    {
        await using var page = LivePlan.At("http://localhost/plan/pistes", new BrowserStorage(), holdPaint: true);
        await page.LoadAsync("pistes");

        await page.PaintAsync();

        var tab = await page.FindAsync<PistesTab>();
        tab.View.Rows.Select(row => (row.Lever, row.State)).ShouldBe([
            (LeverKind.Vouchers, PisteState.Priced), (LeverKind.Rent, PisteState.Asking), (LeverKind.Copyright, PisteState.Asking),
        ]);
    }

    // Answering and trying change nothing of the river; Keep adds the lever with its own moves' values through
    // the page's one edit, and the idea leaves the view.
    [Fact]
    public async Task Plan_vouchersTriedThenKept_changeTheRiverOnlyOnKeep()
    {
        await using var page = LivePlan.At("http://localhost/plan/pistes", new BrowserStorage(), holdPaint: true);
        await page.LoadAsync("pistes");
        await page.PaintAsync();
        var tab = await page.FindAsync<PistesTab>();
        var before = (await page.FindAsync<BucketBar>()).Run;

        await page.InvokeAsync(() => tab.Answered.InvokeAsync(tab.View.Rows[0].Trial.WithGranted(true)));
        await page.InvokeAsync(() => tab.Tried.InvokeAsync(LeverKind.Vouchers));
        var whileTrying = (await page.FindAsync<BucketBar>()).Run;
        var tried = (await page.FindAsync<PistesTab>()).View.Rows[0];
        await page.InvokeAsync(() => tab.Kept.InvokeAsync(LeverKind.Vouchers));

        var kept = (await page.FindAsync<BucketBar>()).Run.Year.ShouldNotBeNull().Scenario.Vouchers;
        page.ShouldSatisfyAllConditions(
            () => whileTrying.ShouldBeSameAs(before),
            () => (tried.Tried, tried.State, tried.CanKeep).ShouldBe((true, PisteState.Priced, true)),
            () => (kept.EcoVouchers, kept.SportCultureVouchers).ShouldBe((250m, 100m)),
            () => page.Errors.ShouldBeEmpty());
        (await page.FindAsync<PistesTab>()).View.Rows.Select(row => row.Lever).ShouldNotContain(LeverKind.Vouchers);
    }

    // Drop discards the trial: the river is as it was, the idea untried.
    [Fact]
    public async Task Plan_vouchersTriedThenDropped_leaveTheRiverAsItWas()
    {
        await using var page = LivePlan.At("http://localhost/plan/pistes", new BrowserStorage(), holdPaint: true);
        await page.LoadAsync("pistes");
        await page.PaintAsync();
        var tab = await page.FindAsync<PistesTab>();
        var before = (await page.FindAsync<BucketBar>()).Run;

        await page.InvokeAsync(() => tab.Answered.InvokeAsync(tab.View.Rows[0].Trial.WithGranted(true)));
        await page.InvokeAsync(() => tab.Tried.InvokeAsync(LeverKind.Vouchers));
        await page.InvokeAsync(() => tab.Dropped.InvokeAsync(LeverKind.Vouchers));

        var vouchers = (await page.FindAsync<PistesTab>()).View.Rows[0];
        ((await page.FindAsync<BucketBar>()).Run, vouchers.Tried, vouchers.Trial.Granted).ShouldBe((before, false, false));
    }
}
