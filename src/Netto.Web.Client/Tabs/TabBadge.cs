using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What a tab's badge says after its name. Public, with the badge, because the tab bar, a component,
// takes them as a parameter.
public enum TabBadgeKind
{
    // The tab holds an amount the engine refuses.
    Refused,

    // What the moves from the plan on screen add to it (Amount).
    MoreThanPlan,

    // The next advance payment's deadline (Date).
    NextBy,

    // The moves are being looked for (MovesSource).
    Searching,

    // How many pistes are priced (Count) and how many questions wait (Questions).
    Pistes,
}

// One tab's badge: its kind and the figure it shows, an amount, a date or counts.
public sealed record TabBadge(TabBadgeKind Kind, decimal Amount = 0m, DateOnly Date = default, int Count = 0, int Questions = 0);

// The pistes the Pistes view found for the plan on screen: how many are priced and how many questions wait
// for the viewer. Supplied by whatever searches them (the page's Pistes); while nothing does there are
// none to say, and the tab has no badge and the page no nudge. Public, with the badge, for the tab bar.
public sealed record PistesCount(int Priced, int Questions)
{
    public bool IsEmpty => Priced == 0 && Questions == 0;
}

// The tabs' badges, from the views their contents draw, so a badge says only what its tab shows: the
// moves' total on Optimisation once they add something to the plan on screen (MovesView), or that they
// are being looked for; on My plan the next advance payment's date while one is left (CalendarView.Next,
// the deadline the by-date lens shows, staying in the band), and, which wins over a date, that the river
// or what the fund bills is refused; on Pistes how many are priced and how many questions wait, once
// something supplies them. A tab with nothing to say has none.
internal static class TabBadges
{
    public static IReadOnlyDictionary<PlanTab, TabBadge> Of(RiverRun run, MovesView moves, CalendarView? calendar, PistesCount? pistes)
    {
        var badges = new Dictionary<PlanTab, TabBadge>();
        if (run.Error is not null || calendar is { BilledRefused: not null })
            badges[PlanTab.Plan] = new TabBadge(TabBadgeKind.Refused);
        else if (calendar?.Next is { } next)
            badges[PlanTab.Plan] = new TabBadge(TabBadgeKind.NextBy, Date: next.Date);
        if (moves.State == MovesState.Searching)
            badges[PlanTab.Optimisation] = new TabBadge(TabBadgeKind.Searching);
        else if (moves.State == MovesState.Moves)
            badges[PlanTab.Optimisation] = new TabBadge(TabBadgeKind.MoreThanPlan, Amount: moves.Total);
        if (pistes is { IsEmpty: false } found)
            badges[PlanTab.Pistes] = new TabBadge(TabBadgeKind.Pistes, Count: found.Priced, Questions: found.Questions);

        return badges;
    }
}
