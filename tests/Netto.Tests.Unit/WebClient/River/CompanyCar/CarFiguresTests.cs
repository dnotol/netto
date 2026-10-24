using Netto.Core.CompanyCar;
using Netto.Core.Rules;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.WebClient.River.CompanyCar;

// The car's figures, line by line, as its card shows them before where its money goes (CarFigures): keys
// of the card's texts with their amounts and their labels' arguments, on CarCalculatorTests' made-up cars
// of income year 2026 and CarViewTests' petrol cars.
public class CarFiguresTests
{
    private static readonly CarRules Rules = CarRules.IncomeYear2026;

    // CarViewTests' petrol car leased in January 2026: 45,000 at 120 g/km, a benefit of 4,050.00 at
    // 10.5%, and 9,000 of costs, of which the company deducts only the 4,050.00 equal to the benefit.
    private static readonly CarFacts Petrol = ElectricCar with
    {
        CatalogueValue = 45_000m, YearlyCosts = 9_000m, Engine = CarEngine.Petrol, Co2 = 120, Acquired = new DateOnly(2026, 1, 5),
    };

    [Fact]
    public void Benefit_electricCar_isItsBenefitItsCo2PercentageItsPeriodTheBenefitTaxedTheDeductionAndTheAddBack()
    {
        Lines(CarFigures.Benefit(Compute(ElectricCar))).ShouldBe(
        [
            ("Figure_Benefit", 1_714.29m, ""),
            ("Figure_Co2", 0.04m, ""),
            ("Figure_Period_Year", 1_714.29m, "January|December|100%"),
            ("Figure_BenefitTaxed", 1_714.29m, ""),
            ("Figure_Deduction_Full", 8_000m, "100%"),
            ("Figure_AddBack", 291.43m, "17%"),
        ]);
    }

    // The CO2 percentage keeps its decimals; every other line is in euros.
    [Fact]
    public void Benefit_co2Percentage_readsAsAPercentage()
    {
        CarFigures.Benefit(Compute(Petrol)).Select(line => (line.Key, line.Format)).Where(line => line.Format == FigureFormat.Percentage)
            .ShouldBe([("Figure_Co2", FigureFormat.Percentage)]);
    }

    // The contribution comes off the benefit after the minimum, a line that takes from it, before the
    // benefit taxed as pay.
    [Fact]
    public void Benefit_withAContribution_takesItOffBeforeTheBenefitTaxed()
    {
        Lines(CarFigures.Benefit(Compute(ElectricCar with { PersonalContribution = 600m }))).Take(5).ShouldBe(
        [
            ("Figure_Benefit", 1_714.29m, ""),
            ("Figure_Co2", 0.04m, ""),
            ("Figure_Period_Year", 1_714.29m, "January|December|100%"),
            ("Figure_Contribution", -600m, ""),
            ("Figure_BenefitTaxed", 1_114.29m, ""),
        ]);
    }

    // A contribution above the benefit leaves nothing taxed: the line says so, a plain zero.
    [Fact]
    public void Benefit_contributionAboveTheBenefit_saysNothingIsTaxed()
    {
        var taxed = CarFigures.Benefit(Compute(ElectricCar with { PersonalContribution = 2_000m })).Single(line => line.Key == "Figure_BenefitTaxed");

        (taxed.Amount, decimal.IsNegative(taxed.Amount)).ShouldBe((0m, false));
    }

    // At one coefficient all year, its one period is the year.
    [Fact]
    public void Benefit_oneCoefficientAllYear_isOnePeriodForTheYear()
    {
        Lines(CarFigures.Benefit(Compute(ElectricCar with { CatalogueValue = 80_000m, FirstRegistration = new DateOnly(2019, 3, 5) })))
            .Where(line => line.Key.StartsWith("Figure_Period", StringComparison.Ordinal))
            .ShouldBe([("Figure_Period_Year", 1_920m, "January|December|70%")]);
    }

    // First registered on 21.6.2025: January to May at 100% and June to December at 94%, each period a
    // line of its months and its coefficient.
    [Fact]
    public void Benefit_carTurningOneDuringTheYear_isALinePerPeriod()
    {
        Lines(CarFigures.Benefit(Compute(ElectricCar with { CatalogueValue = 60_000m, FirstRegistration = new DateOnly(2025, 6, 21) })))
            .Where(line => line.Key.StartsWith("Figure_Period", StringComparison.Ordinal))
            .ShouldBe([("Figure_Period", 851.04m, "January|May|100%"), ("Figure_Period", 1_133.74m, "June|December|94%")]);
    }

