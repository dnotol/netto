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

    // The module whose point of the line a line into the pocket or out of it meets: its other end's module, or
    // the lever whose point of the company's line it leaves for or joins from.
    private static RiverModule PointOf(MapLink link)
    {
        var other = link.To == Pocket ? link.From : link.To;
        return other.Module ?? other.Lever ?? throw new ArgumentOutOfRangeException(nameof(link), other.Id, "A line into the pocket from no module.");
    }
}
