using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Web.Client.River.Copyright;

namespace Netto.Web.Client.River;

// What the Composition menu draws: how many optional modules the plan holds, and each one in the
// river's order, whether it is in the plan, what it brings to the pocket while it is, read from its
// card's own view of the one run, and whether values of the user's wait for it while it is out. It
// brings nothing to show while there is no year to read it from or it brings nothing yet; values wait
// once one of its amounts is not nothing.
internal sealed record CompositionView(int Count, IReadOnlyList<ModuleChoice> Modules)
{
    public static CompositionView From(RiverInputs inputs, RiverRun run) =>
        new(inputs.Composition.Count, [.. PlanComposition.Modules.Select(module => Choice(inputs, run.Year, module))]);

    private static ModuleChoice Choice(RiverInputs inputs, YearResult? year, OptionalModule module)
    {
        var inPlan = inputs.InPlan(module);
        var toPocket = inPlan && year is not null ? ToPocket(year, inputs.Composition, module) : 0m;

        return new ModuleChoice(module, inPlan, toPocket == 0m ? null : toPocket, !inPlan && HasValues(inputs, module));
    }

    // What the module's card says it brings: the vouchers and the allowances whole, the rent once its
    // tax is paid, the car what it costs the company a year, the royalty its part within the limits once its
    // tax apart is paid, and the partner what the marital quotient saves the pay's tax.
    internal static decimal ToPocket(YearResult year, PlanComposition composition, OptionalModule module) => module switch
    {
        OptionalModule.Vouchers => VoucherView.From(year, composition).Total,
        OptionalModule.Allowances => AllowanceView.From(year, composition).Total,
        OptionalModule.Rent => RentView.From(year, composition).AfterTax,
        OptionalModule.Copyright => CopyrightView.From(year).AfterTax,
        OptionalModule.Car => year.Car.Costs,
        OptionalModule.Household => HouseholdView.From(year.DirectorPay, year.Scenario.Household.BothAged66)?.QuotientSaves ?? 0m,
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, "A module the Composition menu cannot say the pocket of."),
    };

    internal static bool HasValues(RiverInputs inputs, OptionalModule module) => module switch
    {
        OptionalModule.Vouchers => inputs.Vouchers.Values.Any,
        OptionalModule.Allowances => inputs.Allowances.Values.Any,
        OptionalModule.Rent => inputs.Rent.Values != HomeOfficeRent.None,
        OptionalModule.Copyright => inputs.Copyright.Values != RiverInputs.Example.Copyright.Values,
        OptionalModule.Car => inputs.Car.Values != RiverInputs.Example.Car.Values,
        OptionalModule.Household => inputs.Household.Values != RiverInputs.Example.Household.Values,
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, "A module the Composition menu cannot say the values of."),
    };
}

// One switch of the menu. ToPocket is null when there is nothing to say the module brings.
internal sealed record ModuleChoice(OptionalModule Module, bool InPlan, decimal? ToPocket, bool ValuesKept);

// Which module the notice of a removal speaks of once the plan's composition went from before to
// after: the module just taken out, else the one it spoke of while that one is still out. Back in the
// plan, from the menu or the notice's undo, it leaves nothing to undo.
internal static class RemovalNotice
{
    public static OptionalModule? About(OptionalModule? shown, PlanComposition before, PlanComposition after) =>
        after.TakenOutSince(before) ?? (shown is { } module && !after.Holds(module) ? module : null);
}
