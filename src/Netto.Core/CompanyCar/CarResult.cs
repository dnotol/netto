using Netto.Core.BenefitsInKind;
using Netto.Core.Rules;

namespace Netto.Core.CompanyCar;

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

    // The CO2 in g/km the benefit counts (nothing for an electric car), and its CO2 percentage, with
    // the amounts that percentage used and the texts beside them, for the benefit's step.
    public decimal Co2Counted { get; init; }

    public decimal Co2Percentage { get; init; }

    // What Co2Counted rests on, and the year's reference emission for the car's fuel it is measured
    // against, none for an electric car.
    public CarCo2Basis Co2Basis { get; init; }

    public RuleAmount? Co2Reference { get; init; }

    public IReadOnlyList<RuleAmount> Co2Amounts { get; init; } = [];

    public IReadOnlyList<Source> Co2Sources { get; init; } = [];

    // The rate the company deducts its costs at beyond CarvedOut, the part equal to the benefit taxed
    // and the contribution, deducted in full up to the costs (art. 66 §3, 3°), with the amounts that
    // rate used and the texts it rests on, for the costs' step.
    public decimal DeductionRate { get; init; }

    public decimal CarvedOut { get; init; }

    public IReadOnlyList<RuleAmount> DeductionAmounts { get; init; } = [];

    public IReadOnlyList<Source> DeductionSources { get; init; } = [];

    // The benefit was raised to the year's minimum.
    public bool AtTheMinimum => BenefitBeforeContribution > BenefitOnTheScale;

    // What the car does to the year, as YearSimulation composes it beside the other levers: the benefit
    // taxed as pay, the contribution, the costs, and what the company cannot deduct of them with the
    // share of the benefit it adds back.
    public BenefitInKind BenefitInKind => new(Benefit, Contribution, Costs, NotDeductibleCosts + AddBack);

    // A plain zero when the company deducts them all, never a negative one.
    private decimal NotDeductibleCosts => Costs == Deductible ? 0m : Costs - Deductible;
}

// A part of the income year at one age coefficient, from its first day to its last, its calendar days.
public sealed record CarAgePeriod(DateOnly From, DateOnly To, int Days, RuleAmount AgeCoefficient);

// A period's share of the benefit on the scale, rounded to the cent.
public sealed record CarBenefitPeriod(CarAgePeriod Period, decimal Benefit);
