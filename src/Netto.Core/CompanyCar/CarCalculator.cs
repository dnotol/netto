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
    public const string AcquiredAfterYear = "car_acquired_after_year";
    public const string PlugInFuelCapped = "car_plug_in_first_half_2023";

    // The car as the rules take it, or the key of what they refuse. The benefit is the FAQ's: each
    // part of the year at one age coefficient, catalogue value × coefficient × its days out of the
    // year's × 6/7 × the CO2 percentage, rounded to the cent, then their sum at least the minimum,
    // less the contribution, never below zero. The company adds back its share of the benefit before
    // the contribution, rounded to the cent. It deducts the part of the costs equal to the benefit
    // taxed and the contribution in full, up to the costs (art. 66 §3, 3°), and the rest at the rate
    // the car's order date, engine and CO2 give, rounded to the cent. A car with a combustion engine
    // needs its CO2, or the user's word that the registration holds none, a fake hybrid its matching
    // car's; a car ordered or leased after the year is not the company's in it, and a plug-in hybrid
    // acquired in the first half of 2023, whose fuel is capped apart from its other costs, is not
    // taken yet.
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
        if (car.Acquired > new DateOnly(rules.IncomeYear, 12, 31))
            return (null, AcquiredAfterYear);
        if (car.Engine.IsPlugInHybrid() && car.Acquired >= rules.PlugInFuelCappedFrom && car.Acquired <= rules.PlugInFuelCappedUntil)
            return (null, PlugInFuelCapped);

        var co2 = Co2(rules, car);
        var deduction = Deduction(rules, car, co2);
        var daysInYear = DateTime.IsLeapYear(rules.IncomeYear) ? 366 : 365;
        var periods = AgePeriods(rules.AgeBands, rules.IncomeYear, car.FirstRegistration)
            .Select(period => new CarBenefitPeriod(period, Money.RoundToCent(
                car.CatalogueValue * period.AgeCoefficient.Value * period.Days / daysInYear * rules.CatalogueShare.Value * co2.Percentage)))
            .ToList();
        var onTheScale = periods.Sum(period => period.Benefit);
        var beforeContribution = Math.Max(onTheScale, rules.MinimumBenefit.Value);
        var benefit = Math.Max(0m, beforeContribution - car.PersonalContribution);
        var addBackRate = car.CompanyPaysPrivateEnergy ? rules.AddBackRateWithPrivateEnergy : rules.AddBackRate;
        var carvedOut = Math.Min(car.YearlyCosts, benefit + car.PersonalContribution);

        return (new CarResult(
            periods,
            onTheScale,
            beforeContribution,
            car.PersonalContribution,
            benefit,
            addBackRate,
            Money.RoundToCent(beforeContribution * addBackRate.Value),
            car.YearlyCosts,
            carvedOut + Money.RoundToCent((car.YearlyCosts - carvedOut) * deduction.Rate))
        {
            Co2Counted = co2.Counted,
            Co2Percentage = co2.Percentage,
            Co2Basis = co2.Basis,
            Co2Reference = co2.Reference,
            Co2Amounts = co2.Amounts,
            Co2Sources = co2.Sources,
            DeductionRate = deduction.Rate,
            CarvedOut = carvedOut,
            DeductionAmounts = deduction.Amounts,
            DeductionSources = deduction.Sources,
        }, null);
    }

    // The rate a company deducts the car's costs at beyond the carve-out. An electric car in full. A
    // car that emits CO2 by the period its order or lease date falls in: its fixed rate, or the
    // texts' rate without CO2 data or from the high emission, or the formula 120% − 0.5% × coefficient
    // × the CO2 counted, rounded to a tenth of a percent, held between the period's floor (nothing
    // without one) and its ceiling. With the amounts it used and the texts it rests on, for the costs'
    // step.
    private static DeductionTaken Deduction(CarRules rules, CarFacts car, Co2Taken co2)
    {
        if (!car.Engine.EmitsCo2())
            return new DeductionTaken(rules.ZeroEmissionDeduction.Value, [rules.ZeroEmissionDeduction], [rules.Acquisition]);

        var plugIn = car.Engine.IsPlugInHybrid();
        var period = rules.DeductionPeriods.Last(period => period.AcquiredFrom <= car.Acquired);
        if (period.Fixed is { } fixedRate)
            return new DeductionTaken(fixedRate.Value, [fixedRate], plugIn ? [rules.Acquisition, rules.CompanyExclusions, rules.PlugInDeduction] : [rules.Acquisition]);

        List<Source> sources = [rules.Acquisition, rules.FormulaDeduction, rules.CompanyExclusions];
        if (FakeHybridHolds(rules, car))
            sources.Add(rules.PlugInDeduction);
        else if (plugIn && period.PlugInReading is { } reading)
            sources.Add(reading);

        if (car.NoCo2Data && period.WithoutCo2Data is { } withoutData)
            return new DeductionTaken(withoutData.Value, [withoutData], sources);
        if (period.HighEmissionFrom is { } highEmission && co2.Counted >= highEmission.Value && period.WithoutCo2Data is { } highEmissionRate)
            return new DeductionTaken(highEmissionRate.Value, [highEmission, highEmissionRate], sources);

        var coefficient = Coefficient(rules, car);
        List<RuleAmount> amounts = [rules.FormulaBase, rules.FormulaPerGram, coefficient];
        if (co2.Basis == CarCo2Basis.OwnTimesMultiplier)
            amounts.Add(rules.FakeHybridWithoutMatch);
        var rate = Math.Round(rules.FormulaBase.Value - rules.FormulaPerGram.Value * coefficient.Value * co2.Counted, 3, MidpointRounding.AwayFromZero);
        if (period.Floor is { } floor && rate < floor.Value)
        {
            rate = floor.Value;
            amounts.Add(floor);
        }
        else if (period.Ceiling is { } ceiling && rate > ceiling.Value)
        {
            rate = ceiling.Value;
            amounts.Add(ceiling);
        }

        return new DeductionTaken(Math.Max(0m, rate), amounts, sources);
    }

    // The formula's coefficient (art. 550 al. 1): 1 for a car running on diesel alone, 0.95 for a
    // diesel hybrid (question 96) and the others, 0.90 for natural gas under 12 fiscal horsepower.
    private static RuleAmount Coefficient(CarRules rules, CarFacts car) => car.Engine switch
    {
        CarEngine.Diesel => rules.DieselCoefficient,
        CarEngine.DieselHybrid or CarEngine.DieselPlugInHybrid => rules.DieselHybridCoefficient,
        CarEngine.NaturalGas when car.SmallNaturalGas => rules.SmallNaturalGasCoefficient,
        _ => rules.OtherFuelCoefficient,
    };

    private sealed record DeductionTaken(decimal Rate, IReadOnlyList<RuleAmount> Amounts, IReadOnlyList<Source> Sources);

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
            return new Co2Taken(CarCo2Basis.None, 0m, null, rules.MinimumCo2Percentage.Value, [rules.MinimumCo2Percentage], []);

        var diesel = car.Engine.RunsOnDiesel();
        var reference = diesel ? rules.DieselReferenceEmission : rules.PetrolReferenceEmission;
        List<RuleAmount> amounts = [rules.BaseCo2Percentage, rules.Co2PercentagePerGram, reference];
        List<Source> sources = [rules.ReferenceDecree];
        CarCo2Basis basis;
        decimal counted;
        if (car.NoCo2Data)
        {
            var withoutData = diesel ? rules.DieselEmissionWithoutData : rules.PetrolEmissionWithoutData;
            (basis, counted) = (CarCo2Basis.WithoutData, withoutData.Value);
            amounts.Add(withoutData);
        }
        else if (FakeHybridHolds(rules, car))
        {
            sources.Add(rules.FakeHybrid);
            if (car.NoMatchingCar)
            {
                (basis, counted) = (CarCo2Basis.OwnTimesMultiplier, car.Co2 * rules.FakeHybridWithoutMatch.Value);
                amounts.Add(rules.FakeHybridWithoutMatch);
            }
            else
            {
                (basis, counted) = (CarCo2Basis.MatchingCar, car.MatchingCarCo2);
            }
        }
        else
        {
            (basis, counted) = (CarCo2Basis.Registered, car.Co2);
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

        return new Co2Taken(basis, counted, reference, percentage, amounts, sources);
    }

    private sealed record Co2Taken(
        CarCo2Basis Basis, decimal Counted, RuleAmount? Reference, decimal Percentage, IReadOnlyList<RuleAmount> Amounts, IReadOnlyList<Source> Sources);

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
