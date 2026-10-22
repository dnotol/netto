using Netto.Core.Rules;

namespace Netto.Core.Car;

// The car as the rules take it. Periods are the parts of the year at one age coefficient, each with
// its benefit rounded to the cent; BenefitOnTheScale is their sum, BenefitBeforeContribution that sum
// at least the year's minimum, the amount the company adds back a share of (AddBackRate, AddBack), and
// Benefit what the director is taxed on as pay, less their Contribution, never below zero. Costs are
// what the car costs the company, Deductible what it deducts of them. Without a car, all nothing.
public sealed record CarResult(
    IReadOnlyList<CarBenefitPeriod> Periods,
    decimal BenefitOnTheScale,
    decimal BenefitBeforeContribution,
    decimal Contribution,
    decimal Benefit,
    RuleAmount? AddBackRate,
    decimal AddBack,
    decimal Costs,
    decimal Deductible)
{
    public static CarResult None { get; } = new([], 0m, 0m, 0m, 0m, null, 0m, 0m, 0m);

    // The benefit was raised to the year's minimum.
    public bool AtTheMinimum => BenefitBeforeContribution > BenefitOnTheScale;
}

// A part of the income year at one age coefficient, from its first day to its last, its calendar days.
public sealed record CarAgePeriod(DateOnly From, DateOnly To, int Days, RuleAmount AgeCoefficient);

// A period's share of the benefit on the scale, rounded to the cent.
public sealed record CarBenefitPeriod(CarAgePeriod Period, decimal Benefit);
