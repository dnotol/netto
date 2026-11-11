using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// My plan read through its lenses, on the plan page as the browser runs it (LivePlan): by bucket the map, by
// date the calendar, the lens a query on the page's address. Switching lens keeps the page, its river and the
// focus where the viewer is, since My plan's head is drawn once above the lens; switching to another view
// moves the focus to that view's title. The focus is read from the calls the page makes of the script; where
// it lands, and the keyboard's order, are the browser check's.
public class PlanLensTests
{
    [Fact]
    public async Task Plan_byDateAddress_drawsTheCalendarAndNoMap()
    {
        await using var page = LivePlan.At("http://localhost/plan?by=date", new BrowserStorage());

        await page.LoadAsync();

        (await page.FindAllAsync<WhenToPayTab>()).Count.ShouldBe(1);
        (await page.FindAllAsync<MapTab>()).Count.ShouldBe(0);
        (await page.FindAsync<PlanLensBar>()).Current.ShouldBe(PlanLens.ByDate);
        page.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Plan_planAddress_drawsTheMapByBucket()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());

        await page.LoadAsync();

        (await page.FindAllAsync<MapTab>()).Count.ShouldBe(1);
        (await page.FindAllAsync<WhenToPayTab>()).Count.ShouldBe(0);
        (await page.FindAsync<PlanLensBar>()).Current.ShouldBe(PlanLens.ByBucket);
    }

    // The lens link followed: the page stays, the content swaps, and the focus stays where it was, on the
    // link the viewer pressed, the head not being drawn again.
    [Fact]
    public async Task Plan_lensSwitched_swapsTheContentAndMovesNoFocus()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();
        var before = page.Focuses;

        await page.GoToAsync("http://localhost/plan?by=date", null);

        (await page.FindAllAsync<WhenToPayTab>()).Count.ShouldBe(1);
        (await page.FindAllAsync<MapTab>()).Count.ShouldBe(0);
        (page.Focuses - before, page.Errors.Count).ShouldBe((0, 0));
    }

    [Fact]
    public async Task Plan_lensSwitchedBack_drawsTheMapAgainAndMovesNoFocus()
    {
        await using var page = LivePlan.At("http://localhost/plan?by=date", new BrowserStorage());
        await page.LoadAsync();
        var before = page.Focuses;

        await page.GoToAsync("http://localhost/plan", null);

        (await page.FindAllAsync<MapTab>()).Count.ShouldBe(1);
        (page.Focuses - before, page.Errors.Count).ShouldBe((0, 0));
    }

    // Another view is another head: its title takes the focus, as on any arrival.
    [Fact]
    public async Task Plan_anotherViewFromTheLens_movesTheFocusToItsTitleOnce()
    {
        await using var page = LivePlan.At("http://localhost/plan?by=date", new BrowserStorage());
        await page.LoadAsync();
        var before = page.Focuses;

        await page.GoToAsync("http://localhost/plan/pistes", "pistes");

        (page.Focuses - before, (await page.FindAllAsync<PistesTab>()).Count, page.Errors.Count).ShouldBe((1, 1, 0));
    }

    // The river is the page's whichever lens: an edit made by date, what the fund bills, is the river the
    // map shows after.
    [Fact]
    public async Task Plan_billedAmountEditedByDate_isTheRiverTheMapShowsAfter()
    {
        await using var page = LivePlan.At("http://localhost/plan?by=date", new BrowserStorage());
        await page.LoadAsync();
        var calendar = await page.FindAsync<WhenToPayTab>();

        await page.InvokeAsync(() => calendar.InputsChanged.InvokeAsync(calendar.Inputs.WithBilledContributions(1_000m)));
        await page.GoToAsync("http://localhost/plan", null);

        (await page.FindAsync<MapTab>()).Inputs.BilledContributions.Value.ShouldBe(1_000m);
    }

    // A bucket pressed in the band while My plan is read by date brings the viewer to the plan by bucket,
    // where the map draws its card; on the map it goes nowhere.
    [Fact]
    public async Task Plan_bucketPressedByDate_goesToThePlanByBucket()
    {
        await using var page = LivePlan.At("http://localhost/plan?by=date", new BrowserStorage());
        await page.LoadAsync();

        await page.PressAsync("data-bucket-choose", "expenses");

        page.Navigations.ShouldBe([PlanTabRoutes.Href(PlanTab.Plan)]);
    }

    [Fact]
    public async Task Plan_bucketPressedByBucket_staysOnTheMap()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();

        await page.PressAsync("data-bucket-choose", "expenses");

        page.Navigations.ShouldBeEmpty();
    }

    // A card a link names is for the map: arriving by date, it is not chosen.
    [Fact]
    public async Task Plan_cardNamedByDate_isNotChosenAndStaysForTheMap()
    {
        await using var page = LivePlan.At("http://localhost/plan?by=date&card=split", new BrowserStorage());
        await page.LoadAsync();

        await page.GoToAsync("http://localhost/plan?card=split", null);

        (await page.FindAsync<MapTab>()).Chosen.ShouldBe(MapChoice.Of(RiverModule.Split));
    }

    // Once the moves are found, the line of nudges says what Optimise can add, as a link; nothing supplies ideas.
    [Fact]
    public async Task Plan_movesFound_nudgeToOptimiseAndNothingMore()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), holdPaint: true);
        await page.LoadAsync();
        var gainBefore = (await page.FindAsync<PlanNudges>()).Gain;

        await page.PaintAsync();

        var nudges = await page.FindAsync<PlanNudges>();
        (gainBefore, nudges.Gain > 0m, nudges.Pistes).ShouldBe((null, true, null));
    }
}
