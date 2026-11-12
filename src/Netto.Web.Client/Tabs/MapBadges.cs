using MudBlazor;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The badges of the modules' boxes, read from every link of the run whatever the choice draws, so a line
// left undrawn hides no badge: each condition one module sets on another marks the module it acts on with
// its own badge (MapConditions), in the engine's order, but the office rule's, which always holds and has
// none; then each module a tax base of another module's card reaches is marked once with what that base
// gains, the sum of the card lines reaching it, each to the cent as its card draws it, which for Corporate tax is the year's disallowed
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

        // A tax base the module's own card draws, the marital quotient to the partner, is said on its line, its
        // amount beside the box, which the partner's narrow box could not say whole again.
        foreach (var reached in links.Where(link => link.Kind == MapLinkKind.TaxBase && link.To.Module != link.Card).GroupBy(link => link.To.Module))
        {
            var gains = MapBundle.Sum(reached);
            if (reached.Key is { } taxed && gains != 0m)
                Add(badges, taxed, new MapBadge(MapLinkKind.TaxBase, new MapLabel(MapText.Map, MapTabText.TaxBaseBadge(taxed), [Formats.Euros(gains)]), MapBadgeTone.Amount));
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
        if (!list.Contains(badge))
            list.Add(badge);
    }
}

// A mark in a module's box: what a condition another module sets on it costs the plan, in a few words, or what
// its tax base gains, its amount, worded in the map's texts. It is part of the
// module's button, so its words are part of the name a screen reader hears.
internal sealed record MapBadge(MapLinkKind Kind, MapLabel Words, MapBadgeTone Tone);

// A badge's words as its box draws them: those seen, and those only heard, out of sight, part of the button's
// name. A badge whose words hold an amount, what a tax base gains, shows them up to its amount, its sign and the
// amount itself, so it reads whole on the narrowest map; the words after it are heard, its dotted outline, its
// line's, saying what it is, as the key does. A condition's badge, with no amount, is seen whole.
internal static class MapBadgeWords
{
    public static (string Seen, string Heard) Of(MapBadge badge, string template)
    {
        var (before, after, holdsAmount) = MapGateText.Parts(template);
        return holdsAmount ? (before + badge.Words.Args[0], after) : (template, "");
    }
}

// How a badge is drawn: what is lost in the error colour, outlined and with its icon, its words saying it too,
// never the colour alone; what a tax base gains, an amount, outlined dotted as its line is.
internal enum MapBadgeTone
{
    Lost,

    Amount,
}

// How each tone of badge is drawn, beside its words: the class the stylesheet draws it by, and its icon, an error
// mark for what is lost, none for an amount.
internal static class MapBadgeStyle
{
    public static string Class(MapBadgeTone tone) => tone switch
    {
        MapBadgeTone.Lost => "lost",
        MapBadgeTone.Amount => "amount",
        _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, "A tone of badge the map has no style for."),
    };

    public static string? Icon(MapBadgeTone tone) => tone switch
    {
        MapBadgeTone.Lost => Icons.Material.Rounded.ErrorOutline,
        MapBadgeTone.Amount => null,
        _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, "A tone of badge the map has no style for."),
    };
}
