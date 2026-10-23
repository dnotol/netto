using System.Text.RegularExpressions;
using Netto.Core.CompanyCar;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
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

    private static readonly RiverInputs ElectricCar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    // CarViewTests' petrol car leased in January 2026, whose costs the company cannot deduct in full.
    private static readonly RiverInputs PetrolCar = ElectricCar.WithCatalogueValue(45_000m).WithCarCosts(9_000m)
        .WithCarEngine(CarEngine.Petrol).WithCarCo2(120).WithCarAcquired(new DateOnly(2026, 1, 5));

    // Each card's words in sight on the filled plan, every card open, before the story's cut, as the static
    // render counts them (the browser counted 294 on the car's).
    public static TheoryData<string, int> WordsBefore => new() { { "car", 288 } };

    [Theory]
    [MemberData(nameof(WordsBefore))]
    public async Task VisibleWords_cardOnTheFilledPlan_areAtMostFiftyFivePercentOfTheirCountBefore(string card, int before)
    {
        var html = CardHtml.Card(await CardHtml.RenderPlanOpenAsync(Filled), card);

        CardHtml.VisibleWords(html).Count.ShouldBeLessThanOrEqualTo(before * 55 / 100);
    }

    // Every way each card's summary reads: a car not given yet, a car, a car with the royalty in the plan.
    public static TheoryData<string, Type, OptionalModule, RiverInputs> Summaries => new()
    {
        { "no car yet", typeof(CarCard), OptionalModule.Car, RiverInputs.Example.AddToPlan(OptionalModule.Car) },
        { "a car", typeof(CarCard), OptionalModule.Car, ElectricCar },
        { "a car and a royalty", typeof(CarCard), OptionalModule.Car, ElectricCar.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m) },
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
            () => labels.Split('·').Select(Words).ShouldAllBe(words => words <= 5));
    }

    // The plans each card is held to: the electric car, a petrol car whose costs are not all deducted, with a
    // contribution and a private cost, a car at the year's minimum, and one held to the highest CO2
    // percentage, its private charging paid.
    public static TheoryData<string, Type, OptionalModule, RiverInputs> Plans => new()
    {
        { "an electric car", typeof(CarCard), OptionalModule.Car, ElectricCar },
        { "a petrol car with a contribution and a private cost", typeof(CarCard), OptionalModule.Car, PetrolCar.WithCarContribution(600m).WithCarPrivateCost(10_000m) },
        { "a car at the minimum", typeof(CarCard), OptionalModule.Car, ElectricCar.WithCatalogueValue(30_000m).WithCarPrivateCost(1_000m) },
        { "a diesel at the highest percentage", typeof(CarCard), OptionalModule.Car, PetrolCar.WithCarEngine(CarEngine.Diesel).WithCarCo2(250).WithPrivateEnergyPaid(true) },
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

    [Fact]
    public async Task Cards_filledPlan_leaveNoKey()
    {
        var html = await CardHtml.RenderPlanOpenAsync(Filled);

        CardHtml.LeakedKeys(CardHtml.Card(html, "car")).ShouldBeEmpty();
    }

    // An amount as Formats writes it, in euros or as a percentage, with its sign.
    private static readonly Regex Amount = new("[−+-]?€[0-9][0-9,]*(?:\\.[0-9]+)?|[0-9]+(?:\\.[0-9]+)?%");

    private static string Unsigned(string amount) => amount.TrimStart('−', '+', '-');

    private static int Words(string text) => Regex.Split(text, "\\s+").Count(word => word.Any(char.IsLetterOrDigit));

    // A lever's card opened by the viewer, on its inputs' own run.
    private static Task<string> RenderOpenAsync(Type card, OptionalModule module, RiverInputs inputs)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(ModuleFolds.Name(module), true);

        return RenderAsync(card, folds, new() { ["Inputs"] = inputs, ["Run"] = inputs.Run(), ["Refused"] = null });
    }
}
