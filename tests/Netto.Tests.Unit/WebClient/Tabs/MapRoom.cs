using Netto.Web.Client;
using Netto.Web.Client.Tabs;

namespace Netto.Tests.WebClient.Tabs;

// Whether the map gives each line of money a stretch of its own for its amount, which the Map tab is to draw on the
// line itself: a place on the line, or right beside it, at the size the map's stylesheet will draw the amount,
// clear of the boxes, the quiet rows, the buckets, every arrowhead and the map's edge, where no other line of money
// runs along it, so that a reader matches the amount to its line by place. A line running across it at a right
// angle may pass under it. Each line is held to it on its own; whether every amount fits beside the others is the
// placement's, which the slice drawing them builds. A place is anchored to a point of its line and keeps its pixels
// however large the map is drawn, so it is checked where the map is smallest and at 1,920 px
// (MapLayoutTests.Sizes). The size is an estimate from the amount's characters, in pixels, as doubles: a drawing's
// geometry, no money.
internal static class MapRoom
{
    // An amount's text at 0.72rem: a figure, the euro sign or a minus 0.62em, a comma or a point 0.28em; its
    // ground 0.3rem each side; a line of 1.25 and 0.05rem above and below it.
    private const double RemPixels = 16;
    private const double FontPixels = 0.72 * RemPixels;
    private const double GroundPixels = 0.3 * RemPixels;

    public static double Height => FontPixels * 1.25 + 2 * 0.05 * RemPixels;

    public static double Width(string text) => text.Sum(character => character is ',' or '.' ? 0.28 : 0.62) * FontPixels + 2 * GroundPixels;

    // How far from the end of its stretch an amount on its line stays, so the line shows each side of it; how
    // far beside its line one stands; the room kept from a box or an arrowhead.
    private const double ShowsEachSide = 4;
    private const double BesideGap = 2;
    private const double Clearance = 1;

    // How finely the places along a stretch are tried, in pixels on the smallest map.
    private const double Step = 2;

    // The lines of money of a map with no stretch of their own for their amount, by their kind and ends.
    public static IReadOnlyList<string> Without(MapView view)
    {
        var lines = view.Bundles();
        IReadOnlyList<MapSlot> boxes = [.. view.Places().Select(place => place.Slot), .. view.Buckets.Select(bucket => bucket.Slot)];

        return [.. lines.Where(Money).Where(line => !Places(view, line).Any(place => MapLayoutTests.Sizes.All(size => Clear(place, line, lines, boxes, Size(view, size.Width)))))
            .Select(line => $"{line.Kind} {line.From.Id} to {line.To.Id}")];
    }

    private static bool Money(MapBundle line) => line.Kind is MapLinkKind.PassedOn or MapLinkKind.IntoBucket;

    // The map's box in pixels at a width, a chain row at its smallest.
    private static (double Width, double Height) Size(MapView view, decimal width) => ((double)width, (double)(view.Units * MapLayoutTests.RowPixels));

    // Every place along a line's stretches, on the smallest map, from the middle of each stretch outwards: on it,
    // before it (over a stretch across, left of one down) and after it, and past the end of a line out of the river.
    private static IEnumerable<Place> Places(MapView view, MapBundle line)
    {
        var (width, height) = Size(view, MapLayoutTests.Sizes[0].Width);
        var (w, h) = (Width(Formats.Euros(line.Amount ?? 0m)), Height);
        var points = line.Route.Points;
        for (var index = 0; index + 1 < points.Count; index++)
        {
            var (a, b) = (points[index], points[index + 1]);
            var across = a.Y == b.Y;
            var last = index + 2 == points.Count;
            var gap = Math.Max((double)line.Route.Width / 2, last && line.Arrowhead ? (double)MapLineWidths.ArrowHalf(line.Route.Width) : 0) + BesideGap;
            var (low, high) = across
                ? ((double)Math.Min(a.X, b.X) * width / 100, (double)Math.Max(a.X, b.X) * width / 100)
                : ((double)Math.Min(a.Y, b.Y) * height / 100, (double)Math.Max(a.Y, b.Y) * height / 100);
            var along = across ? w : h;
            foreach (var mode in new[] { Mode.On, Mode.Before, Mode.After })
            {
                var margin = mode == Mode.On ? ShowsEachSide : 0;
                var (from, to) = (low + along / 2 + margin, high - along / 2 - margin);
                if (to < from)
                    continue;

                var middle = (from + to) / 2;
                for (var offset = 0.0; offset <= (to - from) / 2; offset += Step)
                {
                    yield return At(middle - offset);
                    if (offset > 0)
                        yield return At(middle + offset);
                }

                Place At(double centre) => across
                    ? new Place(centre / width * 100, (double)a.Y, across, mode, gap, w, h)
                    : new Place((double)a.X, centre / height * 100, across, mode, gap, w, h);
            }

            if (last && line.To.IsOut)
                yield return new Place((double)b.X, (double)b.Y, true, Mode.Beyond, BesideGap, w, h);
        }
    }

