using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Where the Ideas view takes its ideas from: the plan on screen and the Optimiser's own plan for it, the
// « without » side every idea shares. Once the Optimiser has searched, one idea per paint, never while the
// page draws, once per plan, date and answers; an idea whose answers are typed only once « Try » is pressed.
// A search that notes what it is asked stands in for the engine's (PisteTrialTests holds its figures), and a
// wait the test may hold stands in for the browser's paint.
public class PistesSourceTests
{
    private static readonly DateOnly On11November = new(2026, 11, 11);

    private static readonly DateOnly On12November = new(2026, 11, 12);

    // The example with the home's facts kept while the rent is out: the vouchers and the rent can be measured
    // at once, the royalty asks for its creative share.
    private static readonly RiverInputs WithHome = RiverInputs.Example.WithCadastralIncome(1_500m).WithShareLet(0.15m);

    private static readonly MovePlan OptimisersPlan = new(60_000m, [], 65_000m);

    private static readonly Refusal PlanRefused = new("plan_refused", RefusedInput.Revenue);

    // Until the Optimiser has searched the plan on screen, every idea that needs no answer waits, and none is
    // searched: there is nothing to measure it against.
    [Fact]
    public async Task SearchNext_beforeTheOptimiserFound_searchesNothing()
    {
        var search = new Search();
        var paint = new Wait();
        var source = new PistesSource(() => WithHome, _ => null, search.Run);

        var searched = await source.SearchNextOncePaintedAsync(On11November, paint.After);

        (searched, paint.Asked, search.Asked.Count, source.View(On11November).Rows[0].State).ShouldBe((false, 0, 0, PisteState.Searching));
    }

    // The browser paints what the page drew before an idea is searched.
    [Fact]
    public void SearchNext_paintHeld_searchesNothingYet()
    {
        var search = new Search();
        var paint = new Wait(held: true);
        var source = Source(WithHome, search);

        var searching = source.SearchNextOncePaintedAsync(On11November, paint.After);

        (searching.IsCompleted, paint.Asked, search.Asked.Count).ShouldBe((false, 1, 0));
    }

    // A search already waiting for the paint is not asked twice.
    [Fact]
    public async Task SearchNext_alreadyWaiting_searchesNothingMore()
    {
        var search = new Search();
        var paint = new Wait(held: true);
        var source = Source(WithHome, search);
        var first = source.SearchNextOncePaintedAsync(On11November, paint.After);

        var second = await source.SearchNextOncePaintedAsync(On11November, paint.After);
        paint.Release();
        await first;

        (second, search.Asked.Count).ShouldBe((false, 1));
    }

    // One idea per paint, in the river's order; an idea still asking is never searched.
    [Fact]
    public async Task SearchNext_eachPaint_searchesOneIdea()
    {
        var search = new Search();
        var paint = new Wait();
        var source = Source(WithHome, search);

        var searched = new[]
        {
            await source.SearchNextOncePaintedAsync(On11November, paint.After),
            await source.SearchNextOncePaintedAsync(On11November, paint.After),
            await source.SearchNextOncePaintedAsync(On11November, paint.After),
        };

        searched.ShouldBe([true, true, false]);
        search.Asked.Select(asked => asked.Trial.Lever).ShouldBe([LeverKind.Vouchers, LeverKind.Rent]);
        source.View(On11November).Rows.Select(row => row.State).ShouldBe([PisteState.Priced, PisteState.Priced, PisteState.Asking]);
    }

    // Every idea is measured against the Optimiser's own plan, searched once for them all.
    [Fact]
    public async Task SearchNext_measuresAgainstTheOptimisersPlan()
    {
        var search = new Search();
        var source = Source(WithHome, search);

        await source.SearchNextOncePaintedAsync(On11November, new Wait().After);

        search.Asked.ShouldHaveSingleItem().ShouldBe((PisteTrial.Of(WithHome, LeverKind.Vouchers), OptimisersPlan, On11November));
    }

    // What was found for a plan and date is not searched again; another date or plan is.
    [Fact]
    public async Task SearchNext_anotherDateOrPlan_searchesAgain()
    {
        var search = new Search();
        var screen = new Screen(RiverInputs.Example);
        var source = new PistesSource(() => screen.Plan, _ => (OptimisersPlan, null), search.Run);
        var paint = new Wait();

        var first = await source.SearchNextOncePaintedAsync(On11November, paint.After);
        var again = await source.SearchNextOncePaintedAsync(On11November, paint.After);
        var nextDay = await source.SearchNextOncePaintedAsync(On12November, paint.After);
        screen.Plan = RiverInputs.Example.WithDirectorPay(30_000m);
        var edited = await source.SearchNextOncePaintedAsync(On12November, paint.After);

        (first, again, nextDay, edited).ShouldBe((true, false, true, true));
        search.Asked.Select(asked => (asked.Trial.Plan, asked.Date)).ShouldBe([
            (RiverInputs.Example, On11November), (RiverInputs.Example, On12November), (screen.Plan, On12November),
        ]);
    }

    // The Optimiser refusing the plan, every idea that needs no answer carries its refusal.
    [Fact]
    public void View_planRefused_carriesTheRefusal()
    {
        var source = new PistesSource(() => WithHome, _ => (null, PlanRefused), new Search().Run);

        source.View(On11November).Rows.Select(row => (row.State, row.Error)).ShouldBe([
            (PisteState.Refused, PlanRefused), (PisteState.Refused, PlanRefused), (PisteState.Asking, null),
        ]);
    }

