namespace Netto.Web.Client.Tabs;

// What the legend of the kinds of link draws: a row for every kind, in the order of the kinds, whatever the
// plan holds, so a kind the viewer hid stays in sight to be shown again, with how many links the plan has
// of it and whether it is shown. The counts are the plan's, whatever is hidden, so a row says what showing
// its kind brings back and does not change as the viewer ticks. It only regroups the map's links.
internal sealed record MapFilterView(IReadOnlyList<MapFilterRow> Rows)
{
    // Every link the plan has, and the ones of the kinds shown.
    public int Total => Rows.Sum(row => row.Count);

    public int Shown => Rows.Where(row => row.Shown).Sum(row => row.Count);

    // The viewer hid every kind, whatever the plan holds.
    public bool AllHidden => Rows.All(row => !row.Shown);

    public static MapFilterView Of(MapLinkFilter filter, IReadOnlyDictionary<MapLinkKind, int> counts) =>
        new([.. Enum.GetValues<MapLinkKind>().Select(kind => new MapFilterRow(kind, counts.GetValueOrDefault(kind), filter.Shows(kind)))]);
}

internal sealed record MapFilterRow(MapLinkKind Kind, int Count, bool Shown);
