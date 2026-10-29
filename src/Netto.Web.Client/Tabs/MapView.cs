using System.Globalization;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the Map tab draws of the one run of the river: the modules the river shows with figures, each in
// its slot (MapLayout), the five buckets of the bar whatever the plan holds, and the links between them,
// each a line a card already shows (MapLinks), with its route. It only selects and regroups the cards' own
// figures; nothing here decides a tax outcome. While the engine refuses an amount the map has no link, as
// the bar has no figure, and the module holding the refused field is marked (RiverErrors.Owner); a Company
// year the viewer skipped stays on the map, marked skipped, with no link; a lever that moves no money of
// its own is quiet, in a quiet pill with no link of its own, the company's line running on past the point
// beside it. A module's box holds the badges of the conditions and the tax bases reaching it (MapBadges),
// read before any filter, and a gate's box the lines it says (MapGates). The columns are labelled by whose
// money they hold (Columns), and the pay crosses under its pill (Pill). A filter shows some links and leaves
// the others out as `view with { Links = shown }`: the modules, their badges, the routes and the amounts
// placed on them (MapAmounts) are the run's, so nothing moves. Units is the map's height in chain rows,
// which the stylesheet makes its height of.
internal sealed record MapView(IReadOnlyList<MapNode> Modules, IReadOnlyList<MapNode> Buckets, decimal Units)
{
    public IReadOnlyList<MapLink> Links { get; init; } = [];

    // The engine's refusal, which leaves the map without a link.
    public Refusal? Refusal { get; init; }

    // The bucket of what leaves the pay names the rent too once its tax is there, as the bar names it.
    public bool TaxesOnPayIncludeRent { get; init; }

    // The columns of the map, each with whose money it holds, framed and labelled over its top.
    public IReadOnlyList<(MapOwner Owner, MapSlot Slot)> Columns { get; init; } = [];

    // The pay's pill where the pay crosses the gates' column, and what it says; none without a pay.
    public MapPill? Pill { get; init; }

    public static MapView From(RiverInputs inputs, RiverRun run)
    {
        var modules = ModulesOf(inputs);
        var refused = run.Error is { } error ? RiverErrors.Owner(error, inputs.Company is null) : (RiverModule?)null;

        // The run's links, each a line of a card, then the conditions one module sets on another, as the
        // engine names them for the year, between modules on the map; a run without a year, refused or
        // with the company skipped, has none. The levers they leave moving no money of their own are quiet
        // (MapQuiet), the company's line folded past every point of it where nothing leaves it or joins it.
        // The conditions and the tax bases mark the modules they reach with badges (MapBadges), lines of their
        // boxes; what each gate says is a line of its box too (MapGates).
        IReadOnlyList<MapLink> links =
            [.. MapLinks.Of(inputs, run), .. run.Year is { } year ? MapConditions.From(YearConditions.Of(year), modules) : []];
        var quiet = MapQuiet.Of(run, modules, links, MapQuiet.Holding(inputs));
        var badges = MapBadges.Of(links);
        var gates = MapGates.Of(inputs, run);
        var grid = MapLayout.Grid(modules, quiet, MapBadges.Lines(badges), modules.ToDictionary(module => module, module => gates.For(module).Count));
        var routed = MapLayout.Routed(grid, MapQuiet.Fold(links));
        var pay = routed.FirstOrDefault(link => link.Kind == MapLinkKind.PassedOn && link.From == MapEnd.Of(RiverModule.Split) && link.To == MapEnd.Of(RiverModule.DirectorPay));

        return new MapView(
            [.. modules.Select(module => new MapNode(MapEnd.Of(module), grid.Slot(MapEnd.Of(module)))
            {
                Refused = module == refused,
                Skipped = module == RiverModule.CompanyYear && inputs.Company is null,
                Quiet = quiet.Contains(module),
                Badges = badges.GetValueOrDefault(module, []),
                Gate = MapLayout.Place(module).Column == MapColumn.Gates,
                Lines = gates.For(module),
            })],
            [.. BucketsView.Keys.Select(key => new MapNode(MapEnd.InBucket(key), grid.Slot(MapEnd.InBucket(key))))],
            grid.Units)
        {
            Links = routed,
            Refusal = run.Error,
            TaxesOnPayIncludeRent = BucketsView.From(run)?.TaxesOnPayIncludeRent ?? false,
            Columns = grid.Columns(),
            Pill = gates.Pay is { } words && pay?.Route is { } route ? new MapPill(MapLayout.PillOn(grid, route), words) : null,
        };
    }

