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

    // The Plan page with every card open, each drawn once for the tests that read it: on the filled plan,
    // on the river's example, on the example with nothing withheld on the pay, which leaves a surcharge
    // due on its tax, and with the whole base of that tax paid by 10 April, which earns a bonus.
    private static readonly Dictionary<string, Lazy<Task<string>>> Pages = new()
    {
        ["filled"] = new(() => CardHtml.RenderPlanOpenAsync(Filled)),
        ["example"] = new(() => CardHtml.RenderPlanOpenAsync(RiverInputs.Example)),
        ["nothing withheld"] = new(() => CardHtml.RenderPlanOpenAsync(RiverInputs.Example.WithTaxWithheld(0m))),
        ["bonus"] = new(() => CardHtml.RenderPlanOpenAsync(RiverInputs.Example.WithTaxWithheld(0m).WithTaxAdvancePayment(0, 12_014m))),
    };

    // An amount as the page draws it, in euros, signed or not, or as a share.
    private static readonly Regex Amount = new(@"[−+]?€[0-9][0-9,]*(?:\.[0-9]+)?|[0-9]+(?:\.[0-9]+)?%");

    public static TheoryData<string> Cards => new() { "split", "director-pay", "corporate-tax" };

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
    public static TheoryData<string, string> CardsOfEachPlan => new()
    {
        { "filled", "split" }, { "example", "split" },
        { "filled", "director-pay" }, { "example", "director-pay" }, { "nothing withheld", "director-pay" }, { "bonus", "director-pay" },
        { "filled", "corporate-tax" }, { "example", "corporate-tax" },
    };

    [Theory]
    [MemberData(nameof(CardsOfEachPlan))]
    public async Task Card_open_proseRestatesNoAmountOfItsLines(string plan, string card)
    {
        var html = await CardAsync(card, plan);
        var lines = CardHtml.LineAmounts(html).Select(Unsigned).ToHashSet();

        CardHtml.Prose(html).Where(paragraph => Amount.Matches(paragraph).Any(amount => lines.Contains(Unsigned(amount.Value)))).ShouldBeEmpty();
    }

    // The words in sight the static render counted on the filled plan before the story, and the most a
    // card now draws: 60% of them for the split; Director pay and Corporate tax keep their drain lines,
    // their summaries and the lines of their settings, which hold most of what they drew, and are held
    // to what they draw once their sentences are lines.
    public static TheoryData<string, int, int> Words => new() { { "split", 197, 118 }, { "director-pay", 131, 110 }, { "corporate-tax", 125, 98 } };

    [Theory]
    [MemberData(nameof(Words))]
    public async Task VisibleWords_filledPlan_fallFromThoseBefore(string card, int before, int atMost)
    {
        CardHtml.VisibleWords(await CardAsync(card)).Count.ShouldBeLessThanOrEqualTo(atMost, $"{card} drew {before} words in sight before the story.");
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

    // How far the pay goes is a bound, in sight under its field and never in a bubble; why, the field's
    // bubble says, in the words the card said it in.
    [Fact]
    public async Task Split_filledPlan_keepsThePaysBoundInSightAndWhyInItsBubble()
    {
        var html = await CardAsync("split");
        var bubble = WebUtility.HtmlDecode(Regex.Match(FieldHead(html, "river-split-pay"), "class=\"info-bubble__text\"[^>]*>([^<]*)<").Groups[1].Value);

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Prose(html).ShouldContain("At most €76,326.08"),
            () => bubble.ShouldStartWith("Up to where the royalty that follows your pay takes the rest of what your pay and royalty share."),
            () => bubble.ShouldNotContain("€76,326.08"));
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

    // The gross the split gives, from which the lines go on: what the car's benefit adds to what the pay
    // is taxed on, then where the pay goes.
    [Fact]
    public async Task DirectorPay_filledPlan_drawsWhatThePayIsTaxedOnThenWhereItGoes()
    {
        var html = await CardAsync("director-pay");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Prose(html)[0].ShouldBe("Gross pay from the split derived €50,000.00"),
            () => CardHtml.Lines(html).ShouldBe(
            [
                ("Benefit in kind", "€1,714.29", null),
                ("Taxable pay", "€51,714.29", null),
                ("Social contributions", "−€8,577.72", "17.2%"),
                ("Income tax", "−€11,958.11", "23.9%"),
                ("Municipal surcharge", "−€837.07", "1.7%"),
                ("Net pay", "€28,627.10", "57.3%"),
            ]));
    }

    // With no surcharge due, the card says why in a few words, in sight.
    [Fact]
    public async Task DirectorPay_surchargeUnderTheMinimum_saysWhyNoneIsDue()
    {
        CardHtml.Prose(await CardAsync("director-pay")).ShouldContain("No surcharge: what is left is under the minimum.");
    }

    // Due, the surcharge is a drain line, and what more by the last deadline leaves none due a line of
    // its own.
    [Fact]
    public async Task DirectorPay_nothingWithheld_drawsWhatMoreByTheLastDeadlineLeavesNoneDue()
    {
        CardHtml.Lines(await CardAsync("director-pay", "nothing withheld")).ShouldContain(("More by 21 December 2026 leaves none due", "€14,317.50", null));
    }

    // How the withholding is derived from the pay slip, and what the key formula is, are rules: the
    // withheld field's bubble says them, in the words the card said them in, and no note under the field
    // says them again. A refused advance payment opens the field's disclosure here.
    [Fact]
    public async Task DirectorPay_withheldAsThePaySlip_explainsTheWithholdingInTheFieldsBubble()
    {
        var html = WebUtility.HtmlDecode(CardHtml.Card(await CardHtml.RenderPlanOpenAsync(RiverInputs.Example.WithTaxAdvancePayment(0, -1m)), "director-pay"));

        html.ShouldSatisfyAllConditions(
            () => FieldHead(html, "river-tax-withheld").ShouldContain(
                "Derived from the pay slip: twelve times the month's withholding by the key formula, the administration's formula for the tax a company "
                + "withholds on each month's pay, set in annex III of the royal decree implementing the income tax code. Enter the amount actually withheld if it differs."),
            () => CardHtml.Prose(html).ShouldNotContain(paragraph => paragraph.Contains("Derived from the pay slip", StringComparison.Ordinal)));
    }

    // An amount typed replaces the pay slip's, as its origin says: only the way back to the pay slip's
    // amount stays under the field.
    [Fact]
    public async Task DirectorPay_amountWithheldTyped_keepsOnlyTheWayBackToThePaySlipsAmount()
    {
        var html = WebUtility.HtmlDecode(CardHtml.Card(await CardHtml.RenderPlanOpenAsync(RiverInputs.Example.WithTaxWithheld(-1m)), "director-pay"));

        (html.Contains("Use the pay slip's amount", StringComparison.Ordinal), CardHtml.Prose(html).Any(paragraph => paragraph.Contains("replaces the pay slip", StringComparison.Ordinal)))
            .ShouldBe((true, false));
    }

    // What the taxable income adds back to the profit the split leaves the company, then where that
    // profit goes, each drain out of it.
    [Fact]
    public async Task CorporateTax_filledPlan_drawsWhatItAddsBackThenWhereTheProfitGoes()
    {
        CardHtml.Lines(await CardAsync("corporate-tax")).ShouldBe(
        [
            ("Taxable income", "€31,808.58", null),
            ("Added back: vouchers", "€1,430.20", null),
            ("Added back: car benefit", "€291.43", null),
            ("Corporate tax", "−€6,361.72", "21.1%"),
            ("Advance-payment surcharge", "−€429.42", "1.4%"),
            ("Profit after tax", "€23,295.81", "77.4%"),
        ]);
    }

    // Which condition decided the rate stays in sight, with the minimum pay and the share of it the
    // benefits may reach, figures no line shows.
    [Fact]
    public async Task CorporateTax_filledPlan_keepsTheConditionOfTheRateInSight()
    {
        CardHtml.Prose(await CardAsync("corporate-tax")).ShouldContain("Reduced rate kept: your pay with benefits reaches the €51,000.00 minimum; benefits within 20%.");
    }

    private static async Task<string> CardAsync(string card, string plan = "filled") => CardHtml.Card(await Pages[plan].Value, card);

    private static string Unsigned(string amount) => amount.TrimStart('−', '+');

    private static int WordCount(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(word => word.Any(char.IsLetter));
}