    // First registered on 10.2.2022: January alone at 82%, a period of one month named by it.
    [Fact]
    public void Benefit_periodOfOneMonth_namesThatMonth()
    {
        Lines(CarFigures.Benefit(Compute(ElectricCar with { CatalogueValue = 70_000m, FirstRegistration = new DateOnly(2022, 2, 10) })))
            .Where(line => line.Key.StartsWith("Figure_Period", StringComparison.Ordinal))
            .ShouldBe([("Figure_Period_Month", 167.15m, "January|January|82%"), ("Figure_Period", 1_669.08m, "February|December|76%")]);
    }

    // 30,000 × 6/7 × 4% = 1,028.57 is below the minimum: the benefit is the minimum, 1,690, above its
    // period's 1,028.57.
    [Fact]
    public void Benefit_belowTheMinimum_isTheMinimumAboveItsPeriod()
    {
        Lines(CarFigures.Benefit(Compute(ElectricCar with { CatalogueValue = 30_000m }))).Take(3).ShouldBe(
        [
            ("Figure_Benefit", 1_690m, ""),
            ("Figure_Co2", 0.04m, ""),
            ("Figure_Period_Year", 1_028.57m, "January|December|100%"),
        ]);
    }

    // A car that emits CO2 ordered from 2026 deducts only the part of its costs equal to the benefit
    // taxed and the contribution; the rest is added back.
    [Fact]
    public void Benefit_petrolCarOrderedIn2026_deductsThePartEqualToTheBenefitAndSaysWhatItCannotDeduct()
    {
        Lines(CarFigures.Benefit(Compute(Petrol))).Skip(3).ShouldBe(
        [
            ("Figure_BenefitTaxed", 4_050m, ""),
            ("Figure_Deduction_None", 4_050m, ""),
            ("Figure_NotDeductible", 4_950m, ""),
            ("Figure_AddBack", 688.50m, "17%"),
        ]);
    }

    // Ordered in November 2025, at 100 g/km: the 2,185.71 equal to the benefit in full and the rest at
    // 50%, 6,092.86 of its 10,000 in all.
    [Fact]
    public void Benefit_carOrderedIn2025_deductsTheRestAtItsRate()
    {
        var car = Compute(Petrol with { CatalogueValue = 30_000m, YearlyCosts = 10_000m, Co2 = 100, Acquired = new DateOnly(2025, 11, 1) });

        Lines(CarFigures.Benefit(car)).Where(line => line.Key.StartsWith("Figure_Deduction", StringComparison.Ordinal) || line.Key == "Figure_NotDeductible")
            .ShouldBe([("Figure_Deduction_Rate", 6_092.86m, "50%"), ("Figure_NotDeductible", 3_907.14m, "")]);
    }

    // Costs of 3,500 no more than its benefit of 4,932.00 are all deducted: nothing is added back of them.
    [Fact]
    public void Benefit_costsNoMoreThanTheBenefit_areAllDeducted()
    {
        var car = Compute(Petrol with
        {
            CatalogueValue = 60_000m, FirstRegistration = new DateOnly(2019, 3, 5), YearlyCosts = 3_500m, Engine = CarEngine.Diesel, Co2 = 140,
            Acquired = new DateOnly(2019, 2, 1),
        });

        Lines(CarFigures.Benefit(car)).Where(line => line.Key.StartsWith("Figure_Deduction", StringComparison.Ordinal) || line.Key == "Figure_NotDeductible")
            .ShouldBe([("Figure_Deduction_AllCarvedOut", 3_500m, "")]);
    }

    // Without costs there is nothing to deduct: no line says so.
    [Fact]
    public void Benefit_noCosts_hasNoDeductionLine()
    {
        CarFigures.Benefit(Compute(ElectricCar with { YearlyCosts = 0m })).Select(line => line.Key)
            .ShouldBe(["Figure_Benefit", "Figure_Co2", "Figure_Period_Year", "Figure_BenefitTaxed", "Figure_AddBack"]);
    }