    // The modules the map draws, in the river's order: the cards the river draws with figures, an
    // optional module only while it is in the plan, as the Plan page draws them (Pages/Plan); with the
    // company skipped, the cards before the pay and the company's are drawn waiting for its year and the map
    // leaves them out, but Company year stays, drawn skipped, so that it can be chosen to enter its year
    // again, and the map starts at Director pay, the partner beside it while in the plan, the pay taxed
    // jointly still. Cheap: it reads the inputs alone, so the page can clear a choice of a module that left
    // the map without running anything.
    public static IReadOnlyList<RiverModule> ModulesOf(RiverInputs inputs) => inputs.Company is null
        ? [RiverModule.CompanyYear, RiverModule.DirectorPay, .. inputs.InPlan(OptionalModule.Household) ? [RiverModule.Household] : Array.Empty<RiverModule>()]
        : [.. MapModules.Order.Where(module => RiverModules.Lever(module) is not { } lever || inputs.InPlan(lever))];

    // The module chosen, while the map draws it; none once it has left the map.
    public RiverModule? Choice(RiverModule? chosen) => chosen is { } module && Modules.Any(node => node.End.Module == module) ? module : null;

    // The modules as the map draws them, in the river's order: a box each, but the quiet levers, which
    // share one place where they follow one another down the company's line (MapLayout gives them one slot),
    // the royalty quiet in its own.
    public IReadOnlyList<MapPlace> Places()
    {
        var places = new List<MapPlace>();
        foreach (var node in Modules)
        {
            if (node.Quiet && places is [.., { Quiet: true } last] && last.Slot == node.Slot)
                places[^1] = last with { Nodes = [.. last.Nodes, node] };
            else
                places.Add(new MapPlace(node.Slot, [node]));
        }

        return places;
    }

    // The lines drawn, links of one route as one, in the order of the links.
    public IReadOnlyList<MapBundle> Bundles() =>
        [.. Links.Where(link => link.Route is not null).GroupBy(link => link.Route!).Select(bundle => new MapBundle(bundle.Key, [.. bundle]))];

    // The lines in the order they are drawn, each faded while another module is chosen: the chosen one's
    // last, over the others.
    public IReadOnlyList<(MapBundle Bundle, bool Faded)> Drawn(RiverModule? chosen) =>
        [.. Bundles().Select(bundle => (Bundle: bundle, Faded: chosen is { } module && !bundle.Touches(module))).OrderByDescending(line => line.Faded)];

    // The amounts the map draws, of those placed from every link of the run (MapAmounts.Of the run's map):
    // the lines this map shows, and, a module chosen, only its own lines', whichever card draws them, each
    // where it was placed, so hiding a kind of line or choosing a module moves none; the others' are left out
    // rather than faded, so choosing quiets the map.
    public IReadOnlyList<MapAmount> Amounts(IReadOnlyList<MapAmount> placed, RiverModule? chosen = null)
    {
        var shown = new HashSet<MapRoute>(Links.Select(link => link.Route).OfType<MapRoute>(), ReferenceEqualityComparer.Instance);
        return [.. placed.Where(amount => shown.Contains(amount.Bundle.Route) && (chosen is not { } module || amount.Bundle.Touches(module)))];
    }
}

// A module or a bucket of the map in its box; the module holding a refused field is marked, and so is a
// Company year the viewer skipped, and a lever that moves no money of its own in this plan (MapQuiet),
// whose slot is its quiet row's. A module's box holds the badges of the conditions and the tax bases
// reaching it (MapBadges), none on a bucket. A way across is a gate, its box holding the lines it says
// (MapGates).
internal sealed record MapNode(MapEnd End, MapSlot Slot)
{
    public bool Refused { get; init; }

    public bool Skipped { get; init; }

    public bool Quiet { get; init; }

    public IReadOnlyList<MapBadge> Badges { get; init; } = [];

    public bool Gate { get; init; }

    public IReadOnlyList<MapGateLine> Lines { get; init; } = [];
}

// The pay's pill: where the map draws it, on the pay's line in the gates' column, and what it says.
internal sealed record MapPill(MapSlot Slot, MapGateLine Words);

// Where the map draws modules: one box, or a quiet row of the quiet levers in it.
internal sealed record MapPlace(MapSlot Slot, IReadOnlyList<MapNode> Nodes)
{
    public bool Quiet => Nodes[0].Quiet;
}

