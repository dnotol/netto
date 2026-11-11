using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// Where an idea stands. Public because the Ideas tab, a component, takes it.
public enum PisteState
{
    // Its gain still needs an answer (PisteTrial.Open).
    Asking,

    // Answers typed and complete, searched once « Try » is pressed.
    Ready,

    // Waiting for its search, which runs once the browser has painted, one idea per paint (PistesSource).
    Searching,

    // Measured, its plan leaving more than the plan without it (Gain).
    Priced,

    // Measured, its plan leaving no more than the plan without it.
    NoGain,

    // The engine refuses the plan with the lever, or the plan itself (Error).
    Refused,
}

// What Netto does not model, which the Ideas view names on a muted line, with no promise and no search.
public enum NotInNetto
{
    // Pension accrual: PLCI, EIP, pension savings.
    Pension,
}

// One idea as the view draws it: its trial (questions, answers, whether it can be tried), where it stands,
// what the search found for that very trial, and whether « Try » was pressed on it. Public because the
// Ideas tab, a component, takes it.
public sealed record PisteRow(PisteTrial Trial, PisteState State, Piste? Piste, Refusal? Error, bool Tried)
{
    public LeverKind Lever => Trial.Lever;

    public OptionalModule Module => PisteTrial.ModuleOf(Trial.Lever);

    // The tuned gain, only once measured with one: never an untuned figure, nor one for another trial.
    public decimal? Gain => State == PisteState.Priced ? Piste?.Gain : null;
}

// What the Ideas view draws: one row per idea not in the plan, in the river's order (PisteSearch.Levers),
// none for a plan that starts at the pay, which has no moves either; what Netto does not model; and the
// count My plan shows, the ideas with a gain (Priced) and those still asking (Questions). It selects and
// orders what the search found; every figure is the engine's. Public because the Ideas tab, a component,
// takes it.
public sealed record PistesView(IReadOnlyList<PisteRow> Rows, IReadOnlyList<NotInNetto> NotYet)
{
    public int Priced => Rows.Count(row => row.State == PisteState.Priced);

    public int Questions => Rows.Count(row => row.State == PisteState.Asking);

    // Whether an idea is still to be searched, for the count to say so meanwhile.
    public bool Searching => Rows.Any(row => row.State == PisteState.Searching);

    // From the plan on screen, the trial of each idea not in it (trials, for the levers out of the plan, in any
    // order), what the search found for a trial (found: none until it has searched that very trial), and
    // whether « Try » was pressed on one (tried).
    internal static PistesView Of(
        RiverInputs plan, IEnumerable<PisteTrial> trials, Func<PisteTrial, (Piste? Value, Refusal? Error)?> found, Func<PisteTrial, bool> tried)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Company is null)
            return new PistesView([], [NotInNetto.Pension]);

        var byLever = trials.ToDictionary(trial => trial.Lever);
        var rows = PisteSearch.Levers.Where(lever => !plan.LeversInPlan.Contains(lever))
            .Select(lever => byLever.TryGetValue(lever, out var trial) ? trial : PisteTrial.Of(plan, lever))
            .Select(trial => Row(trial, found(trial), tried(trial)));

        return new PistesView([.. rows], [NotInNetto.Pension]);
    }

    private static PisteRow Row(PisteTrial trial, (Piste? Value, Refusal? Error)? found, bool tried)
    {
        if (trial.Open.Count > 0)
            return new PisteRow(trial, PisteState.Asking, null, null, tried);
        if (found is not { } result)
            return new PisteRow(trial, trial.Answered && !tried ? PisteState.Ready : PisteState.Searching, null, null, tried);
        if (result.Value is not { } piste)
            return new PisteRow(trial, PisteState.Refused, null, result.Error, tried);

        return new PisteRow(trial, piste.Gain > 0m ? PisteState.Priced : PisteState.NoGain, piste, null, tried);
    }
}
