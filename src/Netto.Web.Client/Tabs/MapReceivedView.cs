using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the module chosen on the Map tab receives, and from where: the one line above its card beside the
// map, the card's drain list saying where its money goes. A part for each line of money the map draws into
// the module (MapBundle), in the order of the map's links, its amount the one the map says on that line, the
// sum of the card's lines it holds (MapBundle.Amount), and the end it comes from, a module the viewer can go
// to or a bucket, named only. It is made from every link of the run, whatever the legend ticks, as the
// card's figures are. Nothing is received by Company year, which starts the river, by a lever that moves no
// money of its own in this plan (MapQuiet), whose chain's line runs on past it to the box below, nor while
// the engine refuses an amount and the map draws no line.
internal sealed record MapReceivedView(IReadOnlyList<MapReceived> Lines)
{
    public static MapReceivedView Of(MapView view, RiverModule module, Func<MapEnd, string> endName) =>
        new([.. view.Bundles()
            .Where(bundle => MapLinkStyle.IsMoney(bundle.Kind) && bundle.To.Module == module)
            .Select(bundle => new MapReceived(
                Formats.Euros(bundle.Amount ?? 0m),
                endName(bundle.From),
                bundle.From.Module is { } from && view.Modules.Any(node => node.End.Module == from) ? from : null))]);
}

// One line of money into the module chosen: its amount, the name of the end it comes from, and that end's
// module where the viewer can go to it on the map, none for a bucket.
internal sealed record MapReceived(string Amount, string From, RiverModule? Module);
