using System.Text.RegularExpressions;
using Netto.Core.CompanyCar;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River.CompanyCar;

// The Company car card opened on the river's example with YearSimulationCarTests' electric car, which
// takes 6,162.66 of cash from the pocket (CarViewTests), drawn as a viewer meets it (RiverRendering): its
// figures as lines before where its money goes, what it asks of the same car bought privately and what it
// says against it, the sentences it keeps in sight and the rules it moved into its bubbles and its
// details; and, on the example's own car, the origins it names. Its texts are read from its .resx, so
// rewording one leaves these tests standing.
public class CarCardTests
{
    private static readonly ResxText Resources = ResxText.For<CarCard>();

    // What the engine's refusals say, which CarAndRoyaltyCardsTests reads too.
    internal static readonly ResxText Refusals = ResxText.For<RefusedMessage>();

    // The field of what the same car would cost privately.
    private const string PrivateCost = "river-car-private-cost";

    // The car fixtures CarAndRoyaltyCardsTests holds the card to as well.
    internal static readonly RiverInputs ElectricCar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    // CarViewTests' petrol car leased in January 2026: 45,000 at 120 g/km, against the 2026 reference of
    // 70 g/km for its fuel, 10.5%; its costs are not all deducted.
    internal static readonly RiverInputs PetrolCar = ElectricCar.WithCatalogueValue(45_000m).WithCarCosts(9_000m)
        .WithCarEngine(CarEngine.Petrol).WithCarCo2(120).WithCarAcquired(new DateOnly(2026, 1, 5));

