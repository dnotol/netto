using MudBlazor;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The badges of the modules' boxes, read from every link of the run whatever the choice draws, so a line
// left undrawn hides no badge: each condition one module sets on another that is lost marks the module it acts
// on with its own badge (MapConditions), in the engine's order, once where two say the same; the office rule, a
// rule that holds and a royalty that changes nothing have none. A tax base marks no module: its dotted line
// says its amount when the module is chosen, and the Effects list beside the card says it in words. A refused
// run has no link, so no badge; nothing reaches a quiet lever, so none either. Nothing here tests a rule:
// whether a condition holds is the engine's.
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

// A mark in a module's box: what a condition another module sets on it costs the plan, in a few words, worded in
// the map's texts. It is part of the
// module's button, so its words are part of the name a screen reader hears.
internal sealed record MapBadge(MapLinkKind Kind, MapLabel Words, MapBadgeTone Tone);

// How a badge is drawn: what is lost in the error colour, outlined and with its icon, its words saying it too,
// never the colour alone.
internal enum MapBadgeTone
{
    Lost,
}

// How each tone of badge is drawn, beside its words: the class the stylesheet draws it by, and its icon, an error
// mark for what is lost.
internal static class MapBadgeStyle
{
    public static string Class(MapBadgeTone tone) => tone switch
    {
        MapBadgeTone.Lost => "lost",
        _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, "A tone of badge the map has no style for."),
    };

    public static string? Icon(MapBadgeTone tone) => tone switch
    {
        MapBadgeTone.Lost => Icons.Material.Rounded.ErrorOutline,
        _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, "A tone of badge the map has no style for."),
    };
}
