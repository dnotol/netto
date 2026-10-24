using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What a tab's badge says after its name. Public, with the badge, because the tab bar, a component,
// takes them as a parameter.
public enum TabBadgeKind
{
    // The tab holds an amount the engine refuses.
    Refused,

    // What the moves from the plan starred « Today » add to it (Amount).
    MoreThanToday,

    // What the moves from the plan in the river, while none is starred Today, add to it (Amount).
    MoreThanPlan,

    // The next advance payment's deadline (Date).
    NextBy,

    // How many saved plans can be compared (Count).
    SavedPlans,

    // The moves are being looked for, once the page has painted (MovesSource).
    Searching,
}

// One tab's badge: its kind and the figure it shows, an amount, a date or a count.
public sealed record TabBadge(TabBadgeKind Kind, decimal Amount = 0m, DateOnly Date = default, int Count = 0);

// The tabs' badges, from the views their contents draw, so a badge says only what its tab shows: the
// moves' total on Optimisation once they add something to the plan they are from, Today or the plan in
// the river (MovesView), or that they are being looked for until the page has searched them, once
// painted; the next advance payment's date on When to pay while one is left
// (CalendarView.Next), and on the tab holding an amount the engine refuses, that refusal, which wins over
// a date: the river's on Plan, what the fund bills on When to pay, and on Compare plans how many saved
// plans can be compared once there are two, the plans being the tab's own whatever the river does. A tab
// with nothing to say has none.
internal static class TabBadges
{
    public static IReadOnlyDictionary<PlanTab, TabBadge> Of(RiverRun run, MovesView moves, CalendarView? calendar, int comparablePlans)
    {
        var badges = new Dictionary<PlanTab, TabBadge>();
        if (run.Error is not null)
            badges[PlanTab.Plan] = new TabBadge(TabBadgeKind.Refused);
        if (moves.State == MovesState.Searching)
            badges[PlanTab.Optimisation] = new TabBadge(TabBadgeKind.Searching);
        else if (moves.State == MovesState.Moves)
            badges[PlanTab.Optimisation] = new TabBadge(moves.From.IsToday ? TabBadgeKind.MoreThanToday : TabBadgeKind.MoreThanPlan, Amount: moves.Total);
        if (calendar is { BilledRefused: not null })
            badges[PlanTab.WhenToPay] = new TabBadge(TabBadgeKind.Refused);
        else if (calendar?.Next is { } next)
            badges[PlanTab.WhenToPay] = new TabBadge(TabBadgeKind.NextBy, Date: next.Date);
        if (comparablePlans >= 2)
            badges[PlanTab.Compare] = new TabBadge(TabBadgeKind.SavedPlans, Count: comparablePlans);

        return badges;
    }
}
