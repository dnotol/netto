using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the Ideas view draws: one row per idea not in the plan, in the river's order, asking first what Netto
// cannot see, then its tuned gain, never one measured for another trial; a muted line for what Netto does not
// model; and the count My plan shows, the ideas with a gain and those still asking. What the search found
// stands in for the engine's (PisteSearchTests, PisteTrialTests hold its figures).
public class PistesViewTests
{
    private static readonly RiverInputs ExampleWithRoyalty =
        RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.25m).WithRoyaltyFollowingPay();

    private static readonly Refusal Refused = new("searched_refusal", RefusedInput.Revenue);

    // Every idea out of the plan has its row, in the river's order; the levers in it have none.
    [Fact]
    public void Rows_areTheIdeasNotInThePlan()
    {
        PistesView.Of(RiverInputs.Example, Trials(RiverInputs.Example), NothingFound, NotTried).Rows.Select(row => row.Lever)
            .ShouldBe([LeverKind.Vouchers, LeverKind.Rent, LeverKind.Copyright]);
        PistesView.Of(ExampleWithRoyalty, Trials(ExampleWithRoyalty), NothingFound, NotTried).Rows.Select(row => row.Lever)
            .ShouldBe([LeverKind.Vouchers, LeverKind.Rent]);
    }

    // A plan that starts at the pay has no ideas, as it has no moves.
    [Fact]
    public void Rows_aPlanStartingAtThePay_areNone()
    {
        var plan = RiverInputs.Example.SkipCompany();

        PistesView.Of(plan, Trials(plan), NothingFound, NotTried).Rows.ShouldBeEmpty();
    }

    // An idea whose gain still needs an answer asks; one that needs none waits for its search; none shows a gain.
    [Fact]
    public void Rows_onTheExampleBeforeAnySearch_askOrSearch()
    {
        var view = PistesView.Of(RiverInputs.Example, Trials(RiverInputs.Example), NothingFound, NotTried);

        view.Rows.Select(row => (row.Lever, row.State, row.Gain)).ShouldBe([
            (LeverKind.Vouchers, PisteState.Searching, (decimal?)null),
            (LeverKind.Rent, PisteState.Asking, null),
            (LeverKind.Copyright, PisteState.Asking, null),
        ]);
    }

    // What the search found for a trial is its gain.
    [Fact]
    public void Rows_found_arePricedWithTheirGain()
    {
        var view = PistesView.Of(ExampleWithRoyalty, Trials(ExampleWithRoyalty), trial => (Gaining(trial.Lever, 63.00m), null), NotTried);

        var vouchers = view.Rows[0];
        (vouchers.State, vouchers.Gain, vouchers.Piste.ShouldNotBeNull().Gain).ShouldBe((PisteState.Priced, 63.00m, 63.00m));
    }

    // An idea that adds nothing says so, and shows no gain.
    [Fact]
    public void Rows_foundAddingNothing_haveNoGain()
    {
        var view = PistesView.Of(ExampleWithRoyalty, Trials(ExampleWithRoyalty), trial => (Gaining(trial.Lever, 0m), null), NotTried);

        (view.Rows[0].State, view.Rows[0].Gain).ShouldBe((PisteState.NoGain, (decimal?)null));
    }

    // A trial the engine refuses carries the refusal, for its question to say.
    [Fact]
    public void Rows_refused_carryTheRefusal()
    {
        var view = PistesView.Of(ExampleWithRoyalty, Trials(ExampleWithRoyalty), _ => (null, Refused), NotTried);

        (view.Rows[0].State, view.Rows[0].Error, view.Rows[0].Gain).ShouldBe((PisteState.Refused, Refused, (decimal?)null));
    }

    // Answers typed but not tried yet wait for « Try »: they are searched only then.
    [Fact]
    public void Rows_answeredNotTried_areReady()
    {
        var trials = Trials(RiverInputs.Example).Select(trial => trial.Lever == LeverKind.Rent ? trial.WithCadastralIncome(1_500m).WithShareLet(0.15m) : trial).ToList();

        var view = PistesView.Of(RiverInputs.Example, trials, NothingFound, NotTried);

        view.Rows[1].State.ShouldBe(PisteState.Ready);
    }

    // Tried, they are being searched.
    [Fact]
    public void Rows_answeredAndTried_areSearching()
    {
        var trials = Trials(RiverInputs.Example).Select(trial => trial.Lever == LeverKind.Rent ? trial.WithCadastralIncome(1_500m).WithShareLet(0.15m) : trial).ToList();

        var view = PistesView.Of(RiverInputs.Example, trials, NothingFound, trial => trial.Lever == LeverKind.Rent);

        (view.Rows[1].State, view.Rows[1].Tried).ShouldBe((PisteState.Searching, true));
    }

    // The row carries its trial: its questions, what is still open and whether it can be tried.
    [Fact]
    public void Rows_carryTheirTrial()
    {
        var view = PistesView.Of(RiverInputs.Example, Trials(RiverInputs.Example), NothingFound, NotTried);

        var rent = view.Rows[1];
        (rent.Trial.Lever, rent.Module, rent.Trial.Open.Count, rent.Trial.CanTry).ShouldBe((LeverKind.Rent, OptionalModule.Rent, 2, false));
    }

    // My plan's count on the position paper's plan: the vouchers priced, the rent asking: « 1 · 1 ? ».
    [Fact]
    public void Count_onThePositionPapersPlan_isOnePricedAndOneAsking()
    {
        var view = PistesView.Of(ExampleWithRoyalty, Trials(ExampleWithRoyalty), trial => (Gaining(trial.Lever, 63.00m), null), NotTried);

        (view.Priced, view.Questions).ShouldBe((1, 1));
    }

    [Fact]
    public void Count_beforeAnySearch_pricesNone()
    {
        var view = PistesView.Of(RiverInputs.Example, Trials(RiverInputs.Example), NothingFound, NotTried);

        (view.Priced, view.Questions, view.Searching).ShouldBe((0, 2, true));
    }

    // What Netto does not model is one muted line, with no promise and no search.
    [Fact]
    public void NotYet_isThePension()
    {
        PistesView.Of(RiverInputs.Example, Trials(RiverInputs.Example), NothingFound, NotTried).NotYet.ShouldBe([NotInNetto.Pension]);
    }

    private static List<PisteTrial> Trials(RiverInputs plan) =>
        [.. PisteSearch.Levers.Where(lever => !plan.LeversInPlan.Contains(lever)).Select(lever => PisteTrial.Of(plan, lever))];

    private static (Piste? Value, Refusal? Error)? NothingFound(PisteTrial trial) => null;

    private static bool NotTried(PisteTrial trial) => false;

    // An idea whose plan leaves gain more than the plan without it.
    private static Piste Gaining(LeverKind lever, decimal gain) =>
        new(lever, new MovePlan(60_000m, [], 65_000m), new MovePlan(60_000m, [], 65_000m + gain), []);
}
