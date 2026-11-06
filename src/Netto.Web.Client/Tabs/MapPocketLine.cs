using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What reaches the director runs into one gold line down the left of their column, the pocket line, into the
// pocket. Each card's line into the pocket joins it at its module's point (MapEnd.OnPocketLine), as it was but
// for that end; what leaves the pocket, the director's contribution to a company car, leaves it at the car's
// point into the company's line. The line runs down the map's rows from one point to the next and into the
// pocket, each stretch a link of its own: what has reached the director once its upper point's lines joined or
// left, the sum of the lines into the pocket above it less those out of it, each to the cent as its card draws
// it (MapBundle.Sum), so its last is the pocket the bar says. This is the one line of the map that is no card's:
// arithmetic of display across the cards' lines into the pocket, of one kind, never a tax figure, held to the
// cards and to the bar by MapCardsTests.
//
// A line joining the pocket line says no amount: its gate's box says it, or, for Director pay, the total under
// it. A stretch says what has reached the director so far, but a stretch out of a point where money only leaves
// the line, which the next total under a join says, and a stretch below nothing, which says no total
// (MapView.From marks them, MapLink.Unsaid).
internal static class MapPocketLine
{
    private static readonly MapEnd Pocket = MapEnd.InBucket("pocket");

    public static IReadOnlyList<MapLink> Of(IReadOnlyList<MapLink> links)
    {
        var moving = links.Where(link => link.To == Pocket || link.From == Pocket).ToList();
        if (moving.Count == 0)
            return links;

        var points = moving.Select(PointOf).Distinct().OrderBy(module => MapLayout.Place(module).Row).ToList();
        var stretches = new List<MapLink>(points.Count);
        var total = 0m;
        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            total += MapBundle.Sum(moving.Where(link => link.To == Pocket && PointOf(link) == point))
                - MapBundle.Sum(moving.Where(link => link.From == Pocket && PointOf(link) == point));
            stretches.Add(new MapLink(MapLinkKind.IntoBucket, MapEnd.OnPocketLine(point), index + 1 < points.Count ? MapEnd.OnPocketLine(points[index + 1]) : Pocket,
                point, new MapLabel(MapText.Map, MapTabText.PocketSoFar, []), total));
        }

        return
        [
            .. links.Select(link => link.To == Pocket ? link with { To = MapEnd.OnPocketLine(PointOf(link)) }
                : link.From == Pocket ? link with { From = MapEnd.OnPocketLine(PointOf(link)) }
                : link),
            .. stretches,
        ];
    }

    // The pocket line as the map draws it, one shape under every other line: a step for each stretch, centred on
    // the line and as wide as its stretch (MapLineWidths.Money of what has reached the director so far), from where
    // the line joining it at its upper point ends its turn down into it (MapView.Turn), the highest of them there, or
    // from that point where none joins it, to a pixel past where the next step begins, so no seam shows between
    // them, the last on under the pocket to its middle. Square-ended, so each join reads as the line widening.
    public static IReadOnlyList<MapPocketStep> Steps(MapView view)
    {
        var bundles = view.Bundles();
        var stretches = bundles.Where(bundle => bundle.StretchOfThePocketLine).OrderBy(stretch => stretch.Route.Points[0].Y).ToList();
        var tops = stretches.Select(stretch => TopOf(view, bundles, stretch)).ToList();
        var pixel = 1m / view.Scale.Y;
        return
        [
            .. stretches.Select((stretch, index) =>
                new MapPocketStep(stretch, stretch.Route.Points[0].X, tops[index], index + 1 < stretches.Count ? tops[index + 1] + pixel : stretch.Route.Drawn[^1].Y)),
        ];
    }

    private static decimal TopOf(MapView view, IReadOnlyList<MapBundle> bundles, MapBundle stretch)
    {
        var top = stretch.Route.Points[0].Y;
        var join = bundles.Where(bundle => bundle.JoinsThePocketLine && bundle.To == stretch.From).OrderBy(bundle => bundle.Route.End.Y).FirstOrDefault();
        return join is not null && join.Route.End.Y == top ? top + view.Turn(join) / view.Scale.Y : top;
    }

    // The module whose point of the line a line into the pocket or out of it meets: its other end's module, or
    // the lever whose point of the company's line it leaves for or joins from.
    private static RiverModule PointOf(MapLink link)
    {
        var other = link.To == Pocket ? link.From : link.To;
        return other.Module ?? other.Lever ?? throw new ArgumentOutOfRangeException(nameof(link), other.Id, "A line into the pocket from no module.");
    }
}

// The pocket line as the map draws it: its steps, top to bottom, and whether it is faded, which it only ever is
// whole; nothing fades it yet, whichever module is chosen.
internal sealed record MapPocketShape(IReadOnlyList<MapPocketStep> Steps)
{
    public bool Faded { get; init; }
}

// A step of the pocket line: its stretch, and where the map draws it, in percent of the map: down its line (X)
// from its top to its bottom, as wide as its stretch, in pixels.
internal sealed record MapPocketStep(MapBundle Stretch, decimal X, decimal Top, decimal Bottom)
{
    public decimal Width => Stretch.Route.Width;
}
