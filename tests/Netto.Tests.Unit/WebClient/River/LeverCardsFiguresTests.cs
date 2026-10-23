using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The lever cards before the split, Company year and Dividend read in figures, each drawn open on the
// Plan page as the viewer meets it (CardHtml): a summary that is figures with their labels, no sentence,
// and words in sight held under the cut the story makes, two thirds of the page's; and what they share
// with the other cards said in as few words: where a drain line passes its money on, and the line their
// conditions fold to.
public class LeverCardsFiguresTests
{
    // The names IStringLocalizer<TComponent> reads River/<Component>.resx under.
    private static readonly ResourceManager DrainListResources = new("Netto.Web.Client.River.DrainList", typeof(DrainList).Assembly);

    private static readonly ResourceManager ConditionListResources = new("Netto.Web.Client.River.ConditionList", typeof(ConditionList).Assembly);

    // An amount as Formats writes it, a share among them, signed or not.
    private static readonly Regex Amounts = new("[−+]?€[0-9,]+(?:[.][0-9]+)?|[0-9]+(?:[.][0-9]+)?%");

    // Every lever in the plan with an amount of each kind given, each under its maximum so that a bound
    // under its field never reads as one of the card's lines, a rent above its floor, profits kept from
    // earlier years and part of the year's kept in the company: the plan the story counts the cards on.
    private static readonly NamedRiver Filled = new("the filled plan", PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module))
        .WithMealVoucherDays(200).WithEcoVouchers(200m).WithSportCultureVouchers(80m)
        .WithInternetAllowance(200m).WithComputerAllowance(200m).WithPhoneCosts(600m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_000m)
        .WithCatalogueValue(50_000m).WithCarCosts(8_000m).WithCarContribution(600m)
        .WithEarlierProfits(20_000m).WithReserve(5_000m));

    // Each card's summary, by its fold's name, on the river it is drawn on.
    public static TheoryData<string, NamedRiver, string> Summaries => new()
    {
        { "company-year", Filled, "€120,000.00 in · €100,000.00 before your pay" },
        { "vouchers", Filled, "€2,062.00 to your pocket · €97,938.00 left" },
    };

    [Theory]
    [MemberData(nameof(Summaries))]
    public async Task Summary_card_isItsFiguresWithTheirLabels(string card, NamedRiver river, string expected)
    {
        CardHtml.Summary(await OpenCardAsync(river, card)).ShouldBe(expected);
    }

    public static TheoryData<string, NamedRiver> Cards => new()
    {
        { "company-year", Filled },
        { "vouchers", Filled },
        { "allowances", Filled },
        { "dividend", Filled },
        { "dividend", Filled.With("nothing kept", inputs => inputs.WithReserve(0m)) },
    };

    // Between its middle dots, a summary holds a figure and a few words naming it: no sentence, so no
    // punctuation once its amounts are left out.
    [Theory]
    [MemberData(nameof(Cards))]
    public async Task Summary_card_holdsNoSentence(string card, NamedRiver river)
    {
        var summary = CardHtml.Summary(await OpenCardAsync(river, card)).ShouldNotBeNull();

        var labels = Amounts.Replace(summary, "").Split('·').Select(label => label.Trim()).ToList();
        labels.ShouldSatisfyAllConditions(
            () => labels.Where(label => label.IndexOfAny(['.', ',', ':', ';', '?']) >= 0).ShouldBeEmpty(),
            () => labels.Where(label => label.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 5).ShouldBeEmpty());
    }

    // An open card's paragraphs say what its lines cannot: none restates an amount one of its lines shows,
    // whichever way the line's amount goes.
    public static TheoryData<string, NamedRiver> CardsWithLines => new()
    {
        { "vouchers", Filled },
    };

    [Theory]
    [MemberData(nameof(CardsWithLines))]
    public async Task Card_open_proseRestatesNoAmountOfItsLines(string card, NamedRiver river)
    {
        var html = await OpenCardAsync(river, card);

        var amounts = CardHtml.LineAmounts(html).Select(Unsigned).ToHashSet();
        CardHtml.Prose(html).Where(paragraph => Amounts.Matches(paragraph).Any(amount => amounts.Contains(Unsigned(amount.Value)))).ShouldBeEmpty();
    }

    // The words in sight on each card of the filled plan, open, held under its cut: before the story it
    // drew 26 on Company year and 131 on Vouchers.
    public static TheoryData<string, int> Cuts => new()
    {
        { "company-year", 24 },
        { "vouchers", 100 },
    };

    [Theory]
    [MemberData(nameof(Cuts))]
    public async Task VisibleWords_cardOfTheFilledPlan_areAtMostItsCut(string card, int cut)
    {
        CardHtml.VisibleWords(await OpenCardAsync(Filled, card)).Count.ShouldBeLessThanOrEqualTo(cut);
    }

    // Skipped, Company year says where the river starts; that the cards which need the company's figures
    // wait for them, each of them says.
    [Fact]
    public async Task CompanyYear_skipped_saysWhereTheRiverStarts()
    {
        var html = await OpenCardAsync(Filled.With("no company figures", inputs => inputs.SkipCompany()), "company-year");

        CardHtml.Prose(html).ShouldBe(["Skipped: no company figures, so the river starts at your pay."]);
    }

    // What a module passes on goes to the card under it, named in a few words; what a royalty leaves
    // beyond its limits says it is taxed there as pay. Where a line goes into a bucket says the bucket's
    // name as the bar does (BucketNamesTests).
    public static TheoryData<string, string> NextModules => new()
    {
        { "allowances", "to the allowances" },
        { "rent", "to the rent" },
        { "car", "to the car" },
        { "split", "to the split" },
        { "dividend", "to the dividend" },
        { "director_pay", "to Director pay, taxed as your pay" },
    };

    [Theory]
    [MemberData(nameof(NextModules))]
    public void Destination_passedOnToTheNextModule_isToItsShortName(string module, string expected)
    {
        var drain = new Drain("left", DrainKind.PassedOn, 1m, module);

        DrainListResources.GetString(DrainListText.Destination(drain), CultureInfo.InvariantCulture).ShouldBe(expected);
    }

    // A lever's conditions fold to one line that counts them and says why they are listed.
    [Theory]
    [InlineData(1, "1 condition Netto cannot check")]
    [InlineData(5, "5 conditions Netto cannot check")]
    public void ConditionsHeading_count_isTheConditionsNettoCannotCheck(int count, string expected)
    {
        var heading = ConditionListResources.GetString(ConditionListText.Heading(count), CultureInfo.InvariantCulture) ?? "";

        string.Format(CultureInfo.InvariantCulture, heading, count).ShouldBe(expected);
    }

    // The card of that fold's name on the Plan page, every card open.
    private static async Task<string> OpenCardAsync(NamedRiver river, string card) => CardHtml.Card(await CardHtml.RenderPlanOpenAsync(river.Inputs), card);

    // An amount without the sign a line gives what it takes or what comes back: −€400.00 is €400.00.
    private static string Unsigned(string amount) => amount.TrimStart('−', '+');

    // A river the cards are drawn on, named for what it holds, as a test names it.
    public sealed record NamedRiver(string Name, RiverInputs Inputs)
    {
        public NamedRiver With(string change, Func<RiverInputs, RiverInputs> changed) => new($"{Name}, {change}", changed(Inputs));

        public override string ToString() => Name;
    }
}
