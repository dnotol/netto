using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Where the Optimisation tab's moves and its badge come from: the plan on screen, searched once per plan and
// date, never while the page draws but once the browser has painted what it drew; at once on the first
// search and while Optimisation is shown, else once the edits have paused. A search that notes what it is
// asked stands in for the engine's, whose moves MovesViewTests holds, and waits the test may hold for the
// browser's paint and for the pause.
public class MovesSourceTests
{
    private static readonly DateOnly On23October = new(2026, 10, 23);

    private static readonly DateOnly On24October = new(2026, 10, 24);

    // The plan on screen, and the plans the viewer edits it to.
    private static readonly RiverInputs Edited = RiverInputs.Example.WithDirectorPay(30_000m);

    private static readonly RiverInputs Opened = RiverInputs.Example.WithDirectorPay(40_000m);

    private static readonly RiverInputs Other = RiverInputs.Example.WithDirectorPay(10_000m);

    // The moves are measured from the plan on screen, whatever it is.
    [Fact]
    public void Plan_isThePlanOnScreen()
    {
        var screen = new Screen(Edited);
        var source = Source(screen, new Wait(), new Search());

        var before = source.Plan;
        screen.Plan = Opened;

        (before, source.Plan).ShouldBe((Edited, Opened));
    }

    // The page draws before it searches: until then the moves are being looked for, and drawing them
    // searches nothing.
    [Fact]
    public void View_beforeTheSearch_isSearchingAndSearchesNothing()
    {
        var search = new Search();
        var source = Source(new Screen(Edited), new Wait(), search);

        var view = source.View(On23October);

        (view.State, view.From, source.Searching(On23October), source.Found(On23October), search.Asked.Count)
            .ShouldBe((MovesState.Searching, Edited, true, null, 0));
    }

    // A plan that starts at the pay has no moves to look for: the split, and every move, starts from it.
    [Fact]
    public async Task View_aPlanStartingAtThePay_startsThereWithoutASearch()
    {
        var search = new Search();
        var paint = new Wait();
        var source = Source(new Screen(Edited.SkipCompany()), new Wait(), search);

        var searched = await source.SearchOncePaintedAsync(On23October, atOnce: false, paint.After);

        (source.View(On23October).State, source.Searching(On23October), searched, paint.Asked, search.Asked.Count)
            .ShouldBe((MovesState.StartsAtPay, false, false, 0, 0));
    }

    // The browser paints what the page drew before the search, which takes the page's one thread a while.
    [Fact]
    public void SearchOncePainted_paintHeld_searchesNothingYet()
    {
        var search = new Search();
        var paint = new Wait(held: true);
        var source = Source(new Screen(Edited), new Wait(), search);

        var searching = source.SearchOncePaintedAsync(On23October, atOnce: false, paint.After);

        (searching.IsCompleted, paint.Asked, search.Asked.Count, source.View(On23October).State).ShouldBe((false, 1, 0, MovesState.Searching));
    }

    // The first search, on opening the page, waits for the paint alone.
    [Fact]
    public async Task SearchOncePainted_painted_searchesThePlanOnScreenAtOnceAndShowsWhatItFound()
    {
        var search = new Search();
        var paint = new Wait(held: true);
        var quiet = new Wait();
        var source = Source(new Screen(Edited), quiet, search);
        var searching = source.SearchOncePaintedAsync(On23October, atOnce: false, paint.After);

        paint.Release();
        var searched = await searching;

        var view = source.View(On23October);
        (searched, search.Asked.Single(), quiet.Asked, view.State, view.Error).ShouldBe((true, (Edited, On23October), 0, MovesState.Refused, Search.Refusal));
    }

    // Every render after the first asks again while the paint is awaited: one wait, one search.
    [Fact]
    public async Task SearchOncePainted_askedAgainBeforeThePaint_waitsOnceAndSearchesOnce()
    {
        var search = new Search();
        var paint = new Wait(held: true);
        var source = Source(new Screen(Edited), new Wait(), search);
        var first = source.SearchOncePaintedAsync(On23October, atOnce: false, paint.After);

        var again = await source.SearchOncePaintedAsync(On23October, atOnce: false, paint.After);
        paint.Release();

        (await first, again, paint.Asked, search.Asked.Count).ShouldBe((true, false, 1, 1));
    }

    // A wait for the paint that fails is the page's error to show, and leaves no wait behind: the next render
    // waits again and searches.
    [Fact]
    public async Task SearchOncePainted_paintFails_releasesTheWaitForTheNextRender()
    {
        var search = new Search();
        var source = Source(new Screen(Edited), new Wait(), search);

        await Should.ThrowAsync<InvalidOperationException>(() => source.SearchOncePaintedAsync(On23October, atOnce: false, () => throw new InvalidOperationException("No paint.")));
        var searched = await source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);

