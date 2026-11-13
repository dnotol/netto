using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
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
    // plan without it, in cash, then where its money goes: its costs into the expenses, as no share of what
    // the company has left before it, and nothing of what it leaves, which is the map's company line.
    [Fact]
    public async Task Card_withACar_drawsItsFiguresThenTheCashThenWhereItsMoneyGoes()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar);
        var comparison = CarComparison.From(ElectricCar, ElectricCar.Run()).ShouldNotBeNull();

        CardHtml.Lines(html).ShouldBe(
        [
            (Text("Figure_Co2_Electric"), "4%", null),
            (Text("Figure_BenefitTaxed"), "€1,714.29", null),
            (Text("Figure_AddBack", "17%"), "€291.43", null),
            (Text("Figure_WithCar"), Formats.Euros(comparison.PocketWith), null),
            (Text("Figure_WithoutCar"), Formats.Euros(comparison.PocketWithout), null),
            (Text("Figure_TaxesOnPay"), Formats.Euros(comparison.TaxesOnPayAdded), null),
            (Drains("Drain_car_costs"), "−€8,000.00", null),
        ]);
    }

    // Whatever its costs and the director's contribution, the royalty in the plan or not, the summary says
    // what the car adds to the pocket or takes from it, the plan with it less the plan without at the same
    // pay, in cash, and ends on no rate: what the car pays out is its costs, a cost of the company. No
    // sentence, and no key drawn.
    public static TheoryData<string, RiverInputs> Cars => new()
    {
        { "costs and no contribution", ElectricCar },
        { "costs above the contribution", ElectricCar.WithCarContribution(600m) },
        { "a contribution above the costs", ElectricCar.WithCarCosts(0m).WithCarContribution(600m) },
        { "a contribution above the costs and a royalty", ElectricCar.WithCarCosts(500m).WithCarContribution(600m).AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m) },
        { "costs above what the company earns", ElectricCar.WithCarCosts(150_000m) },
    };

    [Theory]
    [MemberData(nameof(Cars))]
    public async Task Card_aCar_summaryIsWhatItAddsToThePocketWithNoRate(string _, RiverInputs inputs)
    {
        var gain = LeverGain.Of(inputs, inputs.Run(), OptionalModule.Car).ShouldNotBeNull().Gain;

        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, inputs);
        var summary = CardHtml.Summary(html).ShouldNotBeNull();

        html.ShouldSatisfyAllConditions(
            () => summary.ShouldBe(Text(LeverGainText.Summary(gain), Formats.Euros(Math.Abs(gain)))),
            () => CardHtml.Rate(html).ShouldBeNull(),
            () => CardHtml.SummaryFaults(summary).ShouldBeEmpty(),
            () => CardHtml.LeakedKeys(html).ShouldBeEmpty());
    }

    // The example's car takes cash from the pocket: 6,162.66 less than without it.
    [Fact]
    public async Task Card_withACar_saysTheCashItTakesFromThePocket()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar);

        CardHtml.Summary(html).ShouldBe(Text("Summary_Less", "€6,162.66"));
    }

    // A pay set above what the company can pay once the car's costs are paid is held there, and both plans of
    // the comparison run at it: the card says so above them, once, with that pay, and says nothing of the
    // kind while the pay set is the pay both plans run at.
    [Fact]
    public async Task Card_payHeld_namesThePayBothPlansRunAtAboveTheComparisonOnly()
    {
        var held = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar.WithCarCosts(20_000m).WithDirectorPay(90_000m));
        var notHeld = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar.WithCarCosts(20_000m));
        var note = Text("PayHeld", "€80,000.00");

        (CardHtml.Prose(held).Count(sentence => sentence == note), held.IndexOf(note, StringComparison.Ordinal) < held.IndexOf(Text("Figure_WithCar"), StringComparison.Ordinal),
                CardHtml.Prose(notHeld).Any(sentence => sentence.StartsWith("Both plans run at", StringComparison.Ordinal)))
            .ShouldBe((1, true, false));
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

    // A car at the year's minimum draws the benefit taxed once, worded as the minimum, what its scale gives in
    // its bubble; no CO2 percentage, no period, and no sentence saying it is held there.
    [Fact]
    public async Task Card_atTheMinimum_drawsTheMinimumOnceWithWhatTheScaleGivesInItsBubble()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar.WithCatalogueValue(30_000m));
        var lines = CardHtml.Lines(html);

        html.ShouldSatisfyAllConditions(
            () => lines.Take(2).ShouldBe([(Text("Figure_BenefitTaxed_Minimum"), "€1,690.00", null), (Text("Figure_AddBack", "17%"), "€287.30", null)]),
            () => CardHtml.BubbleAfter(html, Text("Figure_BenefitTaxed_Minimum")).ShouldContain("€1,028.57"),
            () => lines.Count(line => line.Amount == "€1,690.00").ShouldBe(1));
    }

    // What a figure cannot say stays in sight: a CO2 percentage held to the lowest or the highest, and a car
    // not counted until its catalogue value is given. A benefit held to the year's minimum is its line's
    // label, no sentence.
    public static TheoryData<string, RiverInputs, string> Notes => new()
    {
        { "a diesel at 250 g/km", PetrolCar.WithCarEngine(CarEngine.Diesel).WithCarCo2(250), Text("Co2_HeldToMaximum") },
        { "a plug-in hybrid at 20 g/km above the minimum", PetrolCar.WithCatalogueValue(80_000m).WithCarEngine(CarEngine.PetrolPlugInHybrid).WithCarCo2(20), Text("Co2_HeldToMinimum") },
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
            () => CardHtml.Summary(html).ShouldNotBeNull().ShouldContain("€6,162.66"),
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
        var message = Refusals.Get($"Error_{CarCalculator.PrivateCostNegative.Key}").ShouldNotBeNull();

        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, ElectricCar.WithCarPrivateCost(-1m));

        (html.Contains(message), CardHtml.Amounts(Gap(html)).Count).ShouldBe((true, 0));
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

    // « Your car » asks the day the director has the car from and the day they give it back on, each labelled
    // and named for its field, empty while held all year, which is assumed: a « default » chip on each.
    [Fact]
    public async Task Card_open_asksTheDaysTheCarIsHeld()
    {
        var html = System.Net.WebUtility.HtmlDecode(await RenderOpenAsync<CarCard>(OptionalModule.Car, RiverInputs.Example.AddToPlan(OptionalModule.Car)));

        html.ShouldSatisfyAllConditions(
            () => FieldHead(html, HeldFrom).ShouldContain(Resources.Get("HeldFromLabel").ShouldNotBeNull()),
            () => FieldHead(html, ReturnedOn).ShouldContain(Resources.Get("ReturnedOnLabel").ShouldNotBeNull()),
            () => FieldHead(html, HeldFrom).ShouldContain("origin-chip--default"),
            () => FieldHead(html, ReturnedOn).ShouldContain("origin-chip--default"));
    }

    // A day given is the user's, and the field shows it.
    [Fact]
    public async Task Card_dayGiven_showsItAsTheUsers()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, RiverInputs.Example.AddToPlan(OptionalModule.Car).WithCarHeldFrom(new DateOnly(2026, 9, 17)));

        (FieldHead(html, HeldFrom).Contains("origin-chip--default", StringComparison.Ordinal), Regex.IsMatch(html, $"id=\"{HeldFrom}\"[^>]*value=\"17/09/2026\""))
            .ShouldBe((false, true));
    }

    // A car given back before the day it is had from is refused on the day it is given back, and « Your car »
    // opens on it: that field is marked and given the engine's words for a screen reader, the other neither,
    // and the card says them in sight below the fields.
    [Fact]
    public async Task Card_returnedBeforeHeld_marksTheReturnDayWithTheEnginesMessage()
    {
        var inputs = ElectricCar.WithCarHeldFrom(new DateOnly(2026, 5, 1)).WithCarReturnedOn(new DateOnly(2026, 4, 1));
        var run = inputs.Run();
        var message = Refusals.Get($"Error_{CarCalculator.ReturnedBeforeHeld.Key}").ShouldNotBeNull();

        var html = await RenderAsync(typeof(CarCard), new Folds(new BrowserStorage()), new() { ["Inputs"] = inputs, ["Run"] = run, ["Refused"] = run.Error });

        (run.Error, Marked(html, ReturnedOn), CardHtml.FieldMessage(html, ReturnedOn), Marked(html, HeldFrom), CardHtml.FieldMessage(html, HeldFrom),
                Regex.Count(System.Net.WebUtility.HtmlDecode(html), Regex.Escape(message)))
            .ShouldBe((CarCalculator.ReturnedBeforeHeld, true, message, false, null, 2));
    }

    // A field MudBlazor draws in error: the input's own frame carries its error class.
    private static bool Marked(string html, string field) =>
        Regex.IsMatch(html, $"<div[^>]*class=\"[^\"]*mud-input-error[^\"]*\"[^>]*>(?:(?!</div>).)*?id=\"{field}\"", RegexOptions.Singleline);

    private const string HeldFrom = "river-car-held-from";

    private const string ReturnedOn = "river-car-returned-on";

    // A car ordered after the end of the year is refused, in « Your car », which opens on it: its year
    // field is marked as every number field of the river is, the engine's message tied to it beside the
    // card's own, so MudBlazor's validation cannot clear the mark after the next edit (RiverNumericField);
    // the first registration's year, accepted, is not marked.
    [Fact]
    public async Task Card_orderedAfterTheYear_marksTheOrdersYearWithTheEnginesMessage()
    {
        var inputs = ElectricCar.WithCarAcquired(new DateOnly(2027, 3, 1));
        var run = inputs.Run();
        var message = Refusals.Get($"Error_{CarCalculator.AcquiredAfterYear.Key}").ShouldNotBeNull();

        var html = System.Net.WebUtility.HtmlDecode(
            await RenderAsync(typeof(CarCard), new Folds(new BrowserStorage()), new() { ["Inputs"] = inputs, ["Run"] = run, ["Refused"] = run.Error }));
        var marked = Regex.Matches(html, "river-number--refused\"[^>]*>.*?<input[^>]*value=\"(?<year>[^\"]*)\"", RegexOptions.Singleline);

        (run.Error, string.Join(" ", marked.Select(field => field.Groups["year"].Value)), Regex.Count(html, Regex.Escape(message)))
            .ShouldBe((CarCalculator.AcquiredAfterYear, "2027", 2));
    }

    // The fake-hybrid rule starts with the cars ordered or leased in 2018 (the engine's decision): before
    // it the card offers neither the tick nor the matching car's CO2, and its line says the car's own CO2
    // counts, though both values are kept. From 2018 it offers them and states the matching car's CO2.
    [Theory]
    [InlineData(2017, false)]
    [InlineData(2024, true)]
    public async Task Card_plugInHybridWithTheFakeTickKept_offersTheFakeHybridFieldsOnlyFrom2018(int acquiredIn, bool offered)
    {
        // No catalogue value yet, so the card opens « Your car » by itself, its fields in sight.
        var inputs = ElectricCar.WithCatalogueValue(0m).WithCarEngine(CarEngine.PetrolPlugInHybrid).WithCarCo2(45).WithCarAcquired(new DateOnly(acquiredIn, 3, 1))
            .WithFakeHybrid(true).WithMatchingCarCo2(150);
        var folds = new Folds(new BrowserStorage());
        folds.Set(ModuleFolds.Name(OptionalModule.Car), true);

        var html = System.Net.WebUtility.HtmlDecode(await RenderAsync(typeof(CarCard), folds,
            new() { ["Inputs"] = inputs, ["Run"] = inputs.Run(), ["Refused"] = null },
            services => services.AddSingleton<IComponentActivator>(new OpenDisclosures())));

        (html.Contains(Text("FakeHybridLabel"), StringComparison.Ordinal), html.Contains(Text("MatchingCo2Label"), StringComparison.Ordinal),
                html.Contains(Text("Car_FakeHybrid", "150 g/km"), StringComparison.Ordinal),
                html.Contains(Text("Car_FakeHybrid_NotYet", "2018"), StringComparison.Ordinal))
            .ShouldBe((offered, offered, offered, !offered));
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
