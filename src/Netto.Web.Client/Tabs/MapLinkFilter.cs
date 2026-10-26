namespace Netto.Web.Client.Tabs;

// Which kinds of link the Map tab shows: a preference of the viewer's, kept in the browser so a reload
// keeps it, and never part of a plan. The rules are marked on the modules they reach (MapBadges), so the
// lines of a tax base and of a condition are hidden by default, until the viewer ticks them, and what is
// stored is the choice, whole: the words of the kinds hidden, none when the viewer ticked every box, and
// nothing stored at all is no choice. The key of the first version of the filter (netto.map.links) is read
// by no one and left where it is. The storage is read when the filter is made, before the first draw of
// the tab, so the first draw already leaves the hidden kinds out. Scoped, like the folds: the tab is built
// again each time the viewer comes back to it, and a browser that refuses storage keeps the choice for as
// long as the page is open.
internal sealed class MapLinkFilter(IBrowserStorage storage)
{
    internal const string StorageKey = "netto.map.lines";

    // The kinds whose lines the map hides until the viewer ticks them: what only changes a tax base and
    // what one module sets on another, each said by a badge on the module it reaches.
    private static readonly MapLinkKind[] HiddenByDefault = [MapLinkKind.TaxBase, MapLinkKind.Condition];

    private readonly HashSet<MapLinkKind> _hidden = Parse(storage.Get(StorageKey));

    public bool Shows(MapLinkKind kind) => !_hidden.Contains(kind);

    // The viewer's word, remembered with the whole of their choice, the kinds hidden by default among the
    // hidden ones until they are ticked. A choice already made is not written again.
    public void Set(MapLinkKind kind, bool shown)
    {
        if (shown ? !_hidden.Remove(kind) : !_hidden.Add(kind))
            return;

        storage.Set(StorageKey, string.Join(';', Enum.GetValues<MapLinkKind>().Where(_hidden.Contains).Select(Word)));
    }

    // The map without the links of the kinds hidden: the modules, the buckets and the route of each link
    // left are the map's, so nothing moves. The map given is the one the run makes, with every link.
    public MapView Showing(MapView view) => _hidden.Count == 0
        ? view
        : view with { Links = [.. view.Links.Where(link => Shows(link.Kind))] };

    // The links of each kind in the map given, which is the run's, with every link whatever is hidden: what
    // the legend says a kind would show, a kind the plan has none of counted as none.
    public static IReadOnlyDictionary<MapLinkKind, int> Counts(MapView view) =>
        Enum.GetValues<MapLinkKind>().ToDictionary(kind => kind, kind => view.Links.Count(link => link.Kind == kind));

    // What is stored for a kind, so a kind renamed keeps the choice made of it.
    internal static string Word(MapLinkKind kind) => kind switch
    {
        MapLinkKind.PassedOn => "passed_on",
        MapLinkKind.IntoBucket => "into_bucket",
        MapLinkKind.TaxBase => "tax_base",
        MapLinkKind.Condition => "condition",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A kind of link the filter has no word for."),
    };

    // The words of the hidden kinds, joined by ';': nothing stored is no choice, and neither is a value that
    // names no kind, so the kinds hidden by default are hidden then; a value of separators alone, as an empty
    // one, is a viewer who ticked every box. A word this version does not write is no kind.
    private static HashSet<MapLinkKind> Parse(string? stored)
    {
        var words = (stored ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (stored is not null && words.Length == 0)
            return [];

        HashSet<MapLinkKind> hidden = [.. Enum.GetValues<MapLinkKind>().Where(kind => words.Contains(Word(kind)))];

        return hidden.Count == 0 ? [.. HiddenByDefault] : hidden;
    }
}
