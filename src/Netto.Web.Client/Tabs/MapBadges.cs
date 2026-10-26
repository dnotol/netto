using MudBlazor;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The badges of the modules' boxes, read from every link of the run before a filter hides any, so hiding a
// kind of link hides no badge: each condition one module sets on another marks the module it acts on with
// its own badge (MapConditions), in the engine's order, but the office rule's, which always holds and has
// none; then each module a tax base reaches is marked once with what that base gains, the sum of the card
// lines reaching it, each to the cent as its card draws it, which for Corporate tax is the year's disallowed
// expenses: the sum a viewer makes of the lines, arithmetic of display, no tax figure. A base gaining
// nothing marks nothing. A refused run has no link, so no badge; nothing reaches a quiet lever, so none
// either. Nothing here tests a rule: whether a condition holds is the engine's.
internal static class MapBadges
{
    public static IReadOnlyDictionary<RiverModule, IReadOnlyList<MapBadge>> Of(IReadOnlyList<MapLink> links)
    {
        var badges = new Dictionary<RiverModule, List<MapBadge>>();
        foreach (var link in links)
        {
            if (link is { Kind: MapLinkKind.Condition, Badge: { } badge, To.Module: { } actedOn })
                Add(badges, actedOn, badge);
        }

        foreach (var reached in links.Where(link => link.Kind == MapLinkKind.TaxBase).GroupBy(link => link.To.Module))
        {
            var gains = MapBundle.Sum(reached);
            if (reached.Key is { } taxed && gains != 0m)
                Add(badges, taxed, new MapBadge(MapLinkKind.TaxBase, new MapLabel(MapText.Map, MapTabText.TaxBaseBadge, [Formats.Euros(gains)]), MapBadgeTone.Amount));
        }

        return badges.ToDictionary(module => module.Key, module => (IReadOnlyList<MapBadge>)module.Value);
    }

    // How many lines of badges each module's box holds, one per badge.
    public static IReadOnlyDictionary<RiverModule, int> Lines(IReadOnlyDictionary<RiverModule, IReadOnlyList<MapBadge>> badges) =>
        badges.ToDictionary(module => module.Key, module => module.Value.Count);

    private static void Add(Dictionary<RiverModule, List<MapBadge>> badges, RiverModule module, MapBadge badge)
    {
        if (!badges.TryGetValue(module, out var list))
            badges[module] = list = [];
        list.Add(badge);
    }
}

// A mark in a module's box: a condition another module sets on it, in a few words saying which rule and
// whether it holds, or what its tax base gains, its amount, worded in the map's texts. It is part of the
// module's button, so its words are part of the name a screen reader hears.
internal sealed record MapBadge(MapLinkKind Kind, MapLabel Words, MapBadgeTone Tone);

// How a badge is drawn: a condition that holds muted with a check; one that does not hold but loses
// nothing, the royalty lowering no pay, muted with a dash; one that does not hold and costs the plan its
// rate or its reserve in the error colour, outlined and with its icon, its words saying it too, never the
// colour alone; what a tax base gains, an amount, outlined dotted as its line is.
internal enum MapBadgeTone
{
    Holds,

    NoEffect,

    Lost,

    Amount,
}

// How each tone of badge is drawn, beside its words: the class the stylesheet draws it by, and its icon, a
// check for a rule that holds, a dash for the royalty lowering no pay, an error mark for a rule lost, none
// for an amount.
internal static class MapBadgeStyle
{
    public static string Class(MapBadgeTone tone) => tone switch
    {
        MapBadgeTone.Holds => "holds",
        MapBadgeTone.NoEffect => "no-effect",
        MapBadgeTone.Lost => "lost",
        MapBadgeTone.Amount => "amount",
        _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, "A tone of badge the map has no style for."),
    };

    public static string? Icon(MapBadgeTone tone) => tone switch
    {
        MapBadgeTone.Holds => Icons.Material.Rounded.Check,
        MapBadgeTone.NoEffect => Icons.Material.Rounded.Remove,
        MapBadgeTone.Lost => Icons.Material.Rounded.ErrorOutline,
        MapBadgeTone.Amount => null,
        _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, "A tone of badge the map has no style for."),
    };
}
