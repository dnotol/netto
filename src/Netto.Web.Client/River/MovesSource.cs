using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// Where the Optimisation tab and its badge take their moves from: the plan they are measured from
// (MovesBaseline) and what the search found for it. That plan is the one starred « Today » while a
// readable one is; while none is, the plan in the river as it stood at one of three moments: when the
// page opened, when a plan was opened from Plans, and when the river was saved as a plan, which it is then
// opened from. It is named while it is that saved plan as saved. A change of the river moves nothing
// here, so the moves stand still while the pay is dragged or an amount typed, and the search, which runs
// the split at every move, runs once per plan and date. The page owns one, for as long as it is shown.
internal sealed class MovesSource
{
    private readonly SavedPlans _plans;
    private readonly Func<RiverInputs, DateOnly, (MovePlan? Value, string? Error)?> _search;

    // The plan in the river as it stood at the last of those moments, and the plan Plans had then opened.
    private RiverInputs _plan;
    private string? _opened;

    // What the search last found, for which plan and on which date.
    private (RiverInputs Plan, DateOnly Date, (MovePlan? Value, string? Error)? Moves)? _found;

    // The river as the page opens; the search is the engine's (RiverInputs.FindMoves) but in a test.
    public MovesSource(SavedPlans plans, RiverInputs river, Func<RiverInputs, DateOnly, (MovePlan? Value, string? Error)?>? search = null)
    {
        _plans = plans;
        _plan = river;
        _opened = plans.Opened;
        _search = search ?? ((plan, date) => plan.FindMoves(date));
    }

    public MovesBaseline Baseline => _plans.TodayPlan is { Inputs: { } today }
        ? MovesBaseline.Today(today)
        : new MovesBaseline(_plan, IsToday: false, _plans.OpenedPlan is { Inputs: { } opened } plan && opened == _plan ? plan.Name : null);

    // The moves from the baseline on that date, searched again only for another plan or date: a plan
    // equal to the one searched, such as the river saved unchanged, is the same plan.
    public (MovePlan? Value, string? Error)? Found(DateOnly date)
    {
        var plan = Baseline.Plan;
        if (_found is not { } found || found.Plan != plan || found.Date != date)
            _found = found = (plan, date, _search(plan, date));

        return found.Moves;
    }

    public MovesView View(DateOnly date) => MovesView.Of(Baseline, Found(date));

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
}
