namespace Netto.Web.Client.Tabs;

// Which kinds of link the Map tab shows: a preference of the viewer's, kept in the browser so a reload
// keeps it, and never part of a plan. Every kind shows until the viewer hides it, so what is stored is the
// kinds they hid, and a kind the map gains later shows without being asked. The storage is read when the
// filter is made, before the first draw of the tab, so the first draw already leaves the hidden kinds
// out. Scoped, like the folds: the tab is built again each time the viewer comes back to it, and a browser
// that refuses storage keeps the choice for as long as the page is open.
internal sealed class MapLinkFilter(IBrowserStorage storage)
{
    internal const string StorageKey = "netto.map.links";

    private readonly HashSet<MapLinkKind> _hidden = Parse(storage.Get(StorageKey));

    public bool Shows(MapLinkKind kind) => !_hidden.Contains(kind);

    // The viewer's word, remembered. A choice already made is not written again.
    public void Set(MapLinkKind kind, bool shown)
    {
        if (shown ? !_hidden.Remove(kind) : !_hidden.Add(kind))
            return;

        storage.Set(StorageKey, string.Join(';', Enum.GetValues<MapLinkKind>().Where(_hidden.Contains).Select(Word)));
    }

    // The map without the links of the kinds hidden, which it keeps apart as hidden: the modules, the
    // buckets and the route of each link left are the map's, so nothing moves. The map given is the one
    // the run makes, with every link.
    public MapView Showing(MapView view) => _hidden.Count == 0
        ? view
        : view with
        {
            Links = [.. view.Links.Where(link => Shows(link.Kind))],
            Hidden = [.. view.Links.Where(link => !Shows(link.Kind))],
        };

    // The links of each kind in the plan, whatever is hidden: what the legend says a kind would show, a
    // kind the plan has none of counted as none.
    public static IReadOnlyDictionary<MapLinkKind, int> Counts(MapView view) =>
        Enum.GetValues<MapLinkKind>().ToDictionary(kind => kind, kind => view.Links.Count(link => link.Kind == kind) + view.Hidden.Count(link => link.Kind == kind));

    // What is stored for a kind, so a kind renamed keeps the choice made of it.
    internal static string Word(MapLinkKind kind) => kind switch
    {
        MapLinkKind.PassedOn => "passed_on",
        MapLinkKind.IntoBucket => "into_bucket",
        MapLinkKind.TaxBase => "tax_base",
        MapLinkKind.Condition => "condition",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A kind of link the filter has no word for."),
    };

    // The words of the hidden kinds, joined by ';'. A word this version does not write is no kind, and
    // anything else under Netto's key is no choice: every kind shows.
    private static HashSet<MapLinkKind> Parse(string? stored)
    {
        var hidden = new HashSet<MapLinkKind>();
        foreach (var word in (stored ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var kind in Enum.GetValues<MapLinkKind>().Where(kind => Word(kind) == word))
                hidden.Add(kind);
        }

        return hidden;
    }
}
