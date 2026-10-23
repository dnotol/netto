using System.Globalization;
using System.Net;
using System.Resources;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River.CompanyCar;

// The Company car card opened on the river's example with YearSimulationCarTests' electric car, which
// takes 6,162.66 of cash from the pocket (CarViewTests), drawn as a viewer meets it (RiverRendering):
// what it asks of the same car bought privately, and what it says against it. Its sentences are read
// from its .resx, so rewording one leaves these tests standing.
public class CarCardTests
{
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.CompanyCar.CarCard", typeof(CarCard).Assembly);

    private static readonly ResourceManager Refusals = new("Netto.Web.Client.River.RefusedMessage", typeof(RefusedMessage).Assembly);

    private static readonly RiverInputs ElectricCar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    // Until the user gives it, the card asks what the same car would cost privately, with no origin
    // beside the field, states the cash the car takes, and says the comparison with a private car waits.
    [Fact]
    public async Task Card_noPrivateCost_asksItStatesTheCashAndWaits()
    {
        var html = await RenderOpenAsync(ElectricCar);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain(Text("PrivateCostLabel")),
            () => PrivateCostHead(html).ShouldNotContain("origin-chip"),
            () => html.ShouldContain(Text("Cash_Takes", "€6,162.66")),
            () => html.ShouldContain(Text("Private_Waiting")));
    }

    // Once given, the cost is the user's, and the card states what the company car gains against it:
    // 10,000 less the 6,162.66 of cash it takes.
    [Fact]
    public async Task Card_privateCostGiven_saysItIsYoursAndWhatTheCarGains()
    {
        var html = await RenderOpenAsync(ElectricCar.WithCarPrivateCost(10_000m));

        html.ShouldSatisfyAllConditions(
            () => PrivateCostHead(html).ShouldContain("origin-chip--yours"),
            () => html.ShouldContain(Text("Private_Gain", "€3,837.34", "€10,000.00")));
    }

    [Fact]
    public async Task Card_negativePrivateCost_saysWhyItIsRefused()
    {
        var html = await RenderOpenAsync(ElectricCar.WithCarPrivateCost(-1m));

        (html.Contains(Refusals.GetString($"Error_{CarComparison.PrivateCostNegative}", CultureInfo.InvariantCulture) ?? "-"), html.Contains(Text("Private_Waiting")))
            .ShouldBe((true, true));
    }

    // Nothing the card says names a muddy pocket.
    [Fact]
    public async Task Card_withACar_namesNoMuddyPocket()
    {
        var html = await RenderOpenAsync(ElectricCar.WithCarContribution(600m).WithCarPrivateCost(10_000m));

        html.ShouldNotContain("muddy", Case.Insensitive);
    }

    // The card opened by the viewer, its HTML decoded to the text a viewer reads.
    private static async Task<string> RenderOpenAsync(RiverInputs inputs)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(ModuleFolds.Name(OptionalModule.Car), true);

        return WebUtility.HtmlDecode(await RenderAsync<CarCard>(folds, new() { ["Inputs"] = inputs, ["Run"] = inputs.Run(), ["Refused"] = null }));
    }

    // From the private cost's label to its input: its name, its help and the origin beside them, if any.
    private static string PrivateCostHead(string html)
    {
        var label = html.IndexOf("id=\"river-car-private-cost-label\"", StringComparison.Ordinal);
        label.ShouldBeGreaterThan(-1);
        return html[label..html.IndexOf("id=\"river-car-private-cost\"", label, StringComparison.Ordinal)];
    }

    private static string Text(string key, params object[] figures) =>
        string.Format(CultureInfo.InvariantCulture, Resources.GetString(key, CultureInfo.InvariantCulture) ?? key, figures);
}
