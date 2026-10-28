using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// The names of the optional modules' cards. A viewer's folds inside a card are remembered under its name,
// and the map's elements and a link to the plan name it by them, so the names are kept from one version to
// the next.
internal static class ModuleFolds
{
    public static string Name(OptionalModule module) => module switch
    {
        OptionalModule.Vouchers => "vouchers",
        OptionalModule.Allowances => "allowances",
        OptionalModule.Rent => "rent",
        OptionalModule.Car => "car",
        OptionalModule.Copyright => "copyright",
        OptionalModule.Household => "household",
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, null),
    };

    // The modules the plan holds after a change that it did not hold before, in the river's order: the
    // levers the viewer just asked for, from « Add to plan » or by applying a move, whose card the
    // plan then chooses on the map to be filled.
    public static IReadOnlyList<OptionalModule> Added(PlanComposition before, PlanComposition after) =>
        [.. PlanComposition.Modules.Where(module => after.Holds(module) && !before.Holds(module))];
}
