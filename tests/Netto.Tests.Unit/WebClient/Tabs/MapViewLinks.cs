using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;

namespace Netto.Tests.WebClient.Tabs;

// A module's links on a map, as the Map tab's tests read them: what comes into it, what goes out, and the
// line running past it while it is quiet, every link that touches it (MapLink.Touches). The app reads a
// module's lines through what it draws (MapView.Drawn, Amounts) and no longer lists them, so this reading is
// the tests' own.
internal static class MapViewLinks
{
    public static IEnumerable<MapLink> LinksOf(this MapView view, RiverModule module) => view.Links.Where(link => link.Touches(module));
}