// One line of the map: the links of one route, all lines of one card between the same two ends and of one
// kind. Its amount is their sum, each figure to the cent as its card draws it, so it is the sum a viewer
// makes of the card's lines: arithmetic of display, no tax figure. A line back out of a bucket is a route
// of its own, so what comes back is never taken off the way in. A condition has none.
internal sealed record MapBundle(MapRoute Route, IReadOnlyList<MapLink> Links)
{
    public MapLinkKind Kind => Links[0].Kind;

    public MapEnd From => Links[0].From;

    public MapEnd To => Links[0].To;

    public RiverModule Card => Links[0].Card;

    public decimal? Amount => Kind == MapLinkKind.Condition ? null : Sum(Links);

    // What links carry together, each figure to the cent as its card draws it: the sum a viewer makes of
    // the card's lines, the one a line and a badge say (MapBadges).
    public static decimal Sum(IEnumerable<MapLink> links) => links.Sum(link => Math.Round(link.Amount ?? 0m, 2, MidpointRounding.AwayFromZero));

    // Whether the line ends in an arrowhead: where it reaches a module, money passed on, what comes back out
    // of a bucket or a rule. A line into a bucket has none: it runs on under
    // the bucket (MapRoute.Drawn), which is where its money stops, so the pocket's lines side by side end
    // as lines, not as a row of arrowheads, and money passed on is told from money into a bucket by its end.
    public bool Arrowhead => EndsInArrowhead(From, To);

    // Whether a line between two ends ends in an arrowhead: into a module, or into the company's line from a
    // bucket, the car's contribution joining it; none into a bucket, nor from one point of the company's line
    // to the next, a stretch of one line.
    public static bool EndsInArrowhead(MapEnd from, MapEnd to) => to.Bucket is null && (to.Lever is null || from.Bucket is not null);

    // Whether the map says the line's amount on it: every line but a gate's into the pocket, which the gate's
    // box says (MapGates), the pay, which its pill says, and what Corporate tax adds back of a gate, which its
    // badge says, the sum its tax base gains (MapBadges).
    public bool SaysAmount =>
        !(To.Bucket == "pocket" && From.Module is { } gate && MapLayout.Place(gate).Column == MapColumn.Gates)
        && !(Kind == MapLinkKind.PassedOn && From == MapEnd.Of(RiverModule.Split) && To == MapEnd.Of(RiverModule.DirectorPay))
        && !(Kind == MapLinkKind.TaxBase && To == MapEnd.Of(RiverModule.CorporateTax));

    public bool Touches(RiverModule module) => Links[0].Touches(module);
}

// What a link of the map carries, told apart at a glance: money passed on to the next module (the company's
// line and a lever's share of it, the split's shares, a royalty's part beyond its limits, taxed as pay), money
// ending in a
// bucket (a car's costs among the expenses) or coming back out of one (a refund, a credit, the
// director's contribution to a car), what only changes a tax base (a car's benefit taxed with the pay,
// what Corporate tax adds back), and a condition one module sets on another. Public because the map's
// filter, a component, takes counts keyed by it.
public enum MapLinkKind
{
    PassedOn,

    IntoBucket,

    TaxBase,

    Condition,
}

// How each kind of link is drawn, told apart at a glance with no colour needed: its dash, none for a solid
// line, and its ink. Money passed on is solid and strong, money into a bucket solid and light, what only
// changes a tax base dotted, a condition dashed; no two kinds share a style. A line's width is in pixels,
// which it keeps however the map stretches (vector-effect): a line of money as thick as its amount
// (MapLineWidths), a line carrying none its kind's (Fixed). The map's lines and arrowheads and the
// filter's samples read it, no stylesheet keeping an ink of its own for them. Every line is drawn over a
// halo of the map's ground, its width and a gap each side (HaloGap), so a line drawn over another leaves a
// gap on each side of it and the crossing reads as over and under.
internal static class MapLinkStyle
{
    // The gap, in pixels, the colour of the map's ground on each side of every line.
    public const decimal HaloGap = 2m;

    public static string? Dash(MapLinkKind kind) => kind switch
    {
        MapLinkKind.PassedOn or MapLinkKind.IntoBucket => null,
        MapLinkKind.TaxBase => "1 4",
        MapLinkKind.Condition => "7 5",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A kind of link the map has no style for."),
    };

