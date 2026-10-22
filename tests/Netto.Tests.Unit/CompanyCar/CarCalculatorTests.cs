using Netto.Core.BenefitsInKind;
using Netto.Core.CompanyCar;
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
    // contribution, the contribution, the costs, what the company cannot deduct with the add-back
    // (an electric car's costs in full, so only the add-back), and the muddy pocket at the costs.
    [Fact]
    public void Compute_electricCar_handsTheYearItsBenefitInKind()
    {
        var result = Compute(Car(50_000m, new DateOnly(2026, 1, 10), yearlyCosts: 8_000m, contribution: 600m));

        result.BenefitInKind.ShouldBe(new BenefitInKind(TaxedAsPay: 1_114.29m, Contribution: 600m, Costs: 8_000m, NotDeductible: 291.43m, MuddyPocket: 8_000m));
    }

    [Fact]
    public void Compute_none_handsNoBenefitInKind()
    {
        Compute(CarFacts.None).BenefitInKind.ShouldBe(BenefitInKind.None);
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
