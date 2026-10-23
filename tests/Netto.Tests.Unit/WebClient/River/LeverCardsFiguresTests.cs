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

    // The split shares the profit before pay alone, without a royalty to pay beside the pay.
    private static readonly NamedRiver NoRoyalty = Filled.With("the royalty out of the plan", inputs => inputs.TakeOutOfPlan(OptionalModule.Copyright));

    // Under the floor of 463.68 the 12% let of the home sets (RentFiguresTests).
    private static readonly NamedRiver UnderTheFloor = Filled.With("a rent under its floor", inputs => inputs.WithRent(300m));

    // All of what can be paid out goes as a dividend.
    private static readonly NamedRiver NothingKept = Filled.With("nothing kept", inputs => inputs.WithReserve(0m));

    // Each card's summary, by its fold's name, on the river it is drawn on.
    public static TheoryData<string, NamedRiver, string> Summaries => new()
    {
        { "company-year", Filled, "€120,000.00 in · €100,000.00 before your pay" },
        { "vouchers", Filled, "€2,062.00 to your pocket · €97,938.00 left" },
        { "allowances", Filled, "€1,000.00 to your pocket · €96,938.00 left" },
        { "rent", Filled, "€711.11 to your pocket · €88,538.00 before your pay and royalty" },
        { "rent", NoRoyalty, "€711.11 to your pocket · €88,538.00 before your pay" },
        { "dividend", Filled, "€44,978.48 paid out · €37,032.29 to your pocket" },
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
        { "rent", Filled },
        { "rent", NoRoyalty },
        { "dividend", Filled },
        { "dividend", NothingKept },
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
        { "allowances", Filled },
        { "rent", Filled },
        { "rent", UnderTheFloor },
        { "dividend", Filled },
        { "dividend", NothingKept },
    };

    [Theory]
    [MemberData(nameof(CardsWithLines))]
    public async Task Card_open_proseRestatesNoAmountOfItsLines(string card, NamedRiver river)
    {
        var html = await OpenCardAsync(river, card);

        var amounts = CardHtml.LineAmounts(html).Select(Unsigned).ToHashSet();
        CardHtml.Prose(html).Where(paragraph => Amounts.Matches(paragraph).Any(amount => amounts.Contains(Unsigned(amount.Value)))).ShouldBeEmpty();
    }

    // An open card draws its figure line by line, each label with its amount, above where its money goes:
    // the figure's lines first, then the first drain line. The vouchers cost the company 200 × €8.91 and
    // the eco and sport/culture vouchers given, €4.00 of each meal voucher deducted; the rent above its
    // floor loses 40% of flat charges, under it the floor of €463.68 its own (RentFiguresTests); the
    // dividend pays this year's profit after tax and the €20,000.00 from earlier years, less what is kept.
    public static TheoryData<string, NamedRiver, string[], string> FiguresAboveDrains => new()
    {
        { "vouchers", Filled, ["The company pays €2,062.00", "Added back to its taxable income €1,262.00"], "Meal vouchers" },
        { "rent", Filled, ["Taxed on the rent €1,000.00", "Flat charges −€400.00", "Joins your taxable income €600.00"], "Income tax on the rent" },
        { "rent", UnderTheFloor, ["Taxed on the floor €463.68", "Flat charges −€185.47", "Joins your taxable income €278.21"], "Income tax on the rent" },
        { "dividend", Filled, ["This year's profit after tax €29,978.48", "Dividend €44,978.48"], "Withholding tax" },
        { "dividend", NothingKept, ["This year's profit after tax €29,978.48", "Dividend €49,978.48"], "Withholding tax" },
    };

    [Theory]
    [MemberData(nameof(FiguresAboveDrains))]
    public async Task Card_open_drawsItsFigureLinesAboveItsDrainLines(string card, NamedRiver river, string[] figures, string firstDrain)
    {
        var lines = CardHtml.Lines(await OpenCardAsync(river, card));

        string?[] drawn = [.. lines.Take(figures.Length).Select(line => $"{line.Label} {line.Amount}"), lines.Skip(figures.Length).Select(line => line.Label).FirstOrDefault()];
        drawn.ShouldBe([.. figures, firstDrain]);
    }

    // How the cap under the rent is set is in the bubble after the rent's label: 5/3 of the cadastral
    // income of the 12% let, €144.00, revalued by the 2026 coefficient.
    [Fact]
    public async Task Rent_rentField_saysHowTheCapIsSetInItsBubble()
    {
        var html = await OpenCardAsync(Filled, "rent");

        CardHtml.BubbleAfter(html, "Rent for the year").ShouldBe(
            "At most €1,380.00 for the year: 5/3 of €144.00, the cadastral income of the part let, revalued by 5.75. Above it, the excess would be taxed as director pay.");
    }

    // Under the floor, the bubbles after the rent's lines say what the floor and the flat charges are.
    [Theory]
    [InlineData("Taxed on the floor", "The rent is under the floor, so you are taxed on the floor: the cadastral income of the part let, indexed, plus 40%.")]
    [InlineData("Flat charges",
        "A flat deduction for maintenance and repairs in place of real costs: a share of the rent, capped by the revalued cadastral income of the part let.")]
    public async Task Rent_underTheFloor_explainsItsLinesInTheirBubbles(string line, string bubble)
    {
        CardHtml.BubbleAfter(await OpenCardAsync(UnderTheFloor, "rent"), line).ShouldBe(bubble);
    }

    // Every text the page draws on a plan whose levers hold amounts, in sight or not, is in its component's
    // texts: the figure lines, their bubbles and the home's facts among them.
    public static TheoryData<NamedRiver> FilledPlans => new() { Filled, UnderTheFloor, NothingKept };

    [Theory]
    [MemberData(nameof(FilledPlans))]
    public async Task RenderPlanOpenAsync_filledPlan_leavesNoKeyOnThePage(NamedRiver river)
    {
        CardHtml.LeakedKeys(await CardHtml.RenderPlanOpenAsync(river.Inputs)).ShouldBeEmpty();
    }

    // The words in sight on each card of the filled plan, open, held under its cut: before the story it
    // drew 26 on Company year, 131 on Vouchers, 161 on Expense allowances, 192 on Home office rent and 99 on
    // Dividend.
    public static TheoryData<string, int> Cuts => new()
    {
        { "company-year", 24 },
        { "vouchers", 100 },
        { "allowances", 101 },
        { "rent", 115 },
        { "dividend", 88 },
    };

    [Theory]
    [MemberData(nameof(Cuts))]
    public async Task VisibleWords_cardOfTheFilledPlan_areAtMostItsCut(string card, int cut)
    {
        CardHtml.VisibleWords(await OpenCardAsync(Filled, card)).Count.ShouldBeLessThanOrEqualTo(cut);
    }

    // What an open card keeps in sentences is what its figures cannot say: the bounds under its fields,
    // with the button that takes the rent's cap, and the line a folded disclosure states its facts on.
    // The adornments of its fields, a € alone, say no words.
    public static TheoryData<string, NamedRiver, string[]> Sentences => new()
    {
        { "vouchers", Filled, ["€8.91 each from the company at most, €1.09 from you.", "At most €250.00 a year.", "At most €100.00 a year."] },
        { "allowances", Filled, ["At most €1,938.44 for the year.", "At most €20.00 a month, €240.00 for the year.", "At most €20.00 a month, €240.00 for the year."] },
        { "rent", Filled, ["Cadastral income €1,200.00 · 12% let to the company", "At most €1,380.00 for the year. Use €1,380.00"] },
        { "dividend", Filled, ["€20,000.00 kept from earlier years · VVPRbis shares, third distribution on, 18% withheld (default)"] },
    };

    [Theory]
    [MemberData(nameof(Sentences))]
    public async Task Card_open_keepsInSentencesOnlyWhatItsFiguresCannotSay(string card, NamedRiver river, string[] sentences)
    {
        CardHtml.Prose(await OpenCardAsync(river, card)).Where(paragraph => paragraph.Any(char.IsLetter)).ShouldBe(sentences);
    }

    // The office allowance's yearly maximum stays under its field; the monthly maxima it adds up, which
    // change during the year, are in the bubble after its label.
    [Fact]
    public async Task Allowances_officeField_saysItsMonthlyMaximaInItsBubble()
    {
        var html = await OpenCardAsync(Filled, "allowances");

        CardHtml.BubbleAfter(html, "Office allowance for the year")
            .ShouldBe("At most €157.83 a month from January, €160.99 a month from March, €164.21 a month from September.");
    }

    // The rate a dividend is withheld at is declared: what VVPRbis shares are, and which of their rates is
    // for which distribution, are in the bubble after its label, inside the disclosure a refusal opens.
    [Fact]
    public async Task Dividend_withholdingRate_saysWhatVvprbisSharesAreInItsBubble()
    {
        var html = await OpenCardAsync(Filled.With("earlier profits refused", inputs => inputs.WithEarlierProfits(-1m)), "dividend");

        CardHtml.BubbleAfter(html, "Withholding rate you declare").ShouldBe(
            "VVPRbis shares are shares a small company issued in return for a new contribution in cash, under the regime's conditions: " +
            "their dividends can be withheld at a reduced rate from a later distribution on. They pay the ordinary rate on their first " +
            "distributions; the second-distribution rate is for a contribution made by 2025.");
    }

    // Skipped, Company year says where the river starts; that the cards which need the company's figures
    // wait for them, each of them says.
    [Fact]
    public async Task CompanyYear_skipped_saysWhereTheRiverStarts()
    {
        var html = await OpenCardAsync(Filled.With("no company figures", inputs => inputs.SkipCompany()), "company-year");

        CardHtml.Prose(html).ShouldBe(["Skipped: no company figures, so the river starts at your pay."]);
    }

    // Where the river's amounts come from is said where the river starts, in the bubble after Company
    // year's title, rather than in the page's lede.
    [Fact]
    public async Task CompanyYear_bubble_saysTheRiverStartsOnAnExample()
    {
        var html = await OpenCardAsync(Filled, "company-year");

        CardHtml.BubbleAfter(html, "Company year")
            .ShouldBe("The river starts on an example year: replace its amounts with yours, or clear the company's figures to start at your pay.");
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
