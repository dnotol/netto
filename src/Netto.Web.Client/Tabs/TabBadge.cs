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

    // The next advance payment's deadline (Date), a link to the When to pay section of Optimise.
    NextBy,

    // What the fund bills, a field of the When to pay section, is refused: a link to the section.
    BilledRefused,

    // The moves are being looked for (MovesSource).
    Searching,
}

// One badge of a tab: its kind and the figure it shows, an amount or a date.
public sealed record TabBadge(TabBadgeKind Kind, decimal Amount = 0m, DateOnly Date = default)
{
    // Whether the badge is a link of its own, to the When to pay section, beside its tab's link rather than in
    // it: a link cannot hold another.
    public bool LinksToSection => Kind is TabBadgeKind.NextBy or TabBadgeKind.BilledRefused;
}

// The tabs' badges, from the views their contents draw, so a badge says only what its tab shows: on Optimise
// the moves' total once they add something to the plan on screen (MovesView), or that they are being looked
// for, then what its When to pay section says, the next advance payment's date while one is left
// (CalendarView.Next) or that what the fund bills is refused, which wins over a date; on My plan that the
// river is refused. A tab with nothing to say has none; one with two says the gain first.
internal static class TabBadges
{
    public static IReadOnlyDictionary<PlanTab, IReadOnlyList<TabBadge>> Of(RiverRun run, MovesView moves, CalendarView? calendar)
    {
        var badges = new Dictionary<PlanTab, IReadOnlyList<TabBadge>>();
        if (run.Error is not null)
            badges[PlanTab.Plan] = [new TabBadge(TabBadgeKind.Refused)];

        List<TabBadge> optimisation = [];
        if (moves.State == MovesState.Searching)
            optimisation.Add(new TabBadge(TabBadgeKind.Searching));
        else if (moves.State == MovesState.Moves)
            optimisation.Add(new TabBadge(TabBadgeKind.MoreThanPlan, Amount: moves.Total));
        if (calendar is { BilledRefused: not null })
            optimisation.Add(new TabBadge(TabBadgeKind.BilledRefused));
        else if (calendar?.Next is { } next)
            optimisation.Add(new TabBadge(TabBadgeKind.NextBy, Date: next.Date));
        if (optimisation.Count > 0)
            badges[PlanTab.Optimisation] = optimisation;

        return badges;
    }
}
