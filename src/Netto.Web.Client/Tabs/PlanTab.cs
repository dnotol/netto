namespace Netto.Web.Client.Tabs;

// The plan page's tabs, by purpose, under the bar of buckets: the plan, its modules from above with the
// money between them and the card of the one chosen, the moves Netto proposes from the plan starred
// « Today » or from the plan in the river while none is, when the plan's payments fall, and the saved
// plans side by side. Public because the tab bar, a component, takes one as a parameter.
public enum PlanTab
{
    Plan,
    Optimisation,
    WhenToPay,
    Compare,
}

// Where each tab lives: /plan for the plan, /plan/<segment> for the others, one page (Pages/Plan) for
// all, so the river's inputs and what the page found stay as they are from one tab to the next. A new tab
// is a value above, its segment here and its place in the order.
internal static class PlanTabRoutes
{
    // The order the tab bar shows them in.
    public static IReadOnlyList<PlanTab> Order { get; } = [PlanTab.Plan, PlanTab.Optimisation, PlanTab.WhenToPay, PlanTab.Compare];

    // The tab's address from the page's base (<base href="/">), as a link writes it.
    public static string Href(PlanTab tab) => Segment(tab) is { } segment ? $"plan/{segment}" : "plan";

    // The tab a URL's segment after /plan/ names, the plan for none, or none for a segment Netto has no
    // tab for. The router matches a URL whatever its case, so the segment is read the same way.
    public static PlanTab? From(string? segment) => segment is null
        ? PlanTab.Plan
        : Order.Where(tab => string.Equals(Segment(tab), segment, StringComparison.OrdinalIgnoreCase)).Cast<PlanTab?>().FirstOrDefault();

    // Whether the address, as NavigationManager.ToBaseRelativePath gives it, is one of the plan page's tabs:
    // the router matches whatever its case and ignores a trailing slash, a query and a fragment, and so does
    // this. A segment Netto has no tab for is the page not found, drawn at that address.
    public static bool IsAt(string relativePath)
    {
        var path = relativePath.Split('?', '#')[0].TrimEnd('/');
        if (string.Equals(path, "plan", StringComparison.OrdinalIgnoreCase))
            return true;

        const string prefix = "plan/";
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && From(path[prefix.Length..]) is not null;
    }

    private static string? Segment(PlanTab tab) => tab switch
    {
        PlanTab.Plan => null,
        PlanTab.Optimisation => "optimisation",
        PlanTab.WhenToPay => "when-to-pay",
        PlanTab.Compare => "compare",
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, null),
    };
}
