using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The levers that move no money of their own in this plan, read from the map's own links, and the chain's
// hand-offs folded past them. A lever is quiet when every link it has is a hand-off along the chain, money
// passed on from the module above it down the middle or on to the one below, so that it passes on all it
// receives: no line of its own into a bucket, no tax base, no condition, a condition
// counting as any link does (the office allowance beside a paid rent keeps an empty Expense allowances in
// its box). The royalty, beside the chain, is quiet when it has no link at all: it comes to nothing; so is the
// partner, while the marital quotient attributes nothing, in a year taxed separately among others. Read
// from every link of the run before a filter hides any, so hiding a kind of link folds nothing; a refused
// run has no link to read what its levers move, so it folds nothing. Nothing here tests a rule or computes
// a figure: it reads which lines the cards draw.
internal static class MapQuiet
{
    public static IReadOnlySet<RiverModule> Of(RiverRun run, IReadOnlyList<RiverModule> modules, IReadOnlyList<MapLink> links) =>
        run.Error is not null || run.Year is null
            ? new HashSet<RiverModule>()
            : modules.Where(module => RiverModules.Lever(module) is not null && links.Where(link => link.Touches(module)).All(HandOff)).ToHashSet();

    // The chain's line past the quiet levers: the hand-off into the first of them runs on to the next module
    // with a box below them, naming those it runs past (MapLink.Through), with its own words, card and
    // amount, a line of the card above them, which each of them passes on whole; their own hand-offs, of
    // that amount, are left out, so the chain says it once. No link is left with a quiet end.
    public static IReadOnlyList<MapLink> Fold(IReadOnlyList<MapLink> links, IReadOnlyList<RiverModule> modules, IReadOnlySet<RiverModule> quiet)
    {
        if (quiet.Count == 0)
            return links;

        var chain = modules.Where(InChain).ToList();
        return
        [
            .. links.Where(link => link.From.Module is not { } from || !quiet.Contains(from))
                .Select(link => link.To.Module is { } to && quiet.Contains(to) ? Past(link, [.. chain.SkipWhile(module => module != to)], quiet) : link),
        ];
    }

    // The line into a run of quiet levers, on past them to the first module of the chain with a box, the
    // split at the latest, which is no lever.
    private static MapLink Past(MapLink link, IReadOnlyList<RiverModule> fromRun, IReadOnlySet<RiverModule> quiet) => link with
    {
        To = MapEnd.Of(fromRun.First(module => !quiet.Contains(module))),
        Through = [.. fromRun.TakeWhile(quiet.Contains)],
    };

    private static bool HandOff(MapLink link) =>
        link.Kind == MapLinkKind.PassedOn && link.From.Module is { } from && link.To.Module is { } to && InChain(from) && InChain(to);

    private static bool InChain(RiverModule module) => MapLayout.Place(module).Column == MapColumn.Chain;
}