    // A rate reads as its rule set states it, decimals kept: an add-back of 17.5% is no 18%, and an age
    // coefficient of 94.5% no 95%.
    [Fact]
    public void Benefit_ratesOfItsRules_readAsTheRulesStateThem()
    {
        var car = Compute(ElectricCar);
        var period = car.Periods.Single();
        var stated = car with
        {
            AddBackRate = new RuleAmount("car_add_back_rate", 0.175m, Rules.AddBackRate.Source),
            Periods = [period with { Period = period.Period with { AgeCoefficient = period.Period.AgeCoefficient with { Value = 0.945m } } }],
        };

        Lines(CarFigures.Benefit(stated)).Where(line => line.Key is "Figure_AddBack" or "Figure_Period_Year").Select(line => line.Args)
            .ShouldBe(["January|December|94.5%", "17.5%"]);
    }

    [Fact]
    public void Benefit_noCar_isNone()
    {
        CarFigures.Benefit(CarResult.None).ShouldBeEmpty();
    }

    // What a line's label cannot say is in its bubble: the share of its value the car's age leaves, that
    // the benefit is never paid in cash, how the company deducts costs it does not deduct in full, where
    // those it cannot deduct go, and what is added back of the benefit.
    [Fact]
    public void Benefit_linesNeedingExplaining_carryTheirBubbles()
    {
        CarFigures.Benefit(Compute(Petrol with { PersonalContribution = 600m })).Select(line => (line.Key, line.Help)).ShouldBe(
        [
            ("Figure_Benefit", null),
            ("Figure_Co2", null),
            ("Figure_Period_Year", "Help_Period"),
            ("Figure_Contribution", null),
            ("Figure_BenefitTaxed", "Help_BenefitTaxed"),
            ("Figure_Deduction_None", "Help_Deduction_None"),
            ("Figure_NotDeductible", "Help_NotDeductible"),
            ("Figure_AddBack", "Help_AddBack"),
        ]);
    }

    // The rate the rest is deducted at is in the bubble of a car ordered in 2025 too; a car deducted in
    // full needs none.
    [Fact]
    public void Benefit_deductionAtARate_saysTheRateInItsBubble()
    {
        var car = Compute(Petrol with { CatalogueValue = 30_000m, YearlyCosts = 10_000m, Co2 = 100, Acquired = new DateOnly(2025, 11, 1) });
        var deduction = CarFigures.Benefit(car).Single(line => line.Key == "Figure_Deduction_Rate");
        var full = CarFigures.Benefit(Compute(ElectricCar)).Single(line => line.Key == "Figure_Deduction_Full");

        (deduction.Help, string.Join("|", deduction.HelpArgs), full.Help).ShouldBe(("Help_Deduction_Rate", "50%", (string?)null));
    }

    // The plan with the car against the same plan without it: the pocket in each, the cash the car takes,
    // and what its benefit adds to the contributions and tax on pay, with where they are in its bubble.
    [Fact]
    public void Cash_comparison_isThePocketWithAndWithoutTheCashTheCarTakesAndWhatItsBenefitAdds()
    {
        var lines = CarFigures.Cash(new CarComparison(40_000m, 43_000m, 1_000m));

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).ShouldBe([("Figure_WithCar", 40_000m, ""), ("Figure_WithoutCar", 43_000m, ""), ("Figure_Cash_Takes", 3_000m, ""), ("Figure_TaxesOnPay", 1_000m, "")]),
            () => lines.Select(line => line.Help).ShouldBe([null, null, null, "Help_TaxesOnPay"]));
    }

    // The cash each way it can fall, its amount the cash itself: some taken, none, or more left in the
    // pocket than without the car.
    public static TheoryData<decimal, decimal, string, decimal> Cash => new()
    {
        { 40_000m, 43_000m, "Figure_Cash_Takes", 3_000m },
        { 40_000m, 40_000m, "Figure_Cash_None", 0m },
        { 41_000m, 40_000m, "Figure_Cash_Adds", 1_000m },
    };

    [Theory]
    [MemberData(nameof(Cash))]
    public void Cash_eachWay_isItsLineWithTheCash(decimal pocketWith, decimal pocketWithout, string key, decimal cash)
    {
        var line = CarFigures.Cash(new CarComparison(pocketWith, pocketWithout, 0m))[2];

        (line.Key, line.Amount, decimal.IsNegative(line.Amount)).ShouldBe((key, cash, false));
    }

    private static CarResult Compute(CarFacts car)
    {
        var (result, error) = CarCalculator.Compute(Rules, car);
        error.ShouldBeNull();
        return result.ShouldNotBeNull();
    }

    // Each line as its key, its amount and its label's arguments, its bubble and how it reads aside.
    private static List<(string Key, decimal Amount, string Args)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, string.Join("|", line.Args)))];
}
