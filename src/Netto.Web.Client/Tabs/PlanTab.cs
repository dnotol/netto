namespace Netto.Web.Client.Tabs;

// The plan page's three views, each answering one question, in the top row of the bar of buckets: My plan,
// where I stand (the plan, its modules from above with the money between them and the card of the one
// chosen, or the same plan by date), Optimise, how to do better with the facts I gave (the moves Netto
// proposes from the plan on screen), and Ideas, what I could add (« Pistes »). Public because the tab
// bar, a component, takes one as a parameter.
public enum PlanTab
{
    Plan,
    Optimisation,
    Pistes,
}

// How My plan is read: by bucket, the map and the chosen card, or by date, when the plan's payments fall
// (WhenToPayTab). The lens is a state of My plan, not a view, so it is a query on its address.
public enum PlanLens
{
    ByBucket,
    ByDate,
}

// Where each view lives: /plan for My plan, /plan/<segment> for the others, one page (Pages/Plan) for
// all, so the river's inputs and what the page found stay as they are from one view to the next. A new
// view is a value above, its segment here and its place in the order.
internal static class PlanTabRoutes
{
    // The query that names the lens, /plan?by=date; none is the by-bucket lens.
    public const string LensQuery = "by";

    // The query that opens the Plans panel on arrival, /plan?plans=compare, on its compare mode.
    public const string PlansQuery = "plans";

    private const string ByDate = "date";

    private const string Compare = "compare";

    // The order the tab bar shows them in.
    public static IReadOnlyList<PlanTab> Order { get; } = [PlanTab.Plan, PlanTab.Optimisation, PlanTab.Pistes];

    // The tab's address from the page's base (<base href="/">), as a link writes it.
    public static string Href(PlanTab tab) => Segment(tab) is { } segment ? $"plan/{segment}" : "plan";

    // The address of a tab read through a lens: only My plan has one, by date as a query, by bucket being
    // the plan's own address.
    public static string Href(PlanTab tab, PlanLens lens) =>
        tab == PlanTab.Plan && lens == PlanLens.ByDate ? $"plan?{LensQuery}={ByDate}" : Href(tab);

    // The lens a query value names, by bucket for none or a value Netto has no lens for.
    public static PlanLens LensFrom(string? value) =>
        string.Equals(value, ByDate, StringComparison.OrdinalIgnoreCase) ? PlanLens.ByDate : PlanLens.ByBucket;

    // The tab a URL's segment after /plan/ names, My plan for none, or none for a segment Netto has no
    // tab for. The router matches a URL whatever its case, so the segment is read the same way.
    public static PlanTab? From(string? segment) => segment is null
        ? PlanTab.Plan
        : Order.Where(tab => string.Equals(Segment(tab), segment, StringComparison.OrdinalIgnoreCase)).Cast<PlanTab?>().FirstOrDefault();

    // Where a view folded into another has gone, from its old segment: When to pay is the by-date lens of
    // My plan, Compare plans a mode of the Plans panel. None for any other segment. Both addresses replace
    // themselves (Pages/PlanMoved), so a bookmark keeps working.
    public static string? Moved(string? segment)
    {
        if (string.Equals(segment, "when-to-pay", StringComparison.OrdinalIgnoreCase))
            return Href(PlanTab.Plan, PlanLens.ByDate);

        return string.Equals(segment, "compare", StringComparison.OrdinalIgnoreCase) ? $"plan?{PlansQuery}={Compare}" : null;
    }

    // Where the address, as NavigationManager.ToBaseRelativePath gives it, has moved to: the new place of a
    // view folded into another, none for any other address.
    public static string? MovedFrom(string relativePath)
    {
        var path = relativePath.Split('?', '#')[0].TrimEnd('/');
        const string prefix = "plan/";
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? Moved(path[prefix.Length..]) : null;
    }

    // Whether the Plans panel opens on its compare mode, from the query value that asks for it.
    public static bool OpensCompare(string? value) => string.Equals(value, Compare, StringComparison.OrdinalIgnoreCase);

    // Whether the address, as NavigationManager.ToBaseRelativePath gives it, is one of the plan page's views
    // or one that has moved into it (the redirect draws no site header either): the router matches
    // whatever its case and ignores a trailing slash, a query and a fragment, and so does this. A segment
    // Netto has no view for is the page not found, drawn at that address.
    public static bool IsAt(string relativePath)
    {
        var path = relativePath.Split('?', '#')[0].TrimEnd('/');
        if (string.Equals(path, "plan", StringComparison.OrdinalIgnoreCase))
            return true;

        const string prefix = "plan/";
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && (From(path[prefix.Length..]) is not null || MovedFrom(path) is not null);
    }

    private static string? Segment(PlanTab tab) => tab switch
    {
        PlanTab.Plan => null,
        PlanTab.Optimisation => "optimisation",
        PlanTab.Pistes => "pistes",
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, null),
    };
}
