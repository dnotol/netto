namespace Netto.Web.Client.River;

// The optional modules of the river, in its order: levers a plan holds only once the user adds them,
// all before the split but the copyright royalty, which the split pays beside the pay and which runs at
// the top of the director's stream after it; and the partner, a spouse or legal cohabitant the director
// is taxed jointly with, beside Director pay, no lever: it takes nothing off what the company earns and
// hands nothing to the pay. The core modules, Company year, the split, Director pay, Corporate tax and
// Dividend, are always in it. Public because RiverInputs, a parameter of the cards, holds them.
public enum OptionalModule
{
    Vouchers,
    Allowances,
    Rent,
    Car,
    Copyright,

    Household,
}

// Which optional modules are in the plan, one of the plan's inputs. A module out of the plan neither
// shows nor counts, so every figure traces back to a card on screen; its values stay in the river's
// inputs meanwhile, and come back with it.
public sealed record PlanComposition(bool Vouchers, bool Allowances, bool Rent, bool Car, bool Copyright, bool Household)
{
    public static PlanComposition None { get; } = new(false, false, false, false, false, false);

    // Every optional module, in the river's order, in the plan or not.
    public static IReadOnlyList<OptionalModule> Modules { get; } = Enum.GetValues<OptionalModule>();

    public int Count => Modules.Count(Holds);

    public bool Holds(OptionalModule module) => module switch
    {
        OptionalModule.Vouchers => Vouchers,
        OptionalModule.Allowances => Allowances,
        OptionalModule.Rent => Rent,
        OptionalModule.Car => Car,
        OptionalModule.Copyright => Copyright,
        OptionalModule.Household => Household,
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, null),
    };

    public PlanComposition With(OptionalModule module, bool inPlan) => module switch
    {
        OptionalModule.Vouchers => this with { Vouchers = inPlan },
        OptionalModule.Allowances => this with { Allowances = inPlan },
        OptionalModule.Rent => this with { Rent = inPlan },
        OptionalModule.Car => this with { Car = inPlan },
        OptionalModule.Copyright => this with { Copyright = inPlan },
        OptionalModule.Household => this with { Household = inPlan },
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, null),
    };

    // Where what a lever before the split leaves goes on to: the next one in the plan down the river, or
    // none when the split comes next. Never the royalty, after the split, nor the partner, beside the
    // pay, and nothing goes on from them.
    public OptionalModule? NextAfter(OptionalModule module) =>
        Modules.Where(next => next > module && next is not (OptionalModule.Copyright or OptionalModule.Household) && Holds(next))
            .Select(next => (OptionalModule?)next).FirstOrDefault();

    // The module this composition no longer holds since the one before, if one was taken out.
    public OptionalModule? TakenOutSince(PlanComposition before) =>
        Modules.Where(module => before.Holds(module) && !Holds(module)).Select(module => (OptionalModule?)module).FirstOrDefault();
}
