using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the module chosen on the map receives, and from where: the one line above its card beside the
// map, the card's drain list saying where its money goes. A part for each line of money the map draws into
// the module (MapBundle), in the order of the map's links, its amount the one the map says on that line, the
// sum of the card's lines it holds (MapBundle.Amount), and the end it comes from, a module, named, which the
// viewer can go to while it is on the map. A bucket is never an origin: what comes back out of one (the exempt
// dividend's refund) is its card's drain line, not money the module receives. It is made from every link of the run, whatever the choice
// draws, as the card's figures are. Nothing is received by Company year, which starts the river, by a lever,
// whose card says what it receives as its payout, nor while the engine
// refuses an amount and the map draws no line. What comes off the company's line, a lever's share or what the
// split shares, comes from Company year, whose line it is.
internal sealed record MapReceivedView(IReadOnlyList<MapReceived> Lines)
{
    public static MapReceivedView Of(MapView view, RiverModule module, Func<MapEnd, string> endName) =>
        new([.. view.Bundles()
            .Where(bundle => !IsLever(module) && MapLinkStyle.IsMoney(bundle.Kind) && bundle.To.Module == module && bundle.From.Bucket is null)
            .Select((bundle, index) => (Bundle: bundle, From: bundle.From.Lever is null ? bundle.From : MapEnd.Of(RiverModule.CompanyYear), First: index == 0))
            .Select(line => new MapReceived(
                Formats.Euros(line.Bundle.Amount ?? 0m),
                endName(line.From),
                line.From.Module is { } from && view.Modules.Any(node => node.End.Module == from) ? from : null,
                line.First))]);

    // A lever's card says what it receives as its payout, « Paid out by the company » or its one drain line:
    // the line above it would say the amount a second time. The household moves no money of its own.
    private static bool IsLever(RiverModule module) => RiverModules.Lever(module) is { } lever && lever != OptionalModule.Household;
}

// One line of money into the module chosen: its amount, the name of the end it comes from, that end's
// module where the viewer can go to it on the map, and whether it is the line's first
// part, the others each said after a separator.
internal sealed record MapReceived(string Amount, string From, RiverModule? Module, bool First);
