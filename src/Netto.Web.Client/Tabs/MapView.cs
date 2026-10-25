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