    // The trial of an idea starts from the plan on screen; answers replace it, and are searched only on « Try ».
    [Fact]
    public async Task Answer_searchesNothingUntilTried()
    {
        var search = new Search();
        var source = Source(RiverInputs.Example, search);
        var paint = new Wait();
        await source.SearchNextOncePaintedAsync(On11November, paint.After);

        source.Answer(source.Trial(LeverKind.Rent).WithCadastralIncome(1_500m).WithShareLet(0.15m));
        var beforeTry = await source.SearchNextOncePaintedAsync(On11November, paint.After);
        var tried = source.Try(LeverKind.Rent);
        var afterTry = await source.SearchNextOncePaintedAsync(On11November, paint.After);

        (beforeTry, tried, afterTry).ShouldBe((false, true, true));
        var rent = source.View(On11November).Rows[1];
        (rent.State, rent.Tried, rent.Trial.River.Rent.CadastralIncome.Value).ShouldBe((PisteState.Priced, true, 1_500m));
    }

    // While its answers are typed, an idea shows no gain measured for other answers.
    [Fact]
    public async Task Answer_afterASearch_showsNoGainUntilTriedAgain()
    {
        var source = Source(WithHome, new Search());
        var paint = new Wait();
        await source.SearchNextOncePaintedAsync(On11November, paint.After);
        await source.SearchNextOncePaintedAsync(On11November, paint.After);

        source.Answer(source.Trial(LeverKind.Rent).WithShareLet(0.2m));

        (source.View(On11November).Rows[1].State, source.View(On11November).Rows[1].Gain).ShouldBe((PisteState.Ready, (decimal?)null));
    }

    // The vouchers are tried only after the yes; the rent only once answered.
    [Fact]
    public void Try_beforeItCanBe_isRefused()
    {
        var source = Source(RiverInputs.Example, new Search());

        (source.Try(LeverKind.Vouchers), source.Try(LeverKind.Rent)).ShouldBe((false, false));
        source.Answer(source.Trial(LeverKind.Vouchers).WithGranted(true));
        source.Try(LeverKind.Vouchers).ShouldBeTrue();
    }

    // Answers belong to the plan they were given on: once the plan on screen changes, the trial starts again
    // from it.
    [Fact]
    public void Trial_afterThePlanChanges_startsFromTheNewPlan()
    {
        var screen = new Screen(RiverInputs.Example);
        var source = new PistesSource(() => screen.Plan, _ => (OptimisersPlan, null), new Search().Run);
        source.Answer(source.Trial(LeverKind.Rent).WithCadastralIncome(1_500m));

        screen.Plan = RiverInputs.Example.WithDirectorPay(30_000m);

        source.Trial(LeverKind.Rent).ShouldBe(PisteTrial.Of(screen.Plan, LeverKind.Rent));
    }

    [Fact]
    public void Answer_aTrialOfAnotherPlan_throws()
    {
        var source = Source(RiverInputs.Example, new Search());

        Should.Throw<ArgumentException>(() => source.Answer(PisteTrial.Of(WithHome, LeverKind.Rent)));
    }

    // « Drop » discards the answers: the trial is the plan's again.
    [Fact]
    public void Drop_discardsTheAnswers()
    {
        var source = Source(RiverInputs.Example, new Search());
        source.Answer(source.Trial(LeverKind.Rent).WithCadastralIncome(1_500m).WithShareLet(0.15m));
        source.Try(LeverKind.Rent);

        source.Drop(LeverKind.Rent);

        (source.Trial(LeverKind.Rent), source.View(On11November).Rows[1].Tried).ShouldBe((PisteTrial.Of(RiverInputs.Example, LeverKind.Rent), false));
    }

    // « Keep » gives the river with the lever once the idea is measured and can be tried, and the trial ends.
    [Fact]
    public async Task Keep_measured_givesTheRiverWithTheLever()
    {
        var plan = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.25m).WithRoyaltyFollowingPay();
        var source = new PistesSource(() => plan, date => plan.FindMoves(date));
        await source.SearchNextOncePaintedAsync(On11November, new Wait().After);

        var withoutYes = source.Keep(LeverKind.Vouchers, On11November);
        source.Answer(source.Trial(LeverKind.Vouchers).WithGranted(true));
        var kept = source.Keep(LeverKind.Vouchers, On11November);

        withoutYes.ShouldBeNull();
        (kept.ShouldNotBeNull().InPlan(OptionalModule.Vouchers), kept.Vouchers.EcoVouchers.Value, kept.Vouchers.SportCultureVouchers.Value).ShouldBe((true, 250m, 100m));
        source.Trial(LeverKind.Vouchers).Granted.ShouldBeFalse();
    }

    // Nothing is kept before it is measured.
    [Fact]
    public void Keep_notMeasured_isNone()
    {
        var source = Source(RiverInputs.Example, new Search());
        source.Answer(source.Trial(LeverKind.Vouchers).WithGranted(true));

        source.Keep(LeverKind.Vouchers, On11November).ShouldBeNull();
    }

    private static PistesSource Source(RiverInputs plan, Search search) => new(() => plan, _ => (OptimisersPlan, null), search.Run);

    private sealed class Screen(RiverInputs plan)
    {
        public RiverInputs Plan { get; set; } = plan;
    }

    // The engine's search stood in for: each trial, plan it is measured against and date, in order, and an
    // idea gaining 10.00 as what it found.
    private sealed class Search
    {
        public List<(PisteTrial Trial, MovePlan Without, DateOnly Date)> Asked { get; } = [];

        public (Piste? Value, Refusal? Error)? Run(PisteTrial trial, MovePlan without, DateOnly date)
        {
            Asked.Add((trial, without, date));
            return (new Piste(trial.Lever, without, without with { PlanInPocket = without.PlanInPocket + 10m }, []), null);
        }
    }

    // The browser's next paint stood in for: how many times the page waited for it, and, held, done only once
    // the test releases it.
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