    // Whether an amount at a place, the map drawn at a size, overlaps no box, no arrowhead and no edge, and has
    // no other line of money along it: parallel to the stretch it stands on, or across the end it stands past.
    private static bool Clear(Place place, MapBundle line, IReadOnlyList<MapBundle> lines, IReadOnlyList<MapSlot> boxes, (double Width, double Height) size)
    {
        var at = place.At(size.Width, size.Height);
        var (sx, sy) = (size.Width / 100, size.Height / 100);
        return at.Left >= 0 && at.Top >= 0 && at.Right <= size.Width && at.Bottom <= size.Height
            && !boxes.Any(box => Hits(at, ((double)box.Left * sx, (double)box.Top * sy, (double)box.Right * sx, (double)box.Bottom * sy), Clearance))
            && !lines.Where(other => other.Arrowhead).Any(other => Hits(at, Head(other.Route, sx, sy), Clearance))
            && !lines.Where(other => !ReferenceEquals(other.Route, line.Route) && Money(other))
                .Any(other => Strokes(other.Route, sx, sy).Any(stroke => stroke.Across == place.Across && Hits(at, stroke.Area, 0)));
    }

    // What each stretch of a line covers, its width and a halo's gap each side, and whether it runs across.
    private static IEnumerable<(bool Across, (double Left, double Top, double Right, double Bottom) Area)> Strokes(MapRoute route, double sx, double sy)
    {
        var half = (double)(route.Width / 2 + MapLinkStyle.HaloGap);
        foreach (var (a, b) in route.Points.Zip(route.Points.Skip(1)))
        {
            var (ax, ay, bx, by) = ((double)a.X * sx, (double)a.Y * sy, (double)b.X * sx, (double)b.Y * sy);
            yield return ay == by
                ? (true, (Math.Min(ax, bx), ay - half, Math.Max(ax, bx), ay + half))
                : (false, (ax - half, Math.Min(ay, by), ax + half, Math.Max(ay, by)));
        }
    }

    // What a line's arrowhead covers, its point on the line's end.
    private static (double Left, double Top, double Right, double Bottom) Head(MapRoute route, double sx, double sy)
    {
        var (half, length, x, y) = ((double)MapLineWidths.ArrowHalf(route.Width), (double)MapLineWidths.ArrowLength(route.Width), (double)route.End.X * sx, (double)route.End.Y * sy);
        return route.Arrow switch
        {
            MapDirection.Down => (x - half, y - length, x + half, y),
            MapDirection.Up => (x - half, y, x + half, y + length),
            MapDirection.Right => (x - length, y - half, x, y + half),
            _ => (x, y - half, x + length, y + half),
        };
    }

    private static bool Hits((double Left, double Top, double Right, double Bottom) a, (double Left, double Top, double Right, double Bottom) b, double clearance) =>
        a.Left < b.Right + clearance && b.Left < a.Right + clearance && a.Top < b.Bottom + clearance && b.Top < a.Bottom + clearance;

    private enum Mode
    {
        On,
        Before,
        After,
        Beyond,
    }

    // A place for an amount: a point of its line, in percent of the map, how it stands there and its gap in
    // pixels, and its size; the area it covers at a size of the map.
    private sealed record Place(double X, double Y, bool Across, Mode Mode, double Gap, double W, double H)
    {
        public (double Left, double Top, double Right, double Bottom) At(double width, double height)
        {
            var (x, y) = (X * width / 100, Y * height / 100);
            return (Mode, Across) switch
            {
                (Mode.On, _) => (x - W / 2, y - H / 2, x + W / 2, y + H / 2),
                (Mode.Beyond, _) => (x + Gap, y - H / 2, x + Gap + W, y + H / 2),
                (Mode.Before, true) => (x - W / 2, y - Gap - H, x + W / 2, y - Gap),
                (Mode.After, true) => (x - W / 2, y + Gap, x + W / 2, y + Gap + H),
                (Mode.Before, false) => (x - Gap - W, y - H / 2, x - Gap, y + H / 2),
                _ => (x + Gap, y - H / 2, x + Gap + W, y + H / 2),
            };
        }
    }
}
