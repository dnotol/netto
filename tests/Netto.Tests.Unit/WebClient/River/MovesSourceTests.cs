using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Where the Optimisation tab's moves and its badge come from: the plan starred Today while a readable one
// is, or else the plan in the river as it was when the page opened, when a plan was opened from Plans or
// when the river was saved as one, named while it is that saved plan as saved; and the search, run once
// per plan and date, never at a change of the river nor while the page draws, but once the browser has
// painted what it drew. A search that notes what it is asked stands in for the engine's, whose moves
// MovesViewTests holds, and a paint the test may hold for the browser's.
public class MovesSourceTests
{
    private static readonly DateOnly On23October = new(2026, 10, 23);

    private static readonly DateOnly On24October = new(2026, 10, 24);

    // The river as a plan was opened from Plans, as the viewer has changed it since, and other plans.
    private static readonly RiverInputs Opened = RiverInputs.Example.WithDirectorPay(40_000m);

    private static readonly RiverInputs Edited = RiverInputs.Example.WithDirectorPay(30_000m);

    private static readonly RiverInputs Starred = RiverInputs.Example.WithDirectorPay(20_000m);

    private static readonly RiverInputs Other = RiverInputs.Example.WithDirectorPay(10_000m);

    [Fact]
    public void Baseline_pageOpened_isTheRiver()
    {
        var source = new MovesSource(PlanFixtures.Saved(), Edited);

        source.Baseline.ShouldBe(new MovesBaseline(Edited, IsToday: false));
    }

