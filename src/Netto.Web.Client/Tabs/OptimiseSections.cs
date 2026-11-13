namespace Netto.Web.Client.Tabs;

// The Optimise view's three sub-tabs (docs/architecture/decisions/2026-11-13-optimise-in-three-sub-tabs.md):
// the moves, the pay curve and When to pay, the payment calendar. Public because the view, a component, takes
// one as a parameter.
public enum OptimiseSection
{
    Moves,
    Pay,
    When,
}

// Where each sub-tab lives: the address holds the choice, through the show query the page reads, so a sub-tab
// is a bookmark and a history entry, Back and Forward walk them, and the deadline badge, a payment move's link
// and the old addresses of When to pay (PlanTabRoutes.WhenToPayHref) name the calendar.
internal static class OptimiseSections
{
    private const string Pay = "pay";

    // The order the row of sub-tabs shows them in.
    public static IReadOnlyList<OptimiseSection> Order { get; } = [OptimiseSection.Moves, OptimiseSection.Pay, OptimiseSection.When];

    // The sub-tab the show query names, the moves for none or for a value Netto has no sub-tab for. The router
    // matches whatever the case, and so does this.
    public static OptimiseSection From(string? show) =>
        PlanTabRoutes.OpensWhenToPay(show) ? OptimiseSection.When
        : string.Equals(show, Pay, StringComparison.OrdinalIgnoreCase) ? OptimiseSection.Pay
        : OptimiseSection.Moves;

    // The sub-tab's address from the page's base (<base href="/">), as a link writes it.
    public static string Href(OptimiseSection section) => section switch
    {
        OptimiseSection.Moves => PlanTabRoutes.Href(PlanTab.Optimisation),
        OptimiseSection.Pay => $"{PlanTabRoutes.Href(PlanTab.Optimisation)}?{PlanTabRoutes.ShowQuery}={Pay}",
        OptimiseSection.When => PlanTabRoutes.WhenToPayHref,
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
    };
}

// Which sub-tab of Optimise the address arrives at, asked on every set of the page's parameters as a card's
// arrival is (CardArrival): the sub-tab once per arrival, so the page brings its panel into focus. Arriving at
// Optimise on its moves is no arrival at a sub-tab: the view's title takes the focus (TabHead). From one sub-tab
// to another within Optimise, by Back, Forward or a link, the new one is arrived at, the moves included; the
// same address given again arrives nowhere more, so the viewer who has moved on is not pulled back.
internal sealed class OptimiseArrival
{
    // The sub-tab the address named when last asked, none on another view.
    private OptimiseSection? _named;

    public OptimiseSection? Take(PlanTab tab, string? show)
    {
        OptimiseSection? named = tab == PlanTab.Optimisation ? OptimiseSections.From(show) : null;
        var arriving = named is { } section && section != _named && (_named is not null || section != OptimiseSection.Moves)
            ? named
            : null;
        _named = named;

        return arriving;
    }
}
