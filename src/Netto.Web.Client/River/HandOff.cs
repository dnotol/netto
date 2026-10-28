using Netto.Core.Simulation;
using Netto.Web.Client.River.CompanyCar;

namespace Netto.Web.Client.River;

// What each lever before the split hands on down the river, from the year's figures: the vouchers, the
// expense allowances, the rent and the car each receive what the levers above leave of what the company
// earns before it pays its director anything (Before), take what they cost the company (Cost) and pass
// on the rest (After), the last of them what the split shares between the pay, a royalty and the company
// (Shared). A module out of the plan costs nothing: the scenario hands the engine none of it.
internal static class HandOff
{
    // The levers before the split, in the river's order.
    private static readonly OptionalModule[] Levers = [OptionalModule.Vouchers, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car];

    // What a module takes off what the company earns: what reaches the pocket of the vouchers and the
    // allowances, the rent, the car's costs less the director's contribution, and the royalty the split
    // pays beside the pay; nothing for the partner, which changes only how the pay is taxed.
    public static decimal Cost(YearResult year, OptionalModule module) => module switch
    {
        OptionalModule.Vouchers => year.Vouchers.Total,
        OptionalModule.Allowances => year.Steps.Where(step => step.Event == TaxableEvent.ExpenseAllowances && step.Track == Track.InPocket).Sum(step => step.Amount),
        OptionalModule.Rent => year.Rent.Rent,
        OptionalModule.Car => CarView.FromProfit(year.Car),
        OptionalModule.Copyright => year.Copyright.Royalty,
        OptionalModule.Household => 0m,
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, "A module that takes nothing off what the company earns."),
    };

    // What the split shares: the profit before pay, with the royalty it pays beside the pay.
    public static decimal Shared(YearResult year) => year.ProfitBeforePay + Cost(year, OptionalModule.Copyright);

    // What a lever passes on: what the split shares and what the levers after it take.
    public static decimal After(YearResult year, OptionalModule lever) =>
        Shared(year) + Levers.Skip(IndexOf(lever) + 1).Sum(next => Cost(year, next));

    // What a lever receives: what it passes on and what it takes.
    public static decimal Before(YearResult year, OptionalModule lever) => After(year, lever) + Cost(year, lever);

    // The line a lever's card ends on: the last lever before the split hands it the profit before pay,
    // and the royalty too while Copyright royalty is in the plan; any other says what is left after it,
    // on to the next lever in the plan.
    public static Drain Drain(YearResult year, OptionalModule lever, PlanComposition composition) => NextModule.After(lever, composition) switch
    {
        "split" => new Drain(composition.Copyright ? "profit_before_pay_and_royalty" : "profit_before_pay", DrainKind.PassedOn, After(year, lever), "split"),
        var next => new Drain(LeftAfter(lever), DrainKind.PassedOn, After(year, lever), next),
    };

    private static int IndexOf(OptionalModule lever) => Array.IndexOf(Levers, lever) is var index and >= 0
        ? index
        : throw new ArgumentOutOfRangeException(nameof(lever), lever, "A module after the split hands nothing on.");

    // A lever another follows before the split; the car is always the last.
    private static string LeftAfter(OptionalModule lever) => lever switch
    {
        OptionalModule.Vouchers => "left_after_vouchers",
        OptionalModule.Allowances => "left_after_allowances",
        OptionalModule.Rent => "left_after_rent",
        _ => throw new ArgumentOutOfRangeException(nameof(lever), lever, "A lever no other follows before the split."),
    };
}
