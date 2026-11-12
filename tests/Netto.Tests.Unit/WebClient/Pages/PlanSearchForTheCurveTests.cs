using System.Reflection;
using Netto.Core.Simulation;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The plan page as the browser runs it (LivePlan, the paint held: none of this is about the moves' search),
// on the Optimisation tab: the page hands the tab the search across every pay it runs for the river, from
// which the pay curve draws (PayCurveTests). The tab runs no search of its own, so what it draws is the river's
// as the page holds it, and the page searches again only when an input other than the pay changes
// (RiverInputs.SameBesidesPay); it is read here from the parameters the page handed the tab.
public class PlanSearchForTheCurveTests
{
    private static readonly RiverInputs Today = RiverInputs.Example.WithRevenue(168_000m).WithRunningCosts(20_000m);

    private static readonly RiverInputs WithARoyalty = Today.WithCreativeShare(0.8m).AddToPlan(OptionalModule.Copyright);

    // Opened on the tab: the search the page runs for the river is the tab's, on the first draw.
    [Fact]
    public async Task Plan_onOptimisation_handsTheTabTheSearchOfTheRiver()
    {
        await using var page = LivePlan.At("http://localhost/plan/optimisation", new BrowserStorage(), holdPaint: true, river: Today);

        await page.LoadAsync("optimisation");

        var tab = await page.FindAsync<OptimisationTab>();
        (tab.Search.ShouldNotBeNull().Best.ShouldNotBeNull().Pay, page.Errors.Count).ShouldBe((Today.SearchSplit().ShouldNotBeNull().Best.ShouldNotBeNull().Pay, 0));
    }

    // A fact of the river changes from the tab, as « Apply » changes the river: the tab is handed the
    // search for the river as it is now, not the one found before. The split names 51,000 for the royalty.
    [Fact]
    public async Task Plan_riverChangedFromTheTab_handsTheTabTheSearchOfTheChangedRiver()
    {
        await using var page = LivePlan.At("http://localhost/plan/optimisation", new BrowserStorage(), holdPaint: true, river: Today);
        await page.LoadAsync("optimisation");
        var tab = await page.FindAsync<OptimisationTab>();
        var before = tab.Search.ShouldNotBeNull().Best.ShouldNotBeNull().Pay;

        await page.InvokeAsync(() => tab.InputsChanged.InvokeAsync(WithARoyalty));

        var after = (await page.FindAsync<OptimisationTab>()).Search.ShouldNotBeNull().Best.ShouldNotBeNull().Pay;
        (before, after, page.Errors.Count).ShouldBe((20_799m, 51_000m, 0));
    }

    // The pay alone changes: the page keeps the search it ran, which is the tab's still, so no second search
    // runs for the tab.
    [Fact]
    public async Task Plan_payChangedFromTheTab_keepsTheSearchTheTabWasHanded()
    {
        await using var page = LivePlan.At("http://localhost/plan/optimisation", new BrowserStorage(), holdPaint: true, river: Today);
        await page.LoadAsync("optimisation");
        var tab = await page.FindAsync<OptimisationTab>();
        var before = tab.Search.ShouldNotBeNull();

        await page.InvokeAsync(() => tab.InputsChanged.InvokeAsync(Today.WithDirectorPay(30_000m)));

        (await page.FindAsync<OptimisationTab>()).Search.ShouldBeSameAs(before);
    }

    // The search runs the year at every pay on the browser's one thread, and only the Optimise view reads it:
    // an edit on My plan runs none, whatever the input.
    [Fact]
    public async Task Plan_editedOnMyPlan_runsNoSplitSearch()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), holdPaint: true, river: Today);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithARoyalty));

        (await SplitSearchHeldAsync(page), page.Errors.Count).ShouldBe((null, 0));
    }

    // Arriving at the view after edits made on My plan: the curve is the search of the plan on screen, not of
    // the plan the page opened on.
    [Fact]
    public async Task Plan_arrivingAtOptimisationAfterEditsOnMyPlan_handsTheTabTheSearchOfThePlanOnScreen()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), holdPaint: true, river: Today);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithARoyalty));

        await page.SwitchTabAsync("optimisation");

        var tab = await page.FindAsync<OptimisationTab>();
        (tab.Search.ShouldNotBeNull().Best.ShouldNotBeNull().Pay, page.Errors.Count).ShouldBe((51_000m, 0));
    }

    // The pay alone edited on My plan: the curve found before is still the plan's, so arriving again keeps it.
    [Fact]
    public async Task Plan_arrivingAtOptimisationAgainAfterAPayEditOnMyPlan_keepsTheSearchFound()
    {
        await using var page = LivePlan.At("http://localhost/plan/optimisation", new BrowserStorage(), holdPaint: true, river: Today);
        await page.LoadAsync("optimisation");
        var before = (await page.FindAsync<OptimisationTab>()).Search.ShouldNotBeNull();
        await page.SwitchTabAsync(null);
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(Today.WithDirectorPay(30_000m)));

        await page.SwitchTabAsync("optimisation");

        (await page.FindAsync<OptimisationTab>()).Search.ShouldBeSameAs(before);
    }

    // The search the page holds, read as its private state: no seam counts the searches, and what a tab was
    // handed is only drawn on Optimise.
    private static async Task<SplitSearchResult?> SplitSearchHeldAsync(LivePlan page)
    {
        var plan = await page.PageAsync();
        return (SplitSearchResult?)typeof(Plan).GetField("_search", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(plan);
    }
}
