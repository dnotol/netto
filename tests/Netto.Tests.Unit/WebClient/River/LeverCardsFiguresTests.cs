using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The lever cards before the split, Company year and Dividend read in figures, each drawn open on the
// Plan page as the viewer meets it (CardHtml): a summary that is figures with their labels, no sentence;
// the figure lines above the drain lines, their bubbles drawn, and sentences only for what a figure
// cannot say. Their figures are held, the amounts and the order they are drawn in, and the shape of what
// stands around them, never the words: a card may be reworded and these tests stand. What they share
// with the other cards: where a drain line passes its money on, and the line their conditions fold to.
public class LeverCardsFiguresTests
{
    // The names IStringLocalizer<TComponent> reads River/<Component>.resx under.
    private static readonly ResxText DrainListResources = ResxText.For<DrainList>();

    private static readonly ResxText ConditionListResources = ResxText.For<ConditionList>();

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

    // The amounts each card's summary draws, in order, by its fold's name, on the river it is drawn on.
    public static TheoryData<string, NamedRiver, string[]> Summaries => new()
    {
        { "company-year", Filled, ["€120,000.00", "€100,000.00"] },
        { "vouchers", Filled, ["€2,062.00", "€97,938.00"] },
        { "allowances", Filled, ["€1,000.00", "€96,938.00"] },
        { "rent", Filled, ["€711.11", "€88,538.00"] },
        { "rent", NoRoyalty, ["€711.11", "€88,538.00"] },
        { "dividend", Filled, ["€44,978.48", "€37,032.29"] },
    };

