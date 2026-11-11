using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// Where the Ideas view and My plan's count take the ideas from: the plan on screen, the trial of each idea
// not in it, and what the search found for each trial, measured against the Optimiser's own plan for the
// plan on screen (MovesSource.Found), the « without » side every idea shares. An idea's search costs about
// a search of the moves, a few tenths of a second in the browser on the page's one thread, so it never runs
// while the page draws: once the Optimiser has searched, the page asks after each render, and one idea is
// searched once the browser has painted (SearchNextOncePaintedAsync), the next one after the next paint. One
// search per plan, date and answers: what was found for another trial is never given for this one. An idea
// that needs no answer, or whose answers the plan already keeps, is searched by itself; one whose answers
// the viewer typed only once « Try » is pressed on it. Answers belong to the plan they were given on: once
// the plan on screen changes, the trial starts again from it. The page owns one, for as long as it is shown.
internal sealed class PistesSource
{
    private readonly Func<RiverInputs> _onScreen;
    private readonly Func<DateOnly, (MovePlan? Value, Refusal? Error)?> _without;
    private readonly Func<PisteTrial, MovePlan, DateOnly, (Piste? Value, Refusal? Error)?> _search;

    // The trial of each idea with the viewer's answers, and the trial « Try » was pressed on.
    private readonly Dictionary<LeverKind, PisteTrial> _trials = [];
    private readonly Dictionary<LeverKind, PisteTrial> _tried = [];

    // What the search found, for which trial, its answers, plan and lever, and on which date.
    private readonly Dictionary<(LeverKind Lever, RiverInputs Plan, RiverInputs River, DateOnly Date), (Piste? Value, Refusal? Error)?> _found = [];

    // Whether a search waits for the browser to paint.
    private bool _waiting;

    // The plan on screen as the page holds it, and what the Optimiser found for it on a date (MovesSource.Found:
    // none while it is still to be searched); the search is the engine's (PisteTrial.Search) but in a test.
    public PistesSource(
        Func<RiverInputs> onScreen, Func<DateOnly, (MovePlan? Value, Refusal? Error)?> without,
        Func<PisteTrial, MovePlan, DateOnly, (Piste? Value, Refusal? Error)?>? search = null)
    {
        _onScreen = onScreen;
        _without = without;
        _search = search ?? ((trial, plan, date) => trial.Search(plan, date));
    }

    public PistesView View(DateOnly date) => PistesView.Of(Plan, Trials(), trial => Found(trial, date), Tried);

    // The idea's trial: the viewer's answers on the plan on screen, or the plan's own facts.
    public PisteTrial Trial(LeverKind lever) =>
        _trials.TryGetValue(lever, out var trial) && trial.Plan == Plan ? trial : PisteTrial.Of(Plan, lever);

    // The viewer's answers, on the plan on screen: searched only once « Try » is pressed. An answer changed
    // closes the trial, even back to answers tried before; the same answers given again, a field left
    // without a change, keep it open.
    public void Answer(PisteTrial trial)
    {
        ArgumentNullException.ThrowIfNull(trial);
        if (trial.Plan != Plan)
            throw new ArgumentException("Answers are given on the plan on screen.", nameof(trial));

        if (trial == Trial(trial.Lever))
            return;

        _trials[trial.Lever] = trial;
        _tried.Remove(trial.Lever);
    }

    // « Try » on the idea as it stands: searched once the browser has painted, unless found already. False
    // while it cannot be tried (PisteTrial.CanTry).
    public bool Try(LeverKind lever)
    {
        var trial = Trial(lever);
        if (!trial.CanTry)
            return false;

        _tried[lever] = trial;
        return true;
    }

    // « Drop »: the answers are discarded, the trial is the plan's own again.
    public void Drop(LeverKind lever)
    {
        _trials.Remove(lever);
        _tried.Remove(lever);
    }

    // « Keep »: the river with the lever, its answers and its own moves' values (PisteTrial.Keep), once the idea
    // as it stands is measured and can be tried; the trial then ends. None otherwise.
    public RiverInputs? Keep(LeverKind lever, DateOnly date)
    {
        var trial = Trial(lever);
        if (!trial.CanTry || Found(trial, date)?.Value is not { } piste)
            return null;

        Drop(lever);
        return trial.Keep(piste);
    }

    // Once the browser has painted what the page drew (painted), the next idea to measure on that date, unless
    // none is or a search already waits: a tried one first, then in the river's order. The idea searched is
    // the one waiting then. True when it searched, for the page to draw what it found and ask again.
    public async Task<bool> SearchNextOncePaintedAsync(DateOnly date, Func<ValueTask> painted)
    {
        ArgumentNullException.ThrowIfNull(painted);
        if (_waiting || Next(date) is null)
            return false;

        _waiting = true;
        try
        {
            await painted();
        }
        finally
        {
            _waiting = false;
        }

        if (Next(date) is not { } trial || _without(date)?.Value is not { } without)
            return false;

        foreach (var stale in _found.Keys.Where(key => key.Plan != Plan).ToList())
            _found.Remove(stale);
        _found[Key(trial, date)] = _search(trial, without, date);
        return true;
    }

    private RiverInputs Plan => _onScreen();

    // The trial of each idea not in the plan on screen, none for a plan that starts at the pay.
    private List<PisteTrial> Trials() =>
        Plan.Company is null ? [] : [.. PisteSearch.Levers.Where(lever => !Plan.LeversInPlan.Contains(lever)).Select(Trial)];

    private bool Tried(PisteTrial trial) => _tried.TryGetValue(trial.Lever, out var tried) && tried == trial;

    // What was found for that trial on that date: the Optimiser's refusal of the plan for any, none while the
    // Optimiser or the idea is still to be searched.
    private (Piste? Value, Refusal? Error)? Found(PisteTrial trial, DateOnly date) => _without(date) switch
    {
        null => null,
        { Value: null, Error: var error } => (null, error),
        _ => _found.GetValueOrDefault(Key(trial, date)),
    };

    // The idea to search next: nothing left to answer, the Optimiser's plan found, not searched yet for its
    // answers and date, and either holding no answer the viewer typed or tried.
    private PisteTrial? Next(DateOnly date) =>
        _without(date)?.Value is null
            ? null
            : Trials().Where(trial => trial.Open.Count == 0 && !_found.ContainsKey(Key(trial, date)) && (!trial.Answered || Tried(trial)))
                .OrderByDescending(Tried).FirstOrDefault();

    private static (LeverKind, RiverInputs, RiverInputs, DateOnly) Key(PisteTrial trial, DateOnly date) => (trial.Lever, trial.Plan, trial.River, date);
}
