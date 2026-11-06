using System.Globalization;
using System.Text;

namespace Netto.Web.Client.Tabs;

// How the map draws a line through its points: an SVG path, straight along each side and turning each corner on
// an arc, so its lines meet and part as a river does rather than as a circuit's traces. A route stays the
// layout's orthogonal points (MapLayout), which its rules and crossings are held on; the turn is the drawing's
// alone. An arc is 8 px on the map where it is drawn smallest (MapScale.Smallest), the scale every pixel of the
// layout is worked out at, never more than half the shorter side beside it, so two turns sharing a side never
// overlap, and on a last side ending in an arrowhead never under the arrowhead's base. The lines are drawn in an
// SVG stretched to the map's box, so its radii are in percent of the map each way: a circle on the narrowest map,
// drawn wider where the map is (operator, 2026-11-06). A halo cut short at either end keeps its line's arcs, the
// cut stopping where an arc begins. Arithmetic of drawing: nothing here is a figure.
internal static class MapPath
{
    // The radius a corner turns on, in pixels, where the map is drawn smallest.
    public const decimal Radius = 8m;

    // The path data of a line through the points given, at the scale given, the arrowhead ending it as long as
    // given, none for a line without; cut short by the pixels given at its start and its end, as a halo is where
    // its line meets the pocket line. Nothing for no points, or a cut taking all of it.
    public static string Of(IReadOnlyList<MapPoint> points, MapScale scale, decimal arrowLength = 0m, decimal trimStart = 0m, decimal trimEnd = 0m)
    {
        var line = Simplified(points);
        if (line.Count == 0)
            return "";

        var radii = RadiiOf(line, scale, arrowLength);
        var first = Cut(line[0], line.Count > 1 ? line[1] : line[0], trimStart, line.Count > 2 ? radii[0] : 0m, scale);
        var last = Cut(line[^1], line.Count > 1 ? line[^2] : line[^1], trimEnd, line.Count > 2 ? radii[^1] : 0m, scale);
        if (first is null || last is null || (line.Count == 2 && Length(line[0], first, scale) + Length(line[1], last, scale) >= Length(line[0], line[1], scale)))
            return "";

        var path = new StringBuilder("M ").Append(Point(first));
        var at = first;
        for (var corner = 1; corner < line.Count - 1; corner++)
        {
            var radius = radii[corner - 1];
            var (into, outOf) = (Toward(line[corner], line[corner - 1], radius, scale), Toward(line[corner], line[corner + 1], radius, scale));
            if (into != at)
                path.Append(" L ").Append(Point(into));
            if (radius > 0m)
            {
                var sweep = Cross(line[corner - 1], line[corner], line[corner + 1]) > 0m ? 1 : 0;
                path.Append(CultureInfo.InvariantCulture, $" A {Number(radius / scale.X)} {Number(radius / scale.Y)} 0 0 {sweep} ").Append(Point(outOf));
            }

            at = outOf;
        }

        if (last != at)
            path.Append(" L ").Append(Point(last));
        return path.ToString();
    }

    // Each corner of a line through the points given, in its order, with the radius it turns on, in pixels.
    public static IReadOnlyList<(MapPoint Corner, decimal Radius)> Turns(IReadOnlyList<MapPoint> points, MapScale scale, decimal arrowLength = 0m)
    {
        var line = Simplified(points);
        var radii = RadiiOf(line, scale, arrowLength);
        return [.. radii.Select((radius, index) => (line[index + 1], radius))];
    }

    // The radius of each corner: 8 px, at most half the shorter side beside it, and on a last side ending in an
    // arrowhead, at most what the arrowhead leaves of it.
    private static List<decimal> RadiiOf(List<MapPoint> line, MapScale scale, decimal arrowLength)
    {
        var radii = new List<decimal>();
        for (var corner = 1; corner < line.Count - 1; corner++)
        {
            var (before, after) = (Length(line[corner - 1], line[corner], scale), Length(line[corner], line[corner + 1], scale));
            var radius = Math.Min(Radius, Math.Min(before, after) / 2m);
            if (corner == line.Count - 2 && arrowLength > 0m)
                radius = Math.Min(radius, Math.Max(after - arrowLength, 0m));
            radii.Add(radius);
        }

        return radii;
    }

    // An end of a line moved the pixels given along its side toward the next point, never past where the corner
    // there begins its arc; none where its side has not that room before the end of the line.
    private static MapPoint? Cut(MapPoint end, MapPoint next, decimal pixels, decimal radius, MapScale scale)
    {
        if (pixels <= 0m)
            return end;

        var room = Length(end, next, scale) - radius;
        return radius == 0m && pixels >= room ? null : Toward(end, next, Math.Min(pixels, room), scale);
    }

    // The points without a point of no length after the one before, nor a point in line with its neighbours.
    private static List<MapPoint> Simplified(IReadOnlyList<MapPoint> points)
    {
        var line = new List<MapPoint>(points.Count);
        foreach (var point in points)
        {
            if (line.Count > 0 && line[^1] == point)
                continue;
            if (line.Count > 1 && Cross(line[^2], line[^1], point) == 0m)
                line[^1] = point;
            else
                line.Add(point);
        }

        return line;
    }

    // How long a side is, in pixels: across or down, a route being orthogonal.
    private static decimal Length(MapPoint a, MapPoint b, MapScale scale) => Math.Abs(a.X - b.X) * scale.X + Math.Abs(a.Y - b.Y) * scale.Y;

    // A point the pixels given from a corner toward another point along their side.
    private static MapPoint Toward(MapPoint from, MapPoint to, decimal pixels, MapScale scale) =>
        new(from.X + Math.Sign(to.X - from.X) * pixels / scale.X, from.Y + Math.Sign(to.Y - from.Y) * pixels / scale.Y);

    // The sign of the turn at b: positive where the line bends clockwise on the screen (y running down), zero
    // where the three points are in line.
    private static decimal Cross(MapPoint a, MapPoint b, MapPoint c) => (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);

    private static string Point(MapPoint point) => $"{Number(point.X)},{Number(point.Y)}";

    private static string Number(decimal value) => Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
}
