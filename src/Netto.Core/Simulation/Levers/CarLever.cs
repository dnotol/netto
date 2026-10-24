using Netto.Core.CompanyCar;
using Netto.Core.DirectorPay;

namespace Netto.Core.Simulation.Levers;

// A car the company puts at the director's disposal, as the engine composes it into the year: here, where
// LeverOrder lists it, YearSimulation reading its result by its type for YearResult.Car alone. Its
// costs less what the director pays the company for its private use come off what the company
// earns once its catalogue value is given, a cost of the company like its running costs (operator,
// 2026-10-23). It hands the year its benefit in kind (BenefitInKind), plain figures a laptop or a phone
// will give the same way: the benefit is taxed as pay but never paid, the contribution leaves the pocket
// for the company, and what the company cannot deduct of the costs and the share of the benefit it adds
// back join its taxable income.
internal sealed record CarLever(CarRules Rules) : Lever
{
    public override decimal FromProfit(Scenario scenario) => scenario.Car.Any ? scenario.Car.YearlyCosts - scenario.Car.PersonalContribution : 0m;

    public override (LeverYear? Value, string? Error) Compute(Scenario scenario, decimal left)
    {
        var (car, error) = CarCalculator.Compute(Rules, scenario.Car);
        return car is null ? (null, error) : (new CarYear(Rules, car, scenario.Car.Any), null);
    }
}

// The car as the rules took it, Used once there is a car.
internal sealed record CarYear(CarRules Rules, CarResult Car, bool Used) : LeverYear
{
    public override decimal ToPocket => Money.Minus(0m, Car.BenefitInKind.Contribution);

    public override decimal Disallowed => Car.BenefitInKind.NotDeductible;

    public override TaxedWithPay TaxedWithPay => new() { BenefitInKind = Car.BenefitInKind.TaxedAsPay };

    // The car's costs, at what the company spends on them, a cost of the company that only sets its
    // profit and so its tax base, as the royalty it pays does, with the rate it deducts them at; its
    // benefit on the scale, at least the minimum, with its CO2 percentage's amounts and each age
    // coefficient it used; the director's contribution leaving the pocket, a plain zero without one;
    // the benefit it leaves taxed as pay, in Director pay's figures; for a car the company does not
    // deduct in full, the part of its costs it cannot deduct, which joins its taxable income; and the
    // share of the benefit before the contribution the company adds back.
    public override IReadOnlyList<Step> Steps() => !Used ? [] :
    [
        new Step("car_costs", Layer.Corporate, TaxableEvent.CompanyCar, Track.TaxBaseOnly, Car.Costs,
            Rules.Deduction, Car.DeductionAmounts) { AlsoCites = Car.DeductionSources },
        new Step("car_benefit", Layer.Personal, TaxableEvent.CompanyCar, Track.TaxBaseOnly, Car.BenefitBeforeContribution,
            Rules.Benefit, [Rules.CatalogueShare, .. Car.Co2Amounts, .. Car.Periods.Select(period => period.Period.AgeCoefficient).Distinct(), Rules.MinimumBenefit])
        {
            AlsoCites = [Rules.BenefitMethod, .. Car.Co2Sources],
        },
        new Step("car_contribution", Layer.Personal, TaxableEvent.CompanyCar, Track.InPocket, Money.Minus(0m, Car.Contribution),
            Rules.Benefit, []),
        new Step("car_benefit_taxed", Layer.Personal, TaxableEvent.CompanyCar, Track.TaxBaseOnly, Car.Benefit,
            Rules.BenefitAsPay, []) { AlsoCites = [Rules.BenefitInContributions] },
        .. Car.DeductionRate < 1m
            ? [new Step("car_costs_not_deductible", Layer.Corporate, TaxableEvent.CompanyCar, Track.TaxBaseOnly,
                Car.NotDeductibleCosts, Rules.Deduction, [])]
            : Array.Empty<Step>(),
        new Step("car_add_back", Layer.Corporate, TaxableEvent.CompanyCar, Track.TaxBaseOnly, Car.AddBack,
            Rules.AddBack, [Car.AddBackRate ?? Rules.AddBackRate]),
    ];
}
