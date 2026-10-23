using System.Globalization;
using System.Net;
using System.Resources;
using System.Text.RegularExpressions;
using Netto.Core.CompanyCar;
using Netto.Tests.WebClient.River.CompanyCar;
using Netto.Tests.WebClient.River.Copyright;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The Company car and Copyright royalty cards read in figures before sentences, as a viewer meets them
// open (CardHtml): a summary of figures with their labels and no sentence, a sentence kept only for what
// a figure cannot say and never restating one of the card's lines, no key drawn for a text, and at most
// 55% of the words they drew in sight before the story's cut.
public class CarAndRoyaltyCardsTests
{
    // The plan filled with every lever, as the hivemind counts the page's words: vouchers, allowances and a
    // home office rent with amounts, CarViewTests' electric car (50,000, first registered in January 2026,
    // 8,000 of costs), and a royalty following the pay at half the working time.
    private static readonly RiverInputs Filled = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module))
        .WithEcoVouchers(250m).WithMealVoucherDays(220).WithInternetAllowance(240m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
        .WithCreativeShare(0.5m);

    private static readonly ResourceManager RoyaltyTexts = new("Netto.Web.Client.River.Copyright.CopyrightCard", typeof(CopyrightCard).Assembly);

    // Each card's words in sight on the filled plan, every card open, before the story's cut, as the static
    // render counts them (the browser counted 294 on the car's and 420 on the royalty's).
    public static TheoryData<string, int> WordsBefore => new() { { "car", 288 }, { "copyright", 424 } };

    [Theory]
    [MemberData(nameof(WordsBefore))]
    public async Task VisibleWords_cardOnTheFilledPlan_areAtMostFiftyFivePercentOfTheirCountBefore(string card, int before)
    {
        var html = CardHtml.Card(await CardHtml.RenderPlanOpenAsync(Filled), card);

        CardHtml.VisibleWords(html).Count.ShouldBeLessThanOrEqualTo(before * 55 / 100);
    }

    // Every way each card's summary reads: a car not given yet, a car, a car with the royalty in the plan;
    // no royalty yet, a royalty within its limits, one beyond them, and one all taxed as pay. The cars are
    // CarCardTests', the royalties CopyrightViewTests' beside the example's 50,000 of pay: 10,000 within 30%
    // of 60,000, and 30,000 beyond 30% of 80,000 by 6,000.
    public static TheoryData<string, Type, OptionalModule, RiverInputs> Summaries => new()
    {
        { "no car yet", typeof(CarCard), OptionalModule.Car, RiverInputs.Example.AddToPlan(OptionalModule.Car) },
        { "a car", typeof(CarCard), OptionalModule.Car, CarCardTests.ElectricCar },
        { "a car and a royalty", typeof(CarCard), OptionalModule.Car, CarCardTests.ElectricCar.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m) },
        { "no royalty yet", typeof(CopyrightCard), OptionalModule.Copyright, RiverInputs.Example.AddToPlan(OptionalModule.Copyright) },
        { "a royalty within its limits", typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Within },
        { "a royalty beyond its limits", typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Beyond },
        { "a royalty all taxed as pay", typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Within.WithAverageWithinCap(false) },
    };

    // Figures with their labels: once its amounts are taken out, no punctuation a sentence would hold, and
    // no more than five words around each figure.
    [Theory]
    [MemberData(nameof(Summaries))]
    public async Task Summary_card_isFiguresWithTheirLabelsAndNoSentence(string _, Type card, OptionalModule module, RiverInputs inputs)
    {
        var summary = CardHtml.Summary(await RenderOpenAsync(card, module, inputs)).ShouldNotBeNull();
        var labels = Amount.Replace(summary, "");

        labels.ShouldSatisfyAllConditions(
            () => labels.ShouldNotMatch("[.,:;?]"),
            () => labels.Split('·').Select(segment => CardHtml.VisibleWords(segment).Count).ShouldAllBe(words => words <= 5));
    }

    // The plans each card is held to: the electric car, a petrol car whose costs are not all deducted, with a
    // contribution and a private cost, a car at the year's minimum, and one held to the highest CO2
    // percentage, its private charging paid; a royalty within its limits, one beyond them, one with an
    // attestation, one all taxed as pay, and one following the pay.
    public static TheoryData<string, Type, OptionalModule, RiverInputs> Plans => new()
    {
        { "an electric car", typeof(CarCard), OptionalModule.Car, CarCardTests.ElectricCar },
        { "a petrol car with a contribution and a private cost", typeof(CarCard), OptionalModule.Car, CarCardTests.PetrolCar.WithCarContribution(600m).WithCarPrivateCost(10_000m) },
        { "a car at the minimum", typeof(CarCard), OptionalModule.Car, CarCardTests.ElectricCar.WithCatalogueValue(30_000m).WithCarPrivateCost(1_000m) },
        { "a diesel at the highest percentage", typeof(CarCard), OptionalModule.Car, CarCardTests.PetrolCar.WithCarEngine(CarEngine.Diesel).WithCarCo2(250).WithPrivateEnergyPaid(true) },
        { "a royalty within its limits", typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Within },
        { "a royalty beyond its limits", typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Beyond },
        { "a royalty with an attestation", typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Within.WithAttestation(true) },
        { "a royalty all taxed as pay", typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Within.WithAverageWithinCap(false) },
        { "a royalty following the pay", typeof(CopyrightCard), OptionalModule.Copyright, RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.5m) },
    };

    // A sentence the card keeps says what a figure cannot: none states an amount one of its lines shows.
    [Theory]
    [MemberData(nameof(Plans))]
    public async Task Card_open_proseRestatesNoAmountOfItsLines(string _, Type card, OptionalModule module, RiverInputs inputs)
    {
        var html = await RenderOpenAsync(card, module, inputs);
        var lines = CardHtml.LineAmounts(html).Select(Unsigned).ToHashSet();

        CardHtml.Prose(html).Where(sentence => Amount.Matches(sentence).Any(amount => lines.Contains(Unsigned(amount.Value)))).ShouldBeEmpty();
    }

    // Every text the card draws open, in sight or not, is in its .resx.
    [Theory]
    [MemberData(nameof(Plans))]
    public async Task Card_open_leavesNoKey(string _, Type card, OptionalModule module, RiverInputs inputs)
    {
        CardHtml.LeakedKeys(await RenderOpenAsync(card, module, inputs)).ShouldBeEmpty();
    }

    // A refusal stays in sight, never in a bubble: the engine's words for the field it refuses, in the
    // card's alert (RefusedMessage), the field keeping its own copy out of sight for a screen reader.
    public static TheoryData<string, Type, RiverInputs> Refusals => new()
    {
        { "a negative catalogue value", typeof(CarCard), CarCardTests.ElectricCar.WithCatalogueValue(-1m) },
        { "a creative share above all of the time", typeof(CopyrightCard), RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(1.5m) },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public async Task Card_refused_saysWhyInSight(string _, Type card, RiverInputs inputs)
    {
        var run = inputs.Run();
        var error = run.Error.ShouldNotBeNull();

        var html = await RenderAsync(card, new Folds(new BrowserStorage()), new() { ["Inputs"] = inputs, ["Run"] = run, ["Refused"] = error });

        Alerts(html).ShouldBe([CarCardTests.Refusals.GetString($"Error_{error}", CultureInfo.InvariantCulture) ?? error]);
    }

    [Fact]
    public async Task Cards_filledPlan_leaveNoKey()
    {
        var html = await CardHtml.RenderPlanOpenAsync(Filled);

        new[] { "car", "copyright" }.SelectMany(card => CardHtml.LeakedKeys(CardHtml.Card(html, card))).ShouldBeEmpty();
    }

    // The royalty and how its limits split it, the note of which limit set it, here the 30% limit with the
    // engine's 18,000 (30% of 60,000), then the plan with it and without it, then where its money goes, the
    // drains' own lines.
    [Fact]
    public async Task RoyaltyCard_withinItsLimits_drawsItsFiguresThenThePlanWithAndWithoutItThenWhereItGoes()
    {
        var html = await RenderOpenAsync(typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Within);
        var comparison = CopyrightComparison.From(CopyrightViewTests.Within, CopyrightViewTests.Within.Run()).ShouldNotBeNull();
        var drains = DrainLines.Shown(CopyrightView.From(CopyrightViewTests.Within.Run().Year.ShouldNotBeNull()).Drains);

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).Take(10).ShouldBe(
            [
                (Royalty("Figure_Royalty"), "€10,000.00", null),
                (Royalty("Figure_WithinLimits"), "€10,000.00", null),
                (Royalty("Figure_Withheld", "15%"), "€1,500.00", null),
                (Royalty("Figure_Pocket"), Formats.Euros(comparison.PocketWith), null),
                (Royalty("Figure_TaxesOnPay"), Formats.Euros(comparison.TaxesOnPayWith), null),
                (Royalty("Figure_TaxesOnProfit"), Formats.Euros(comparison.TaxesOnProfitWith), null),
                (Royalty("Figure_Pocket"), Formats.Euros(comparison.PocketWithout), null),
                (Royalty("Figure_TaxesOnPay"), Formats.Euros(comparison.TaxesOnPayWithout), null),
                (Royalty("Figure_TaxesOnProfit"), Formats.Euros(comparison.TaxesOnProfitWithout), null),
                (Royalty("Figure_Gain_More"), Formats.Euros(comparison.Gain), null),
            ]),
            () => CardHtml.Lines(html).Skip(10).Select(line => line.Label).ShouldBe(drains.Select(drain => CarCardTests.Drains(DrainListText.Drain(drain.Key)))),
            () => CardHtml.Prose(html).ShouldContain("Within 30% of it and your gross pay, €18,000.00, and the cap."));
    }

    // Where no text settles a point, the reading Netto takes is in the royalty's details, word for word,
    // above the sources: out of sight while they are folded.
    [Fact]
    public async Task RoyaltyCard_detailsOpen_statesItsReadingsWordForWordAboveTheSources()
    {
        // The 2026 minimum director pay that keeps the reduced corporate rate.
        var readings = Royalty("Readings", "€51,000.00");

        var details = await RenderDetailsOpenAsync(typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Within);
        var folded = await RenderOpenAsync(typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Within);

        (CardHtml.Prose(details).Contains(readings), details.IndexOf(readings, StringComparison.Ordinal) < details.IndexOf("copyright__sources", StringComparison.Ordinal),
                CardHtml.Prose(folded).Contains(readings))
            .ShouldBe((true, true, false));
    }

    // An amount as Formats writes it, in euros or as a percentage, with its sign.
    private static readonly Regex Amount = new("[−+-]?€[0-9][0-9,]*(?:\\.[0-9]+)?|[0-9]+(?:\\.[0-9]+)?%");

    private static string Unsigned(string amount) => amount.TrimStart('−', '+', '-');

    private static string Royalty(string key, params object[] figures) =>
        string.Format(CultureInfo.InvariantCulture, RoyaltyTexts.GetString(key, CultureInfo.InvariantCulture) ?? key, figures);

    // What each refusal alert of the card says.
    private static List<string> Alerts(string html) =>
    [
        .. Regex.Matches(html, "class=\"[^\"]*\\brefused-message\\b[^\"]*\">.*?<div class=\"mud-alert-message\">(.*?)</div>", RegexOptions.Singleline)
            .Select(alert => WebUtility.HtmlDecode(alert.Groups[1].Value)),
    ];
}
