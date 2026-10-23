using System.Globalization;
using System.Resources;
using Netto.Core.CompanyCar;
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

    private static IEnumerable<string> Missing(IEnumerable<string> keys) => keys.Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