    // The ink a kind's lines, their arrowheads and the legend's sample of them are drawn in, the one place
    // it is set: money passed on the strongest, money into a bucket and a tax base lighter, a condition
    // muted, each a token of both themes. What reaches the pocket is the map's one gold.
    public static string Ink(MapLinkKind kind) => kind switch
    {
        MapLinkKind.PassedOn => "var(--netto-data-strong)",
        MapLinkKind.IntoBucket or MapLinkKind.TaxBase => "var(--netto-data-high)",
        MapLinkKind.Condition => "var(--netto-text-muted)",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A kind of link the map has no ink for."),
    };

    public const string PocketInk = "var(--netto-gold)";

    // How the legend's sample of a kind ends, as the lines it stands for end on the map (MapBundle.Arrowhead):
    // money passed on, a tax base and a condition in an arrowhead on the module a line reaches; money into a
    // bucket in a bar its sample runs into, the bucket's edge, as a line of that kind ending in a bucket runs
    // on under it. A line of that kind that comes back out of a bucket ends in an arrowhead instead, which
    // the kind's words in the legend say (MapFilter.resx).
    public static MapLineEnd SampleEnd(MapLinkKind kind) => kind switch
    {
        MapLinkKind.IntoBucket => MapLineEnd.Bucket,
        MapLinkKind.PassedOn or MapLinkKind.TaxBase or MapLinkKind.Condition => MapLineEnd.Arrowhead,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A kind of link the map has no end for."),
    };

    // The bar the legend's sample of money into a bucket runs into: a bucket's edge, in its frame's ink.
    public const string BucketEdge = "var(--netto-line-input)";

    // The width of a line that carries no money, which it keeps whatever the plan: a tax base's, a
    // condition's; none for a line of money, as thick as its amount.
    public static decimal? Fixed(MapLinkKind kind) => kind switch
    {
        MapLinkKind.PassedOn or MapLinkKind.IntoBucket => null,
        MapLinkKind.TaxBase => 2m,
        MapLinkKind.Condition => 1.75m,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A kind of link the map has no style for."),
    };

    // Whether a kind carries money, a line as thick as its amount, which is a kind with no width of its own;
    // every other kind is a rule. The one place that tells them apart: the lines' widths, the placing of the
    // amounts and what a module receives all ask it here.
    public static bool IsMoney(MapLinkKind kind) => Fixed(kind) is null;

    // The width of the legend's sample of a kind: its lines' own, or, a kind of money, a sample's, one width
    // for both, saying the kind rather than an amount; their ends tell them apart (SampleEnd).
    public static string Sample(MapLinkKind kind) => Pixels(Fixed(kind) ?? MoneySample);

    private const decimal MoneySample = 3m;

    // A line's width as the map draws it, its route's (MapLineWidths).
    public static string Width(decimal width) => Pixels(width);

    // The halo a line is drawn over: the line's width and the gap each side, the one rule for every line.
    public static string Halo(decimal width) => Pixels(width + 2 * HaloGap);

    private static string Pixels(decimal pixels) => pixels.ToString("0.##", CultureInfo.InvariantCulture);
}

// How a line ends: an arrowhead where it reaches a module, or under the bucket it runs into.
internal enum MapLineEnd
{
    Arrowhead,

    Bucket,
}

// One end of a link: a module of the map, a bucket of the bar (BucketsView.Keys), or the point of the
// company's line beside a lever before the split (Lever), where the lever takes its share of what the company
// has left, and where what the company pays or receives for it leaves or joins that line. Id names it in the
// map's elements: a module by its card's fold name, a bucket by its key, a point of the company's line by
// its lever's.
internal sealed record MapEnd
{
    private MapEnd(RiverModule? module, string? bucket, RiverModule? lever) => (Module, Bucket, Lever) = (module, bucket, lever);

    public RiverModule? Module { get; }

    public string? Bucket { get; }

    // The lever before the split whose point of the company's line this is, none for a module or a bucket.
    public RiverModule? Lever { get; }

    public string Id => (Module, Bucket, Lever) switch
    {
        ({ } module, _, _) => MapModules.Name(module),
        (_, { } bucket, _) => bucket,
        (_, _, { } lever) => $"company-line-{MapModules.Name(lever)}",
        _ => throw new InvalidOperationException("An end with neither a module, a bucket nor a lever."),
    };

    public static MapEnd Of(RiverModule module) => new(module, null, null);

    public static MapEnd InBucket(string key) => BucketsView.Keys.Contains(key)
        ? new(null, key, null)
        : throw new ArgumentOutOfRangeException(nameof(key), key, "A bucket the bar does not have.");

