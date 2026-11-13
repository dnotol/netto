using Netto.Core;
using Netto.Core.CompanyCar;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Shouldly;

namespace Netto.Tests.WebClient.River.CompanyCar;

// The car card builds resource keys from what it compares and the lines it draws. A key without a text in
// its .resx would show on screen as the key itself.
public class CarCardTextTests
{
    private static readonly ResxText Resources = ResxText.For<CarCard>();

    private static readonly CarRules Rules = CarRules.IncomeYear2026;

    // The company car against the same car bought privately, each way it can fall, and the field that
    // asks what that car would cost, with what to count.
    [Fact]
    public void CarCardResx_everyPrivateSentenceAndTheField_haveATextEach()
    {
        var comparisons = new[]
        {
            new CarComparison(40_000m, 43_000m, 1_000m),
            new CarComparison(40_000m, 43_000m, 1_000m) { PrivateCost = 5_000m },
            new CarComparison(40_000m, 43_000m, 1_000m) { PrivateCost = 1_000m },
            new CarComparison(40_000m, 43_000m, 1_000m) { PrivateCost = 3_000m },
        };

        comparisons.Select(CarCardText.Private).ShouldBe(["Private_Waiting", "Private_Gain", "Private_Loss", "Private_Tie"]);
        Resources.Missing(comparisons.Select(CarCardText.Private).Concat(["PrivateCostLabel", "PrivateCostHint"])).ShouldBeEmpty();
    }

    // The gain or the loss against the same car bought privately is the sentence's amount, none where the
    // two tie; the private cost it is weighed against is in its field, just above, so the sentence does
    // not say it again.
    [Theory]
    [InlineData("Private_Gain", "€2,000.00")]
    [InlineData("Private_Loss", "€2,000.00")]
    [InlineData("Private_Tie", "")]
    public void Private_eachWay_drawsTheGapAndNeverThePrivateCost(string key, string amount)
    {
        string.Join(' ', CardHtml.Amounts(Resources.Format(key, "€2,000.00", "€9,000.00"))).ShouldBe(amount);
    }

    // The note naming the pay both plans of the comparison run at, when the pay set is held below.
    [Fact]
    public void CarCardResx_payHeldNote_hasATextNamingThePay()
    {
        Resources.Missing(["PayHeld"]).ShouldBeEmpty();
        CardHtml.Amounts(Resources.Format("PayHeld", "€80,000.00")).ShouldBe(["€80,000.00"]);
    }

    // Every engine's name, the private energy it is worded with, its hint and its reading.
    [Fact]
    public void CarCardResx_everyEngine_hasItsTexts()
    {
        var keys = Enum.GetValues<CarEngine>().SelectMany(engine => new[] { CarCardText.Engine(engine), CarCardText.PrivateEnergy(engine), CarCardText.PrivateEnergyHint(engine) }
            .Concat(CarCardText.Reading(engine) is { } reading ? [reading] : []));

        Resources.Missing(keys).ShouldBeEmpty();
    }

    // Every way the CO2 percentage is taken, and every way the company deducts costs it does not deduct in
    // full, in the bubble of what it cannot deduct.
    [Fact]
    public void CarCardResx_everyCo2SentenceAndDeductionBubble_hasAText()
    {
        var keys = Enum.GetValues<CarCo2Basis>().Select(basis => CarCardText.Co2(CarResult.None with { Co2Basis = basis }))
            .Concat(["Co2_HeldToMinimum", "Co2_HeldToMaximum", "Help_Deduction_Rate", "Help_Deduction_None"]);

        Resources.Missing(keys).ShouldBeEmpty();
    }

    // The bubble of what the company cannot deduct: that part alone, or the rest at its rate; none while it
    // deducts them all.
    [Theory]
    [InlineData(0, 4_950, "Help_Deduction_None")]
    [InlineData(0.5, 3_907.14, "Help_Deduction_Rate")]
    [InlineData(1, 0, null)]
    [InlineData(0, 0, null)]
    public void DeductionHelp_eachWay_isItsBubble(decimal rate, decimal notDeductible, string? key)
    {
        var car = CarResult.None with { Costs = 10_000m, Deductible = 10_000m - notDeductible, DeductionRate = rate };

        CarCardText.DeductionHelp(car).ShouldBe(key);
    }

