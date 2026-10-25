using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the Map tab draws of the one run of the river: the modules the river shows with figures, each in
// its slot (MapLayout), the four buckets of the bar whatever the plan holds, and the links between them,
// each a line a card already shows (MapLinks), with its route. It only selects and regroups the cards' own
// figures; nothing here decides a tax outcome. While the engine refuses an amount the map has no link, as
// the bar has no figure, and the module holding the refused field is marked (RiverErrors.Owner). A filter
// shows some links and hides the others as `view with { Links = shown, Hidden = hidden }`: the modules
// and the routes are the run's, so nothing moves. Units is the map's height in chain rows, which the
// stylesheet makes its height of.
internal sealed record MapView(IReadOnlyList<MapNode> Modules, IReadOnlyList<MapNode> Buckets, decimal Units)
{
    // How far a card's words stand from its box.
    private const decimal BlockGap = 1m;

    public IReadOnlyList<MapLink> Links { get; init; } = [];

    // The links a filter hides, still each module's.
    public IReadOnlyList<MapLink> Hidden { get; init; } = [];

    // The engine's refusal, which leaves the map without a link.
    public Refusal? Refusal { get; init; }

    // The bucket of what leaves the pay names the rent too once its tax is there, as the bar names it.
    public bool TaxesOnPayIncludeRent { get; init; }

    public static MapView From(RiverInputs inputs, RiverRun run)
    {
        var modules = ModulesOf(inputs);
        var grid = MapLayout.Grid(modules);
        var refused = run.Error is { } error ? RiverErrors.Owner(error, inputs.Company is null) : (RiverModule?)null;

        // The run's links, each a line of a card, then the conditions one module sets on another, as the
        // engine names them for the year, between modules on the map; a run without a year, refused or
        // with the company skipped, has none.
        IReadOnlyList<MapLink> links =
            [.. MapLinks.Of(inputs, run), .. run.Year is { } year ? MapConditions.From(YearConditions.Of(year), modules) : []];

        return new MapView(
            [.. modules.Select(module => new MapNode(MapEnd.Of(module), grid.Slot(MapEnd.Of(module))) { Refused = module == refused })],
            [.. BucketsView.Keys.Select(key => new MapNode(MapEnd.InBucket(key), grid.Slot(MapEnd.InBucket(key))))],
            grid.Units)
        {
            Links = MapLayout.Routed(grid, links),
            Refusal = run.Error,
            TaxesOnPayIncludeRent = BucketsView.From(run)?.TaxesOnPayIncludeRent ?? false,
        };
    }

    // The modules the map draws, in the river's order: the cards the river draws with figures, an
    // optional module only while it is in the plan, as the Plan page draws them (Pages/Plan); with the
    // company skipped, the cards before the pay and the company's are drawn waiting for its year, and the
    // map starts at Director pay. Cheap: it reads the inputs alone, so the page can clear a choice of a
    // module that left the map without running anything.
    public static IReadOnlyList<RiverModule> ModulesOf(RiverInputs inputs) => inputs.Company is null
        ? [RiverModule.DirectorPay]
        : [.. MapModules.Order.Where(module => RiverModules.Lever(module) is not { } lever || inputs.InPlan(lever))];

    // A module's links the map shows: what comes into it and what goes out.
    public IEnumerable<MapLink> LinksOf(RiverModule module) => Links.Where(link => link.Touches(module));

    public IEnumerable<MapLink> HiddenOf(RiverModule module) => Hidden.Where(link => link.Touches(module));

    // The module chosen, while the map draws it; none once it has left the map.
    public RiverModule? Choice(RiverModule? chosen) => chosen is { } module && Modules.Any(node => node.End.Module == module) ? module : null;

    // The lines drawn, links of one route as one, in the order of the links.
    public IReadOnlyList<MapBundle> Bundles() =>
        [.. Links.Where(link => link.Route is not null).GroupBy(link => link.Route!).Select(bundle => new MapBundle(bundle.Key, [.. bundle]))];

    // The lines in the order they are drawn, each faded while another module is chosen: the chosen one's
    // last, over the others.
    public IReadOnlyList<(MapBundle Bundle, bool Faded)> Drawn(RiverModule? chosen) =>
        [.. Bundles().Select(bundle => (Bundle: bundle, Faded: chosen is { } module && !bundle.Touches(module))).OrderByDescending(line => line.Faded)];

    // Where each line's words are drawn: a card's lines leaving one side of its box together, beside that
    // side, in the middle of its height; a line between two boxes one over the other beside itself, halfway
    // down the gap.
    public IReadOnlyList<MapLabelBlock> Blocks()
    {
        var bundles = Bundles();
        var gaps = bundles.Where(bundle => bundle.Route.Side == MapLabelSide.Gap)
            .Select(bundle => new MapLabelBlock(bundle.Card, MapLabelSide.Gap, bundle.Route.Beside, [bundle]));
        var sides = bundles.Where(bundle => bundle.Route.Side != MapLabelSide.Gap)
            .GroupBy(bundle => (bundle.Card, bundle.Route.Side))
            .Select(block => new MapLabelBlock(block.Key.Card, block.Key.Side, BesideBox(block.Key.Card, block.Key.Side), [.. block]));

        return [.. sides, .. gaps];
    }

