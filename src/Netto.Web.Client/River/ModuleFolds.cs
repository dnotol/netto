using Netto.Core.Simulation;

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

    // The Composition menu's change: the river with the module in the plan or out of it. The card of a
    // module just added opens so it can be filled; one taken out keeps its fold, so Undo brings it back
    // as it was.
    public static RiverInputs Toggled(Folds folds, RiverInputs river, OptionalModule module, bool inPlan)
    {
        var changed = inPlan ? river.AddToPlan(module) : river.TakeOutOfPlan(module);
        RevealAdded(folds, river.Composition, changed.Composition);

        return changed;
    }

    // The Optimisation tab's « Apply »: the river with the move's values (MovesView.Apply). A move that grants a
    // lever the plan lacks adds its module, so its card opens for the user to see what the move put
    // there; one that only moves a value opens nothing.
    public static RiverInputs Applied(Folds folds, MovesView view, PlannedMove move, RiverInputs river)
    {
        var applied = view.Apply(move, river);
        RevealAdded(folds, river.Composition, applied.Composition);

        return applied;
    }
}
