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

    // What the car takes in cash from the pocket, each way it can fall: some, none, or more cash left in
    // the pocket than without it, which a benefit that keeps the reduced corporate rate can do.
    [Fact]
    public void CarCardResx_everyCashLine_hasALabel()
    {
        var comparisons = new[] { new CarComparison(40_000m, 43_000m, 1_000m), new CarComparison(40_000m, 40_000m, 0m), new CarComparison(41_000m, 40_000m, 1_000m) };

        comparisons.Select(CarCardText.Cash).ShouldBe(["Figure_Cash_Takes", "Figure_Cash_None", "Figure_Cash_Adds"]);
        Resources.Missing(comparisons.Select(CarCardText.Cash)).ShouldBeEmpty();
    }

    // The line's amount is the cash; its label says which way it goes.
    [Theory]
    [InlineData("Figure_Cash_Takes", "Cash the car takes")]
    [InlineData("Figure_Cash_None", "Cash the car takes")]
    [InlineData("Figure_Cash_Adds", "Cash the car adds")]
    public void Cash_eachWay_isLabelledByWhichWayTheCashGoes(string key, string expected)
    {
        Resources.Get(key).ShouldBe(expected);
    }

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

    // The gain or the loss against the same car bought privately; the private cost it is weighed against
    // is in its field, just above, so the sentence does not say it again.
    [Theory]
    [InlineData("Private_Gain", "The company car leaves you €2,000.00 more than a private one.")]
    [InlineData("Private_Loss", "The company car costs you €2,000.00 more than a private one.")]
    [InlineData("Private_Tie", "The company car costs you what a private one would.")]
    public void Private_eachWay_saysTheGainOrLossAgainstThePrivateCar(string key, string expected)
    {
        Resources.Format(key, "€2,000.00", "€9,000.00").ShouldBe(expected);
    }

    // Every engine's name, the private energy it is worded with, its hint and its reading.
    [Fact]
    public void CarCardResx_everyEngine_hasItsTexts()
    {
        var keys = Enum.GetValues<CarEngine>().SelectMany(engine => new[] { CarCardText.Engine(engine), CarCardText.PrivateEnergy(engine), CarCardText.PrivateEnergyHint(engine) }
            .Concat(CarCardText.Reading(engine) is { } reading ? [reading] : []));

        Resources.Missing(keys).ShouldBeEmpty();
    }

    // Every way the CO2 percentage is taken, and every way the company deducts the costs, with the bubble of
    // each that does not deduct them at one rate.
    [Fact]
    public void CarCardResx_everyCo2SentenceAndDeductionLine_hasAText()
    {
        var keys = Enum.GetValues<CarCo2Basis>().Select(basis => CarCardText.Co2(CarResult.None with { Co2Basis = basis }))
            .Concat(["Co2_HeldToMinimum", "Co2_HeldToMaximum", "Figure_Deduction_Full", "Figure_Deduction_Rate", "Figure_Deduction_None", "Figure_Deduction_AllCarvedOut",
                "Help_Deduction_Rate", "Help_Deduction_None", "Help_Deduction_AllCarvedOut"]);

        Resources.Missing(keys).ShouldBeEmpty();
    }

    // A part of the benefit's year by its months, a month begun counting whole: the whole year, one month, or
    // from one month to another.
    [Fact]
    public void CarCardResx_everyPeriodLine_hasALabel()
    {
        var keys = new[]
        {
            CarCardText.Period(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            CarCardText.Period(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)),
            CarCardText.Period(new DateOnly(2026, 6, 1), new DateOnly(2026, 12, 31)),
        };

        keys.ShouldSatisfyAllConditions(
            () => keys.ShouldBe(["Figure_Period_Year", "Figure_Period_Month", "Figure_Period"]),
            () => Resources.Missing(keys).ShouldBeEmpty());
    }

    // Every line the car's figures draw, and what its bubble says, on cars that take each of them: new or
    // turning a year older during the year, at the minimum, with a contribution, deducted each way, its
    // private charging paid; and the cash each way it falls.
    [Fact]
    public void CarCardResx_everyLineOfTheFiguresAndItsBubble_hasAText()
    {
        var electric = new CarFacts(50_000m, new DateOnly(2026, 1, 10), 8_000m, false, 600m);
        var petrol = electric with { CatalogueValue = 45_000m, YearlyCosts = 9_000m, Engine = CarEngine.Petrol, Co2 = 120, Acquired = new DateOnly(2026, 1, 5) };
        CarFacts[] cars =
        [
            electric, electric with { FirstRegistration = new DateOnly(2022, 2, 10), CompanyPaysPrivateEnergy = true }, electric with { CatalogueValue = 30_000m },
            petrol, petrol with { Acquired = new DateOnly(2025, 11, 1), Co2 = 100 }, petrol with { YearlyCosts = 1_000m },
        ];
        var lines = cars.SelectMany(car => CarFigures.Benefit(CarCalculator.Compute(Rules, car).Value.ShouldNotBeNull()))
            .Concat(new[] { new CarComparison(40_000m, 43_000m, 1_000m), new CarComparison(40_000m, 40_000m, 0m), new CarComparison(41_000m, 40_000m, 1_000m) }
                .SelectMany(CarFigures.Cash))
            .ToList();

        Resources.Missing(lines.Select(line => line.Key).Concat(lines.Select(line => line.Help).OfType<string>()).Distinct()).ShouldBeEmpty();
    }

    // The car's summary ends on what the split shares: « before your pay », and « before your pay and
    // royalty » once the royalty is in the plan.
    [Fact]
    public void CarCardResx_everySummary_hasAText()
    {
        Resources.Missing([CarCardText.Summary(true, false), CarCardText.Summary(true, true), CarCardText.Summary(false, false), CarCardText.Summary(false, true)])
            .ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, false, "Summary")]
    [InlineData(true, true, "Summary_Royalty")]
    [InlineData(false, false, "SummaryNoCar")]
    [InlineData(false, true, "SummaryNoCar_Royalty")]
    public void CarSummary_royaltyInThePlan_saysBeforeYourPayAndRoyalty(bool hasCar, bool royalty, string key)
    {
        CarCardText.Summary(hasCar, royalty).ShouldBe(key);
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
            }),
        ];
        var phrases = variations.SelectMany(inputs => CarSettingsView.From(inputs).Car);

        Resources.Missing(phrases.Select(phrase => phrase.Key).Append("CarTitle")).ShouldBeEmpty();
    }
}
