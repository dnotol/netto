using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the module chosen on the Map tab does to another module's tax base or under another's condition, and what
// is done to it so: the short list under the line of what it receives, beside its card, an item for each line of a
// tax base or a condition the map draws for it (MapView.ShowingFor), in the order of the map's links. A tax base
// says what changes it, the words of the card lines its line holds, its amount, the sum the map says on that line
// (MapBundle.Amount), and the other module, the one whose tax base it changes or the one it comes from; a condition
// says which rule, whether it holds in the word its line says (MapLink.State), and the other module, the one it is
// set on or the one that sets it. The other module is one the viewer can go to on the map. It is made from every
// link of the run, as the lines are drawn for the choice; none for a module with no such line, nor while the
// engine refuses an amount and the map draws no line. The item of the line under the viewer's pointer is marked
// (Marking). Nothing here decides a tax outcome: it regroups the lines the cards already draw.
internal sealed record MapEffectsView(IReadOnlyList<MapEffect> Items)
{
    public static MapEffectsView Of(MapView view, RiverModule chosen) =>
        new([.. view.ShowingFor(MapChoice.Of(chosen)).Bundles().Where(line => !MapLinkStyle.IsMoney(line.Kind) && line.Touches(chosen)).Select(line => Effect(line, chosen))]);

    // The list with the item of the line given marked, the line under the viewer's pointer, none marked for none.
    public MapEffectsView Marking(MapRoute? route) =>
        new([.. Items.Select(item => item with { Marked = route is not null && ReferenceEquals(item.Route, route) })]);

    private static MapEffect Effect(MapBundle line, RiverModule chosen)
    {
        var sets = line.From.Module == chosen;
        var other = (sets ? line.To.Module : line.From.Module)
            ?? throw new ArgumentOutOfRangeException(nameof(line), $"{line.From.Id} to {line.To.Id}", "A rule's line with no module at its other end.");
        return line.Kind == MapLinkKind.Condition
            ? new MapEffect(line.Route, MapEffectText.Condition(sets), new MapLabel(MapText.Map, MapEffectText.Rule(line.From.Module!.Value, line.To.Module!.Value), []),
                line.Links[0].State, null, other)
            : new MapEffect(line.Route, MapEffectText.TaxBase(sets), null, null, MapEffectText.Amount(line.Amount ?? 0m), other)
            {
                What = [.. line.Links.Select(link => link.Carries)],
            };
    }
}

// One item of the list: the line it is (Route), the key of its sentence (MapEffectText), the rule a condition is and
// the word saying whether it holds, a tax base's amount and the card lines saying what changes it (What), the other
// module, and whether the line is under the viewer's pointer.
internal sealed record MapEffect(MapRoute Route, string Key, MapLabel? Rule, MapLabel? State, string? Amount, RiverModule Other)
{
    public IReadOnlyList<MapLabel> What { get; init; } = [];

    public bool Marked { get; init; }
}
