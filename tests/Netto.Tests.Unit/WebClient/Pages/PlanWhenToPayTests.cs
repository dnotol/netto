using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// When to pay as a section of Optimise, on the plan page as the browser runs it (LivePlan): My plan has no lens
// and no calendar, the section is reached by /plan/optimisation?show=when (once per arrival, its heading taking
// the focus), by the deadline badge beside the Optimise tab and by a move's link, and the old addresses of the
// calendar go there. The focus is read from the calls the page makes of the script; where it lands, and the
// keyboard's order, are the browser check's.
public class PlanWhenToPayTests
{
    private const string Section = "http://localhost/plan/optimisation?show=when";

    [Fact]
    public async Task Plan_oldByDateAddress_replacesItselfWithTheSectionAndDrawsNoMap()
    {
        await using var page = LivePlan.At("http://localhost/plan?by=date", new BrowserStorage());

        await page.LoadAsync();

        page.Navigations.ShouldBe(["plan/optimisation?show=when"]);
        ((await page.FindAllAsync<MapTab>()).Count, (await page.FindAllAsync<BucketBar>()).Count, page.Errors.Count).ShouldBe((0, 0, 0));
    }

    [Fact]
    public async Task Plan_myPlan_drawsTheMapAndNoCalendar()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());

        await page.LoadAsync();

        ((await page.FindAllAsync<MapTab>()).Count, (await page.FindAllAsync<WhenToPaySection>()).Count, page.Navigations.Count).ShouldBe((1, 0, 0));
    }

    [Fact]
    public async Task Plan_sectionAddress_drawsTheCalendarOnOptimise()
    {
        await using var page = LivePlan.At(Section, new BrowserStorage());

        await page.LoadAsync("optimisation");

        ((await page.FindAllAsync<WhenToPaySection>()).Count, (await page.FindAllAsync<MapTab>()).Count, page.Errors.Count).ShouldBe((1, 0, 0));
    }

    // Opened at the section, the title does not take the focus, the section's heading does, once: the moves,
    // found after the paint, never take it again, so a viewer who has gone into a field of the section is not
    // pulled back to its heading mid-edit.
    [Fact]
    public async Task Plan_openedAtTheSection_focusesItOnceAndNeverAgainWhenTheMovesAreFound()
    {
        await using var page = LivePlan.At(Section, new BrowserStorage(), holdPaint: true);

        await page.LoadAsync("optimisation");
        var onLoad = page.Focuses;
        await page.PaintAsync();

        (onLoad, page.Focuses - onLoad, page.Errors.Count).ShouldBe((1, 0, 0));
    }

    // Opened at Optimise alone, the title takes the focus and the section none, before or after the moves.
    [Fact]
    public async Task Plan_openedAtOptimiseAlone_focusesTheTitleOnly()
    {
        await using var page = LivePlan.At("http://localhost/plan/optimisation", new BrowserStorage(), holdPaint: true);

        await page.LoadAsync("optimisation");
        await page.PaintAsync();

        (page.Focuses, page.Errors.Count).ShouldBe((1, 0));
    }

    // The link followed from another view arrives at the section, which takes the focus, the title of the view
    // not taking it.
    [Fact]
    public async Task Plan_sectionLinkFromMyPlan_movesTheFocusToTheSectionOnce()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();
        var before = page.Focuses;

        await page.GoToAsync(Section, "optimisation");

        ((await page.FindAllAsync<WhenToPaySection>()).Count, page.Focuses - before, page.Errors.Count).ShouldBe((1, 1, 0));
    }

    // The same address given again, as Blazor may set the page's parameters again: the viewer has moved on.
    [Fact]
    public async Task Plan_sameSectionAddressAgain_movesNoFocus()
    {
        await using var page = LivePlan.At(Section, new BrowserStorage());
        await page.LoadAsync("optimisation");
        var before = page.Focuses;

        await page.GoToAsync(Section, "optimisation");

        (page.Focuses - before, page.Errors.Count).ShouldBe((0, 0));
    }

    // Left for another view and back by the same link, the section is a new arrival.
    [Fact]
    public async Task Plan_sectionLinkAfterAnotherView_arrivesAgain()
    {
        await using var page = LivePlan.At(Section, new BrowserStorage());
        await page.LoadAsync("optimisation");
        await page.GoToAsync("http://localhost/plan/pistes", "pistes");
        var before = page.Focuses;

        await page.GoToAsync(Section, "optimisation");

        (page.Focuses - before, page.Errors.Count).ShouldBe((1, 0));
    }

    // The badge pressed where the address names the section already: no navigation brings the focus, the
    // page does.
    [Fact]
    public async Task Plan_deadlineBadgePressedAtTheSection_movesTheFocusThere()
    {
        await using var page = LivePlan.At(Section, new BrowserStorage());
        await page.LoadAsync("optimisation");
        var bar = await page.FindAsync<PlanTabBar>();
        var before = page.Focuses;

        await page.InvokeAsync(() => bar.DeadlineChosen.InvokeAsync());

        (page.Focuses - before, page.Errors.Count).ShouldBe((1, 0));
    }

    // The river is the page's whichever view: what the fund bills, edited in the section, is the river the map
    // shows after.
    [Fact]
    public async Task Plan_billedAmountEditedInTheSection_isTheRiverTheMapShowsAfter()
    {
        await using var page = LivePlan.At(Section, new BrowserStorage());
        await page.LoadAsync("optimisation");
        var calendar = await page.FindAsync<WhenToPaySection>();

        await page.InvokeAsync(() => calendar.InputsChanged.InvokeAsync(calendar.Inputs.WithBilledContributions(1_000m)));
        await page.GoToAsync("http://localhost/plan", null);

        (await page.FindAsync<MapTab>()).Inputs.BilledContributions.Value.ShouldBe(1_000m);
    }

    // A bucket pressed in the band from Optimise brings the viewer to My plan, where the map draws its card;
    // on the map it goes nowhere.
    [Fact]
    public async Task Plan_bucketPressedOnOptimise_goesToMyPlan()
    {
        await using var page = LivePlan.At(Section, new BrowserStorage());
        await page.LoadAsync("optimisation");

        await page.PressAsync("data-bucket-choose", "expenses");

        page.Navigations.ShouldBe([PlanTabRoutes.Href(PlanTab.Plan)]);
    }

    [Fact]
    public async Task Plan_bucketPressedOnMyPlan_staysOnTheMap()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();

        await page.PressAsync("data-bucket-choose", "expenses");

        page.Navigations.ShouldBeEmpty();
    }
}
