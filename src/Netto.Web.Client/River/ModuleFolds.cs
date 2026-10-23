namespace Netto.Web.Client.River;

// The folds of the optional modules' cards. A viewer's folds are remembered under the card's name, so
// the names are kept from one version to the next, and they are set here once for the card and for
// whoever opens it (Folds.Reveal) when its module joins the plan.
internal static class ModuleFolds
{
    public static string Name(OptionalModule module) => module switch
    {
        OptionalModule.Vouchers => "vouchers",
        OptionalModule.Allowances => "allowances",
        OptionalModule.Rent => "rent",
        OptionalModule.Car => "car",
        OptionalModule.Copyright => "copyright",
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, null),
    };

    // The modules the plan holds after a change that it did not hold before, in the river's order: the
    // cards that appear, and open so they can be filled.
    public static IReadOnlyList<OptionalModule> Added(PlanComposition before, PlanComposition after) =>
        [.. PlanComposition.Modules.Where(module => after.Holds(module) && !before.Holds(module))];

    // Opens, for the session, the cards a change just added to the plan: the user asked for them, from
    // the Composition menu or by applying a move. A card that comes back by Undo or with a plan opened
    // from Plans does not go through here, and keeps its fold as it was.
    public static void RevealAdded(Folds folds, PlanComposition before, PlanComposition after)
    {
        foreach (var module in Added(before, after))
            folds.Reveal(Name(module));
    }
}
