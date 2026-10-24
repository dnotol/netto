using Netto.Core.CompanyCar;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.River.CompanyCar;

// What the company car costs the company, paid out of the river as a cost of the company like its
// running costs (CompanyCosts, operator, 2026-10-23), and the director's contribution coming back from
// their pocket, then what it leaves, passed on to the split, the last lever before it, as what the pay,
// the company and a royalty share there, from the year's car steps; what it receives is what the levers
// above leave of what the company earns. The card reads the car as the engine took it (Result): its benefit, period by period,
// at its CO2 percentage, at least the minimum, less the contribution, taxed as pay, what the company
// deducts of its costs, and what it adds back of the benefit, 40% when it pays the private energy,
// which for a car it charges, electric or plug-in hybrid, is the administration's reading of « fuel »
// (ChargingReading). Each line says its share of what the card receives, and the card what it passes
// on of it (PassesOn). Without a car, nothing but the profit passed on.
internal sealed record CarView(
    decimal Received,
    CarResult Result,
    bool ChargingReading,
    decimal? PassesOn,
    IReadOnlyList<Drain> Drains,
    IReadOnlyList<StepDetail> Details)
{
    // Where the car's costs go: out of the river, a cost of the company the bar of buckets leaves out.
    public const string CompanyCosts = "company_costs";

    public bool HasCar => Details.Count > 0;

    // What the split shares once the car is paid, the royalty in it, which the summary states: the same
    // at every pay, while the profit before pay alone moves with a royalty that follows the pay.
    public decimal SplitShares { get; init; }

    // What the car takes off what the company earns: its costs, less what the director pays for it.
    public static decimal FromProfit(CarResult car) => car.Costs - car.Contribution;

    public static CarView From(YearResult year, PlanComposition composition)
    {
        var ledger = year.Steps.Where(step => step.Event == TaxableEvent.CompanyCar).ToList();
        var steps = ledger.ToDictionary(step => step.RuleKey);
        var car = year.Car;
        var left = HandOff.After(year, OptionalModule.Car);
        var received = HandOff.Before(year, OptionalModule.Car);
        IEnumerable<Drain> drains = steps.Count > 0
            ?
            [
                new Drain("car_costs", DrainKind.PaidOut, car.Costs, CompanyCosts) { Details = [StepDetail.From(steps["car_costs"])] }.Of(received),
                new Drain("car_contribution", DrainKind.Back, car.Contribution, "pocket") { Details = [StepDetail.From(steps["car_contribution"])] },
            ]
            : [];

        return new CarView(
            received,
            car,
            steps.Count > 0 && year.Scenario.Car.CompanyPaysPrivateEnergy && CarCardText.Reading(year.Scenario.Car.Engine) is not null,
            steps.Count > 0 ? Shares.Of(left, received) : null,
            [
                .. drains,
                HandOff.Drain(year, OptionalModule.Car, composition),
            ],
            ledger.Select(StepDetail.From).ToList()) { SplitShares = left };
    }

    // The conditions the card lists for what drives the car: what makes a plug-in hybrid a fake one
    // only for a plug-in hybrid.
    public static IReadOnlyList<LeverCondition> Conditions(CarRules rules, CarEngine engine) =>
        rules.Conditions.Where(condition => condition.Key != "fake_hybrid" || engine.IsPlugInHybrid()).ToList();
}

// The plan with its car against the same plan without it, at the pay set: what reaches the pocket in
// each, and what its benefit adds to the taxes on pay. CashCost is what the car takes from the pocket,
// in cash (operator, 2026-10-23), below zero when the plan with the car leaves more there. Then the
// company car against the same car bought privately, once the user gives what that car would cost
// them for the year (PrivateCost), which no rule reads: AgainstPrivate is that cost less the cash the
// company car takes, what the company car gains above zero, what it costs more below. Two runs of the
// year, the second the river with the car out of the plan; none until a car counts, or while the engine
// refuses an amount.
internal sealed record CarComparison(decimal PocketWith, decimal PocketWithout, decimal TaxesOnPayAdded)
{
    // What the same car would cost privately cannot be below nothing; the card refuses it, the engine
    // never seeing it.
    public const string PrivateCostNegative = "car_private_cost_negative";

    // As the user gives it, or none until given.
    public decimal? PrivateCost { get; init; }

    public decimal CashCost => Difference(PocketWithout, PocketWith);

    // None until the private cost is given, or while it is refused; a plain zero for a tie.
    public decimal? AgainstPrivate => PrivateCost is { } cost && cost >= 0m ? Difference(cost, CashCost) : null;

    // The private cost's refusal, or none.
    public string? Refused => PrivateCost < 0m ? PrivateCostNegative : null;

    public static CarComparison? From(RiverInputs inputs, RiverRun run)
    {
        if (run.Error is not null || run.Year is not { Scenario.Car.Any: true } with)
            return null;
        if (inputs.TakeOutOfPlan(OptionalModule.Car).Run().Year is not { } without)
            return null;

        return new CarComparison(with.NetInPocket, without.NetInPocket, Difference(TaxesOnPay(with), TaxesOnPay(without)))
        {
            PrivateCost = inputs.Car.PrivateCost.Value,
        };
    }

    // The pay's own taxes, where the car's benefit is taxed with it.
    private static decimal TaxesOnPay(YearResult year) => BucketsView.PaidOut(year.Steps, TaxableEvent.DirectorPay);

    // A plain zero when the two are the same, never a negative one.
    private static decimal Difference(decimal more, decimal less) => more == less ? 0m : more - less;
}
