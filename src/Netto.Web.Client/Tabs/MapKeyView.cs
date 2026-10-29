namespace Netto.Web.Client.Tabs;

// What the key to the map's lines draws: an item for every style of line the map draws, in its order, whatever
// the plan holds, each with a sample of the very line, its dash, width, ink and end (MapLinkStyle), so the key
// can never say another style than the map: money passed on, money into a bucket, what reaches the director,
// the pocket line and the lines joining it, in the map's one gold, a tax base and a condition. It shows and hides
// nothing: the map draws every line of money, and a tax base's or a condition's for the module chosen.
internal static class MapKeyView
{
    public static IReadOnlyList<MapKeyLine> Lines { get; } = Enum.GetValues<MapKeyLine>();

    // The kind of link a style of line is drawn as.
    public static MapLinkKind Kind(MapKeyLine line) => line switch
    {
        MapKeyLine.PassedOn => MapLinkKind.PassedOn,
        MapKeyLine.IntoBucket or MapKeyLine.Pocket => MapLinkKind.IntoBucket,
        MapKeyLine.TaxBase => MapLinkKind.TaxBase,
        MapKeyLine.Condition => MapLinkKind.Condition,
        _ => throw new ArgumentOutOfRangeException(nameof(line), line, "A line the key has no style for."),
    };

    public static string? Dash(MapKeyLine line) => MapLinkStyle.Dash(Kind(line));

    public static string Width(MapKeyLine line) => MapLinkStyle.Sample(Kind(line));

    // Its kind's ink, or the map's one gold for what reaches the director.
    public static string Ink(MapKeyLine line) => line == MapKeyLine.Pocket ? MapLinkStyle.PocketInk : MapLinkStyle.Ink(Kind(line));

    public static MapLineEnd End(MapKeyLine line) => MapLinkStyle.SampleEnd(Kind(line));
}

// A style of line the map draws, as its key names it. Public because the key, a component, words it.
public enum MapKeyLine
{
    PassedOn,

    IntoBucket,

    Pocket,

    TaxBase,

    Condition,
}
