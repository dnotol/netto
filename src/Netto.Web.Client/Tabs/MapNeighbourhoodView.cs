using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The neighbourhood of the module chosen on the Map tab, which the column beside the map lists in place of
// the module's links: what comes into it, what goes out of it, the line of the chain running past it while
// it is quiet (MapQuiet) and the rules it sets or is under, each a link of the run, in the order of the
// map's links. It is made from every link of the run, the legend's ticks and all: the legend says which
// lines the map draws, and this column is where their words are read, so it lists a rule whose line is not
// drawn too. Every figure is a link's own, a line its card draws (MapCardsTests), and a bar is its share of
// the largest amount listed (Shares.Of), whichever way the money runs, since a plan at a loss has negative
// lines (a net pay below nothing): nothing here is computed beyond that division, and no sum is made of the
// lines. Only a tax base has an amount among the rules, and no bar, since it is money that changes a
// base and does not move.
//
// It is made of public records of words and plain values, the tab giving the words of a link (carries) and
// of an end (endName) from its texts, since a component, which is always public, cannot take the map's own
// internal types as parameters. A module on the map is an end the viewer can go to (Module); a bucket and
// the river's edge are named only.
public sealed record MapNeighbourhoodView(
    RiverModule Module,
    string Name,
    bool Refused,
    IReadOnlyList<MapNeighbourhoodRow> ComesIn,
    IReadOnlyList<MapNeighbourhoodRow> PassesThrough,
    IReadOnlyList<MapNeighbourhoodRow> GoesOut,
    IReadOnlyList<MapNeighbourhoodRule> Rules)
{
    // Nothing to list: no money moves through the module and it sets no rule, and the engine refuses nothing.
    public bool IsEmpty => !Refused && ComesIn.Count + PassesThrough.Count + GoesOut.Count + Rules.Count == 0;

    internal static MapNeighbourhoodView Of(MapView view, RiverModule module, Func<MapLabel, string> carries, Func<MapEnd, string> endName)
    {
        var links = view.LinksOf(module).ToList();
        var money = links.Where(link => link.Kind is MapLinkKind.PassedOn or MapLinkKind.IntoBucket).ToList();
        var largest = money.Count == 0 ? 0m : money.Max(link => Math.Abs(link.Amount ?? 0m));

        MapNeighbourhoodEnd End(MapEnd end) => new(endName(end), end.Module is { } onMap && view.Modules.Any(node => node.End.Module == onMap) ? onMap : null);

        MapNeighbourhoodRow Row(MapLink link, bool pastNamed) => new(
            carries(link.Carries),
            Formats.Euros(link.Amount ?? 0m),
            Shares.Of(Math.Abs(link.Amount ?? 0m), largest),
            link.To.Bucket == "pocket",
            End(link.From),
            End(link.To),
            pastNamed ? [.. link.Through.Select(past => endName(MapEnd.Of(past)))] : []);

        return new MapNeighbourhoodView(
            module,
            endName(MapEnd.Of(module)),
            view.Refusal is not null,
            [.. money.Where(link => !link.Through.Contains(module) && link.To.Module == module).Select(link => Row(link, pastNamed: true))],
            [.. money.Where(link => link.Through.Contains(module)).Select(link => Row(link, pastNamed: false))],
            [.. money.Where(link => !link.Through.Contains(module) && link.From.Module == module).Select(link => Row(link, pastNamed: true))],
            [.. links.Where(link => link.Kind is MapLinkKind.TaxBase or MapLinkKind.Condition).Select(link => Rule(link, module, carries, End))]);
    }

    // A rule from the side of the module chosen: the one it sets on the other end, or the one the other end
    // sets on it. A condition is marked when it does not hold, in words (the engine says whether).
    private static MapNeighbourhoodRule Rule(MapLink link, RiverModule module, Func<MapLabel, string> carries, Func<MapEnd, MapNeighbourhoodEnd> end)
    {
        var setsOn = link.From.Module == module;
        return new MapNeighbourhoodRule(
            setsOn,
            end(setsOn ? link.To : link.From),
            carries(link.Carries),
            link.Amount is { } amount ? Formats.Euros(amount) : null,
            link.Holds == false);
    }
}

// One line of money in the neighbourhood: what it carries and its amount in the plan, the bar's share of the
// largest amount listed, whether it reaches the pocket (the one gold), where it runs from and to, and the
// quiet levers the chain's line runs past, named on the ends of that line.
public sealed record MapNeighbourhoodRow(
    string Words,
    string Amount,
    decimal? Share,
    bool ToPocket,
    MapNeighbourhoodEnd From,
    MapNeighbourhoodEnd To,
    IReadOnlyList<string> Past);

// A rule the module sets on another (SetsOn) or is under, the other one at Other: its words, its amount
// where it is a tax base and none for a condition, whose sentence names its figures, and whether a
// condition does not hold.
public sealed record MapNeighbourhoodRule(bool SetsOn, MapNeighbourhoodEnd Other, string Words, string? Amount, bool NotHolding);

// An end of a line, named; a module on the map, which the viewer can go to, has its module.
public sealed record MapNeighbourhoodEnd(string Name, RiverModule? Module);