    [Fact]
    public void Baseline_riverIsTheOpenedPlan_namesIt()
    {
        var source = new MovesSource(WithoutToday(Opened), Opened);

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false, "Plan 2"));
    }

    // The river changed since its plan was opened, and kept for the browser tab, when the page opened: it
    // is no saved plan.
    [Fact]
    public void Baseline_riverEditedBeforeThePageOpened_isNotNamed()
    {
        var source = new MovesSource(WithoutToday(Opened), Edited);

        source.Baseline.ShouldBe(new MovesBaseline(Edited, IsToday: false));
    }

    [Fact]
    public void Baseline_todayStarred_isToday()
    {
        var source = new MovesSource(PlanFixtures.Saved(Starred), Edited);

        source.Baseline.ShouldBe(MovesBaseline.Today(Starred));
    }

    // A plan starred Today that this version cannot read is no Today.
    [Fact]
    public void Baseline_todayUnreadable_isThePlanInTheRiver()
    {
        var source = new MovesSource(Unreadable(PlanFixtures.Saved(Starred)), Edited);

        source.Baseline.ShouldBe(new MovesBaseline(Edited, IsToday: false));
    }

    // Today wins while starred, and the plan opened meanwhile is the baseline again once it is deleted.
    [Fact]
    public void Baseline_todayDeleted_isThePlanOpenedBefore()
    {
        var plans = PlanFixtures.Saved(Starred, Opened);
        var source = new MovesSource(plans, Opened);
        var today = source.Baseline;

        plans.Delete(plans.Plans[0].Id);
        source.Kept();

        (today, source.Baseline).ShouldBe((MovesBaseline.Today(Starred), new MovesBaseline(Opened, IsToday: false, "Plan 2")));
    }

    [Fact]
    public void Opened_aPlan_becomesTheBaseline()
    {
        var plans = WithoutToday(Opened, Other);
        var source = new MovesSource(plans, Other);

        source.Opened(plans.Open(plans.Plans[0].Id).ShouldNotBeNull());

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false, "Plan 2"));
    }

    // Opening the plan the river was opened from takes it as it was saved, the changes made since gone.
    [Fact]
    public void Opened_thePlanAlreadyOpened_becomesItsInputsAgain()
    {
        var plans = WithoutToday(Opened);
        var source = new MovesSource(plans, Edited);

        source.Opened(plans.Open(plans.Plans[0].Id).ShouldNotBeNull());

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false, "Plan 2"));
    }

    // The river, changed since the page opened, is saved as a new plan, which it is then opened from: once
    // Today is gone, the moves are from that plan, not from the river as the page opened.
    [Fact]
    public void Kept_aSave_takesTheRiverAsSaved()
    {
        var plans = PlanFixtures.Saved(Starred);
        var source = new MovesSource(plans, Edited);

        plans.Save(Opened, number => $"Plan {number}");
        source.Kept();
        plans.Delete(plans.Plans[0].Id);
        source.Kept();

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false, "Plan 2"));
    }

    // A star, a rename or an import opens no plan: the moves stay measured from the river as the page
    // opened, once a plan starred meanwhile is gone. Each change is told to the page (PlansPanel.OnChanged).
    [Fact]
    public void Kept_aPlanStarredThenDeleted_keepsTheBaseline()
    {
        var plans = WithoutToday(Other, Opened);
        var source = new MovesSource(plans, Edited);

        plans.Star(plans.Plans[0].Id);
        source.Kept();
        plans.Delete(plans.Plans[0].Id);
        source.Kept();

        source.Baseline.ShouldBe(new MovesBaseline(Edited, IsToday: false));
    }

    [Fact]
    public void Kept_aRename_keepsTheBaseline()
    {
        var plans = WithoutToday(Other, Opened);
        var source = new MovesSource(plans, Edited);

        plans.Rename(plans.Plans[1].Id, "Mine");
        source.Kept();

        source.Baseline.ShouldBe(new MovesBaseline(Edited, IsToday: false));
    }

    // A file without a Today, which stars none here.
    [Fact]
    public void Kept_anImport_keepsTheBaseline()
    {
        var plans = WithoutToday(Other, Opened);
        var source = new MovesSource(plans, Edited);

        plans.Import(WithoutToday(Starred).Export());
        source.Kept();

        source.Baseline.ShouldBe(new MovesBaseline(Edited, IsToday: false));
    }

    [Fact]
    public void Kept_theOpenedPlanDeleted_keepsItUnnamed()
    {
        var plans = WithoutToday(Opened);
        var source = new MovesSource(plans, Opened);

        plans.Delete(plans.Plans[0].Id);
        source.Kept();

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false));
    }

    [Fact]
    public void Kept_theOpenedPlanRenamed_takesItsNewName()
    {
        var plans = WithoutToday(Opened);
        var source = new MovesSource(plans, Opened);

        plans.Rename(plans.Plans[0].Id, "Mine");
        source.Kept();

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false, "Mine"));
    }

    // The page draws before it searches: until then the moves are being looked for, and drawing them
    // searches nothing.
    [Fact]
    public void View_beforeTheSearch_isSearchingAndSearchesNothing()
    {
        var search = new Search();
        var source = new MovesSource(PlanFixtures.Saved(), Edited, search.Run);

        var view = source.View(On23October);

        (view.State, view.From.Plan, source.Searching(On23October), source.Found(On23October), search.Asked.Count)
            .ShouldBe((MovesState.Searching, Edited, true, null, 0));
    }

    // A plan that starts at the pay has no moves to look for: the split, and every move, starts from it.
    [Fact]
    public async Task View_aPlanStartingAtThePay_startsThereWithoutASearch()
    {
        var search = new Search();
        var paint = new Paint();
        var source = new MovesSource(PlanFixtures.Saved(), Edited.SkipCompany(), search.Run);

        var searched = await source.SearchOncePaintedAsync(On23October, paint.After);

        (source.View(On23October).State, source.Searching(On23October), searched, paint.Asked, search.Asked.Count)
            .ShouldBe((MovesState.StartsAtPay, false, false, 0, 0));
    }

    // The browser paints what the page drew before the search, which takes the page's one thread a while.
    [Fact]
    public void SearchOncePainted_paintHeld_searchesNothingYet()
    {
        var search = new Search();
        var paint = new Paint(held: true);
        var source = new MovesSource(PlanFixtures.Saved(), Edited, search.Run);

        var searching = source.SearchOncePaintedAsync(On23October, paint.After);

        (searching.IsCompleted, paint.Asked, search.Asked.Count, source.View(On23October).State).ShouldBe((false, 1, 0, MovesState.Searching));
    }

    [Fact]
    public async Task SearchOncePainted_painted_searchesThePlanAndShowsWhatItFound()
    {
        var search = new Search();
        var paint = new Paint(held: true);
        var source = new MovesSource(PlanFixtures.Saved(), Edited, search.Run);
        var searching = source.SearchOncePaintedAsync(On23October, paint.After);

        paint.Release();
        var searched = await searching;

        var view = source.View(On23October);
        (searched, search.Asked.Single(), view.State, view.Error).ShouldBe((true, (Edited, On23October), MovesState.Refused, Search.Refusal));
    }

    // Every render after the first asks again while the paint is awaited: one wait, one search.
    [Fact]
    public async Task SearchOncePainted_askedAgainBeforeThePaint_waitsOnceAndSearchesOnce()
    {
        var search = new Search();
        var paint = new Paint(held: true);
        var source = new MovesSource(PlanFixtures.Saved(), Edited, search.Run);
        var first = source.SearchOncePaintedAsync(On23October, paint.After);

        var again = await source.SearchOncePaintedAsync(On23October, paint.After);
        paint.Release();

        (await first, again, paint.Asked, search.Asked.Count).ShouldBe((true, false, 1, 1));
    }

    // A wait for the paint that fails is the page's error to show, and leaves no wait behind: the next render
    // waits again and searches.
    [Fact]
    public async Task SearchOncePainted_paintFails_releasesTheWaitForTheNextRender()
    {
        var search = new Search();
        var source = new MovesSource(PlanFixtures.Saved(), Edited, search.Run);

        await Should.ThrowAsync<InvalidOperationException>(() => source.SearchOncePaintedAsync(On23October, () => throw new InvalidOperationException("No paint.")));
        var searched = await source.SearchOncePaintedAsync(On23October, Painted);

        (searched, search.Asked.Count).ShouldBe((true, 1));
    }

    // Once found, the moves are drawn as found, without waiting for another paint.
    [Fact]
    public async Task SearchOncePainted_theSamePlanAndDate_searchesOnce()
    {
        var search = new Search();
        var paint = new Paint();
        var source = new MovesSource(PlanFixtures.Saved(), Edited, search.Run);

        await source.SearchOncePaintedAsync(On23October, paint.After);
        var again = await source.SearchOncePaintedAsync(On23October, paint.After);

        (again, paint.Asked, search.Asked.Count).ShouldBe((false, 1, 1));
    }

    [Fact]
    public async Task SearchOncePainted_aPlanOpened_searchesAgainForIt()
    {
        var search = new Search();
        var plans = WithoutToday(Opened);
        var source = new MovesSource(plans, Edited, search.Run);

        await source.SearchOncePaintedAsync(On23October, Painted);
        source.Opened(plans.Open(plans.Plans[0].Id).ShouldNotBeNull());
        await source.SearchOncePaintedAsync(On23October, Painted);

        search.Asked.ShouldBe([(Edited, On23October), (Opened, On23October)]);
    }

    // Starring a plan Today, then another, searches from each.
    [Fact]
    public async Task SearchOncePainted_todayStarredOrMoved_searchesForIt()
    {
        var search = new Search();
        var plans = WithoutToday(Starred, Opened);
        var source = new MovesSource(plans, Edited, search.Run);

        await source.SearchOncePaintedAsync(On23October, Painted);
        plans.Star(plans.Plans[0].Id);
        source.Kept();
        await source.SearchOncePaintedAsync(On23October, Painted);
        plans.Star(plans.Plans[1].Id);
        source.Kept();
        await source.SearchOncePaintedAsync(On23October, Painted);

        search.Asked.ShouldBe([(Edited, On23October), (Starred, On23October), (Opened, On23October)]);
    }

    // The date decides which deadlines are past.
    [Fact]
    public async Task SearchOncePainted_aNewDate_searchesAgain()
    {
        var search = new Search();
        var source = new MovesSource(PlanFixtures.Saved(), Edited, search.Run);

        await source.SearchOncePaintedAsync(On23October, Painted);
        await source.SearchOncePaintedAsync(On24October, Painted);

        search.Asked.ShouldBe([(Edited, On23October), (Edited, On24October)]);
    }

    // The river saved as it was when the page opened is starred Today, the first plan saved: the same
    // plan, whose moves are already found.
    [Fact]
    public async Task SearchOncePainted_theRiverSavedUnchanged_searchesNoMore()
    {
        var search = new Search();
        var plans = PlanFixtures.Saved();
        var source = new MovesSource(plans, Edited, search.Run);

        await source.SearchOncePaintedAsync(On23October, Painted);
        plans.Save(Edited, number => $"Plan {number}");
        source.Kept();
        var again = await source.SearchOncePaintedAsync(On23October, Painted);

        (source.Baseline, again, search.Asked.Count, source.View(On23October).State).ShouldBe((MovesBaseline.Today(Edited), false, 1, MovesState.Refused));
    }

    // A plan opened while the page waits for the paint is the one searched: the search reads the plan the
    // moves are from as it stands when it runs.
    [Fact]
    public async Task SearchOncePainted_aPlanOpenedWhileWaiting_searchesThePlanOpened()
    {
        var search = new Search();
        var paint = new Paint(held: true);
        var plans = WithoutToday(Opened);
        var source = new MovesSource(plans, Edited, search.Run);
        var searching = source.SearchOncePaintedAsync(On23October, paint.After);

        source.Opened(plans.Open(plans.Plans[0].Id).ShouldNotBeNull());
        paint.Release();
        var searched = await searching;

        (searched, search.Asked.Single(), source.View(On23October).From.Plan, source.View(On23October).State)
            .ShouldBe((true, (Opened, On23October), Opened, MovesState.Refused));
    }

    // What was found for a plan is never shown for another: once the plan the moves are from changes, they
    // are being looked for again, until the page searches that plan.
    [Fact]
    public async Task View_aPlanOpenedAfterTheSearch_isSearchingItAgain()
    {
        var plans = WithoutToday(Opened);
        var source = new MovesSource(plans, Edited, new Search().Run);
        await source.SearchOncePaintedAsync(On23October, Painted);

        source.Opened(plans.Open(plans.Plans[0].Id).ShouldNotBeNull());

        var view = source.View(On23October);
        (view.State, view.From.Plan, source.Found(On23October)).ShouldBe((MovesState.Searching, Opened, null));
    }

    // A paint already made, for a test that is not about the wait.
    private static ValueTask Painted() => ValueTask.CompletedTask;

    // These plans saved in this order, named from Plan 2, the river opened from the last, and none starred
    // Today: the plan saved first, which Plans stars, is deleted.
    private static SavedPlans WithoutToday(params RiverInputs[] inputs)
    {
        var plans = PlanFixtures.Saved([RiverInputs.Example, .. inputs]);
        plans.Delete(plans.Plans[0].Id);
        return plans;
    }

    // The plans as a later version of Netto would find them: each stored with a field this version does not
    // know in place of one it needs, so none can be read. They are read again from the storage.
    private static SavedPlans Unreadable(SavedPlans plans)
    {
        var storage = new BrowserStorage();
        storage.Set(PlanStorage.Key, plans.Export().Replace("\"reserve\"", "\"keptAside\"", StringComparison.Ordinal));
        return new SavedPlans(storage);
    }

    // The engine's search stood in for: each plan and date it is asked for, in order, and a refusal of its
    // own as what it found, for a test to tell its view from the others.
    private sealed class Search
    {
        public static readonly Refusal Refusal = new("searched_refusal", RefusedInput.Revenue);

        public List<(RiverInputs Plan, DateOnly Date)> Asked { get; } = [];

        public (MovePlan? Value, Refusal? Error)? Run(RiverInputs plan, DateOnly date)
        {
            Asked.Add((plan, date));
            return (null, Refusal);
        }
    }

    // The browser's next paint stood in for: how many times the page waited for it, and, held, painted only
    // once the test releases it.
    private sealed class Paint(bool held = false)
    {
        private readonly TaskCompletionSource _painted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Asked { get; private set; }

        public ValueTask After()
        {
            Asked++;
            return held ? new ValueTask(_painted.Task) : ValueTask.CompletedTask;
        }

        public void Release() => _painted.SetResult();
    }
}
