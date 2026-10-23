using System.Globalization;
using System.Resources;
using Netto.Core.CompanyCar;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Shouldly;

namespace Netto.Tests.WebClient.River.CompanyCar;

// The car card builds resource keys from what it compares. A key without a text in its .resx would
// show on screen as the key itself.
public class CarCardTextTests
{
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.CompanyCar.CarCard", typeof(CarCard).Assembly);

    // What the car takes in cash from the pocket, each way it can fall: some, none, or more cash left in
    // the pocket than without it, which a benefit that keeps the reduced corporate rate can do.
    [Fact]
    public void CarCardResx_everyCashSentence_hasAText()
    {
        var comparisons = new[] { new CarComparison(40_000m, 43_000m, 1_000m), new CarComparison(40_000m, 40_000m, 0m), new CarComparison(41_000m, 40_000m, 1_000m) };

        comparisons.Select(CarCardText.Cash).ShouldBe(["Cash_Takes", "Cash_None", "Cash_Adds"]);
        Missing(comparisons.Select(CarCardText.Cash)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Cash_Takes", "The car takes €3,000.00 of cash from your pocket.")]
    [InlineData("Cash_None", "The car takes no cash from your pocket.")]
    [InlineData("Cash_Adds", "The car leaves €3,000.00 more cash in your pocket.")]
    public void Cash_eachWay_saysTheCashInTheSentence(string key, string expected)
    {
        string.Format(CultureInfo.InvariantCulture, Resources.GetString(key, CultureInfo.InvariantCulture) ?? "", "€3,000.00").ShouldBe(expected);
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
        Missing(comparisons.Select(CarCardText.Private).Concat(["PrivateCostLabel", "PrivateCostHint"])).ShouldBeEmpty();
    }

    // The gain or loss first, then the private cost it is weighed against.
    [Theory]
    [InlineData("Private_Gain", "Against the same car bought privately for €9,000.00, the company car leaves you €2,000.00 more.")]
    [InlineData("Private_Loss", "Against the same car bought privately for €9,000.00, the company car costs you €2,000.00 more.")]
    [InlineData("Private_Tie", "The company car costs you what the same car bought privately would, €9,000.00.")]
    public void Private_eachWay_saysTheGainOrLossAgainstThePrivateCost(string key, string expected)
    {
        string.Format(CultureInfo.InvariantCulture, Resources.GetString(key, CultureInfo.InvariantCulture) ?? "", "€2,000.00", "€9,000.00").ShouldBe(expected);
    }

    // Every engine's name, the private energy it is worded with, its hint and its reading.
    [Fact]
    public void CarCardResx_everyEngine_hasItsTexts()
    {
        var keys = Enum.GetValues<CarEngine>().SelectMany(engine => new[] { CarCardText.Engine(engine), CarCardText.PrivateEnergy(engine), CarCardText.PrivateEnergyHint(engine) }
            .Concat(CarCardText.Reading(engine) is { } reading ? [reading] : []));

        Missing(keys).ShouldBeEmpty();
    }

    // Every way the CO2 percentage is taken, and every way the company deducts the costs.
    [Fact]
    public void CarCardResx_everyCo2AndDeductionSentence_hasAText()
    {
        var keys = Enum.GetValues<CarCo2Basis>().Select(basis => CarCardText.Co2(CarResult.None with { Co2Basis = basis }))
            .Concat(["Co2_HeldToMinimum", "Co2_HeldToMaximum", "Deduction_Full", "Deduction_Rate", "Deduction_None", "Deduction_AllCarvedOut"]);

        Missing(keys).ShouldBeEmpty();
    }

    // The car's summary ends on what the split shares: « before your pay », and « before your pay and
    // royalty » once the royalty is in the plan.
    [Fact]
    public void CarCardResx_everySummary_hasAText()
    {
        Missing([CarCardText.Summary(true, false), CarCardText.Summary(true, true), CarCardText.Summary(false, false), CarCardText.Summary(false, true)])
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

        Missing(phrases.Select(phrase => phrase.Key).Append("CarTitle")).ShouldBeEmpty();
    }

    private static IEnumerable<string> Missing(IEnumerable<string> keys) => keys.Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
