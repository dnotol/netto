using Netto.Core.BenefitsInKind;
using Netto.Core.CompanyCar;
using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.CompanyCar;

// Made-up electric cars for income year 2026, derived from the official amounts (art. 36 §2, art. 198
// §1, 9° and 9°bis) and the FAQ's method: each period of the year at its age coefficient, weighed by
// its calendar days out of 365, catalogue value × coefficient × days / 365 × 6/7 × 4%, each part
// rounded to the cent. Tax-Calc takes the benefit as an amount, so none of this has a Tax-Calc check.
public class CarCalculatorTests
{
    private static readonly CarRules Rules = CarRules.IncomeYear2026;

    private static CarFacts Car(decimal catalogueValue, DateOnly firstRegistration, decimal yearlyCosts = 6_000m, bool privateEnergy = false,
        decimal contribution = 0m) =>
        new(catalogueValue, firstRegistration, yearlyCosts, privateEnergy, contribution);

    private static CarResult Compute(CarFacts car)
    {
        var (result, error) = CarCalculator.Compute(Rules, car);
        error.ShouldBeNull();
        return result.ShouldNotBeNull();
    }

    // The FAQ's first example, a car first registered on 21.6.2019: counted from 1.6.2019, 100% up
    // to 31.5.2020 and 94% from 1.6.2020, so 152 and 214 days of the leap year 2020.
    [Fact]
    public void AgePeriods_faqExampleIn2020_splitsTheYearOnTheFirstDayOfTheRegistrationsMonth()
    {
        var periods = CarCalculator.AgePeriods(Rules.AgeBands, 2020, new DateOnly(2019, 6, 21));

        periods.Select(period => (period.From, period.To, period.Days, period.AgeCoefficient.Value)).ShouldBe(new[]
        {
            (new DateOnly(2020, 1, 1), new DateOnly(2020, 5, 31), 152, 1.00m),
            (new DateOnly(2020, 6, 1), new DateOnly(2020, 12, 31), 214, 0.94m),
        });
    }