    private MapPoint BesideBox(RiverModule card, MapLabelSide side)
    {
        var slot = Modules.Single(node => node.End.Module == card).Slot;
        return new MapPoint(side == MapLabelSide.Left ? slot.Left - BlockGap : slot.Right + BlockGap, slot.CenterY);
    }
}

// A module or a bucket of the map in its box; the module holding a refused field is marked.
internal sealed record MapNode(MapEnd End, MapSlot Slot)
{
    public bool Refused { get; init; }
}

// One line of the map: the links of one route, one after the other in its words.
internal sealed record MapBundle(MapRoute Route, IReadOnlyList<MapLink> Links)
{
    public MapLinkKind Kind => Links[0].Kind;

    public MapEnd From => Links[0].From;

    public MapEnd To => Links[0].To;

    public RiverModule Card => Links[0].Card;

    public bool Touches(RiverModule module) => Links[0].Touches(module);
}

// The words of a card's lines drawn together beside one side of its box (At, the side's middle, the
// words ending there on the left, starting there on the right), or of one line beside itself in a gap
// (At, where the words start).
internal sealed record MapLabelBlock(RiverModule Card, MapLabelSide Side, MapPoint At, IReadOnlyList<MapBundle> Bundles);

// What a link of the map carries, told apart at a glance: money passed on to the next module (a lever's
// hand-off, the split's shares, a royalty's part beyond its limits, taxed as pay), money ending in a
// bucket or leaving the river (a car's costs) or coming back out of one (a refund, a credit, the
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
// line, and its width in pixels, which a line keeps however the map stretches (vector-effect). Money
// passed on is solid and heavy, money into a bucket solid and light, what only changes a tax base dotted,
// a condition dashed; no two kinds share a style. The map's lines and the filter's swatches read it.
internal static class MapLinkStyle
{
    public static string? Dash(MapLinkKind kind) => kind switch
    {
        MapLinkKind.PassedOn or MapLinkKind.IntoBucket => null,
        MapLinkKind.TaxBase => "1 4",
        MapLinkKind.Condition => "7 5",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A kind of link the map has no style for."),
    };

    public static string Width(MapLinkKind kind) => kind switch
    {
        MapLinkKind.PassedOn => "2.5",
        MapLinkKind.IntoBucket => "1.25",
        MapLinkKind.TaxBase => "2",
        MapLinkKind.Condition => "1.75",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A kind of link the map has no style for."),
    };
}

// One end of a link: a module of the map, a bucket of the bar (BucketsView.Keys), or the river's edge,
// where a cost of the company leaves it. Id names it in the map's elements: a module by its card's fold
// name, a bucket by its key.
internal sealed record MapEnd
{
    private MapEnd(RiverModule? module, string? bucket) => (Module, Bucket) = (module, bucket);

    public RiverModule? Module { get; }

    public string? Bucket { get; }

    public static MapEnd OutOfRiver { get; } = new(null, null);

    public bool IsOut => Module is null && Bucket is null;

    public string Id => Module is { } module ? MapModules.Name(module) : Bucket ?? "out";

    public static MapEnd Of(RiverModule module) => new(module, null);

    public static MapEnd InBucket(string key) => BucketsView.Keys.Contains(key)
        ? new(null, key)
        : throw new ArgumentOutOfRangeException(nameof(key), key, "A bucket the bar does not have.");
}

// Whose texts word a link: the map's own, a drain list's, or the card whose figure line it is.
internal enum MapText
{
    Map,

    DrainList,

    SplitCard,

    CarCard,

    VoucherCard,
}

// What a link carries: the key of its words in those texts, with their arguments already formatted, as a
// figure line's are (FigureLine.Args); the words its card draws for the same line.
internal sealed record MapLabel(MapText Text, string Key, IReadOnlyList<string> Args);

// One link of the map: its kind, where it runs from and to, the module whose card shows the line it is
// (Card), what it carries and its amount in this plan, the card's own figure; none for a condition, which
// names its figures in its words. A condition says whether it holds (Holds), none for any other link. Its
// line is its route (MapLayout), shared with the links of the same ends and kind, worked out from every
// link of the run, none until the layout has routed it.
internal sealed record MapLink(MapLinkKind Kind, MapEnd From, MapEnd To, RiverModule Card, MapLabel Carries, decimal? Amount)
{
    public bool? Holds { get; init; }

    public MapRoute? Route { get; init; }

    public bool Touches(RiverModule module) => From.Module == module || To.Module == module;
}

// The river's modules as the map places them, in the river's order, the outline's (OutlineRows.Order),
// each by its card's fold name, which the map's elements are named after. A module the river gains is
// added here, or the map fails a test (MapModulesTests).
internal static class MapModules
{
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
        RiverModule.CorporateTax,
        RiverModule.Dividend,
    ];

    // The card's fold name, its row's name in the river's outline.
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
        _ => throw new ArgumentOutOfRangeException(nameof(lever), lever, "A lever the map has no place for."),
    };

}
