namespace Netto.Core.Car;

public static class CarCalculator
{
    public const string CatalogueValueNegative = "catalogue_value_negative";
    public const string YearlyCostsNegative = "car_yearly_costs_negative";
    public const string ContributionNegative = "car_contribution_negative";

    // The car as the rules take it, or the key of what they refuse. The benefit is the FAQ's: each
    // part of the year at one age coefficient, catalogue value × coefficient × its days out of the
    // year's × 6/7 × the CO2 percentage, rounded to the cent, then their sum at least the minimum,
    // less the contribution, never below zero. The company adds back its share of the benefit before
    // the contribution, rounded to the cent, and deducts an electric car's costs in full.
    public static (CarResult? Value, string? Error) Compute(CarRules rules, CompanyCar car)
    {
        if (car.CatalogueValue < 0m)
            return (null, CatalogueValueNegative);
        if (car.YearlyCosts < 0m)
            return (null, YearlyCostsNegative);
        if (car.PersonalContribution < 0m)
            return (null, ContributionNegative);
        if (!car.Any)
            return (CarResult.None, null);

        var daysInYear = DateTime.IsLeapYear(rules.IncomeYear) ? 366 : 365;
        var periods = AgePeriods(rules.AgeBands, rules.IncomeYear, car.FirstRegistration)
            .Select(period => new CarBenefitPeriod(period, Money.RoundToCent(
                car.CatalogueValue * period.AgeCoefficient.Value * period.Days / daysInYear * rules.CatalogueShare.Value * rules.MinimumCo2Percentage.Value)))
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
            Money.RoundToCent(car.YearlyCosts * rules.ZeroEmissionDeduction.Value)), null);
    }

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
