namespace Netto.Web.Client.Tabs;

// The plan page's two views, each answering one question, in the top row of the bar of buckets: My plan,
// where I stand (the plan, its modules from above with the money between them and the card of the one
// chosen), and Optimise, how to do better with the facts I gave (the moves Netto proposes from the plan on
// screen, and under them when the plan's payments fall). Levers are found in the panel over the map. Public
// because the tab bar, a component, takes one as a parameter.
public enum PlanTab
{
    Plan,
    Optimisation,
}

// Where each view lives: /plan for My plan, /plan/<segment> for the others, one page (Pages/Plan) for
// all, so the river's inputs and what the page found stay as they are from one view to the next. A new
// view is a value above, its segment here and its place in the order.
internal static class PlanTabRoutes
{
    // The query that named My plan's lens, /plan?by=date, before When to pay became a section of Optimise: only
    // read, to send an old bookmark to the section.
    public const string LegacyLensQuery = "by";

    // The query that names a section of the Optimise view to arrive at, /plan/optimisation?show=when.
    public const string ShowQuery = "show";

    // The query that opens the Plans panel on arrival, /plan?plans=compare, on its compare mode.
    public const string PlansQuery = "plans";

    private const string LegacyByDate = "date";

    private const string When = "when";

    private const string Compare = "compare";

    // The order the tab bar shows them in.
    public static IReadOnlyList<PlanTab> Order { get; } = [PlanTab.Plan, PlanTab.Optimisation];

    // The tab's address from the page's base (<base href="/">), as a link writes it.
    public static string Href(PlanTab tab) => Segment(tab) is { } segment ? $"plan/{segment}" : "plan";

    // The address of When to pay, the section of the Optimise view that holds the payment calendar, as a link
    // writes it. A query, as a card's is (CardLinks): a fragment would be taken for a navigation.
    public static string WhenToPayHref { get; } = $"{Href(PlanTab.Optimisation)}?{ShowQuery}={When}";

    // Whether the query value that names a section asks for When to pay.
    public static bool OpensWhenToPay(string? show) => string.Equals(show, When, StringComparison.OrdinalIgnoreCase);

    // Where a bookmark to My plan read by date, /plan?by=date, now is: the section. None for any other value.
    public static string? MovedFromLens(string? by) =>
        string.Equals(by, LegacyByDate, StringComparison.OrdinalIgnoreCase) ? WhenToPayHref : null;

    // The tab a URL's segment after /plan/ names, My plan for none, or none for a segment Netto has no
    // tab for. The router matches a URL whatever its case, so the segment is read the same way.
    public static PlanTab? From(string? segment) => segment is null
        ? PlanTab.Plan
        : Order.Where(tab => string.Equals(Segment(tab), segment, StringComparison.OrdinalIgnoreCase)).Cast<PlanTab?>().FirstOrDefault();

    // Where a view that has gone is now, from its old segment: When to pay is a section of Optimise, Compare
    // plans a mode of the Plans panel, and Ideas (« pistes »), whose levers are found in the panel over the
    // map, sends to My plan. None for any other segment. All three addresses replace themselves
    // (Pages/PlanMoved), so a bookmark keeps working.
    public static string? Moved(string? segment)
    {
        if (string.Equals(segment, "when-to-pay", StringComparison.OrdinalIgnoreCase))
            return WhenToPayHref;

        if (string.Equals(segment, "pistes", StringComparison.OrdinalIgnoreCase))
            return Href(PlanTab.Plan);

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
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, null),
    };
}
