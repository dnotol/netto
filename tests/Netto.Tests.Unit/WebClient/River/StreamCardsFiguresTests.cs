using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The split and the cards of the two streams after it, Director pay and Corporate tax, as a viewer reads
// them open on a plan filled with every lever (CardHtml): each draws its figures as lines of label,
// amount and share, and keeps a sentence only for what a figure cannot say. The plan grants 220 days of
// meal vouchers with 250 of eco and 100 of sport/culture vouchers, pays 240 for internet, 240 for a
// computer and 600 of phone costs, lets 12% of a home of 1,200 of cadastral income for 1,380, holds
// YearSimulationCarTests' electric car (50,000 of catalogue value, 8,000 of yearly costs) and pays a
// royalty that follows the pay, at half of the director's time spent creating.
public class StreamCardsFiguresTests
{
    private static readonly RiverInputs Filled = RiverInputs.Example
        .AddToPlan(OptionalModule.Vouchers).WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
        .AddToPlan(OptionalModule.Allowances).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(600m)
        .AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
        .AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.5m);

    // The Plan page with every card open, on the filled plan and on the river's example, each drawn once
    // for the tests that read it.
    private static readonly Dictionary<string, Lazy<Task<string>>> Pages = new()
    {
        ["filled"] = new(() => CardHtml.RenderPlanOpenAsync(Filled)),
        ["example"] = new(() => CardHtml.RenderPlanOpenAsync(RiverInputs.Example)),
    };

    // An amount as the page draws it, in euros, signed or not, or as a share.
    private static readonly Regex Amount = new(@"[−+]?€[0-9][0-9,]*(?:\.[0-9]+)?|[0-9]+(?:\.[0-9]+)?%");

    public static TheoryData<string> Cards => new() { "split" };

    // A card's summary is its figures, each with the few words that label it, and no sentence.
    [Theory]
    [MemberData(nameof(Cards))]
    public async Task Summary_filledPlan_isItsFiguresWithTheirLabels(string card)
    {
        var labels = Amount.Replace(CardHtml.Summary(await CardAsync(card)).ShouldNotBeNull(), "");

        labels.ShouldSatisfyAllConditions(
            () => labels.ShouldNotMatch("[.,:;?]"),
            () => labels.Split('·').Select(WordCount).ShouldAllBe(count => count >= 1 && count <= 5));
    }

    // A sentence that restates a figure its lines show is gone: no paragraph in sight holds an amount
    // one of the card's figure or drain lines draws, on the filled plan or on the example.
    public static TheoryData<string, string> CardsOfEachPlan => new() { { "filled", "split" }, { "example", "split" } };

    [Theory]
    [MemberData(nameof(CardsOfEachPlan))]
    public async Task Card_open_proseRestatesNoAmountOfItsLines(string plan, string card)
    {
        var html = await CardAsync(card, plan);
        var lines = CardHtml.LineAmounts(html).Select(Unsigned).ToHashSet();

        CardHtml.Prose(html).Where(paragraph => Amount.Matches(paragraph).Any(amount => lines.Contains(Unsigned(amount.Value)))).ShouldBeEmpty();
    }

    // The words in sight the static render counted on the filled plan before the story: each card now
    // draws at most 60% of them.
    public static TheoryData<string, int> WordsBefore => new() { { "split", 197 } };

    [Theory]
    [MemberData(nameof(WordsBefore))]
    public async Task VisibleWords_filledPlan_areAtMostSixtyPercentOfThoseBefore(string card, int before)
    {
        CardHtml.VisibleWords(await CardAsync(card)).Count.ShouldBeLessThanOrEqualTo(before * 3 / 5);
    }

    // Every text a card draws, in sight or not, is in its .resx.
    [Theory]
    [MemberData(nameof(Cards))]
    public async Task Card_open_leavesNoKey(string card)
    {
        CardHtml.LeakedKeys(await CardAsync(card)).ShouldBeEmpty();
    }

    // What the split shares, each side out of the 87,229.80 the levers leave it, then the best pay the
    // search found and what it leaves, with what reaches the pocket there and at the pay set out of what
    // leaves the company.
    [Fact]
    public async Task Split_filledPlan_drawsItsSidesThenTheBestPayAsLines()
    {
        CardHtml.Lines(await CardAsync("split")).ShouldBe(
        [
            ("Your pay", "€50,000.00", "57.3%"),
            ("Royalty", "€7,142.85", "8.2%"),
            ("Company profit before tax", "€30,086.95", "34.5%"),
            ("Best pay", "€19,757.00", null),
            ("Dividend", "€46,937.35", null),
            ("Royalty", "€2,822.42", null),
            ("In your pocket there", "€60,866.97", "66.2%"),
            ("More than at your pay", "€2,619.44", null),
            ("In your pocket at your pay", "€58,247.53", "63.3%"),
        ]);
    }

    // How far the pay goes is a bound, in sight under its field; why, the field's bubble says, in the
    // words the card said it in.
    [Fact]
    public async Task Split_filledPlan_keepsThePaysBoundInSightAndWhyInItsBubble()
    {
        var html = await CardAsync("split");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Prose(html).ShouldContain("At most €76,326.08"),
            () => WebUtility.HtmlDecode(FieldHead(html, "river-split-pay"))
                .ShouldContain("Up to €76,326.08, where the royalty that follows your pay takes the rest of what your pay and royalty share."));
    }

    // A refused pay leaves no figure to draw: the refusal stands in sight where the lines were.
    [Fact]
    public async Task Split_payRefused_showsTheRefusalInSightAndNoLines()
    {
        var html = CardHtml.Card(await CardHtml.RenderPlanOpenAsync(RiverInputs.Example.WithDirectorPay(-1m)), "split");

        (CardHtml.Lines(html).Count, Regex.IsMatch(html, "class=\"[^\"]*refused-message")).ShouldBe((0, true));
    }

    // Until the pointer is on the curve, its line keeps its room in the card, empty.
    [Fact]
    public async Task Split_firstDraw_keepsThePointersLineEmpty()
    {
        Regex.IsMatch(await CardAsync("split"), @"<p class=""split__pointer""[^>]*>\s*</p>").ShouldBeTrue();
    }

    private static async Task<string> CardAsync(string card, string plan = "filled") => CardHtml.Card(await Pages[plan].Value, card);

    private static string Unsigned(string amount) => amount.TrimStart('−', '+');

    private static int WordCount(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(word => word.Any(char.IsLetter));
}
