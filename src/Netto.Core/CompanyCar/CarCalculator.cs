using Netto.Core.Rules;

namespace Netto.Core.CompanyCar;

public static class CarCalculator
{
    public const string CatalogueValueNegative = "catalogue_value_negative";
    public const string YearlyCostsNegative = "car_yearly_costs_negative";
    public const string ContributionNegative = "car_contribution_negative";
    public const string Co2Negative = "car_co2_negative";
    public const string Co2Missing = "car_co2_missing";
    public const string MatchingCarCo2Negative = "car_matching_co2_negative";
    public const string MatchingCarCo2Missing = "car_matching_co2_missing";

    // The car as the rules take it, or the key of what they refuse. The benefit is the FAQ's: each
    // part of the year at one age coefficient, catalogue value × coefficient × its days out of the
    // year's × 6/7 × the CO2 percentage, rounded to the cent, then their sum at least the minimum,
    // less the contribution, never below zero. The company adds back its share of the benefit before
    // the contribution, rounded to the cent, and deducts an electric car's costs in full. A car with a
    // combustion engine needs its CO2, or the user's word that the registration holds none, and a
    // fake hybrid its matching car's.
    public static (CarResult? Value, string? Error) Compute(CarRules rules, CarFacts car)
    {
        if (car.CatalogueValue < 0m)
            return (null, CatalogueValueNegative);
        if (car.YearlyCosts < 0m)
            return (null, YearlyCostsNegative);
        if (car.PersonalContribution < 0m)
            return (null, ContributionNegative);
        if (car.Co2 < 0)
            return (null, Co2Negative);
        if (car.MatchingCarCo2 < 0)
            return (null, MatchingCarCo2Negative);
        if (!car.Any)
            return (CarResult.None, null);
        if (car.Engine.EmitsCo2() && car.Co2 == 0 && !car.NoCo2Data)
            return (null, Co2Missing);
        if (FakeHybridHolds(rules, car) && car.MatchingCarCo2 == 0 && !car.NoMatchingCar)
            return (null, MatchingCarCo2Missing);

        var co2 = Co2(rules, car);
        var daysInYear = DateTime.IsLeapYear(rules.IncomeYear) ? 366 : 365;
        var periods = AgePeriods(rules.AgeBands, rules.IncomeYear, car.FirstRegistration)
            .Select(period => new CarBenefitPeriod(period, Money.RoundToCent(
                car.CatalogueValue * period.AgeCoefficient.Value * period.Days / daysInYear * rules.CatalogueShare.Value * co2.Percentage)))
            .ToList();
        var onTheScale = periods.Sum(period => period.Benefit);
        var beforeContribution = Math.Max(onTheScale, rules.MinimumBenefit.Value);
        var addBackRate = car.CompanyPaysPrivateEnergy ? rules.AddBackRateWithPrivateEnergy : rules.AddBackRate;

        return (new CarResult(
            periods,
            onTheScale,
            beforeContribution,
            car.PersonalContribution,
            Math.Max(0m, beforeContribution - car.PersonalContribution),
            addBackRate,
            Money.RoundToCent(beforeContribution * addBackRate.Value),
            car.YearlyCosts,
            Money.RoundToCent(car.YearlyCosts * rules.ZeroEmissionDeduction.Value))
        {
            Co2Counted = co2.Counted,
            Co2Percentage = co2.Percentage,
            Co2Amounts = co2.Amounts,
            Co2Sources = co2.Sources,
        }, null);
    }

    // The fake-hybrid rule holds for a plug-in hybrid bought or leased from its first date on, once
    // the user says it is one (art. 36 §2 al. 9, art. 550 al. 6).
    private static bool FakeHybridHolds(CarRules rules, CarFacts car) =>
        car.Engine.IsPlugInHybrid() && car.FakeHybrid && car.Acquired >= rules.FakeHybridFrom;

    // The CO2 the benefit counts and its percentage (art. 36 §2): 5.5% at the reference of the car's
    // fuel, a tenth of a point per gram above or below, between the minimum and the maximum. A car
    // without CO2 data counts at the texts' emission for its fuel, a fake hybrid at its matching car's,
    // or at its own × 2.5 without one; an electric car takes the minimum. With the amounts it used and
    // the texts beside them, for the benefit's step.
    private static Co2Taken Co2(CarRules rules, CarFacts car)
    {
        if (!car.Engine.EmitsCo2())
            return new Co2Taken(0m, rules.MinimumCo2Percentage.Value, [rules.MinimumCo2Percentage], []);

        var diesel = car.Engine.RunsOnDiesel();
        var reference = diesel ? rules.DieselReferenceEmission : rules.PetrolReferenceEmission;
        List<RuleAmount> amounts = [rules.BaseCo2Percentage, rules.Co2PercentagePerGram, reference];
        List<Source> sources = [rules.ReferenceDecree];
        decimal counted;
        if (car.NoCo2Data)
        {
            var withoutData = diesel ? rules.DieselEmissionWithoutData : rules.PetrolEmissionWithoutData;
            counted = withoutData.Value;
            amounts.Add(withoutData);
        }
        else if (FakeHybridHolds(rules, car))
        {
            sources.Add(rules.FakeHybrid);
            if (car.NoMatchingCar)
            {
                counted = car.Co2 * rules.FakeHybridWithoutMatch.Value;
                amounts.Add(rules.FakeHybridWithoutMatch);
            }
            else
            {
                counted = car.MatchingCarCo2;
            }
        }
        else
        {
            counted = car.Co2;
        }

        var percentage = rules.BaseCo2Percentage.Value + rules.Co2PercentagePerGram.Value * (counted - reference.Value);
        if (percentage < rules.MinimumCo2Percentage.Value)
        {
            percentage = rules.MinimumCo2Percentage.Value;
            amounts.Add(rules.MinimumCo2Percentage);
        }
        else if (percentage > rules.MaximumCo2Percentage.Value)
        {
            percentage = rules.MaximumCo2Percentage.Value;
            amounts.Add(rules.MaximumCo2Percentage);
        }

        return new Co2Taken(counted, percentage, amounts, sources);
    }

    private sealed record Co2Taken(decimal Counted, decimal Percentage, IReadOnlyList<RuleAmount> Amounts, IReadOnlyList<Source> Sources);

    // The parts of the income year at one age coefficient. The age is counted in months from the
    // first day of the month of the first registration, that month being the first (a month begun
    // counts whole), so the coefficient changes on the first day of that month each year. A month
    // before the registration's counts as the first: the car is held all year.
    internal static IReadOnlyList<CarAgePeriod> AgePeriods(IReadOnlyList<CarAgeBand> bands, int incomeYear, DateOnly firstRegistration)
    {
        var periods = new List<CarAgePeriod>();
        for (var month = 1; month <= 12; month++)
        {
            var age = Math.Max(1, (incomeYear - firstRegistration.Year) * 12 + month - firstRegistration.Month + 1);
            var coefficient = bands.Last(band => band.FromMonth <= age).Coefficient;
            var from = new DateOnly(incomeYear, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            if (periods.Count > 0 && periods[^1].AgeCoefficient == coefficient)
                periods[^1] = periods[^1] with { To = to, Days = periods[^1].Days + to.Day };
            else
                periods.Add(new CarAgePeriod(from, to, to.Day, coefficient));
        }

        return periods;
    }
}
