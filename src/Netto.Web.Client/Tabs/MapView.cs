using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the Map tab draws of the one run of the river: the modules the river shows with figures, the four
// buckets of the bar, and the links between them, each a line a card already shows. It only selects and
// regroups the cards' own figures; nothing here decides a tax outcome.
internal sealed record MapView
{
    // The modules the map draws, in the river's order: the cards the river draws with figures, an
    // optional module only while it is in the plan, as the Plan page draws them (Pages/Plan); with the
    // company skipped, the cards before the pay and the company's are drawn waiting for its year, and the
    // map starts at Director pay. Cheap: it reads the inputs alone, so the page can clear a choice of a
    // module that left the map without running anything.
    public static IReadOnlyList<RiverModule> ModulesOf(RiverInputs inputs) => inputs.Company is null
        ? [RiverModule.DirectorPay]
        : [.. MapModules.Order.Where(module => MapModules.Lever(module) is not { } lever || inputs.InPlan(lever))];
}

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
// names its figures in its words. A condition says whether it holds (Holds), none for any other link.
internal sealed record MapLink(MapLinkKind Kind, MapEnd From, MapEnd To, RiverModule Card, MapLabel Carries, decimal? Amount)
{
    public bool? Holds { get; init; }
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
        _ when Lever(module) is { } lever => ModuleFolds.Name(lever),
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

    // The optional module a card is, none for a core card.
    public static OptionalModule? Lever(RiverModule module) => module switch
    {
        RiverModule.Vouchers => OptionalModule.Vouchers,
        RiverModule.Allowances => OptionalModule.Allowances,
        RiverModule.Rent => OptionalModule.Rent,
        RiverModule.Car => OptionalModule.Car,
        RiverModule.Copyright => OptionalModule.Copyright,
        _ => null,
    };
}
