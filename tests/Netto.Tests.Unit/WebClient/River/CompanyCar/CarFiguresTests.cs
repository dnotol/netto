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

    // An electric car on the scale, no contribution, its costs all deducted: its CO2 percentage, named as an
    // electric car's minimum, the benefit taxed once, and what is added back. Its one period is the benefit
    // itself, the benefit before a contribution of nothing the benefit taxed, and costs all deducted are the
    // costs' own line: none of them is a line.
    [Fact]
    public void Benefit_electricCar_isItsCo2PercentageTheBenefitTaxedOnceAndTheAddBack()
    {
        var lines = CarFigures.Benefit(Compute(ElectricCar));

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).ShouldBe([("Figure_Co2", 0.04m, ""), ("Figure_BenefitTaxed", 1_714.29m, ""), ("Figure_AddBack", 291.43m, "17%")]),
            () => lines.Select(line => line.Label).ShouldBe(["Figure_Co2_Electric", null, null]));
    }

    // A car that emits CO2 keeps the plain CO2 percentage's words.
    [Fact]
    public void Benefit_petrolCar_namesItsCo2PercentagePlainly()
    {
        CarFigures.Benefit(Compute(Petrol)).Single(line => line.Key == "Figure_Co2").Label.ShouldBeNull();
    }

    // The CO2 percentage keeps its decimals; every other line is in euros.
    [Fact]
    public void Benefit_co2Percentage_readsAsAPercentage()
    {
        CarFigures.Benefit(Compute(Petrol)).Select(line => (line.Key, line.Format)).Where(line => line.Format == FigureFormat.Percentage)
            .ShouldBe([("Figure_Co2", FigureFormat.Percentage)]);
    }

    // The contribution comes off the benefit, a line that takes from it, before the benefit taxed as pay.
    [Fact]
    public void Benefit_withAContribution_takesItOffBeforeTheBenefitTaxed()
    {
        Lines(CarFigures.Benefit(Compute(ElectricCar with { PersonalContribution = 600m }))).Take(4).ShouldBe(
        [
            ("Figure_Co2", 0.04m, ""),
            ("Figure_Benefit", 1_714.29m, ""),
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

    // At one coefficient all year, its one period is the benefit itself: no line of its own.
    [Fact]
    public void Benefit_oneCoefficientAllYear_drawsNoPeriod()
    {
        CarFigures.Benefit(Compute(ElectricCar with { CatalogueValue = 80_000m, FirstRegistration = new DateOnly(2019, 3, 5) }))
            .Where(line => line.Key.StartsWith("Figure_Period", StringComparison.Ordinal)).ShouldBeEmpty();
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

    // Held from 17.9.2026 (CarCalculatorTests, derived from the 2026 amounts): its one period is a line, by its
    // first and last days held, its coefficient and its days, since the benefit no longer counts the year.
    [Fact]
    public void Benefit_carHeldFromSeptember_drawsItsPeriodByItsDays()
    {
        Lines(CarFigures.Benefit(Compute(ElectricCar with { HeldFrom = new DateOnly(2026, 9, 17) })))
            .Where(line => line.Key.StartsWith("Figure_Period", StringComparison.Ordinal))
            .ShouldBe([("Figure_Period_Days", 497.85m, "17 Sep|31 Dec|100%|106")]);
    }

    // First registered on 20.5.2025 and given back on 8.8.2026: 120 days at 100% and 99 at 94%, each a line.
    [Fact]
    public void Benefit_carReturnedDuringTheYear_drawsEachPeriodByItsDays()
    {
        var car = ElectricCar with { CatalogueValue = 60_000m, FirstRegistration = new DateOnly(2025, 5, 20), ReturnedOn = new DateOnly(2026, 8, 8) };

        Lines(CarFigures.Benefit(Compute(car))).Where(line => line.Key.StartsWith("Figure_Period", StringComparison.Ordinal))
            .ShouldBe([("Figure_Period_Days", 676.32m, "1 Jan|30 Apr|100%|120"), ("Figure_Period_Days", 524.49m, "1 May|7 Aug|94%|99")]);
    }

    // Held 30 days in March below the minimum: the benefit taxed is the minimum for those days, 138.90, worded
    // so with its days, the 57.77 the scale gives and the days in its bubble.
    [Fact]
    public void Benefit_carHeldAMonthBelowTheMinimum_isTheMinimumForItsDays()
    {
        var car = ElectricCar with
        {
            CatalogueValue = 25_000m, FirstRegistration = new DateOnly(2022, 10, 5), HeldFrom = new DateOnly(2026, 3, 1), ReturnedOn = new DateOnly(2026, 3, 31),
        };

        var lines = CarFigures.Benefit(Compute(car));

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).ShouldBe([("Figure_BenefitTaxed", 138.90m, "30"), ("Figure_AddBack", 23.61m, "17%")]),
            () => (lines[0].Label, lines[0].Help, string.Join("|", lines[0].HelpArgs)).ShouldBe(("Figure_BenefitTaxed_MinimumDays", "Help_MinimumDays", "€57.77|30")));
    }

    // 30,000 × 6/7 × 4% = 1,028.57 is below the minimum: the benefit taxed is the minimum, 1,690, one line
    // worded as the year's minimum, the 1,028.57 the scale gives in its bubble; no CO2 percentage nor period,
    // which no longer set it.
    [Fact]
    public void Benefit_belowTheMinimum_isTheMinimumAloneWithWhatTheScaleGivesInItsBubble()
    {
        var lines = CarFigures.Benefit(Compute(ElectricCar with { CatalogueValue = 30_000m }));

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).ShouldBe([("Figure_BenefitTaxed", 1_690m, ""), ("Figure_AddBack", 287.30m, "17%")]),
            () => (lines[0].Label, lines[0].Help, string.Join("|", lines[0].HelpArgs)).ShouldBe(("Figure_BenefitTaxed_Minimum", "Help_Minimum", "€1,028.57")));
    }

    // At the minimum with a contribution, the benefit before it is the minimum, so worded, and the benefit
    // taxed what the contribution leaves.
    [Fact]
    public void Benefit_belowTheMinimumWithAContribution_saysTheMinimumBeforeTheContribution()
    {
        var lines = CarFigures.Benefit(Compute(ElectricCar with { CatalogueValue = 30_000m, PersonalContribution = 600m }));

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).Take(3).ShouldBe([("Figure_Benefit", 1_690m, ""), ("Figure_Contribution", -600m, ""), ("Figure_BenefitTaxed", 1_090m, "")]),
            () => lines.Take(3).Select(line => line.Label).ShouldBe(["Figure_Benefit_Minimum", null, null]));
    }

    // A car that emits CO2 ordered from 2026 deducts only the part of its costs equal to the benefit taxed
    // and the contribution: one line of what it cannot deduct, the rate it deducts the rest at in its label,
    // how in its bubble.
    [Fact]
    public void Benefit_petrolCarOrderedIn2026_saysWhatItCannotDeductAtItsRate()
    {
        var lines = CarFigures.Benefit(Compute(Petrol));
        var line = lines.Single(each => each.Key == "Figure_NotDeductible");

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).Skip(1).ShouldBe([("Figure_BenefitTaxed", 4_050m, ""), ("Figure_NotDeductible", 4_950m, "0%"), ("Figure_AddBack", 688.50m, "17%")]),
            () => (line.Label, line.Help, string.Join("|", line.HelpArgs)).ShouldBe(("Figure_NotDeductible_Rate", "Help_Deduction_None", "0%")));
    }

    // Ordered in November 2025, at 100 g/km: the 2,185.71 equal to the benefit in full and the rest at
    // 50%, so 3,907.14 of its 10,000 it cannot deduct.
    [Fact]
    public void Benefit_carOrderedIn2025_saysWhatItCannotDeductAtItsRate()
    {
        var car = Compute(Petrol with { CatalogueValue = 30_000m, YearlyCosts = 10_000m, Co2 = 100, Acquired = new DateOnly(2025, 11, 1) });

        var line = CarFigures.Benefit(car).Single(each => each.Key == "Figure_NotDeductible");

        (line.Amount, string.Join("|", line.Args), line.Help, string.Join("|", line.HelpArgs)).ShouldBe((3_907.14m, "50%", "Help_Deduction_Rate", "50%"));
    }

    // Costs all deducted, being no more than the benefit, or deducted in full, or none at all: no line.
    public static TheoryData<string, CarFacts> AllDeducted => new()
    {
        { "no more than the benefit", Petrol with { CatalogueValue = 60_000m, FirstRegistration = new DateOnly(2019, 3, 5), YearlyCosts = 3_500m, Engine = CarEngine.Diesel, Co2 = 140, Acquired = new DateOnly(2019, 2, 1) } },
        { "deducted in full", ElectricCar },
        { "no costs", ElectricCar with { YearlyCosts = 0m } },
    };

    [Theory]
    [MemberData(nameof(AllDeducted))]
    public void Benefit_costsAllDeducted_drawNoDeductionLine(string _, CarFacts car)
    {
        CarFigures.Benefit(Compute(car)).Select(line => line.Key).ShouldBe(["Figure_Co2", "Figure_BenefitTaxed", "Figure_AddBack"]);
    }

    // A rate reads as its rule set states it, decimals kept: an add-back of 17.5% is no 18%, and an age
    // coefficient of 94.5% no 95%.
    [Fact]
    public void Benefit_ratesOfItsRules_readAsTheRulesStateThem()
    {
        var car = Compute(ElectricCar with { CatalogueValue = 60_000m, FirstRegistration = new DateOnly(2025, 6, 21) });
        var stated = car with
        {
            AddBackRate = new RuleAmount("car_add_back_rate", 0.175m, Rules.AddBackRate.Source),
            Periods = [.. car.Periods.Select(period => period with { Period = period.Period with { AgeCoefficient = period.Period.AgeCoefficient with { Value = 0.945m } } })],
        };

        Lines(CarFigures.Benefit(stated)).Where(line => line.Key is "Figure_AddBack" or "Figure_Period").Select(line => line.Args)
            .ShouldBe(["January|May|94.5%", "June|December|94.5%", "17.5%"]);
    }

    [Fact]
    public void Benefit_noCar_isNone()
    {
        CarFigures.Benefit(CarResult.None).ShouldBeEmpty();
    }

    // What a line's label cannot say is in its bubble: the share of its value the car's age leaves, that
    // the benefit is never paid in cash, how the company deducts costs it does not deduct in full, and what
    // is added back of the benefit.
    [Fact]
    public void Benefit_linesNeedingExplaining_carryTheirBubbles()
    {
        CarFigures.Benefit(Compute(Petrol with { PersonalContribution = 600m, FirstRegistration = new DateOnly(2025, 6, 21) })).Select(line => (line.Key, line.Help)).ShouldBe(
        [
            ("Figure_Co2", null),
            ("Figure_Period", "Help_Period"),
            ("Figure_Period", "Help_Period"),
            ("Figure_Benefit", null),
            ("Figure_Contribution", null),
            ("Figure_BenefitTaxed", "Help_BenefitTaxed"),
            ("Figure_NotDeductible", "Help_Deduction_None"),
            ("Figure_AddBack", "Help_AddBack"),
        ]);
    }

    // The plan with the car against the same plan without it: the pocket in each, and what its benefit adds
    // to the contributions and income tax, with where they are in its bubble. The cash the car takes is the
    // card's summary.
    [Fact]
    public void Cash_comparison_isThePocketWithAndWithoutAndWhatItsBenefitAdds()
    {
        var lines = CarFigures.Cash(new CarComparison(40_000m, 43_000m, 1_000m));

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).ShouldBe([("Figure_WithCar", 40_000m, ""), ("Figure_WithoutCar", 43_000m, ""), ("Figure_TaxesOnPay", 1_000m, "")]),
            () => lines.Select(line => line.Help).ShouldBe([null, null, "Help_TaxesOnPay"]));
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
