namespace Netto.Web.Client.Tabs;

// How thick the map draws its lines, in pixels, which a line keeps however the map stretches
// (vector-effect): a line of money as thick as its amount, the thinnest and the square root of its share
// of the run's largest line of money the rest of the way to the thickest, so the largest is the thickest,
// a quarter of it halfway, and a line of a few hundred euros beside the year's profit still shows; a line
// that only changes a tax base, or a condition, its kind's width (MapLinkStyle.Fixed), since it carries no
// money. The largest line is the run's, measured before any filter (MapLayout.Routed), so hiding a kind of
// line thickens none, and a line's amount is the one the map says of it (MapBundle.Sum). Arithmetic of
// display: nothing here is a tax figure.
internal static class MapLineWidths
{
    public const decimal Thinnest = 1.5m;

    public const decimal Thickest = 8m;

    // A line of its kind carrying the amount given, beside the run's largest line of money.
    public static decimal Of(MapLinkKind kind, decimal amount, decimal largest) => MapLinkStyle.Fixed(kind) ?? Money(amount, largest);

    // A line of money, to the hundredth of a pixel, as thick as its amount's size whichever way it is
    // signed; the thinnest on a map with no line of money, which has no largest to measure by.
    public static decimal Money(decimal amount, decimal largest) => largest == 0m
        ? Thinnest
        : Math.Round(Thinnest + (Thickest - Thinnest) * (decimal)Math.Sqrt((double)(Math.Abs(amount) / Math.Abs(largest))), 2, MidpointRounding.AwayFromZero);
}