    [Theory]
    [MemberData(nameof(Summaries))]
    public async Task Summary_card_drawsItsFigures(string card, NamedRiver river, string[] amounts)
    {
        CardHtml.Amounts(CardHtml.Summary(await OpenCardAsync(river, card)).ShouldNotBeNull()).ShouldBe(amounts);
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

    // Between its middle dots, a summary holds a figure and the label naming it: no sentence, so no
    // punctuation once its amounts are left out, and no figure without its label.
    [Theory]
    [MemberData(nameof(Cards))]
    public async Task Summary_card_holdsNoSentence(string card, NamedRiver river)
    {
        var summary = CardHtml.Summary(await OpenCardAsync(river, card)).ShouldNotBeNull();

        CardHtml.SummaryFaults(summary).ShouldBeEmpty();
    }

    // An open card's paragraphs say what its lines cannot: none restates an amount one of its lines shows,
    // whichever way the line's amount goes.
    public static TheoryData<string, NamedRiver> CardsWithLines => new()
    {
        { "company-year", Filled },
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
        CardHtml.Prose(html).Where(paragraph => CardHtml.Amounts(paragraph).Any(amount => amounts.Contains(Unsigned(amount)))).ShouldBeEmpty();
    }

    // An open card draws its figure line by line, each with its amount, above where its money goes: the
    // figure's lines first, then the drain lines. The vouchers cost the company 200 × €8.91 and the eco and
    // sport/culture vouchers given, €4.00 of each meal voucher deducted; the rent above its floor loses 40%
    // of flat charges, under it the floor of €463.68 its own (RentFiguresTests); the dividend pays this
    // year's profit after tax and the €20,000.00 from earlier years, less what is kept.
    public static TheoryData<string, NamedRiver, string[]> FiguresAboveDrains => new()
    {
        { "vouchers", Filled, ["€2,062.00", "€1,262.00"] },
        { "rent", Filled, ["€1,000.00", "−€400.00", "€600.00"] },
        { "rent", UnderTheFloor, ["€463.68", "−€185.47", "€278.21"] },
        { "dividend", Filled, ["€29,978.48", "€44,978.48"] },
        { "dividend", NothingKept, ["€29,978.48", "€49,978.48"] },
    };

    [Theory]
    [MemberData(nameof(FiguresAboveDrains))]
    public async Task Card_open_drawsItsFigureLinesAboveItsDrainLines(string card, NamedRiver river, string[] figures)
    {
        var html = await OpenCardAsync(river, card);

        html.ShouldSatisfyAllConditions(
            () => CardHtml.LineRuns(html).Select(run => run.List).ShouldBe(["figure", "drain"]),
            () => CardHtml.LineRuns(html).Select(run => run.Lines).Take(1).ShouldBe([figures.Length]),
            () => CardHtml.LineAmounts(html).Take(figures.Length).ShouldBe(figures));
    }

    // How the cap under the rent is set is in the bubble after the rent's label, with the figures it comes
    // from: the cap, 5/3 of the cadastral income of the 12% let, €144.00, revalued by the 2026 coefficient.
    [Fact]
    public async Task Rent_rentField_saysHowTheCapIsSetInItsBubble()
    {
        var bubble = CardHtml.BubbleAfter(await OpenCardAsync(Filled, "rent"), "Rent for the year");

        bubble.ShouldSatisfyAllConditions(
            () => CardHtml.Amounts(bubble).ShouldBe(["€1,380.00", "€144.00"]),
            () => bubble.ShouldContain("5.75"));
    }

    // Under the floor, the lines of the rent have a bubble each that says what they are.
    [Theory]
    [InlineData("Taxed on the floor")]
    [InlineData("Flat charges")]
    public async Task Rent_underTheFloor_hasABubblePerLine(string line)
    {
        CardHtml.BubbleAfter(await OpenCardAsync(UnderTheFloor, "rent"), line).ShouldNotBeNullOrWhiteSpace();
    }

    // Every text the page draws on a plan whose levers hold amounts, in sight or not, is in its component's
    // texts: the figure lines, their bubbles and the home's facts among them.
    public static TheoryData<NamedRiver> FilledPlans => new() { Filled, UnderTheFloor, NothingKept };

    [Theory]
    [MemberData(nameof(FilledPlans))]
    public async Task RenderCardsOpenAsync_filledPlan_leavesNoKeyOnThePage(NamedRiver river)
    {
        CardHtml.LeakedKeys(await CardHtml.RenderCardsOpenAsync(river.Inputs)).ShouldBeEmpty();
    }

    // What an open card keeps in sentences is what its figures cannot say: the bounds under its fields,
    // with the button that takes the rent's cap, and the line a folded disclosure states its facts on.
    // The adornments of its fields, a € alone, say no words. One entry per sentence in sight, in the
    // page's order: the amounts it states, separated by a space, which are the rules' and the facts' own.
    // A sentence that states none would be an empty entry, so a card cannot regrow an explanation of its
    // own or merge two sentences unnoticed.
    public static TheoryData<string, NamedRiver, string[]> Bounds => new()
    {
        { "company-year", Filled, ["€879,163.44"] },
        { "vouchers", Filled, ["€8.91 €1.09", "€250.00", "€100.00"] },
        { "allowances", Filled, ["€1,938.44", "€20.00 €240.00", "€20.00 €240.00"] },
        { "rent", Filled, ["€1,200.00 12%", "€1,380.00 €1,380.00"] },
        { "dividend", Filled, ["€20,000.00 18%"] },
    };

    [Theory]
    [MemberData(nameof(Bounds))]
    public async Task Card_open_statesInSentencesTheBoundsAndFactsItsFiguresCannotSay(string card, NamedRiver river, string[] amountsOfEachSentence)
    {
        var sentences = CardHtml.Prose(await OpenCardAsync(river, card)).Where(paragraph => paragraph.Any(char.IsLetter));

        sentences.Select(sentence => string.Join(' ', CardHtml.Amounts(sentence))).ShouldBe(amountsOfEachSentence);
    }

    // The office allowance's yearly maximum stays under its field; the monthly maxima it adds up, which
    // change during the year, are in the bubble after its label.
    [Fact]
    public async Task Allowances_officeField_saysItsMonthlyMaximaInItsBubble()
    {
        var html = await OpenCardAsync(Filled, "allowances");

        CardHtml.Amounts(CardHtml.BubbleAfter(html, "Office allowance for the year")).ShouldBe(["€157.83", "€160.99", "€164.21"]);
    }

    // That the company deducts the allowances in full, like its own running costs, is the card's bubble's
    // to say, after its title.
    [Fact]
    public async Task Allowances_title_hasABubble()
    {
        CardHtml.BubbleAfter(await OpenCardAsync(Filled, "allowances"), "Expense allowances").ShouldNotBeNullOrWhiteSpace();
    }

    // The rate a dividend is withheld at is declared: what VVPRbis shares are, and which of their rates is
    // for which distribution, are in the bubble after its label, inside the disclosure a refusal opens.
    [Fact]
    public async Task Dividend_withholdingRate_hasABubble()
    {
        var html = await OpenCardAsync(Filled.With("earlier profits refused", inputs => inputs.WithEarlierProfits(-1m)), "dividend");

        CardHtml.BubbleAfter(html, "Withholding rate you declare").ShouldNotBeNullOrWhiteSpace();
    }

    // Skipped, Company year says where the river starts, in a note and no line; that the cards which need
    // the company's figures wait for them, each of them says.
    [Fact]
    public async Task CompanyYear_skipped_hasOneNoteAndNoLine()
    {
        var html = await OpenCardAsync(Filled.With("no company figures", inputs => inputs.SkipCompany()), "company-year");

        (CardHtml.Prose(html).Count, CardHtml.Lines(html).Count).ShouldBe((1, 0));
    }

    // Open, Company year draws what the company owes INASTI whatever it pays its director as its one
    // figure line, taken off what it earns: the lower band's 409.30.
    [Fact]
    public async Task CompanyYear_open_drawsTheContributionAsItsFigureLine()
    {
        var html = await OpenCardAsync(Filled, "company-year");

        (CardHtml.LineRuns(html).Single(), CardHtml.LineAmounts(html).Single()).ShouldBe((("figure", 1), "−€409.30"));
    }

    // What the contribution is and what each band owes is in the bubble after its line's label; that the
    // running costs leave it out, in the bubble after theirs; that a bill pending may change the year's
    // amounts, in the bubble after the balance sheet's disclosure.
    [Theory]
    [InlineData("INASTI's company contribution")]
    [InlineData("Running costs, excluding VAT and your pay")]
    [InlineData("Your balance sheet")]
    public async Task CompanyYear_contributionRunningCostsAndBalanceSheet_haveABubble(string label)
    {
        CardHtml.BubbleAfter(await OpenCardAsync(Filled, "company-year"), label).ShouldNotBeNullOrWhiteSpace();
    }

    // The upper band's 1,022.38 is the line, and what the summary says the company has before the pay
    // follows it.
    [Fact]
    public async Task CompanyYear_upperBand_drawsItsContributionAndWhatItLeaves()
    {
        var html = await OpenCardAsync(Filled.With("the upper band", inputs => inputs.WithCompanyContributionBand(CompanyContributionBand.Upper)),
            "company-year");

        (CardHtml.LineAmounts(html).Single(), CardHtml.Amounts(CardHtml.Summary(html).ShouldNotBeNull())[1]).ShouldBe(("−€1,022.38", "€99,386.92"));
    }

    // « Your balance sheet » open, as « Change » leaves it (OpenDisclosures): three bands to choose from,
    // the river's own pressed, under the year whose balance sheet sets the band, with a bubble naming what
    // the law exempts.
    [Theory]
    [InlineData(CompanyContributionBand.Lower, "At most €879,163.44")]
    [InlineData(CompanyContributionBand.Upper, "Above €879,163.44")]
    [InlineData(CompanyContributionBand.Exempt, "Exempt")]
    public async Task CompanyYear_balanceSheetOpen_offersTheThreeBandsWithTheRiversPressed(CompanyContributionBand band, string pressed)
    {
        var html = await CompanyYearOpenAsync(RiverInputs.Example.WithCompanyContributionBand(band));

        var chips = CardHtml.Chips(html);
        html.ShouldSatisfyAllConditions(
            () => chips.Select(chip => chip.Text).ShouldBe(["At most €879,163.44", "Above €879,163.44", "Exempt"]),
            () => chips.Where(chip => chip.Pressed).Select(chip => chip.Text).ShouldBe([pressed]),
            () => html.ShouldContain("Balance-sheet total of 2024"),
            () => CardHtml.BubbleAfter(html, "Balance-sheet total of 2024").ShouldNotBeNullOrWhiteSpace());
    }

    // Where the river's amounts come from is said where the river starts, in the bubble after Company
    // year's title, rather than in the page's lede.
    [Fact]
    public async Task CompanyYear_title_hasABubble()
    {
        CardHtml.BubbleAfter(await OpenCardAsync(Filled, "company-year"), "Company year").ShouldNotBeNullOrWhiteSpace();
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

        DrainListResources.Get(DrainListText.Destination(drain)).ShouldBe(expected);
    }

    // A figure list draws only the lines that move its figure, as a drain list draws only those that move
    // money (DrainLines): a line of nothing, the negative zero the engine gives for nothing among them, is
    // left out, and the rest kept in their order.
    [Fact]
    public void FigureLinesShown_linesOfNothing_areLeftOutAndTheRestKeptInOrder()
    {
        FigureLine[] lines = [new("Figure_A", 1_000m), new("Figure_B", 0m), new("Figure_C", decimal.Negate(0.00m)), new("Figure_D", -400m)];

        FigureLines.Shown(lines).Select(line => line.Key).ShouldBe(["Figure_A", "Figure_D"]);
    }

    // A lever's conditions fold to one line that counts them and says why they are listed.
    [Theory]
    [InlineData(1, "1 condition Netto cannot check")]
    [InlineData(5, "5 conditions Netto cannot check")]
    public void ConditionsHeading_count_isTheConditionsNettoCannotCheck(int count, string expected)
    {
        ConditionListResources.Format(ConditionListText.Heading(count), count).ShouldBe(expected);
    }

    // The card of that fold's name on the Plan page, every card open.
    // Company year opened by the viewer on the river given, its disclosures open, decoded to the text a
    // viewer reads.
    private static async Task<string> CompanyYearOpenAsync(RiverInputs inputs)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("company-year", true);

        return WebUtility.HtmlDecode(await RiverRendering.RenderAsync(typeof(CompanyYearCard), folds,
            new() { ["Inputs"] = inputs, ["Run"] = inputs.Run(), ["Refused"] = null },
            services => services.AddSingleton<IComponentActivator>(new OpenDisclosures())));
    }

    private static async Task<string> OpenCardAsync(NamedRiver river, string card) => CardHtml.Card(await CardHtml.RenderCardsOpenAsync(river.Inputs), card);

    // An amount without the sign a line gives what it takes or what comes back: −€400.00 is €400.00.
    private static string Unsigned(string amount) => amount.TrimStart('−', '+');

    // A river the cards are drawn on, named for what it holds, as a test names it.
    public sealed record NamedRiver(string Name, RiverInputs Inputs)
    {
        public NamedRiver With(string change, Func<RiverInputs, RiverInputs> changed) => new($"{Name}, {change}", changed(Inputs));

        public override string ToString() => Name;
    }
}
