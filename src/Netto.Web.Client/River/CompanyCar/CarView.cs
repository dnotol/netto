using Netto.Core.CompanyCar;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.River.CompanyCar;

// What the company car costs the company, paid into its expenses beside its running costs (operator,
// 2026-10-23 and 2026-10-28), and the director's contribution coming back from
// their pocket, then what it leaves, passed on to the split, the last lever before it, as what the pay,
// the company and a royalty share there, from the year's car steps; what it receives is what the levers
// above leave of what the company earns. The card reads the car as the engine took it (Result): its benefit, period by period,
// at its CO2 percentage, at least the minimum, less the contribution, taxed as pay, what the company
// deducts of its costs, and what it adds back of the benefit, 40% when it pays the private energy,
// which for a car it charges, electric or plug-in hybrid, is the administration's reading of « fuel »
// (ChargingReading). Each line says its share of what the card receives, and the card what it passes
// on of it (PassesOn), never more than all of it nor less than none: the costs take nothing once the
// director's contribution covers them, and what the contribution brings beyond them comes from the
// director's pocket, not from what the card received, so it is a figure of its own
// (ContributionBeyondCosts), never a share above 100%. Costs more than the card receives take all of it
// and no more (CostsAboveWhatItReceives), so no share is above 100% or below 0%. Without a car, nothing
// but the profit passed on.
internal sealed record CarView(
    CarResult Result,
    bool ChargingReading,
    decimal? PassesOn,
    IReadOnlyList<Drain> Drains,
    IReadOnlyList<StepDetail> Details)
{
    public bool HasCar => Details.Count > 0;

    // What the split shares once the car is paid, the royalty in it, which the summary states: the same
    // at every pay, while the profit before pay alone moves with a royalty that follows the pay.
    public decimal SplitShares { get; init; }

    // What the director's contribution brings beyond the car's costs, from their pocket: the part of what
    // the card passes on that it did not receive, which the summary states as a figure, since it is no
    // share of what the card receives. None while the costs are at least the contribution, or it is under
    // a cent beyond them: a figure of €0.00 is left out on every card (DrainLines).
    public decimal? ContributionBeyondCosts { get; init; }

    // The costs are more than the card receives: their share stops at all of it, 100%, and the rate at 0%,
    // so the card says so, since no percentage can say it (operator, 2026-10-28). None while it receives
    // nothing to take a share of.
    public bool CostsAboveWhatItReceives { get; init; }

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
                new Drain("car_costs", DrainKind.PaidOut, car.Costs, "expenses")
                {
                    Details = [StepDetail.From(steps["car_costs"])],
                    Share = Shares.Of(Math.Min(car.Costs, received), received),
                },
                new Drain("car_contribution", DrainKind.Back, car.Contribution, "pocket") { Details = [StepDetail.From(steps["car_contribution"])] },
            ]
            : [];

        return new CarView(
            car,
            steps.Count > 0 && year.Scenario.Car.CompanyPaysPrivateEnergy && CarCardText.Reading(year.Scenario.Car.Engine) is not null,
            steps.Count > 0 ? Shares.Of(Math.Max(0m, Math.Min(left, received)), received) : null,
            [
                .. drains,
                HandOff.Drain(year, OptionalModule.Car, composition),
            ],
            ledger.Select(StepDetail.From).ToList()) {
            SplitShares = left,
            ContributionBeyondCosts = BeyondCosts(car),
            CostsAboveWhatItReceives = steps.Count > 0 && received > 0m && car.Costs > received,
        };
    }

    private static decimal? BeyondCosts(CarResult car) =>
        Math.Round(car.Contribution - car.Costs, 2, MidpointRounding.AwayFromZero) > 0m ? car.Contribution - car.Costs : null;

    // The conditions the card lists for what drives the car: what makes a plug-in hybrid a fake one
    // only for a plug-in hybrid.
    public static IReadOnlyList<LeverCondition> Conditions(CarRules rules, CarEngine engine) =>
        rules.Conditions.Where(condition => condition.Key != "fake_hybrid" || engine.IsPlugInHybrid()).ToList();
}

// The plan with its car against the same plan without it, at the pay the plan with the car runs at: what
// reaches the pocket in each, and what its benefit adds to the taxes on pay. That pay is the pay set
// unless the company cannot pay so much once the car's costs are paid (YearSimulation.PayCeiling): the
// river with the car holds it there, but the river without it would pay all of it, so the second run is
// given the first's pay, or the difference would be the pay's as well as the car's. PayHeld says so, the
// pay both plans run at, only then. CashCost is what the car takes from the pocket,
// in cash (operator, 2026-10-23), below zero when the plan with the car leaves more there. Then the
// company car against the same car bought privately, once the user gives what that car would cost
// them for the year (PrivateCost), which no rule reads: AgainstPrivate is that cost less the cash the
// company car takes, what the company car gains above zero, what it costs more below. The two runs are the
// car's LeverGain, its second the river with the car out of the plan; none until a car counts, or while
// the engine refuses an amount.
internal sealed record CarComparison(decimal PocketWith, decimal PocketWithout, decimal TaxesOnPayAdded)
{
    // As the user gives it, or none until given.
    public decimal? PrivateCost { get; init; }

    // The pay both plans run at when the pay set is held below, none otherwise.
    public decimal? PayHeld { get; init; }

    public decimal CashCost => Difference(PocketWithout, PocketWith);

    // None until the private cost is given, or while it is refused; a plain zero for a tie.
    public decimal? AgainstPrivate => PrivateCost is { } cost && cost >= 0m ? Difference(cost, CashCost) : null;

    // The private cost's refusal, or none: it cannot be below nothing. The card refuses it, the engine never
    // seeing it, with the refusal Core declares for it.
    public Refusal? Refused => PrivateCost < 0m ? CarCalculator.PrivateCostNegative : null;

    public static CarComparison? From(RiverInputs inputs, RiverRun run)
    {
        if (run.Year is not { Scenario.Car.Any: true } with || LeverGain.Of(inputs, run, OptionalModule.Car) is not { Without.Year: { } without } gain)
            return null;

        return new CarComparison(gain.PocketWith, gain.PocketWithout, Difference(TaxesOnPay(with), TaxesOnPay(without)))
        {
            PrivateCost = inputs.Car.PrivateCost.Value,
            PayHeld = gain.PayHeld,
        };
    }

    // The pay's own taxes, where the car's benefit is taxed with it.
    private static decimal TaxesOnPay(YearResult year) => BucketsView.PaidOut(year.Steps, TaxableEvent.DirectorPay);

    // A plain zero when the two are the same, never a negative one.
    private static decimal Difference(decimal more, decimal less) => more == less ? 0m : more - less;
}
