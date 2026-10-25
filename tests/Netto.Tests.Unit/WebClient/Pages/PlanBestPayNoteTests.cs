using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The plan page as the browser runs it (LivePlan, the paint held: none of this is about the moves' search),
// on the Optimisation tab: the page hands the tab the search across every pay it runs for the river, from
// which the tab names the river's own best pay under the pay's move (PayNote, OptimisationTabPayNoteTests).
// The tab runs no search of its own, so what it names is the river's as the page holds it, and the page
// searches again only when an input other than the pay changes (RiverInputs.SameBesidesPay). What the tab
// draws of it is OptimisationTabPayNoteTests'; it is read here from the parameters the page handed it.
public class PlanBestPayNoteTests
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
    // search for the river as it is now, not the one found before. The split names 51,000 for the royalty
    // (PayNoteTests).
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
}