    // 50,000 × 6/7 × 4% = 1,714.29, above the minimum: a car first registered in January 2026 is in
    // its first twelve months all year.
    [Fact]
    public void Compute_newCar_isSixSeventhsOfFourPercentOfItsCatalogueValue()
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10)));

        (result.Periods.Count, result.BenefitOnTheScale, result.BenefitBeforeContribution, result.Benefit, result.AtTheMinimum)
            .ShouldBe((1, 1_714.29m, 1_714.29m, 1_714.29m, false));
    }

    // First registered on 21.6.2025: 151 days at 100% (851.04) and 214 at 94% (1,133.74).
    [Fact]
    public void Compute_carTurningOneDuringTheYear_weighsEachCoefficientByItsDays()
    {
        var result = Compute(Car(60_000m, new DateOnly(2025, 6, 21)));

        result.Periods.Select(period => (period.Period.Days, period.Period.AgeCoefficient.Value, period.Benefit)).ShouldBe([(151, 1.00m, 851.04m), (214, 0.94m, 1_133.74m)]);
        result.BenefitOnTheScale.ShouldBe(1_984.78m);
    }

    // First registered on 10.2.2022: January is its 48th month (82%, 31 days, 167.15) and February
    // its 49th (76%, 334 days, 1,669.08).
    [Fact]
    public void Compute_carTurningFourDuringTheYear_takesItsFifthBandFromTheRegistrationsMonth()
    {
        var result = Compute(Car(70_000m, new DateOnly(2022, 2, 10)));

        result.Periods.Select(period => (period.Period.From, period.Period.AgeCoefficient.Value, period.Benefit))
            .ShouldBe([(new DateOnly(2026, 1, 1), 0.82m, 167.15m), (new DateOnly(2026, 2, 1), 0.76m, 1_669.08m)]);
        result.BenefitOnTheScale.ShouldBe(1_836.23m);
    }

    // From its 61st month on, 70%: 80,000 × 70% × 6/7 × 4% = 1,920.00.
    [Fact]
    public void Compute_carOlderThanFiveYears_takesSeventyPercent()
    {
        var result = Compute(Car(80_000m, new DateOnly(2019, 3, 5)));

        (result.Periods.Single().Period.AgeCoefficient.Value, result.BenefitOnTheScale).ShouldBe((0.70m, 1_920.00m));
    }

    // The plan holds the car all year, so a car first registered during it, or later, is new all
    // year.
    [Theory]
    [InlineData(2026, 9, 15)]
    [InlineData(2027, 3, 1)]
    public void Compute_carRegisteredDuringTheYearOrLater_isNewAllYear(int year, int month, int day)
    {
        var result = Compute(Car(50_000m, new DateOnly(year, month, day)));

        (result.Periods.Single().Period.Days, result.Periods.Single().Period.AgeCoefficient.Value, result.BenefitOnTheScale).ShouldBe((365, 1.00m, 1_714.29m));
    }

    // 30,000 × 6/7 × 4% = 1,028.57 is below the minimum of 1,690.
    [Fact]
    public void Compute_benefitBelowTheMinimum_isTheMinimum()
    {
        var result = Compute(Car(30_000m, new DateOnly(2026, 1, 10)));

        (result.BenefitOnTheScale, result.BenefitBeforeContribution, result.Benefit, result.AtTheMinimum).ShouldBe((1_028.57m, 1_690m, 1_690m, true));
    }

    // The contribution comes off after the minimum (art. 36 §2, its last paragraph).
    [Fact]
    public void Compute_withAContribution_lowersTheBenefitTaxed()
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10), contribution: 600m));

        (result.BenefitBeforeContribution, result.Contribution, result.Benefit).ShouldBe((1_714.29m, 600m, 1_114.29m));
    }

    [Fact]
    public void Compute_contributionAboveTheBenefit_leavesNoBenefit()
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10), contribution: 2_000m));

        result.Benefit.ShouldBe(0m);
        decimal.IsNegative(result.Benefit).ShouldBeFalse();
    }

    // 17% of the benefit as art. 36 §2, al. 1 to 12 sets it, before the contribution: 291.43.
    [Fact]
    public void Compute_companyNotPayingPrivateCharging_addsBackSeventeenPercentBeforeTheContribution()
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10), contribution: 600m));

        (result.AddBackRate, result.AddBack).ShouldBe((Rules.AddBackRate, 291.43m));
    }

    [Fact]
    public void Compute_companyPayingPrivateCharging_addsBackFortyPercent()
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10), privateEnergy: true));

        (result.AddBackRate, result.AddBack).ShouldBe((Rules.AddBackRateWithPrivateEnergy, 685.72m));
    }

    // At the minimum, 17% of 1,690.00 = 287.30.
    [Fact]
    public void Compute_benefitAtTheMinimum_addsBackAShareOfTheMinimum()
    {
        Compute(Car(30_000m, new DateOnly(2026, 1, 10))).AddBack.ShouldBe(287.30m);
    }

    // An electric car ordered or leased before 2027 stays deductible in full.
    [Fact]
    public void Compute_electricCar_isDeductedInFull()
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10), yearlyCosts: 9_250.50m));

        (result.Costs, result.Deductible).ShouldBe((9_250.50m, 9_250.50m));
    }

    // A later year may deduct a car that emits no CO2 by the date it was ordered or leased, as it does a
    // car that emits CO2: the period that date falls in gives its rate when it sets one. Made-up rules,
    // the 2026 ones with their last period deducting such a car at 95%.
    private static readonly RuleAmount ReducedZeroEmission = new("zero_emission_from_2026_deduction_rate", 0.95m, Rules.Deduction);

    private static readonly CarRules RulesWithAReducedZeroEmissionPeriod =
        Rules with { DeductionPeriods = [Rules.DeductionPeriods[0], Rules.DeductionPeriods[1], Rules.DeductionPeriods[2] with { ZeroEmission = ReducedZeroEmission }] };

    [Fact]
    public void Compute_electricCarOrderedInAPeriodWithItsOwnRate_isDeductedAtThatRate()
    {
        var car = Car(40_000m, new DateOnly(2026, 1, 10)) with { Acquired = new DateOnly(2026, 3, 1) };

        var result = CarCalculator.Compute(RulesWithAReducedZeroEmissionPeriod, car).Value.ShouldNotBeNull();

        (result.DeductionRate, result.DeductionAmounts.Single()).ShouldBe((0.95m, ReducedZeroEmission));
    }

    [Fact]
    public void Compute_electricCarOrderedInAPeriodWithoutARate_isDeductedAtTheYearsRate()
    {
        var car = Car(40_000m, new DateOnly(2026, 1, 10)) with { Acquired = new DateOnly(2025, 3, 1) };

        var result = CarCalculator.Compute(RulesWithAReducedZeroEmissionPeriod, car).Value.ShouldNotBeNull();

        (result.DeductionRate, result.DeductionAmounts.Single()).ShouldBe((1.00m, Rules.ZeroEmissionDeduction));
    }

    // Until its catalogue value is given there is no car: no benefit, and its costs and contribution
    // count for nothing.
    [Fact]
    public void Compute_noCatalogueValue_isNoCar()
    {
        var result = Compute(Car(0m, new DateOnly(2026, 1, 10), yearlyCosts: 6_000m, contribution: 600m));

        (result.Periods.Count, result.Benefit, result.AddBack, result.Costs, result.Contribution, result.Deductible).ShouldBe((0, 0m, 0m, 0m, 0m, 0m));
    }

    [Fact]
    public void Compute_none_isNoCar()
    {
        Compute(CarFacts.None).ShouldBe(CarResult.None);
    }

    // What the year takes of the car, as plain figures: the benefit taxed as pay after the
    // contribution, the contribution, the costs, and what the company cannot deduct with the add-back
    // (an electric car's costs in full, so only the add-back).
    [Fact]
    public void Compute_electricCar_handsTheYearItsBenefitInKind()
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10), yearlyCosts: 8_000m, contribution: 600m));

        result.BenefitInKind.ShouldBe(new BenefitInKind(TaxedAsPay: 1_114.29m, Contribution: 600m, Costs: 8_000m, NotDeductible: 291.43m));
    }

    [Fact]
    public void Compute_none_handsNoBenefitInKind()
    {
        Compute(CarFacts.None).BenefitInKind.ShouldBe(BenefitInKind.None);
    }

    // A car that emits CO2 takes 5.5% at its fuel's reference for 2026, a tenth of a point per gram
    // above it: petrol at 120 g/km against 70, 10.5%; 45,000 × 6/7 × 10.5% = 4,050.00.
    [Fact]
    public void Compute_petrolCar_takesItsCo2PercentageAgainstThePetrolReference()
    {
        var result = Compute(Car(45_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.Petrol, Co2 = 120 });

        (result.Co2Counted, result.Co2Percentage, result.BenefitOnTheScale).ShouldBe((120m, 0.105m, 4_050.00m));
    }

    // Diesel at 140 g/km against 58: 13.7%; first registered in 2019, so 70% of its catalogue value:
    // 60,000 × 70% × 6/7 × 13.7% = 4,932.00.
    [Fact]
    public void Compute_dieselCar_takesItsCo2PercentageAgainstTheDieselReference()
    {
        var result = Compute(Car(60_000m, new DateOnly(2019, 3, 5)) with { Engine = CarEngine.Diesel, Co2 = 140 });

        (result.Co2Percentage, result.BenefitOnTheScale).ShouldBe((0.137m, 4_932.00m));
    }

    // The reference is petrol's for every car whose engine runs on petrol, LPG or natural gas, and
    // diesel's for every one running on diesel, hybrids and plug-in hybrids included: at 100 g/km,
    // 8.5% against 70 and 9.7% against 58.
    [Theory]
    [InlineData(CarEngine.Petrol, 0.085)]
    [InlineData(CarEngine.Lpg, 0.085)]
    [InlineData(CarEngine.NaturalGas, 0.085)]
    [InlineData(CarEngine.PetrolHybrid, 0.085)]
    [InlineData(CarEngine.PetrolPlugInHybrid, 0.085)]
    [InlineData(CarEngine.Diesel, 0.097)]
    [InlineData(CarEngine.DieselHybrid, 0.097)]
    [InlineData(CarEngine.DieselPlugInHybrid, 0.097)]
    public void Compute_carAtOneHundredGrams_takesItsFuelsReference(CarEngine engine, decimal percentage)
    {
        Compute(Car(50_000m, new DateOnly(2026, 1, 10)) with { Engine = engine, Co2 = 100 }).Co2Percentage.ShouldBe(percentage);
    }

    // 5.5% − 4 points at 30 g/km would be 1.5%: the percentage is never below 4%.
    [Fact]
    public void Compute_carFarBelowTheReference_takesTheMinimumPercentage()
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.PetrolPlugInHybrid, Co2 = 30 });

        (result.Co2Percentage, result.BenefitOnTheScale).ShouldBe((0.04m, 1_714.29m));
    }

    // Diesel at 200 g/km would be 19.7%: the percentage is never above 18%.
    [Fact]
    public void Compute_carFarAboveTheReference_takesTheMaximumPercentage()
    {
        Compute(Car(50_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.Diesel, Co2 = 200 }).Co2Percentage.ShouldBe(0.18m);
    }

    // Without CO2 data at the vehicle registration, a car counts as 205 g/km on petrol, LPG or natural
    // gas and 195 g/km on diesel, so 18% either way.
    [Theory]
    [InlineData(CarEngine.Petrol, 205)]
    [InlineData(CarEngine.Lpg, 205)]
    [InlineData(CarEngine.Diesel, 195)]
    public void Compute_carWithoutCo2Data_countsAsTheTextsEmission(CarEngine engine, int counted)
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10)) with { Engine = engine, NoCo2Data = true });

        (result.Co2Counted, result.Co2Percentage).ShouldBe((counted, 0.18m));
    }

    // A fake hybrid bought or leased from 2018 counts at the CO2 of its matching fuel car: 150 g/km
    // against 70, 13.5%; 55,000 × 6/7 × 13.5% = 6,364.29.
    [Fact]
    public void Compute_fakeHybrid_countsAtItsMatchingCarsCo2()
    {
        var result = Compute(Car(55_000m, new DateOnly(2026, 1, 10)) with
        {
            Engine = CarEngine.PetrolPlugInHybrid, Co2 = 45, Acquired = new DateOnly(2025, 12, 1), FakeHybrid = true, MatchingCarCo2 = 150,
        });

        (result.Co2Counted, result.Co2Percentage, result.BenefitOnTheScale).ShouldBe((150m, 0.135m, 6_364.29m));
    }

    // Without a matching car, its own CO2 × 2.5: 112.5 g/km, 9.75%; 55,000 × 6/7 × 9.75% = 4,596.43.
    [Fact]
    public void Compute_fakeHybridWithoutAMatchingCar_countsTwoAndAHalfTimesItsOwnCo2()
    {
        var result = Compute(Car(55_000m, new DateOnly(2026, 1, 10)) with
        {
            Engine = CarEngine.PetrolPlugInHybrid, Co2 = 45, Acquired = new DateOnly(2025, 12, 1), FakeHybrid = true, NoMatchingCar = true,
        });

        (result.Co2Counted, result.Co2Percentage, result.BenefitOnTheScale).ShouldBe((112.5m, 0.0975m, 4_596.43m));
    }

    // The fake-hybrid rule holds for a plug-in hybrid bought or leased from 2018, and only for a
    // plug-in hybrid: otherwise the car counts at its own CO2, whatever was ticked.
    [Theory]
    [InlineData(CarEngine.PetrolPlugInHybrid, 2017)]
    [InlineData(CarEngine.PetrolHybrid, 2024)]
    [InlineData(CarEngine.Petrol, 2024)]
    public void Compute_fakeHybridTickWhereTheRuleDoesNotHold_keepsTheCarsOwnCo2(CarEngine engine, int acquiredIn)
    {
        var result = Compute(Car(55_000m, new DateOnly(acquiredIn, 6, 1)) with
        {
            Engine = engine, Co2 = 45, Acquired = new DateOnly(acquiredIn, 5, 1), FakeHybrid = true, MatchingCarCo2 = 150,
        });

        result.Co2Counted.ShouldBe(45m);
    }

    // The decision whether the rule can apply is the engine's, which the card binds: a plug-in hybrid
    // ordered or leased from 1 January 2018 (art. 36 §2 al. 9, art. 550 al. 6), whatever was ticked.
    [Theory]
    [InlineData(CarEngine.PetrolPlugInHybrid, 2017, 12, 31, false)]
    [InlineData(CarEngine.DieselPlugInHybrid, 2017, 12, 1, false)]
    [InlineData(CarEngine.PetrolPlugInHybrid, 2018, 1, 1, true)]
    [InlineData(CarEngine.DieselPlugInHybrid, 2026, 1, 1, true)]
    [InlineData(CarEngine.PetrolHybrid, 2024, 5, 1, false)]
    [InlineData(CarEngine.Petrol, 2024, 5, 1, false)]
    [InlineData(CarEngine.Electric, 2024, 5, 1, false)]
    public void FakeHybridApplies_engineAndAcquisitionDate_holdsForAPlugInHybridFrom2018(CarEngine engine, int year, int month, int day, bool applies)
    {
        CarCalculator.FakeHybridApplies(Rules, engine, new DateOnly(year, month, day)).ShouldBe(applies);
    }

    // An electric car emits nothing, whatever its CO2 field holds: 4%.
    [Fact]
    public void Compute_electricCar_takesTheMinimumPercentageWhateverItsCo2Field()
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10)) with { Co2 = 120 });

        (result.Co2Counted, result.Co2Percentage, result.BenefitOnTheScale).ShouldBe((0m, 0.04m, 1_714.29m));
    }

    // What the CO2 counted rests on, and the reference it is measured against, for the card to say.
    [Fact]
    public void Compute_co2Basis_saysWhatTheCo2CountedRestsOn()
    {
        var plugIn = Car(55_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.PetrolPlugInHybrid, Co2 = 45, Acquired = new DateOnly(2025, 12, 1) };

        new[]
            {
                Compute(Car(50_000m, new DateOnly(2026, 1, 10))),
                Compute(plugIn),
                Compute(plugIn with { NoCo2Data = true }),
                Compute(plugIn with { FakeHybrid = true, MatchingCarCo2 = 150 }),
                Compute(plugIn with { FakeHybrid = true, NoMatchingCar = true }),
                Compute(plugIn with { Engine = CarEngine.Diesel }),
            }
            .Select(result => (result.Co2Basis, result.Co2Reference))
            .ShouldBe([
                (CarCo2Basis.None, null), (CarCo2Basis.Registered, Rules.PetrolReferenceEmission), (CarCo2Basis.WithoutData, Rules.PetrolReferenceEmission),
                (CarCo2Basis.MatchingCar, Rules.PetrolReferenceEmission), (CarCo2Basis.OwnTimesMultiplier, Rules.PetrolReferenceEmission),
                (CarCo2Basis.Registered, Rules.DieselReferenceEmission),
            ]);
    }

    // The amounts the benefit's CO2 percentage used, for its step: the base, the step per gram and the
    // fuel's reference, with the emission the texts give a car without data, the fake hybrid's
    // multiplier, and the minimum or the maximum when the percentage is held to it; an electric car's
    // the minimum alone.
    [Fact]
    public void Compute_co2Amounts_areTheOnesThePercentageUsed()
    {
        Compute(Car(50_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.Petrol, Co2 = 120 }).Co2Amounts
            .ShouldBe([Rules.BaseCo2Percentage, Rules.Co2PercentagePerGram, Rules.PetrolReferenceEmission]);
        Compute(Car(50_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.Diesel, NoCo2Data = true }).Co2Amounts
            .ShouldBe([Rules.BaseCo2Percentage, Rules.Co2PercentagePerGram, Rules.DieselReferenceEmission, Rules.DieselEmissionWithoutData, Rules.MaximumCo2Percentage]);
        Compute(Car(50_000m, new DateOnly(2026, 1, 10)) with
            {
                Engine = CarEngine.PetrolPlugInHybrid, Co2 = 20, Acquired = new DateOnly(2025, 12, 1), FakeHybrid = true, NoMatchingCar = true,
            }).Co2Amounts
            .ShouldBe([Rules.BaseCo2Percentage, Rules.Co2PercentagePerGram, Rules.PetrolReferenceEmission, Rules.FakeHybridWithoutMatch, Rules.MinimumCo2Percentage]);
        Compute(Car(50_000m, new DateOnly(2026, 1, 10))).Co2Amounts.ShouldBe([Rules.MinimumCo2Percentage]);
    }

    [Fact]
    public void Compute_negativeCo2_isRefused()
    {
        CarCalculator.Compute(Rules, Car(50_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.Petrol, Co2 = -1 })
            .ShouldBe((null, CarCalculator.Co2Negative));
    }

    // A car with a combustion engine emits CO2: until its figure is given, or the user says the
    // vehicle registration holds none, Netto has nothing to take.
    [Theory]
    [InlineData(CarEngine.Petrol)]
    [InlineData(CarEngine.DieselHybrid)]
    [InlineData(CarEngine.PetrolPlugInHybrid)]
    public void Compute_combustionCarWithoutItsCo2_isRefused(CarEngine engine)
    {
        CarCalculator.Compute(Rules, Car(50_000m, new DateOnly(2026, 1, 10)) with { Engine = engine })
            .ShouldBe((null, CarCalculator.Co2Missing));
    }

    [Fact]
    public void Compute_negativeMatchingCarCo2_isRefused()
    {
        CarCalculator.Compute(Rules, Car(50_000m, new DateOnly(2026, 1, 10)) with
            {
                Engine = CarEngine.PetrolPlugInHybrid, Co2 = 45, Acquired = new DateOnly(2025, 12, 1), FakeHybrid = true, MatchingCarCo2 = -1,
            })
            .ShouldBe((null, CarCalculator.MatchingCarCo2Negative));
    }

    // A fake hybrid needs its matching car's CO2, or the user's word that there is none.
    [Fact]
    public void Compute_fakeHybridWithoutItsMatchingCarsCo2_isRefused()
    {
        CarCalculator.Compute(Rules, Car(50_000m, new DateOnly(2026, 1, 10)) with
            {
                Engine = CarEngine.PetrolPlugInHybrid, Co2 = 45, Acquired = new DateOnly(2025, 12, 1), FakeHybrid = true,
            })
            .ShouldBe((null, CarCalculator.MatchingCarCo2Missing));
    }

    // A company's deduction rate by the car's order or lease date, engine and CO2 (art. 66 and 550 as
    // art. 198bis leaves them to companies): before 01.07.2023 the formula 120% − 0.5% × coefficient
    // × g/km, rounded to a tenth, between 50% and 100%, 40% from 200 g/km or without data, with no
    // minimum for a car acquired before 2018; from 01.07.2023 to 31.12.2025 the formula at most 50%,
    // never below nothing, 0% without data, a plug-in hybrid's too (question 819); from 2026 nothing.
    // The coefficient: 1 on diesel alone, 0.95 a diesel hybrid (question 96) and the others, 0.90
    // natural gas under 12 fiscal horsepower. An electric car, in full.
    [Theory]
    [InlineData(CarEngine.Petrol, 123, 2022, 5, 1, 0.616)]
    [InlineData(CarEngine.Diesel, 130, 2022, 5, 1, 0.55)]
    [InlineData(CarEngine.Diesel, 130, 2017, 6, 1, 0.55)]
    [InlineData(CarEngine.Diesel, 150, 2017, 6, 1, 0.50)]
    [InlineData(CarEngine.Petrol, 30, 2022, 5, 1, 1.00)]
    [InlineData(CarEngine.Petrol, 200, 2023, 6, 30, 0.40)]
    [InlineData(CarEngine.Petrol, 199, 2023, 6, 30, 0.50)]
    [InlineData(CarEngine.DieselHybrid, 110, 2022, 5, 1, 0.678)]
    [InlineData(CarEngine.NaturalGas, 100, 2022, 5, 1, 0.725)]
    [InlineData(CarEngine.PetrolPlugInHybrid, 40, 2022, 12, 31, 1.00)]
    [InlineData(CarEngine.Petrol, 100, 2023, 7, 1, 0.50)]
    [InlineData(CarEngine.Diesel, 160, 2024, 3, 1, 0.40)]
    [InlineData(CarEngine.Diesel, 250, 2025, 12, 31, 0)]
    [InlineData(CarEngine.PetrolPlugInHybrid, 40, 2025, 12, 31, 0.50)]
    [InlineData(CarEngine.Petrol, 90, 2026, 1, 1, 0)]
    [InlineData(CarEngine.PetrolPlugInHybrid, 30, 2026, 6, 1, 0)]
    [InlineData(CarEngine.Electric, 0, 2025, 3, 1, 1.00)]
    [InlineData(CarEngine.Electric, 0, 2026, 3, 1, 1.00)]
    public void Compute_deductionRate_isTheOneItsDateEngineAndCo2Give(CarEngine engine, int co2, int year, int month, int day, decimal rate)
    {
        var car = Car(40_000m, new DateOnly(2026, 1, 10)) with { Engine = engine, Co2 = co2, Acquired = new DateOnly(year, month, day) };

        Compute(car).DeductionRate.ShouldBe(rate);
    }

    // Natural gas under 12 fiscal horsepower takes 0.90: at 100 g/km, 75.0% instead of 72.5%.
    [Fact]
    public void Compute_smallNaturalGasCar_takesTheReducedCoefficient()
    {
        var car = Car(40_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.NaturalGas, Co2 = 100, SmallNaturalGas = true, Acquired = new DateOnly(2022, 5, 1) };

        Compute(car).DeductionRate.ShouldBe(0.75m);
    }

    // Without CO2 data, 40% before 01.07.2023 and nothing after.
    [Theory]
    [InlineData(2022, 5, 1, 0.40)]
    [InlineData(2024, 3, 1, 0)]
    public void Compute_carWithoutCo2Data_isDeductedAtTheTextsRate(int year, int month, int day, decimal rate)
    {
        var car = Car(40_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.Petrol, NoCo2Data = true, Acquired = new DateOnly(year, month, day) };

        Compute(car).DeductionRate.ShouldBe(rate);
    }

    // A fake hybrid is deducted at its matching car's CO2 too: 100 g/km, 72.5%, where its own 40
    // would give 100%.
    [Fact]
    public void Compute_fakeHybrid_isDeductedAtItsMatchingCarsCo2()
    {
        var car = Car(40_000m, new DateOnly(2026, 1, 10)) with
        {
            Engine = CarEngine.PetrolPlugInHybrid, Co2 = 40, Acquired = new DateOnly(2022, 5, 1), FakeHybrid = true, MatchingCarCo2 = 100,
        };

        Compute(car).DeductionRate.ShouldBe(0.725m);
    }

    // Leased in 2026, the petrol car deducts nothing beyond the part equal to its benefit, 4,050.00
    // (art. 66 §3, 3°): 4,950.00 of its 9,000.00 are not deductible, and 17% of the benefit, 688.50, is
    // added back besides (decided with the operator on 2026-10-22).
    [Fact]
    public void Compute_fuelCarLeasedIn2026_deductsOnlyThePartEqualToItsBenefit()
    {
        var car = Car(45_000m, new DateOnly(2026, 1, 10), yearlyCosts: 9_000m) with { Engine = CarEngine.Petrol, Co2 = 120, Acquired = new DateOnly(2026, 1, 5) };

        var result = Compute(car);

        (result.DeductionRate, result.CarvedOut, result.Deductible, result.AddBack).ShouldBe((0m, 4_050.00m, 4_050.00m, 688.50m));
        result.BenefitInKind.NotDeductible.ShouldBe(5_638.50m);
    }

    // The part kept deductible is the benefit taxed and the contribution: 3,450.00 and 600.00 are
    // still 4,050.00, and a contribution above the benefit is kept whole, 5,000.00.
    [Theory]
    [InlineData(600, 4_050.00)]
    [InlineData(5_000, 5_000.00)]
    public void Compute_contribution_isPartOfWhatStaysDeductible(decimal contribution, decimal carvedOut)
    {
        var car = Car(45_000m, new DateOnly(2026, 1, 10), yearlyCosts: 9_000m, contribution: contribution) with
        {
            Engine = CarEngine.Petrol, Co2 = 120, Acquired = new DateOnly(2026, 1, 5),
        };

        Compute(car).CarvedOut.ShouldBe(carvedOut);
    }

    // The diesel from 2019 costs 3,500.00 a year, less than its benefit of 4,932.00: the company
    // deducts its 3,500.00 and no more (decided with the operator on 2026-10-22).
    [Fact]
    public void Compute_carCostingLessThanItsBenefit_deductsItsCostsAndNoMore()
    {
        var car = Car(60_000m, new DateOnly(2019, 3, 5), yearlyCosts: 3_500m) with { Engine = CarEngine.Diesel, Co2 = 140, Acquired = new DateOnly(2019, 2, 1) };

        var result = Compute(car);

        (result.DeductionRate, result.CarvedOut, result.Deductible).ShouldBe((0.50m, 3_500.00m, 3_500.00m));
    }

    // Ordered in November 2025: the carve-out, 2,185.71, in full, and the other 7,814.29 at 50%,
    // 3,907.15; the company cannot deduct 3,907.14 and adds back 371.57.
    [Fact]
    public void Compute_carOrderedIn2025_deductsTheRestOfItsCostsAtItsRate()
    {
        var car = Car(30_000m, new DateOnly(2026, 1, 10), yearlyCosts: 10_000m) with { Engine = CarEngine.Petrol, Co2 = 100, Acquired = new DateOnly(2025, 11, 1) };

        var result = Compute(car);

        (result.BenefitBeforeContribution, result.CarvedOut, result.Deductible, result.AddBack).ShouldBe((2_185.71m, 2_185.71m, 6_092.86m, 371.57m));
        result.BenefitInKind.NotDeductible.ShouldBe(4_278.71m);
    }

    // The amounts and texts the deduction used, for the costs' step: an electric car's full rate and
    // the circular on the order date; a car from 2026 the 0% of art. 66; the formula with its
    // coefficient and the bound it met, with art. 550 and 198bis; without data, the texts' rate.
    [Fact]
    public void Compute_deductionAmounts_areTheOnesTheRateUsed()
    {
        var rules = Rules;
        var petrol = Car(40_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.Petrol, Co2 = 150 };

        Compute(Car(40_000m, new DateOnly(2026, 1, 10))).DeductionAmounts.ShouldBe([rules.ZeroEmissionDeduction]);
        Compute(petrol with { Acquired = new DateOnly(2026, 2, 1) }).DeductionAmounts.ShouldBe([rules.DeductionPeriods[2].Fixed!]);
        Compute(petrol with { Acquired = new DateOnly(2022, 2, 1) }).DeductionAmounts
            .ShouldBe([rules.FormulaBase, rules.FormulaPerGram, rules.OtherFuelCoefficient, rules.DeductionPeriods[0].Floor!]);
        Compute(petrol with { Acquired = new DateOnly(2024, 2, 1), Co2 = 50 }).DeductionAmounts
            .ShouldBe([rules.FormulaBase, rules.FormulaPerGram, rules.OtherFuelCoefficient, rules.DeductionPeriods[1].Ceiling!]);
        Compute(petrol with { Acquired = new DateOnly(2022, 2, 1), Co2 = 0, NoCo2Data = true }).DeductionAmounts
            .ShouldBe([rules.DeductionPeriods[0].WithoutCo2Data!]);
        Compute(petrol with { Acquired = new DateOnly(2022, 2, 1), Co2 = 210 }).DeductionAmounts
            .ShouldBe([rules.DeductionPeriods[0].HighEmissionFrom!, rules.DeductionPeriods[0].WithoutCo2Data!]);
    }

    [Fact]
    public void Compute_deductionSources_areTheTextsTheRateRestsOn()
    {
        var rules = Rules;
        var plugIn = Car(40_000m, new DateOnly(2026, 1, 10)) with { Engine = CarEngine.PetrolPlugInHybrid, Co2 = 40 };

        Compute(Car(40_000m, new DateOnly(2026, 1, 10))).DeductionSources.ShouldBe([rules.Acquisition]);
        Compute(plugIn with { Acquired = new DateOnly(2026, 2, 1) }).DeductionSources.ShouldBe([rules.Acquisition, rules.CompanyExclusions, rules.PlugInDeduction]);
        Compute(plugIn with { Acquired = new DateOnly(2024, 2, 1) }).DeductionSources
            .ShouldBe([rules.Acquisition, rules.FormulaDeduction, rules.CompanyExclusions, rules.DeductionPeriods[1].PlugInReading.ShouldNotBeNull()]);
        Compute(plugIn with { Acquired = new DateOnly(2022, 2, 1), FakeHybrid = true, MatchingCarCo2 = 100 }).DeductionSources
            .ShouldBe([rules.Acquisition, rules.FormulaDeduction, rules.CompanyExclusions, rules.PlugInDeduction]);
    }

    // An order or a lease after the income year is not a car the company holds in it.
    [Fact]
    public void Compute_carAcquiredAfterTheYear_isRefused()
    {
        CarCalculator.Compute(Rules, Car(40_000m, new DateOnly(2026, 1, 10)) with { Acquired = new DateOnly(2027, 1, 1) })
            .ShouldBe((null, CarCalculator.AcquiredAfterYear));
    }

    // A plug-in hybrid acquired in the first half of 2023 deducts its petrol or diesel at 50% at most
    // and its other costs at the formula (art. 550 al. 7): Netto does not split its costs yet.
    [Theory]
    [InlineData(2023, 1, 1)]
    [InlineData(2023, 6, 30)]
    public void Compute_plugInHybridAcquiredInTheFirstHalfOf2023_isRefused(int year, int month, int day)
    {
        CarCalculator.Compute(Rules, Car(40_000m, new DateOnly(2026, 1, 10)) with
            {
                Engine = CarEngine.DieselPlugInHybrid, Co2 = 45, Acquired = new DateOnly(year, month, day),
            })
            .ShouldBe((null, CarCalculator.PlugInFuelCapped));
    }

    [Theory]
    [InlineData(CarEngine.DieselPlugInHybrid, 2022, 12, 31)]
    [InlineData(CarEngine.DieselPlugInHybrid, 2023, 7, 1)]
    [InlineData(CarEngine.DieselHybrid, 2023, 3, 1)]
    public void Compute_carOutsideThatWindowOrNotAPlugIn_isTaken(CarEngine engine, int year, int month, int day)
    {
        CarCalculator.Compute(Rules, Car(40_000m, new DateOnly(2026, 1, 10)) with { Engine = engine, Co2 = 45, Acquired = new DateOnly(year, month, day) })
            .Error.ShouldBeNull();
    }

    [Fact]
    public void Compute_negativeCatalogueValue_isRefused()
    {
        CarCalculator.Compute(Rules, Car(-1m, new DateOnly(2026, 1, 10))).ShouldBe((null, CarCalculator.CatalogueValueNegative));
    }

    [Fact]
    public void Compute_negativeCosts_isRefused()
    {
        CarCalculator.Compute(Rules, Car(50_000m, new DateOnly(2026, 1, 10), yearlyCosts: -1m)).ShouldBe((null, CarCalculator.YearlyCostsNegative));
    }

    [Fact]
    public void Compute_negativeContribution_isRefused()
    {
        CarCalculator.Compute(Rules, Car(50_000m, new DateOnly(2026, 1, 10), contribution: -1m)).ShouldBe((null, CarCalculator.ContributionNegative));
    }
}
