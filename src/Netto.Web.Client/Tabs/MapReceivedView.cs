using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the module chosen on the Map tab receives, and from where: the one line above its card beside the
// map, the card's drain list saying where its money goes. A part for each line of money the map draws into
// the module (MapBundle), in the order of the map's links, its amount the one the map says on that line, the
// sum of the card's lines it holds (MapBundle.Amount), and the end it comes from, a module the viewer can go
// to or a bucket, named only. It is made from every link of the run, whatever the legend ticks, as the
// card's figures are. Nothing is received by Company year, which starts the river, by a lever that moves no
// money of its own in this plan (MapQuiet), the company's line running on past it, nor while the engine
// refuses an amount and the map draws no line. What comes off the company's line, a lever's share or what the
// split shares, comes from Company year, whose line it is.
internal sealed record MapReceivedView(IReadOnlyList<MapReceived> Lines)
{
    public static MapReceivedView Of(MapView view, RiverModule module, Func<MapEnd, string> endName) =>
        new([.. view.Bundles()
            .Where(bundle => MapLinkStyle.IsMoney(bundle.Kind) && bundle.To.Module == module)
            .Select((bundle, index) => (Bundle: bundle, From: bundle.From.Lever is null ? bundle.From : MapEnd.Of(RiverModule.CompanyYear), First: index == 0))
            .Select(line => new MapReceived(
                Formats.Euros(line.Bundle.Amount ?? 0m),
                endName(line.From),
                line.From.Module is { } from && view.Modules.Any(node => node.End.Module == from) ? from : null,
                line.First))]);
}

// One line of money into the module chosen: its amount, the name of the end it comes from, that end's
// module where the viewer can go to it on the map, none for a bucket, and whether it is the line's first
// part, the others each said after a separator.
internal sealed record MapReceived(string Amount, string From, RiverModule? Module, bool First);