    // A part of the benefit's year by its months, a month begun counting whole, one of several: one month, or
    // from one month to another; a car held part of the year, by its days.
    [Fact]
    public void CarCardResx_everyPeriodLine_hasALabel()
    {
        var keys = new[]
        {
            CarCardText.Period(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), heldAllYear: true),
            CarCardText.Period(new DateOnly(2026, 6, 1), new DateOnly(2026, 12, 31), heldAllYear: true),
            CarCardText.Period(new DateOnly(2026, 9, 17), new DateOnly(2026, 12, 31), heldAllYear: false),
        };

        keys.ShouldSatisfyAllConditions(
            () => keys.ShouldBe(["Figure_Period_Month", "Figure_Period", "Figure_Period_Days"]),
            () => Resources.Missing(keys).ShouldBeEmpty());
    }

    // A car held part of the year at the minimum: the minimum for its days, worded so, and its bubble saying it.
    [Theory]
    [InlineData(0, "Figure_Benefit_MinimumDays", "Figure_BenefitTaxed_MinimumDays")]
    [InlineData(600, "Figure_Benefit_MinimumDays", null)]
    public void BenefitLabels_heldPartOfTheYearAtTheMinimum_sayTheDays(decimal contribution, string beforeContribution, string? taxed)
    {
        var car = CarResult.None with
        {
            BenefitBeforeContribution = 138.90m, BenefitOnTheScale = 57.77m, Contribution = contribution,
            DaysHeld = new DaysHeld(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 30), 30, 365),
        };

        (CarCardText.BenefitLabel(car), CarCardText.BenefitTaxedLabel(car), CarCardText.MinimumHelp(car))
            .ShouldBe((beforeContribution, taxed, "Help_MinimumDays"));
        Resources.Missing(["Figure_Benefit_MinimumDays", "Figure_BenefitTaxed_MinimumDays", "Help_MinimumDays", "Help_Minimum"]).ShouldBeEmpty();
    }

    // The CO2 percentage of an electric car is named as the minimum the law gives it; any other car's plainly.
    [Theory]
    [InlineData(CarCo2Basis.None, "Figure_Co2_Electric")]
    [InlineData(CarCo2Basis.Registered, null)]
    public void Co2Label_eachCar_isItsWords(CarCo2Basis basis, string? label)
    {
        CarCardText.Co2Label(CarResult.None with { Co2Basis = basis }).ShouldBe(label);
    }

    // At the year's minimum, the benefit before a contribution is worded as the minimum, and the benefit taxed
    // too while no contribution comes off it; on the scale, both keep their keys' words.
    [Theory]
    [InlineData(1_690, 1_028.57, 0, "Figure_Benefit_Minimum", "Figure_BenefitTaxed_Minimum")]
    [InlineData(1_690, 1_028.57, 600, "Figure_Benefit_Minimum", null)]
    [InlineData(1_714.29, 1_714.29, 0, null, null)]
    [InlineData(1_714.29, 1_714.29, 600, null, null)]
    public void BenefitLabels_atTheMinimumOrOnTheScale_areTheirWords(decimal benefit, decimal scale, decimal contribution, string? beforeContribution, string? taxed)
    {
        var car = CarResult.None with { BenefitBeforeContribution = benefit, BenefitOnTheScale = scale, Contribution = contribution };

        (CarCardText.BenefitLabel(car), CarCardText.BenefitTaxedLabel(car)).ShouldBe((beforeContribution, taxed));
    }

    // What the company cannot deduct is worded with the rate it deducts at; there is no such line while it
    // deducts them all.
    [Theory]
    [InlineData(10_000, 5_050, "Figure_NotDeductible_Rate")]
    [InlineData(10_000, 10_000, null)]
    public void NotDeductibleLabel_eachWay_isItsWords(decimal costs, decimal deductible, string? label)
    {
        CarCardText.NotDeductibleLabel(CarResult.None with { Costs = costs, Deductible = deductible }).ShouldBe(label);
    }

    [Fact]
    public void CarCardResx_everyLabelOfItsOwn_hasAText()
    {
        Resources.Missing(["Figure_Co2_Electric", "Figure_Benefit_Minimum", "Figure_BenefitTaxed_Minimum", "Figure_NotDeductible_Rate"]).ShouldBeEmpty();
    }

    // Every line the car's figures draw, and what its bubble says, on cars that take each of them: new or
    // turning a year older during the year, at the minimum, with a contribution, deducted each way, its
    // private charging paid, at the minimum with a contribution, turning a year older; and the plan with the
    // car against the one without. A line worded by a label of its own is held to that label.
    [Fact]
    public void CarCardResx_everyLineOfTheFiguresAndItsBubble_hasAText()
    {
        var electric = new CarFacts(50_000m, new DateOnly(2026, 1, 10), 8_000m, false, 600m);
        var petrol = electric with { CatalogueValue = 45_000m, YearlyCosts = 9_000m, Engine = CarEngine.Petrol, Co2 = 120, Acquired = new DateOnly(2026, 1, 5) };
        CarFacts[] cars =
        [
            electric, electric with { FirstRegistration = new DateOnly(2022, 2, 10), CompanyPaysPrivateEnergy = true }, electric with { CatalogueValue = 30_000m },
            petrol, petrol with { Acquired = new DateOnly(2025, 11, 1), Co2 = 100 }, petrol with { YearlyCosts = 1_000m },
            electric with { CatalogueValue = 30_000m, PersonalContribution = 0m }, electric with { FirstRegistration = new DateOnly(2025, 6, 21) },
        ];
        var lines = cars.SelectMany(car => CarFigures.Benefit(CarCalculator.Compute(Rules, car).Value.ShouldNotBeNull()))
            .Concat(new[] { new CarComparison(40_000m, 43_000m, 1_000m), new CarComparison(40_000m, 40_000m, 0m), new CarComparison(41_000m, 40_000m, 1_000m) }
                .SelectMany(CarFigures.Cash))
            .ToList();

        Resources.Missing(lines.Select(line => line.Label ?? line.Key).Concat(lines.Select(line => line.Help).OfType<string>()).Distinct()).ShouldBeEmpty();
    }

    // The car's summary says what it adds to the pocket or takes from it, or that there is no car yet.
    [Theory]
    [InlineData(true, -1_200, "Summary_Less")]
    [InlineData(true, 300, "Summary_More")]
    [InlineData(true, 0, "Summary_None")]
    [InlineData(false, 0, "SummaryNoCar")]
    public void CarSummary_aCarOrNone_saysWhatItAddsToThePocketOrThatThereIsNone(bool hasCar, decimal gain, string key)
    {
        (CarCardText.Summary(hasCar, gain), Resources.Get(key) is not null).ShouldBe((key, true));
    }

    // Every way the line of « Your car » reads: each engine, its CO2 given, not given or none
    // registered, a plug-in hybrid real or fake with or without a matching car, a natural gas car small
    // or not, and the dates.
    [Fact]
    public void CarCardResx_everyPhraseOfItsSettingsLine_hasAText()
    {
        var example = RiverInputs.Example.WithCatalogueValue(45_000m).WithFirstRegistration(new DateOnly(2021, 3, 1));
        RiverInputs[] variations =
        [
            example,
            .. Enum.GetValues<CarEngine>().SelectMany(engine => new[]
            {
                example.WithCarEngine(engine),
                example.WithCarEngine(engine).WithCarCo2(118),
                example.WithCarEngine(engine).WithCarCo2(118).WithNoCo2Data(true),
                example.WithCarEngine(engine).WithCarCo2(118).WithFakeHybrid(true).WithMatchingCarCo2(112),
                example.WithCarEngine(engine).WithCarCo2(118).WithFakeHybrid(true).WithNoMatchingCar(true),
                example.WithCarEngine(engine).WithCarCo2(118).WithFakeHybrid(true),
                example.WithCarEngine(engine).WithCarCo2(118).WithSmallNaturalGas(true),
                example.WithCarEngine(engine).WithCarCo2(118).WithCarAcquired(new DateOnly(2017, 6, 1)).WithFakeHybrid(true).WithMatchingCarCo2(112),
            }),
        ];
        var phrases = variations.SelectMany(inputs => CarSettingsView.From(inputs).Car);

        Resources.Missing(phrases.Select(phrase => phrase.Key).Append("CarTitle")).ShouldBeEmpty();
    }
}