        (searched, search.Asked.Count).ShouldBe((true, 1));
    }

    // Once found, the moves are drawn as found, without waiting for another paint.
    [Fact]
    public async Task SearchOncePainted_theSamePlanAndDate_searchesOnce()
    {
        var search = new Search();
        var paint = new Wait();
        var source = Source(new Screen(Edited), new Wait(), search);

        await source.SearchOncePaintedAsync(On23October, atOnce: false, paint.After);
        var again = await source.SearchOncePaintedAsync(On23October, atOnce: false, paint.After);

        (again, paint.Asked, search.Asked.Count).ShouldBe((false, 1, 1));
    }

    // The date decides which deadlines are past.
    [Fact]
    public async Task SearchOncePainted_aNewDate_searchesAgain()
    {
        var search = new Search();
        var source = Source(new Screen(Edited), new Wait(), search);

        await source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);
        await source.SearchOncePaintedAsync(On24October, atOnce: false, Painted);

        search.Asked.ShouldBe([(Edited, On23October), (Edited, On24October)]);
    }

    // What was found for a plan is never shown for another: once the plan on screen changes, the moves are
    // being looked for again, until the page searches it.
    [Fact]
    public async Task View_anEditAfterTheSearch_isSearchingAgain()
    {
        var screen = new Screen(Edited);
        var source = Source(screen, new Wait(), new Search());
        await source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);

        screen.Plan = Opened;

        var view = source.View(On23October);
        (view.State, view.From, source.Found(On23October)).ShouldBe((MovesState.Searching, Opened, null));
    }

    // Off Optimisation, an edit is searched once the edits have paused, the plan searched the one on screen then.
    [Fact]
    public async Task SearchOncePainted_anEditOffOptimisation_waitsForThePauseThenSearchesThePlanOnScreen()
    {
        var search = new Search();
        var quiet = new Wait(held: true);
        var screen = new Screen(Edited);
        var source = Source(screen, quiet, search);
        await source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);
        screen.Plan = Opened;

        var searching = source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);
        var whileWaiting = (searching.IsCompleted, search.Asked.Count);
        quiet.Release();
        var searched = await searching;

        (whileWaiting, searched, search.Asked.Last(), source.View(On23October).State).ShouldBe(((false, 1), true, (Opened, On23October), MovesState.Refused));
    }

    // An edit during the pause waits again, and the plan searched is the last one, once.
    [Fact]
    public async Task SearchOncePainted_anEditDuringThePause_waitsAgainAndSearchesTheLastPlan()
    {
        var search = new Search();
        var quiet = new Wait(held: true);
        var screen = new Screen(Edited);
        var source = Source(screen, quiet, search);
        await source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);
        screen.Plan = Opened;
        var searching = source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);

        screen.Plan = Other;
        var again = await source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);
        quiet.Release();
        await searching;

        (again, quiet.Asked).ShouldBe((false, 2));
        search.Asked.ShouldBe([(Edited, On23October), (Other, On23October)]);
    }

    // On Optimisation, where a move is applied, the plan it leaves is searched as soon as it is painted.
    [Fact]
    public async Task SearchOncePainted_anEditOnOptimisation_searchesAtOnce()
    {
        var search = new Search();
        var quiet = new Wait(held: true);
        var screen = new Screen(Edited);
        var source = Source(screen, quiet, search);
        await source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);
        screen.Plan = Opened;

        var searched = await source.SearchOncePaintedAsync(On23October, atOnce: true, Painted);

        (searched, quiet.Asked, search.Asked.Last()).ShouldBe((true, 0, (Opened, On23October)));
    }

    // Edited back to the plan searched, the moves found for it are drawn again without another search.
    [Fact]
    public async Task SearchOncePainted_anEditUndone_searchesNoMore()
    {
        var search = new Search();
        var screen = new Screen(Edited);
        var source = Source(screen, new Wait(), search);
        await source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);
        screen.Plan = Opened;
        screen.Plan = Edited;

        var again = await source.SearchOncePaintedAsync(On23October, atOnce: false, Painted);

        (again, search.Asked.Count, source.View(On23October).State).ShouldBe((false, 1, MovesState.Refused));
    }

    // A paint already made, for a test that is not about the wait.
    private static ValueTask Painted() => ValueTask.CompletedTask;

    private static MovesSource Source(Screen screen, Wait quiet, Search search) => new(() => screen.Plan, quiet.After, search.Run);

    // The plan the page shows, as the viewer edits it.
    private sealed class Screen(RiverInputs plan)
    {
        public RiverInputs Plan { get; set; } = plan;
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

    // The browser's next paint, or the edits' pause, stood in for: how many times the page waited for it,
    // and, held, done only once the test releases it.
    private sealed class Wait(bool held = false)
    {
        private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Asked { get; private set; }

        public ValueTask After()
        {
            Asked++;
            return held ? new ValueTask(_done.Task) : ValueTask.CompletedTask;
        }

        public void Release() => _done.SetResult();
    }
}
