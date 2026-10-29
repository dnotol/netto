namespace Netto.Web.Client.Tabs;

// How thick the map draws its lines, in pixels, which a line keeps however the map stretches
// (vector-effect): a line of money as thick as its amount, the thinnest and the square root of its share
// of the run's largest line of money the rest of the way to the thickest, so the largest is the thickest,
// a quarter of it halfway, and a line of a few hundred euros beside the year's profit still shows; a line
// that only changes a tax base, or a condition, its kind's width (MapLinkStyle.Fixed), since it carries no
// money. The largest line is the run's, measured from every link (MapLayout.Routed), so drawing fewer kinds of
// line thickens none, and a line's amount is the one the map says of it (MapBundle.Sum). Arithmetic of
// display: nothing here is a tax figure. And the arrowhead ending a line, sized by it.
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

    // The arrowhead ending a line, in pixels: half its base, three pixels wider than half the line, never
    // less than the 0.3rem every arrowhead had before lines grew with their amounts, so it reads on the
    // thickest line as on the thinnest; and its length, half as long again as that half.
    public static decimal ArrowHalf(decimal width) => Math.Max(4.8m, width / 2m + 3m);

    public static decimal ArrowLength(decimal width) => 1.5m * ArrowHalf(width);

    // The stretches of a line, from each of its points to the next, a stretch of no length left out.
    public static IEnumerable<(MapPoint A, MapPoint B)> Stretches(MapRoute route) =>
        route.Points.Zip(route.Points.Skip(1)).Where(stretch => stretch.First != stretch.Second);

    // What each stretch of a line covers on the map drawn at a scale (Stroke), and whether it runs across.
    public static IEnumerable<(bool Across, MapArea Area)> Strokes(MapRoute route, MapScale scale, decimal halo = 0m) =>
        Stretches(route).Select(stretch => (stretch.A.Y == stretch.B.Y, Stroke(stretch.A, stretch.B, route.Width, scale, halo)));

    // What a stretch of line covers on the map drawn at a scale, in pixels: its length, and half its width
    // each side of it, with its halo's gap each side too when asked (MapLinkStyle.HaloGap). A stretch across
    // the map is one whose ends share their height.
    public static MapArea Stroke(MapPoint a, MapPoint b, decimal width, MapScale scale, decimal halo = 0m)
    {
        var (half, ax, ay, bx, by) = (width / 2m + halo, a.X * scale.X, a.Y * scale.Y, b.X * scale.X, b.Y * scale.Y);
        return ay == by && ax != bx
            ? new MapArea(Math.Min(ax, bx), ay - half, Math.Max(ax, bx), ay + half)
            : new MapArea(ax - half, Math.Min(ay, by), ax + half, Math.Max(ay, by));
    }

    // What the arrowhead ending a line covers on the map drawn at a scale, in pixels: its base across the
    // line's end, its point on it.
    public static MapArea Head(MapRoute route, MapScale scale)
    {
        var (half, length, x, y) = (ArrowHalf(route.Width), ArrowLength(route.Width), route.End.X * scale.X, route.End.Y * scale.Y);
        return route.Arrow switch
        {
            MapDirection.Down => new MapArea(x - half, y - length, x + half, y),
            MapDirection.Up => new MapArea(x - half, y, x + half, y + length),
            MapDirection.Right => new MapArea(x - length, y - half, x, y + half),
            _ => new MapArea(x, y - half, x + length, y + half),
        };
    }
}
