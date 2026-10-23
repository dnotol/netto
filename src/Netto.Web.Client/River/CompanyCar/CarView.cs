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
        var left = year.ProfitBeforePay + year.Copyright.Royalty;
        var received = left + FromProfit(car);
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
                NextModule.IntoTheSplit(year, composition),
            ],
            ledger.Select(StepDetail.From).ToList()) { SplitShares = left };
    }

    // The conditions the card lists for what drives the car: what makes a plug-in hybrid a fake one
    // only for a plug-in hybrid.
    public static IReadOnlyList<LeverCondition> Conditions(CarRules rules, CarEngine engine) =>
        rules.Conditions.Where(condition => condition.Key != "fake_hybrid" || engine.IsPlugInHybrid()).ToList();
}

// The plan with its car against the same plan without it, at the pay set: what reaches the pocket in
// each, what the car puts in the muddy pocket, and what its benefit adds to the taxes on pay. CashCost
// is what the car takes from the pocket, Gap what the muddy pocket holds more than that. Two runs of
// the year, the second the river with the car out of the plan; none until a car counts, or while the
// engine refuses an amount.
internal sealed record CarComparison(decimal PocketWith, decimal PocketWithout, decimal Muddy, decimal TaxesOnPayAdded)
{
    public decimal CashCost => Difference(PocketWithout, PocketWith);

    public decimal Gap => Difference(Muddy, CashCost);

    public static CarComparison? From(RiverInputs inputs, RiverRun run)
    {
        if (run.Error is not null || run.Year is not { Scenario.Car.Any: true } with)
            return null;
        if (inputs.TakeOutOfPlan(OptionalModule.Car).Run().Year is not { } without)
            return null;

        return new CarComparison(with.NetInPocket, without.NetInPocket, with.MuddyPocket, Difference(TaxesOnPay(with), TaxesOnPay(without)));
    }

    private static decimal TaxesOnPay(YearResult year) =>
        year.Steps.Where(step => step.Event == TaxableEvent.DirectorPay && step.Track == Track.PaidOut).Sum(step => step.Amount);

    // A plain zero when the two are the same, never a negative one.
    private static decimal Difference(decimal more, decimal less) => more == less ? 0m : more - less;
}