    // The point of the company's line beside a lever before the split: none beside any other module.
    public static MapEnd Beside(RiverModule lever) => MapModules.CompanysLine.Contains(lever) && lever is not (RiverModule.CompanyYear or RiverModule.Split)
        ? new(null, null, lever)
        : throw new ArgumentOutOfRangeException(nameof(lever), lever, "A module the company's line does not run beside.");
}

// Whose texts word a link: the map's own, a drain list's, or the card whose figure line it is.
internal enum MapText
{
    Map,

    DrainList,

    CompanyYearCard,

    SplitCard,

    CarCard,

    VoucherCard,

    AllowanceCard,

    RentCard,

    HouseholdCard,
}

// What a link carries: the key of its words in those texts, with their arguments already formatted, as a
// figure line's are (FigureLine.Args); the words its card draws for the same line.
internal sealed record MapLabel(MapText Text, string Key, IReadOnlyList<string> Args);

// One link of the map: its kind, where it runs from and to, the module whose card shows the line it is
// (Card), what it carries and its amount in this plan, the card's own figure; none for a condition, which
// carries the word or two its line draws (State) and no sentence, the cards saying those with their figures
// (MapConditions). A condition says whether it holds (Holds), and in a word or two (State), none for any
// other link, and the badge it leaves on the module it acts on (Badge), none for the office rule, which
// always holds. Its line is its route (MapLayout), shared with the links of the same
// ends and kind, worked out from every link of the run, none until the layout has routed it. The company's
// line past the points of it where nothing leaves or joins it names their levers (Through, in the river's
// order), and touches each of them as it touches the levers beside its two ends.
internal sealed record MapLink(MapLinkKind Kind, MapEnd From, MapEnd To, RiverModule Card, MapLabel Carries, decimal? Amount)
{
    public bool? Holds { get; init; }

    public MapLabel? State { get; init; }

    public MapBadge? Badge { get; init; }

    public MapRoute? Route { get; init; }

    public IReadOnlyList<RiverModule> Through { get; init; } = [];

    public bool Touches(RiverModule module) =>
        From.Module == module || To.Module == module || From.Lever == module || To.Lever == module || Through.Contains(module);
}

// The river's modules as the map places them, in the river's order,
// each by its card's fold name, which the map's elements are named after. A module the river gains is
// added here, or the map fails a test (MapModulesTests).
internal static class MapModules
{
    // The company's line, top to bottom: from Company year past the levers before the split, each taking its
    // share of it, into the split.
    public static IReadOnlyList<RiverModule> CompanysLine { get; } =
        [RiverModule.CompanyYear, RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent, RiverModule.Car, RiverModule.Split];

    public static IReadOnlyList<RiverModule> Order { get; } =
    [
        RiverModule.CompanyYear,
        RiverModule.Vouchers,
        RiverModule.Allowances,
        RiverModule.Rent,
        RiverModule.Car,
        RiverModule.Split,
        RiverModule.Copyright,
        RiverModule.DirectorPay,
        RiverModule.Household,
        RiverModule.CorporateTax,
        RiverModule.Dividend,
    ];

    // The module a card's fold name names, as a link to the plan names it (CardLinks), none for a name no
    // module has.
    public static RiverModule? Named(string card) => Order.Where(module => Name(module) == card).Cast<RiverModule?>().FirstOrDefault();

    // The card's fold name, which the map's elements and a link to the plan name it by.
    public static string Name(RiverModule module) => module switch
    {
        RiverModule.CompanyYear => "company-year",
        RiverModule.Split => "split",
        RiverModule.DirectorPay => "director-pay",
        RiverModule.CorporateTax => "corporate-tax",
        RiverModule.Dividend => "dividend",
        _ when RiverModules.Lever(module) is { } lever => ModuleFolds.Name(lever),
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, "A module the map has no place for."),
    };

    // The card an optional module is.
    public static RiverModule Of(OptionalModule lever) => lever switch
    {
        OptionalModule.Vouchers => RiverModule.Vouchers,
        OptionalModule.Allowances => RiverModule.Allowances,
        OptionalModule.Rent => RiverModule.Rent,
        OptionalModule.Car => RiverModule.Car,
        OptionalModule.Copyright => RiverModule.Copyright,
        OptionalModule.Household => RiverModule.Household,
        _ => throw new ArgumentOutOfRangeException(nameof(lever), lever, "A lever the map has no place for."),
    };

}
