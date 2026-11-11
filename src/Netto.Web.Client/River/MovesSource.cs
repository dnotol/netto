using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// Where the Optimisation tab and its badge take their moves from: the plan on screen, the river the band
// runs, and what the search found for it. The search runs the split at every move, about a second in the
// browser on the page's one thread, so it never runs while the page draws: the page draws first, the tab
// and its badge saying the moves are being looked for, and searches once the browser has painted that
// (SearchOncePaintedAsync). It searches at once when the page opens and while Optimisation is shown, where
// an applied move is searched again from the plan it leaves; on the other tabs it waits for the edits to
// pause (QuietPeriod), so typing or dragging is not held up by a search for every value. One search per
// plan and date: what was found for another plan is never given for this one. The page owns one, for as
// long as it is shown.
internal sealed class MovesSource
{
    // How long the edits must pause, on a tab other than Optimisation, before the moves are searched again.
    public static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(500);

    private readonly Func<RiverInputs> _onScreen;
    private readonly Func<ValueTask> _quiet;
    private readonly Func<RiverInputs, DateOnly, (MovePlan? Value, Refusal? Error)?> _search;

    // What the search last found, for which plan and on which date.
    private (RiverInputs Plan, DateOnly Date, (MovePlan? Value, Refusal? Error)? Moves)? _found;

    // Whether a search waits for the browser to paint, or for the edits to pause.
    private bool _waiting;

    // The plan on screen as the page holds it, and the wait for the edits to pause (QuietPeriod); the
    // search is the engine's (RiverInputs.FindMoves) but in a test.
    public MovesSource(Func<RiverInputs> onScreen, Func<ValueTask> quiet, Func<RiverInputs, DateOnly, (MovePlan? Value, Refusal? Error)?>? search = null)
    {
        _onScreen = onScreen;
        _quiet = quiet;
        _search = search ?? ((plan, date) => plan.FindMoves(date));
    }

    // The plan the moves are measured from: the one on screen.
    public RiverInputs Plan => _onScreen();

    // Whether the moves from the plan on screen on that date are still to be searched. A plan that starts
    // at the pay has none to search: the split, and so every move, starts from it.
    public bool Searching(DateOnly date) => Plan.Company is not null && !Holds(Plan, date);

    // What the search found for the plan on screen on that date: none while it is still to be searched,
    // and none for a plan that starts at the pay.
    public (MovePlan? Value, Refusal? Error)? Found(DateOnly date) =>
        _found is { } found && Holds(Plan, date) ? found.Moves : null;

    public MovesView View(DateOnly date) => Searching(date) ? MovesView.Searching(Plan) : MovesView.Of(Plan, Found(date));

    // Once the browser has painted what the page drew (painted), the moves from the plan on screen on that
    // date, unless they are found already or a search already waits. The first search, and one asked at
    // once (atOnce: Optimisation shown), runs as soon as the paint is done; any other waits for the edits to
    // pause, waiting again while the plan changes. The plan searched is the one on screen then. True when
    // it searched, for the page to draw what it found.
    public async Task<bool> SearchOncePaintedAsync(DateOnly date, bool atOnce, Func<ValueTask> painted)
    {
        if (_waiting || !Searching(date))
            return false;

        _waiting = true;
        try
        {
            while (true)
            {
                await painted();
                if (!Searching(date))
                    return false;
                if (atOnce || _found is null)
                    break;

                var waitedFor = Plan;
                await _quiet();
                if (Plan == waitedFor)
                    break;
            }
        }
        finally
        {
            _waiting = false;
        }

        if (!Searching(date))
            return false;

        var plan = Plan;
        _found = (plan, date, _search(plan, date));
        return true;
    }

    // Whether what the search last found is for that plan and date.
    private bool Holds(RiverInputs plan, DateOnly date) => _found is { } found && found.Plan == plan && found.Date == date;
}
