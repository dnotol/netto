using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The levers that move no money of their own in this plan, read from the map's own links, and the company's
// line folded past every point of it where nothing leaves it or joins it. A lever is quiet when every link
// it has is a stretch of the company's line beside it, so that it takes no share of it and nothing of the
// company's leaves or joins the line beside it: no share across to its box, no line of its own into a bucket,
// no tax base, no condition, a condition counting as any link does (the office allowance beside a paid rent
// keeps an empty Expense allowances in its box). The royalty, beside the line, is quiet when it has no link at
// all: it comes to nothing; so is the partner, while the marital quotient attributes nothing, in a year taxed
// separately among others. A module holding what the map draws no line of (Holding) is never quiet: a
// household declaring dependants, whose supplements raise a tax-free amount taxed at its own rates, neither
// money nor a tax base, does something in the plan all the same. Read from every link of the run before a
// filter hides any, so hiding a kind of link folds nothing; a refused run has no link to read what its levers
// move, so it folds nothing. Nothing here tests a rule or computes a figure: it reads which lines the cards
// draw, and what the plan declares.
internal static class MapQuiet
{
    public static IReadOnlySet<RiverModule> Of(RiverRun run, IReadOnlyList<RiverModule> modules, IReadOnlyList<MapLink> links, IReadOnlySet<RiverModule> holding) =>
        run.Error is not null || run.Year is null
            ? new HashSet<RiverModule>()
            : modules.Where(module => RiverModules.Lever(module) is not null && !holding.Contains(module) && links.Where(link => link.Touches(module)).All(OnCompanysLine))
                .ToHashSet();

    // The modules holding what the map draws no line of: the household while it declares a dependant.
    public static IReadOnlySet<RiverModule> Holding(RiverInputs inputs) =>
        inputs.InPlan(OptionalModule.Household) && inputs.Household.Dependants.Values.AnyDeclared
            ? new HashSet<RiverModule> { RiverModule.Household }
            : new HashSet<RiverModule>();

    // The company's line past every point of it where nothing leaves it or joins it: the stretch into such a
    // point runs on to the next point where something does, or into the split, naming the levers it runs past
    // (MapLink.Through), with its own words, card and amount, a line of the card above them, which each of them
    // passes on whole; the stretches out of those points are left out, so the line says it once. No link is
    // left with an end at such a point.
    public static IReadOnlyList<MapLink> Fold(IReadOnlyList<MapLink> links)
    {
        var empty = links.Where(OnCompanysLine).Select(link => link.To).Where(end => end.Lever is not null)
            .Where(point => !links.Any(link => !OnCompanysLine(link) && (link.From == point || link.To == point))).ToHashSet();
        if (empty.Count == 0)
            return links;

        return
        [
            .. links.Where(link => !(OnCompanysLine(link) && empty.Contains(link.From)))
                .Select(link => OnCompanysLine(link) && empty.Contains(link.To) ? Past(link, links, empty) : link),
        ];
    }

    // The stretch into a run of points where nothing leaves or joins the line, on past them to the first point
    // where something does, or the split, which ends the line.
    private static MapLink Past(MapLink link, IReadOnlyList<MapLink> links, HashSet<MapEnd> empty)
    {
        var (to, through) = (link.To, new List<RiverModule>());
        while (empty.Contains(to))
        {
            through.Add(to.Lever!.Value);
            to = links.Single(next => OnCompanysLine(next) && next.From == to).To;
        }

        return link with { To = to, Through = [.. link.Through, .. through] };
    }

    // A stretch of the company's line: from Company year or a point of it, to a point of it or the split.
    private static bool OnCompanysLine(MapLink link) =>
        link.Kind == MapLinkKind.PassedOn && (link.From == MapEnd.Of(RiverModule.CompanyYear) || link.From.Lever is not null)
        && (link.To == MapEnd.Of(RiverModule.Split) || link.To.Lever is not null);
}
