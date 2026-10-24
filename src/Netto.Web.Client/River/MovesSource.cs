using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// Where the Optimisation tab and its badge take their moves from: the plan they are measured from
// (MovesBaseline) and what the search found for it. That plan is the one starred « Today » while a
// readable one is; while none is, the plan in the river as it stood at one of three moments: when the
// page opened, when a plan was opened from Plans, and when the river was saved as a plan, which it is then
// opened from. It is named while it is that saved plan as saved. A change of the river moves nothing
// here, so the moves stand still while the pay is dragged or an amount typed, and the search, which runs
// the split at every move, runs once per plan and date. It never runs while the page draws: the page
// draws first, the tab and its badge saying the moves are being looked for, and searches once the browser
// has painted that (SearchOncePaintedAsync), the search taking the page's one thread for a while. The page
// owns one, for as long as it is shown.
internal sealed class MovesSource
{
    private readonly SavedPlans _plans;
    private readonly Func<RiverInputs, DateOnly, (MovePlan? Value, Refusal? Error)?> _search;

    // The plan in the river as it stood at the last of those moments, and the plan Plans had then opened.
    private RiverInputs _plan;
    private string? _opened;

    // What the search last found, for which plan and on which date.
    private (RiverInputs Plan, DateOnly Date, (MovePlan? Value, Refusal? Error)? Moves)? _found;

    // Whether a search waits for the browser to paint.
    private bool _waiting;

    // The river as the page opens; the search is the engine's (RiverInputs.FindMoves) but in a test.
    public MovesSource(SavedPlans plans, RiverInputs river, Func<RiverInputs, DateOnly, (MovePlan? Value, Refusal? Error)?>? search = null)
    {
        _plans = plans;
        _plan = river;
        _opened = plans.Opened;
        _search = search ?? ((plan, date) => plan.FindMoves(date));
    }

    public MovesBaseline Baseline => _plans.TodayPlan is { Inputs: { } today }
        ? MovesBaseline.Today(today)
        : new MovesBaseline(_plan, IsToday: false, _plans.OpenedPlan is { Inputs: { } opened } plan && opened == _plan ? plan.Name : null);

    // Whether the moves from the baseline on that date are still to be searched. A plan that starts at the
    // pay has none to search: the split, and so every move, starts from it.
    public bool Searching(DateOnly date) => Baseline.Plan.Company is not null && !Holds(Baseline.Plan, date);

    // What the search found for the baseline on that date: none while it is still to be searched, and none
    // for a plan that starts at the pay. What was found for another plan or date is never given for this one.
    public (MovePlan? Value, Refusal? Error)? Found(DateOnly date) =>
        _found is { } found && Holds(Baseline.Plan, date) ? found.Moves : null;

    public MovesView View(DateOnly date) => Searching(date) ? MovesView.Searching(Baseline) : MovesView.Of(Baseline, Found(date));

    // Once the browser has painted what the page drew (painted), the moves from the baseline on that date,
    // unless they are found already or a search already waits for the paint: the plan searched is the
    // baseline as it stands then, so a plan opened or starred while waiting is the one searched. A plan
    // equal to the one searched, such as the river saved unchanged, is the same plan. True when it searched,
    // for the page to draw what it found.
    public async Task<bool> SearchOncePaintedAsync(DateOnly date, Func<ValueTask> painted)
    {
        if (_waiting || !Searching(date))
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

        if (!Searching(date))
            return false;

        var plan = Baseline.Plan;
        _found = (plan, date, _search(plan, date));
        return true;
    }

    // A plan opened from Plans, as it was saved: the river shows it, and the moves are measured from it.
    public void Opened(RiverInputs plan)
    {
        _plan = plan;
        _opened = _plans.Opened;
    }

    // The plans were kept. A save opens the river on the plan saved, which the moves are then measured
    // from; a star, a rename, an import or a deletion opens none, and the plan stays as it was, unnamed
    // once the plan it was opened from is deleted.
    public void Kept()
    {
        if (_plans.Opened == _opened)
            return;

        _opened = _plans.Opened;
        if (_plans.OpenedPlan is { Inputs: { } saved })
            _plan = saved;
    }

    // Whether what the search last found is for that plan and date.
    private bool Holds(RiverInputs plan, DateOnly date) => _found is { } found && found.Plan == plan && found.Date == date;
}
