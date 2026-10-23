using System.Globalization;
using System.Resources;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River.CompanyCar;

// The Company car card opened on the river's example with YearSimulationCarTests' electric car, which
// takes 6,162.66 of cash from the pocket (CarViewTests), drawn as a viewer meets it (RiverRendering):
// what it asks of the same car bought privately, and what it says against it; and, on the example's own
// car, the origins it names. Its sentences are read from its .resx, so rewording one leaves these tests
// standing.
public class CarCardTests
{
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.CompanyCar.CarCard", typeof(CarCard).Assembly);

    private static readonly ResourceManager Refusals = new("Netto.Web.Client.River.RefusedMessage", typeof(RefusedMessage).Assembly);

    // The field of what the same car would cost privately.
    private const string PrivateCost = "river-car-private-cost";

    private static readonly RiverInputs ElectricCar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    // Until the user gives it, the card asks what the same car would cost privately, with no origin
    // beside the field, states the cash the car takes, and says the comparison with a private car waits.
    [Fact]
    public async Task Card_noPrivateCost_asksItStatesTheCashAndWaits()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain(Text("PrivateCostLabel")),
            () => FieldHead(html, PrivateCost).ShouldNotContain("origin-chip"),
            () => html.ShouldContain(Text("Cash_Takes", "€6,162.66")),
            () => html.ShouldContain(Text("Private_Waiting")));
    }

    // Once given, the cost is the user's, and the card states what the company car gains against it:
    // 10,000 less the 6,162.66 of cash it takes.
    [Fact]
    public async Task Card_privateCostGiven_saysItIsYoursAndWhatTheCarGains()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar.WithCarPrivateCost(10_000m));

        html.ShouldSatisfyAllConditions(
            () => FieldHead(html, PrivateCost).ShouldContain("origin-chip--yours"),
            () => html.ShouldContain(Text("Private_Gain", "€3,837.34", "€10,000.00")));
    }

    [Fact]
    public async Task Card_negativePrivateCost_saysWhyItIsRefused()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar.WithCarPrivateCost(-1m));

        (html.Contains(Refusals.GetString($"Error_{CarComparison.PrivateCostNegative}", CultureInfo.InvariantCulture) ?? "-"), html.Contains(Text("Private_Waiting")))
            .ShouldBe((true, true));
    }

    // Nothing the card says names a muddy pocket.
    [Fact]
    public async Task Card_withACar_namesNoMuddyPocket()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar.WithCarContribution(600m).WithCarPrivateCost(10_000m));

        html.ShouldNotContain("muddy", Case.Insensitive);
    }

    // The example's car, whose papers open by themselves while its catalogue value is missing: that value,
    // its yearly costs and the contribution are nothing the example holds, beside no origin; its first
    // registration and its order are the example's dates, the card's only « example » chips
    // (ExampleOriginsTests).
    [Fact]
    public async Task Card_exampleCar_drawsTheExamplesChipsOnItsDatesAlone()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, RiverInputs.Example.AddToPlan(OptionalModule.Car));

        html.ShouldSatisfyAllConditions(
            () => new[] { "river-car-catalogue", "river-car-costs", "river-car-contribution" }.Where(field => FieldHead(html, field).Contains("origin-chip")).ShouldBeEmpty(),
            () => OriginChips(html).Count(chip => chip == "example").ShouldBe(2));
    }

    private static string Text(string key, params object[] figures) =>
        string.Format(CultureInfo.InvariantCulture, Resources.GetString(key, CultureInfo.InvariantCulture) ?? key, figures);
}