    // The benefit and what the company does with the costs, then the plan with the car against the same
    // plan without it, in cash, then where its money goes: its costs out of the river, 8% of the 100,000
    // it receives, and the 92,000 the split shares.
    [Fact]
    public async Task Card_withACar_drawsItsFiguresThenTheCashThenWhereItsMoneyGoes()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar);
        var comparison = CarComparison.From(ElectricCar, ElectricCar.Run()).ShouldNotBeNull();

        CardHtml.Lines(html).ShouldBe(
        [
            (Text("Figure_Benefit"), "€1,714.29", null),
            (Text("Figure_Co2"), "4%", null),
            (Text("Figure_Period_Year", "January", "December", "100%"), "€1,714.29", null),
            (Text("Figure_BenefitTaxed"), "€1,714.29", null),
            (Text("Figure_Deduction_Full", "100%"), "€8,000.00", null),
            (Text("Figure_AddBack", "17%"), "€291.43", null),
            (Text("Figure_WithCar"), Formats.Euros(comparison.PocketWith), null),
            (Text("Figure_WithoutCar"), Formats.Euros(comparison.PocketWithout), null),
            (Text("Figure_Cash_Takes"), "€6,162.66", null),
            (Text("Figure_TaxesOnPay"), Formats.Euros(comparison.TaxesOnPayAdded), null),
            (Drains("Drain_car_costs"), "−€8,000.00", "8.0%"),
            (Drains("Drain_profit_before_pay"), "€92,000.00", null),
        ]);
    }

    // Which cars the card counts is no sentence of its own: it is said in the bubble of « Ordered or
    // leased », after what that date is.
    [Fact]
    public async Task Card_open_saysWhichCarsItCountsInTheBubbleOfTheOrderDate()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, RiverInputs.Example.AddToPlan(OptionalModule.Car));

        (CardHtml.BubbleAfter(html, Text("AcquiredLabel")), CardHtml.Prose(html).Contains(Text("Scope"))).ShouldBe(($"{Text("AcquiredHint")} {Text("Scope")}", false));
    }

    // What the CO2 percentage counts and why 40% is added back of a car the company charges are rules the
    // card states in its details, word for word, above their sources: out of sight while they are folded.
    public static TheoryData<string, RiverInputs, string> Rules => new()
    {
        { "a car that emits CO2", PetrolCar, Text("Co2_Registered", "120 g/km", "70 g/km", "10.5%", 2026) },
        { "an electric car", ElectricCar, Text("Co2_Electric", "0 g/km", "0 g/km", "4%", 2026) },
        { "an electric car whose charging the company pays", ElectricCar.WithPrivateEnergyPaid(true), Text("AddBackReading") },
    };

    [Theory]
    [MemberData(nameof(Rules))]
    public async Task Card_detailsOpen_stateTheRuleWordForWordAboveTheSources(string _, RiverInputs inputs, string rule)
    {
        var details = Details(await RenderDetailsOpenAsync(typeof(CarCard), OptionalModule.Car, inputs));
        var folded = await RenderOpenAsync<CarCard>(OptionalModule.Car, inputs);

        (CardHtml.Prose(details).Contains(rule), details.IndexOf(rule, StringComparison.Ordinal) < details.IndexOf("module-card__sources", StringComparison.Ordinal),
                CardHtml.Prose(folded).Contains(rule))
            .ShouldBe((true, true, false));
    }

    // What a figure cannot say stays in sight: a benefit held to the year's minimum, a CO2 percentage held
    // to the lowest or the highest, and a car not counted until its catalogue value is given.
    public static TheoryData<string, RiverInputs, string> Notes => new()
    {
        { "a benefit below the minimum", ElectricCar.WithCatalogueValue(30_000m), Text("AtTheMinimum") },
        { "a diesel at 250 g/km", PetrolCar.WithCarEngine(CarEngine.Diesel).WithCarCo2(250), Text("Co2_HeldToMaximum") },
        { "a plug-in hybrid at 20 g/km", PetrolCar.WithCarEngine(CarEngine.PetrolPlugInHybrid).WithCarCo2(20), Text("Co2_HeldToMinimum") },
        { "no catalogue value", RiverInputs.Example.AddToPlan(OptionalModule.Car), Text("NoCarYet") },
    };

    [Theory]
    [MemberData(nameof(Notes))]
    public async Task Card_figureCannotSayIt_keepsItsSentenceInSight(string _, RiverInputs inputs, string sentence)
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, inputs);

        CardHtml.Prose(html).ShouldContain(sentence);
    }

    // Until the user gives it, the card asks what the same car would cost privately, in a field with a
    // bubble and no origin beside it, states the cash the car takes, and draws its note on the comparison
    // with a private car without an amount: it waits.
    [Fact]
    public async Task Card_noPrivateCost_asksItStatesTheCashAndWaits()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar);
        var field = FieldHead(html, PrivateCost);

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Bubbles(field).ShouldHaveSingleItem().Text.ShouldNotBeNullOrWhiteSpace(),
            () => field.ShouldNotContain("origin-chip"),
            () => CardHtml.LineAmounts(html).ShouldContain("€6,162.66"),
            () => CardHtml.Amounts(Gap(html)).ShouldBeEmpty());
    }

    // Once given, the cost is the user's, and the card states what the company car gains against it:
    // 10,000 less the 6,162.66 of cash it takes, and never the cost it was weighed against.
    [Fact]
    public async Task Card_privateCostGiven_saysItIsYoursAndWhatTheCarGains()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar.WithCarPrivateCost(10_000m));

        html.ShouldSatisfyAllConditions(
            () => FieldHead(html, PrivateCost).ShouldContain("origin-chip--yours"),
            () => CardHtml.Amounts(Gap(html)).ShouldBe(["€3,837.34"]));
    }

    [Fact]
    public async Task Card_negativePrivateCost_saysWhyItIsRefused()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar.WithCarPrivateCost(-1m));

        (html.Contains(Refusals.Get($"Error_{CarComparison.PrivateCostNegative}") ?? "-"), CardHtml.Amounts(Gap(html)).Count)
            .ShouldBe((true, 0));
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
        string[] amounts = ["river-car-catalogue", "river-car-costs", "river-car-contribution"];

        html.ShouldSatisfyAllConditions(
            () => amounts.Where(field => FieldHead(html, field).Contains("origin-chip")).ShouldBeEmpty(),
            () => OriginChips(html).Count(chip => chip == "example").ShouldBe(2));
    }

    // A car ordered after the end of the year is refused, in « Your car », which opens on it: its year
    // field is marked as every number field of the river is, the engine's message tied to it beside the
    // card's own, so MudBlazor's validation cannot clear the mark after the next edit (RiverNumericField);
    // the first registration's year, accepted, is not marked.
    [Fact]
    public async Task Card_orderedAfterTheYear_marksTheOrdersYearWithTheEnginesMessage()
    {
        var inputs = ElectricCar.WithCarAcquired(new DateOnly(2027, 3, 1));
        var run = inputs.Run();
        var message = Refusals.Get($"Error_{CarCalculator.AcquiredAfterYear}").ShouldNotBeNull();

        var html = System.Net.WebUtility.HtmlDecode(
            await RenderAsync(typeof(CarCard), new Folds(new BrowserStorage()), new() { ["Inputs"] = inputs, ["Run"] = run, ["Refused"] = run.Error }));
        var marked = Regex.Matches(html, "river-number--refused\"[^>]*>.*?<input[^>]*value=\"(?<year>[^\"]*)\"", RegexOptions.Singleline);

        (run.Error, string.Join(" ", marked.Select(field => field.Groups["year"].Value)), Regex.Count(html, Regex.Escape(message)))
            .ShouldBe((CarCalculator.AcquiredAfterYear, "2027", 2));
    }

    private static string Text(string key, params object[] figures) =>
        Resources.Format(key, figures);

    // The sentence that weighs the company car against a private one, as a viewer reads it; a card that
    // draws none fails the test that asks for it.
    private static string Gap(string html)
    {
        var gap = Regex.Match(html, "<p class=\"car__gap\"[^>]*>(?<text>.*?)</p>", RegexOptions.Singleline);
        gap.Success.ShouldBeTrue("The card draws no sentence weighing the company car against a private one.");

        return gap.Groups["text"].Value.Trim();
    }

    private static readonly ResxText DrainTexts = ResxText.For<DrainList>();

    // A drain list's text, which CarAndRoyaltyCardsTests reads too.
    internal static string Drains(string key) => DrainTexts.Get(key) ?? key;

    // The card's details, from their fold's body on.
    private static string Details(string html) =>
        html[html.IndexOf("module-card__detail", StringComparison.Ordinal)..];
}
